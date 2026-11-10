// Jauge Claude - application Windows autonome (C#, .NET Framework 4.8 déjà inclus dans Windows).
//
// - Fenêtre claude.ai intégrée (Edge WebView2) : on se connecte une fois, elle reste cachée.
// - Toutes les 5 min : lecture de l'utilisation (session 5 h et semaine).
// - Pastille dans la zone de notification : pourcentage le plus élevé, vert / orange / rouge,
//   gris si pas de chiffres depuis 20 min. Notification à 70 % et 90 %.
// - Clic droit : actualiser, afficher la fenêtre claude.ai, lancer au démarrage, quitter.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace JaugeClaude
{
    static class Program
    {
        public static readonly string Dossier =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "JaugeClaude");

        [STAThread]
        static void Main()
        {
            using (var mutex = new Mutex(true, "JaugeClaudeApp", out bool seul))
            {
                if (!seul)
                {
                    MessageBox.Show("La Jauge Claude tourne déjà (regardez derrière la flèche ^ près de l'horloge).",
                        "Jauge Claude", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                Directory.CreateDirectory(Dossier);
                AppDomain.CurrentDomain.AssemblyResolve += ChargerDllEmbarquee;
                try
                {
                    Lancer();
                }
                catch (Exception e)
                {
                    File.WriteAllText(Path.Combine(Dossier, "erreur.txt"), e.ToString());
                    MessageBox.Show("La Jauge Claude n'a pas pu démarrer :\n" + e.Message, "Jauge Claude",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // Méthode séparée : les types WebView2 ne sont chargés qu'après l'installation de AssemblyResolve.
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void Lancer()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Appli());
        }

        static Assembly ChargerDllEmbarquee(object sender, ResolveEventArgs args)
        {
            var nom = new AssemblyName(args.Name).Name + ".dll";
            using (var s = Assembly.GetExecutingAssembly().GetManifestResourceStream(nom))
            {
                if (s == null) return null;
                var octets = new byte[s.Length];
                s.Read(octets, 0, octets.Length);
                return Assembly.Load(octets);
            }
        }

        /// <summary>Extrait WebView2Loader.dll (natif) à côté des données et renvoie son dossier.</summary>
        public static string ExtraireChargeur()
        {
            var dossier = Path.Combine(Dossier, "bin");
            Directory.CreateDirectory(dossier);
            var cible = Path.Combine(dossier, "WebView2Loader.dll");
            using (var s = Assembly.GetExecutingAssembly().GetManifestResourceStream("WebView2Loader.dll"))
            {
                if (!File.Exists(cible) || new FileInfo(cible).Length != s.Length)
                    using (var f = File.Create(cible)) s.CopyTo(f);
            }
            return dossier;
        }
    }

    /// <summary>Un relevé : pourcentage et heure de remise à zéro (ISO).</summary>
    class Mesure
    {
        public int Pct;
        public string Reset;
    }

    static class Outils
    {
        public static readonly Color Vert = Color.FromArgb(46, 158, 91), Orange = Color.FromArgb(224, 161, 0),
            Rouge = Color.FromArgb(214, 69, 69), Gris = Color.FromArgb(130, 130, 130);

        public static Color Couleur(int? pct) =>
            pct == null ? Gris : pct >= 90 ? Rouge : pct >= 70 ? Orange : Vert;

        public static DateTimeOffset? Date(string iso) =>
            DateTimeOffset.TryParse(iso, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var d)
                ? d : (DateTimeOffset?)null;

        public static string Quand(string iso)
        {
            var d = Date(iso);
            if (d == null) return "?";
            var min = (int)Math.Floor((d.Value - DateTimeOffset.UtcNow).TotalMinutes);
            if (min < 60) return $"dans {Math.Max(min, 0)} min";
            if (min < 24 * 60) return $"dans {min / 60} h {min % 60:00}";
            return d.Value.ToLocalTime().ToString("ddd HH:mm", new CultureInfo("fr-FR"));
        }

        public static Mesure Lire(string pct, string reset)
        {
            if (!double.TryParse(pct, NumberStyles.Float, CultureInfo.InvariantCulture, out var p)) return null;
            return new Mesure { Pct = Math.Max(0, Math.Min((int)Math.Round(p), 999)), Reset = reset ?? "" };
        }

        [DllImport("user32.dll")] static extern bool DestroyIcon(IntPtr h);

        /// <summary>Pastille colorée (carré arrondi), chiffres agrandis au maximum, dessinée à la taille exacte de la zone de notification.</summary>
        public static Icon Dessiner(int? pct, bool obsolete, Icon ancienne)
        {
            int t = Math.Max(16, SystemInformation.SmallIconSize.Width);   // 16, 20, 24, 32 selon la mise à l'échelle
            using (var bmp = new Bitmap(t, t))
            {
                using (var g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    // Carré aux coins arrondis : plus de place qu'un rond pour deux chiffres.
                    float rayon = t * 0.28f;
                    using (var forme = new GraphicsPath())
                    using (var fond = new SolidBrush(obsolete ? Gris : Couleur(pct)))
                    {
                        forme.AddArc(0, 0, 2 * rayon, 2 * rayon, 180, 90);
                        forme.AddArc(t - 2 * rayon, 0, 2 * rayon, 2 * rayon, 270, 90);
                        forme.AddArc(t - 2 * rayon, t - 2 * rayon, 2 * rayon, 2 * rayon, 0, 90);
                        forme.AddArc(0, t - 2 * rayon, 2 * rayon, 2 * rayon, 90, 90);
                        forme.CloseFigure();
                        g.FillPath(fond, forme);
                    }
                    var texte = pct == null ? "?" : pct >= 100 ? "!" : pct.ToString();
                    using (var chemin = new GraphicsPath())
                    {
                        chemin.AddString(texte, new FontFamily("Arial"), (int)FontStyle.Bold, 100, Point.Empty, StringFormat.GenericTypographic);
                        var r = chemin.GetBounds();
                        // Le texte occupe au plus 80 % de la largeur et 60 % de la hauteur : marge tout autour.
                        float echelle = Math.Min(t * 0.80f / r.Width, t * 0.60f / r.Height);
                        using (var m = new Matrix())
                        {
                            m.Translate(t / 2f, t / 2f);
                            m.Scale(echelle, echelle);
                            m.Translate(-(r.X + r.Width / 2), -(r.Y + r.Height / 2));
                            chemin.Transform(m);
                        }
                        g.FillPath(Brushes.White, chemin);
                    }
                }
                var icone = Icon.FromHandle(bmp.GetHicon());
                if (ancienne != null) DestroyIcon(ancienne.Handle);
                return icone;
            }
        }
    }

    class Appli : ApplicationContext
    {
        const string UrlUsage = "https://claude.ai/settings/usage";
        const string CleRun = @"Software\Microsoft\Windows\CurrentVersion\Run";
        static readonly int[] Seuils = { 70, 90 };
        static readonly TimeSpan Intervalle = TimeSpan.FromMinutes(5), ObsoleteApres = TimeSpan.FromMinutes(20);

        // Exécuté dans la page claude.ai : même session que le site. Répond via postMessage.
        const string JsReleve = @"
(async () => {
  const envoyer = m => window.chrome.webview.postMessage(m);
  const get = async u => { const r = await fetch(u, {credentials: 'include', headers: {Accept: 'application/json'}});
                           if (!r.ok) throw new Error('HTTP ' + r.status); return r.json(); };
  const utile = u => u && (u.five_hour || u.seven_day);
  try {
    let org = localStorage.getItem('jauge_org'), u = null;
    if (org) { try { u = await get(`/api/organizations/${org}/usage`); } catch (e) {} }
    if (!utile(u)) {
      u = null;
      for (const o of (await get('/api/organizations')).filter(o => (o.capabilities || []).includes('chat'))) {
        try { const x = await get(`/api/organizations/${o.uuid}/usage`);
              if (utile(x)) { u = x; localStorage.setItem('jauge_org', o.uuid); break; } } catch (e) {}
      }
    }
    if (!u) throw new Error('aucune limite trouvée');
    const f = x => x ? [x.utilization, x.resets_at || ''] : ['', ''];
    envoyer(['ok', ...f(u.five_hour), ...f(u.seven_day)].join('|'));
  } catch (e) { envoyer('err|' + String(e.message || e)); }
})();";

        readonly NotifyIcon icone;
        readonly Form fenetre;
        readonly Microsoft.Web.WebView2.WinForms.WebView2 vue;
        readonly System.Windows.Forms.Timer minuteur;
        readonly ToolStripMenuItem lSession, lSemaine, mDemarrage;
        Mesure session, semaine;
        DateTime recu = DateTime.MinValue, dernierReleve = DateTime.MinValue;
        string erreur;
        bool pret, quitte;

        public Appli()
        {
            PremierReglageDemarrage();

            lSession = new ToolStripMenuItem { Enabled = false };
            lSemaine = new ToolStripMenuItem { Enabled = false };
            mDemarrage = new ToolStripMenuItem("Lancer au démarrage de Windows", null,
                (s, e) => ReglerDemarrage(!AuDemarrage()));
            var menu = new ContextMenuStrip();
            menu.Items.AddRange(new ToolStripItem[] {
                lSession, lSemaine, new ToolStripSeparator(),
                new ToolStripMenuItem("Ouvrir la page Utilisation", null, (s, e) => OuvrirUsage()) { Font = new Font(menu.Font, FontStyle.Bold) },
                new ToolStripMenuItem("Actualiser maintenant", null, (s, e) => Releve()),
                new ToolStripMenuItem("Afficher la fenêtre claude.ai (connexion)", null, (s, e) => Montrer()),
                mDemarrage, new ToolStripSeparator(),
                new ToolStripMenuItem("Quitter", null, (s, e) => Quitter()),
            });
            menu.Opening += (s, e) =>
            {
                lSession.Text = Ligne("Session", session);
                lSemaine.Text = Ligne("Semaine", semaine);
                mDemarrage.Checked = AuDemarrage();
            };

            icone = new NotifyIcon { Icon = Outils.Dessiner(null, true, null), Text = "Jauge Claude : démarrage…",
                                     ContextMenuStrip = menu, Visible = true };
            icone.DoubleClick += (s, e) => OuvrirUsage();

            fenetre = new Form { Text = "Jauge Claude – connexion à claude.ai", Width = 1000, Height = 760,
                                 StartPosition = FormStartPosition.CenterScreen, ShowInTaskbar = true,
                                 Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath) };
            vue = new Microsoft.Web.WebView2.WinForms.WebView2 { Dock = DockStyle.Fill };
            fenetre.Controls.Add(vue);
            fenetre.FormClosing += (s, e) =>
            {
                if (quitte) return;
                e.Cancel = true;          // la croix cache la fenêtre au lieu de quitter
                fenetre.Hide();
            };
            var h = fenetre.Handle;        // la fenêtre existe sans être affichée

            minuteur = new System.Windows.Forms.Timer { Interval = 30_000 };
            minuteur.Tick += (s, e) =>
            {
                if (DateTime.Now - dernierReleve >= Intervalle) Releve();
                Rafraichir();
            };
            minuteur.Start();

            Initialiser();
        }

        async void Initialiser()
        {
            try
            {
                Microsoft.Web.WebView2.Core.CoreWebView2Environment.SetLoaderDllFolderPath(Program.ExtraireChargeur());
                var env = await Microsoft.Web.WebView2.Core.CoreWebView2Environment.CreateAsync(
                    null, Path.Combine(Program.Dossier, "WebView2"));
                await vue.EnsureCoreWebView2Async(env);
                vue.CoreWebView2.WebMessageReceived += (s, e) => Recevoir(e.TryGetWebMessageAsString());
                vue.CoreWebView2.NavigationCompleted += (s, e) => Releve();
                pret = true;
                vue.CoreWebView2.Navigate(UrlUsage);
            }
            catch (Exception e)
            {
                erreur = "WebView2 indisponible";
                File.WriteAllText(Path.Combine(Program.Dossier, "erreur.txt"), e.ToString());
                Rafraichir();
                if (MessageBox.Show("Le composant Microsoft Edge WebView2 est introuvable.\n" +
                                    "Ouvrir la page de téléchargement de Microsoft ?", "Jauge Claude",
                                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                    Process.Start("https://go.microsoft.com/fwlink/p/?LinkId=2124703");
            }
        }

        void Releve()
        {
            if (!pret) return;
            dernierReleve = DateTime.Now;
            var url = vue.CoreWebView2.Source ?? "";
            if (url.StartsWith("https://claude.ai")) _ = vue.CoreWebView2.ExecuteScriptAsync(JsReleve);
            else vue.CoreWebView2.Navigate(UrlUsage);   // le relevé repart à la fin du chargement
        }

        void Recevoir(string msg)
        {
            var p = (msg ?? "").Split('|');
            if (p[0] == "ok" && p.Length >= 5)
            {
                session = Outils.Lire(p[1], p[2]);
                semaine = Outils.Lire(p[3], p[4]);
                recu = DateTime.Now;
                erreur = null;
                Alertes();
            }
            else
            {
                erreur = p.Length > 1 ? p[1] : "réponse illisible";
                if (erreur == "HTTP 401" || erreur == "HTTP 403") Montrer();  // il faut se (re)connecter
            }
            Rafraichir();
        }

        // ------------------------------------------------------------ alertes 70 / 90 %
        void Alertes()
        {
            var fichier = Path.Combine(Program.Dossier, "alertes.txt");
            var deja = new Dictionary<string, DateTime>();
            try
            {
                foreach (var l in File.ReadAllLines(fichier))
                {
                    var t = l.Split('\t');
                    if (t.Length == 2 && DateTime.TryParse(t[1], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var d)
                        && DateTime.Now - d < TimeSpan.FromDays(8)) deja[t[0]] = d;
                }
            }
            catch (IOException) { }

            foreach (var (nom, v) in new[] { ("Session", session), ("Semaine", semaine) })
            {
                if (v == null) continue;
                var d = Outils.Date(v.Reset);   // l'heure varie de quelques µs : arrondie à 10 min
                var periode = d == null ? "?" : Math.Round(d.Value.ToUnixTimeSeconds() / 600.0).ToString(CultureInfo.InvariantCulture);
                foreach (var s in Seuils)
                {
                    var cle = $"{nom}|{periode}|{s}";
                    if (v.Pct < s || deja.ContainsKey(cle)) continue;
                    deja[cle] = DateTime.Now;
                    icone.ShowBalloonTip(10_000, $"Claude : {nom.ToLower()} à {v.Pct} %",
                        $"Seuil de {s} % dépassé. Remise à zéro {Outils.Quand(v.Reset)}.", ToolTipIcon.Warning);
                }
            }
            try { File.WriteAllLines(fichier, deja.Select(kv => kv.Key + "\t" + kv.Value.ToString("o"))); }
            catch (IOException) { }
        }

        // ------------------------------------------------------------ affichage
        static string Ligne(string nom, Mesure v) =>
            v == null ? $"{nom} : —" : $"{nom} : {v.Pct} % (reset {Outils.Quand(v.Reset)})";

        void Rafraichir()
        {
            var vals = new[] { session, semaine }.Where(v => v != null).Select(v => v.Pct).ToList();
            int? pct = vals.Count > 0 ? vals.Max() : (int?)null;
            var obsolete = recu == DateTime.MinValue || DateTime.Now - recu > ObsoleteApres;
            icone.Icon = Outils.Dessiner(pct, obsolete, icone.Icon);

            string texte;
            if (erreur == "HTTP 401" || erreur == "HTTP 403") texte = "Jauge Claude : connectez-vous (clic droit)";
            else if (recu == DateTime.MinValue) texte = "Jauge Claude : " + (erreur != null ? "erreur (" + erreur + ")" : "premier relevé…");
            else
            {
                string c(string n, Mesure v) => v == null ? $"{n} —" : $"{n} {v.Pct} % ({Outils.Quand(v.Reset)})";
                texte = c("Session", session) + "\n" + c("Semaine", semaine);
                if (obsolete) texte = "(ancien) " + texte;
            }
            icone.Text = texte.Length > 63 ? texte.Substring(0, 63) : texte;   // limite Windows
        }

        void Montrer()
        {
            fenetre.Show();
            fenetre.WindowState = FormWindowState.Normal;
            fenetre.Activate();
        }

        static void OuvrirUsage() => Process.Start(UrlUsage);

        void Quitter()
        {
            quitte = true;
            icone.Visible = false;
            minuteur.Stop();
            fenetre.Close();
            ExitThread();
        }

        // ------------------------------------------------------------ démarrage avec Windows
        static bool AuDemarrage()
        {
            using (var k = Registry.CurrentUser.OpenSubKey(CleRun)) return k?.GetValue("JaugeClaude") != null;
        }

        static void ReglerDemarrage(bool actif)
        {
            using (var k = Registry.CurrentUser.CreateSubKey(CleRun))
            {
                if (actif) k.SetValue("JaugeClaude", "\"" + Application.ExecutablePath + "\"");
                else k.DeleteValue("JaugeClaude", false);
            }
        }

        /// <summary>Premier lancement : démarrage automatique activé, raccourci de l'ancienne version retiré.</summary>
        static void PremierReglageDemarrage()
        {
            var marque = Path.Combine(Program.Dossier, "demarrage_regle_exe");
            if (File.Exists(marque))
            {
                if (AuDemarrage()) ReglerDemarrage(true);   // met à jour le chemin si l'exe a été déplacé
                return;
            }
            ReglerDemarrage(true);
            try { File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "Jauge Claude.lnk")); }
            catch (Exception) { }
            File.WriteAllText(marque, "");
        }
    }
}
