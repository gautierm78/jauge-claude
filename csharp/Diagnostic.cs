// Diagnostic de consommation Claude Code.
//
// Portage en C# de « diag run » (https://github.com/Francoisnollet/diagrun), version 0.1.1.
// Code d'origine : Copyright (c) 2026 François Nollet, licence MIT.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software
// and associated documentation files (the "Software"), to deal in the Software without restriction,
// including without limitation the rights to use, copy, modify, merge, publish, distribute,
// sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions: the above copyright notice and this
// permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND.
//
// Ce que ça fait : lit les sessions Claude Code enregistrées sur CET ordinateur
// (%USERPROFILE%\.claude\projects\**\*.jsonl), calcule où partent les tokens et écrit un rapport HTML
// dans le dossier temporaire. Rien n'est envoyé nulle part.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

namespace JaugeClaude
{
    static class Diagnostic
    {
        // ------------------------------------------------------------ constantes
        // Tarifs ($/M tokens) : hypothèse. Sur abonnement, à lire comme des PARTS de consommation.
        static readonly Dictionary<string, double[]> Prix = new Dictionary<string, double[]>
            { ["opus"] = new[] { 5.0, 25.0 }, ["sonnet"] = new[] { 3.0, 15.0 }, ["haiku"] = new[] { 1.0, 5.0 } };
        const double CW = 1.25, CR = 0.1;
        const double CtxAlerte = 100000, CtxHaut = 150000, Gros = 20000, CtxSimple = 40000;
        static readonly HashSet<string> Edition = new HashSet<string> { "Edit", "Write", "MultiEdit", "NotebookEdit" };
        static readonly HashSet<string> Lecture = new HashSet<string> { "Read", "Grep", "Glob", "LS", "WebFetch", "WebSearch" };
        const RegexOptions Opt = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;
        static readonly Regex Complexe = new Regex("architect|refactor|redesign|migrat|investigat|root cause|pourquoi|concoi|conçoi|stratég|sécurit|security|performance|debug|race condition|from scratch", Opt);
        static readonly Regex Ecriture = new Regex(@"r[ée]dige|[ée]cris|draft|write|prospect|e-?mail|mail\b|brouillon|traduis|translate|r[ée]sume|summari[sz]e", Opt);
        static readonly Regex Git = new Regex(@"\b(commit|push|pull request|pr|git (status|log|diff))\b", Opt);
        static readonly Regex BaliseIde = new Regex(@"<(ide_[a-z_]+|system-reminder)>[\s\S]*?</\1>", Opt);
        static readonly Regex Colle = new Regex(@"<pasted_content[^>]*>[\s\S]*?</pasted_content>|<pasted_content[^>]*>[\s\S]*$", Opt);
        static readonly Regex Blancs = new Regex(@"\s+");
        static readonly Dictionary<string, string> Libelle = new Dictionary<string, string>
        {
            ["question courte"] = "questions courtes", ["exploration"] = "recherches dans le code", ["git/commande"] = "commandes git",
            ["petite correction"] = "petites corrections", ["moyenne"] = "tâches moyennes", ["complexe"] = "tâches complexes",
            ["suite de conversation"] = "suites de conversation",
        };
        static readonly Dictionary<string, string> LibelleSingulier = new Dictionary<string, string>
        {
            ["question courte"] = "question courte", ["exploration"] = "recherche dans le code", ["git/commande"] = "commande git",
            ["petite correction"] = "petite correction", ["moyenne"] = "tâche moyenne", ["complexe"] = "tâche complexe",
            ["suite de conversation"] = "suite de conversation",
        };
        // Modèle qui aurait probablement suffi (null = pas de conseil).
        static readonly Dictionary<string, string> Cible = new Dictionary<string, string>
        {
            ["question courte"] = "haiku", ["exploration"] = "haiku", ["git/commande"] = "haiku", ["petite correction"] = "haiku",
            ["moyenne"] = "sonnet", ["complexe"] = null, ["suite de conversation"] = null,
        };
        static readonly Dictionary<string, int> Rang = new Dictionary<string, int> { ["haiku"] = 0, ["sonnet"] = 1, ["opus"] = 2 };

        // ------------------------------------------------------------ données
        class Ev { public long T; public Dictionary<string, object> D; }

        class Msg { public string Model; public double In, Out, Cw, Cr; }

        class Tour
        {
            public string Sid, Prompt, Main, Kind;
            public long Ts;
            public Dictionary<string, Msg> Msgs = new Dictionary<string, Msg>();
            public List<string> Tools = new List<string>(), FilesRead = new List<string>(), Bash = new List<string>(), Big = new List<string>();
            public HashSet<string> Edited = new HashSet<string>();
            public Msg Usage = new Msg();
            public Dictionary<string, double> Fams = new Dictionary<string, double>();
            public double Cost, Peak, Ex100, Ex150;
        }

        class Ajust { public int N; public double Save; public string Kind, From, To; }

        class Analyse
        {
            public double Lo, Hi, CtxProj, Total, Save, CtxPct, CrShare;
            public Dictionary<string, double> ByModel = new Dictionary<string, double>();
            public List<Ajust> Fit = new List<Ajust>();
            public List<Tour> Examples = new List<Tour>();
            public int LongTurns;
            public List<int> Marathons = new List<int>();
            public List<KeyValuePair<string, int>> Reread = new List<KeyValuePair<string, int>>(), Repeat = new List<KeyValuePair<string, int>>();
            public Dictionary<string, int> Big = new Dictionary<string, int>();
        }

        class Vue { public int Days, N; public Analyse A; }

        // ------------------------------------------------------------ JSON (outils)
        static readonly JavaScriptSerializer Json = new JavaScriptSerializer { MaxJsonLength = int.MaxValue, RecursionLimit = 200 };

        static Dictionary<string, object> Dic(object o) => o as Dictionary<string, object>;
        static object Val(Dictionary<string, object> d, string k) => d != null && d.TryGetValue(k, out var v) ? v : null;
        static string Texte(object o) => o as string;
        static bool Vrai(object o) => o is bool b && b;
        static double Nombre(object o) => o == null ? 0 : o is string ? 0 : (o is IConvertible c ? Convert.ToDouble(c, CultureInfo.InvariantCulture) : 0);
        static IEnumerable<Dictionary<string, object>> Blocs(object contenu) =>
            contenu is IList l ? l.Cast<object>().Select(Dic).Where(x => x != null) : Enumerable.Empty<Dictionary<string, object>>();

        static string TexteDe(object contenu)
        {
            if (contenu is string s) return s;
            return string.Join("\n", Blocs(contenu).Where(b => Texte(Val(b, "type")) == "text").Select(b => Texte(Val(b, "text")) ?? ""));
        }

        static int TailleResultat(Dictionary<string, object> b)
        {
            var c = Val(b, "content");
            if (c is string s) return s.Length;
            return Blocs(c).Sum(x => (Texte(Val(x, "text")) ?? "").Length);
        }

        // ------------------------------------------------------------ lecture des sessions
        static string Dossier() =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".claude", "projects");

        static List<string> Fichiers(string dossier)
        {
            try
            {
                return Directory.Exists(dossier)
                    ? Directory.EnumerateFiles(dossier, "*.jsonl", SearchOption.AllDirectories).ToList()
                    : new List<string>();
            }
            catch (Exception) { return new List<string>(); }
        }

        static string NettoyerPrompt(string t)
        {
            var x = BaliseIde.Replace(t, " ");
            x = Colle.Replace(x, " [contenu collé] ");
            x = Blancs.Replace(x, " ").Trim();
            return x.Length > 0 ? x : "(message sans texte)";
        }

        static string VraiPrompt(Dictionary<string, object> e)
        {
            if (Texte(Val(e, "type")) != "user" || Vrai(Val(e, "isSidechain")) || Vrai(Val(e, "isMeta"))) return null;
            var contenu = Val(Dic(Val(e, "message")), "content");
            if (Blocs(contenu).Any(b => Texte(Val(b, "type")) == "tool_result")) return null;
            var brut = TexteDe(contenu).Trim();
            if (brut.Length == 0 || brut.StartsWith("<local-command") || brut.StartsWith("Caveat:") || brut.StartsWith("<system-reminder")) return null;
            return NettoyerPrompt(brut);
        }

        static Dictionary<string, List<Ev>> Charger(int maxJours)
        {
            var sessions = new Dictionary<string, List<Ev>>();
            var coupure = DateTimeOffset.UtcNow.AddDays(-maxJours).ToUnixTimeMilliseconds();
            foreach (var fichier in Fichiers(Dossier()))
            {
                string[] lignes;
                try
                {
                    // Un fichier pas modifié depuis la coupure ne contient que des événements plus anciens.
                    if (new DateTimeOffset(File.GetLastWriteTimeUtc(fichier), TimeSpan.Zero).ToUnixTimeMilliseconds() < coupure) continue;
                    lignes = File.ReadAllLines(fichier, Encoding.UTF8);
                }
                catch (Exception) { continue; }
                foreach (var ligne in lignes)
                {
                    if (ligne.Length == 0) continue;
                    Dictionary<string, object> d;
                    try { d = Json.DeserializeObject(ligne) as Dictionary<string, object>; } catch (Exception) { continue; }
                    var ts = Texte(Val(d, "timestamp"));
                    if (ts == null || !DateTimeOffset.TryParse(ts, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var date)) continue;
                    var t = date.ToUnixTimeMilliseconds();
                    if (t < coupure) continue;
                    var sid = Texte(Val(d, "sessionId")) ?? fichier;
                    if (!sessions.TryGetValue(sid, out var liste)) sessions[sid] = liste = new List<Ev>();
                    liste.Add(new Ev { T = t, D = d });
                }
            }
            return sessions;
        }

        static string Famille(string modele)
        {
            var m = (modele ?? "").ToLowerInvariant();
            return new[] { "opus", "sonnet", "haiku" }.FirstOrDefault(f => m.Contains(f));
        }

        static double Cout(string fam, Msg u, string comme = null)
        {
            var p = Prix[comme ?? fam];
            return (u.In * p[0] + u.Out * p[1] + u.Cw * p[0] * CW + u.Cr * p[0] * CR) / 1e6;
        }

        static List<Tour> Tours(Dictionary<string, List<Ev>> sessions, out Dictionary<string, int> clears)
        {
            var tours = new List<Tour>();
            clears = new Dictionary<string, int>();
            foreach (var kv in sessions)
            {
                var sid = kv.Key;
                var evs = kv.Value.OrderBy(x => x.T).ToList();
                var noms = new Dictionary<string, string>();
                Tour cur = null;
                clears[sid] = 0;
                foreach (var ev in evs)
                {
                    var e = ev.D;
                    var p = VraiPrompt(e);
                    if (p != null)
                    {
                        if (p.Contains("<command-name>/clear")) { clears[sid]++; cur = null; continue; }
                        if (p.StartsWith("<command-name>")) continue;
                        cur = new Tour { Sid = sid, Ts = ev.T, Prompt = p };
                        tours.Add(cur);
                        continue;
                    }
                    if (cur == null) continue;
                    var msg = Dic(Val(e, "message"));
                    var type = Texte(Val(e, "type"));
                    var usage = Dic(Val(msg, "usage"));
                    if (type == "assistant" && usage != null)
                    {
                        var id = Texte(Val(msg, "id")) ?? Texte(Val(e, "uuid")) ?? Guid.NewGuid().ToString();
                        if (!cur.Msgs.TryGetValue(id, out var r)) cur.Msgs[id] = r = new Msg { Model = Texte(Val(msg, "model")) };
                        r.In = Math.Max(r.In, Nombre(Val(usage, "input_tokens")));
                        r.Out = Math.Max(r.Out, Nombre(Val(usage, "output_tokens")));
                        r.Cw = Math.Max(r.Cw, Nombre(Val(usage, "cache_creation_input_tokens")));
                        r.Cr = Math.Max(r.Cr, Nombre(Val(usage, "cache_read_input_tokens")));
                        foreach (var b in Blocs(Val(msg, "content")))
                        {
                            var bid = Texte(Val(b, "id"));
                            if (Texte(Val(b, "type")) != "tool_use" || bid == null || noms.ContainsKey(bid)) continue;
                            var nom = Texte(Val(b, "name")) ?? "?";
                            var inp = Dic(Val(b, "input"));
                            noms[bid] = nom;
                            cur.Tools.Add(nom);
                            if (nom == "Read" && Texte(Val(inp, "file_path")) is string fp && fp.Length > 0) cur.FilesRead.Add(fp);
                            if (Edition.Contains(nom))
                                cur.Edited.Add(Texte(Val(inp, "file_path")) ?? Texte(Val(inp, "notebook_path")) ?? "?");
                            if (nom == "Bash") cur.Bash.Add((Texte(Val(inp, "command")) ?? "").Trim());
                        }
                    }
                    else if (type == "user")
                    {
                        foreach (var b in Blocs(Val(msg, "content")))
                            if (Texte(Val(b, "type")) == "tool_result" && TailleResultat(b) >= Gros)
                                cur.Big.Add(noms.TryGetValue(Texte(Val(b, "tool_use_id")) ?? "", out var n) ? n : "?");
                    }
                }
            }

            var sortie = new List<Tour>();
            foreach (var t in tours)
            {
                double total = 0, pic = 0, ex100 = 0, ex150 = 0;
                foreach (var m in t.Msgs.Values)
                {
                    var f = Famille(m.Model);
                    if (f == null) continue;
                    var c = Cout(f, m);
                    total += c;
                    t.Fams[f] = (t.Fams.TryGetValue(f, out var anc) ? anc : 0) + c;
                    t.Usage.In += m.In; t.Usage.Out += m.Out; t.Usage.Cw += m.Cw; t.Usage.Cr += m.Cr;
                    var ctx = m.In + m.Cw + m.Cr;
                    pic = Math.Max(pic, ctx);
                    var unite = Prix[f][0] * CR / 1e6;   // l'excès est supposé relu depuis le cache
                    ex100 += Math.Max(0, ctx - 100000) * unite;
                    ex150 += Math.Max(0, ctx - 150000) * unite;
                }
                if (t.Fams.Count == 0) continue;
                t.Main = t.Fams.OrderByDescending(x => x.Value).First().Key;
                t.Cost = total; t.Peak = pic; t.Ex100 = ex100; t.Ex150 = ex150;
                sortie.Add(t);
            }
            return sortie;
        }

        // ------------------------------------------------------------ analyse
        static string Classer(Tour t)
        {
            int nt = t.Tools.Count, ed = t.Edited.Count;
            double o = t.Usage.Out;
            var outils = new HashSet<string>(t.Tools);
            if (Complexe.IsMatch(t.Prompt) || ed >= 5 || outils.Contains("Task") || o > 8000) return "complexe";
            if (nt == 0 && t.Peak >= CtxSimple) return "suite de conversation";
            if (nt == 0 && Ecriture.IsMatch(t.Prompt)) return "moyenne";
            if (nt == 0 && o < 1500) return "question courte";
            if (ed == 0 && outils.All(x => Lecture.Contains(x)) && nt <= 10) return "exploration";
            if (Git.IsMatch(t.Prompt) && ed == 0) return "git/commande";
            if (ed <= 2 && o < 3000 && nt <= 8) return "petite correction";
            return "moyenne";
        }

        static Analyse Analyser(List<Tour> tours, Dictionary<string, int> clears)
        {
            var a = new Analyse();
            var total = tours.Sum(t => t.Cost);
            if (total <= 0) total = 1e-9;
            a.Total = total;
            foreach (var t in tours)
                foreach (var kv in t.Fams) a.ByModel[kv.Key] = (a.ByModel.TryGetValue(kv.Key, out var v) ? v : 0) + kv.Value;

            var fit = new Dictionary<string, Ajust>();
            var exemples = new List<KeyValuePair<double, Tour>>();
            foreach (var t in tours)
            {
                t.Kind = Classer(t);
                var tgt = Cible[t.Kind];
                if (tgt == null || Rang[t.Main] <= Rang[tgt]) continue;
                var s = t.Cost - Cout(t.Main, t.Usage, tgt);
                a.Save += s;
                var k = t.Kind + "|" + t.Main + "|" + tgt;
                if (!fit.TryGetValue(k, out var aj)) fit[k] = aj = new Ajust { Kind = t.Kind, From = t.Main, To = tgt };
                aj.N++; aj.Save += s;
                var net = t.Prompt.Replace("[contenu collé]", "").Replace("(message sans texte)", "").Trim();
                if (net.Length > 3) exemples.Add(new KeyValuePair<double, Tour>(s, t));
            }

            double apresLo = 0, apresHi = 0, exSomme = 0;
            foreach (var t in tours)
            {
                var tgt = Cible[t.Kind];
                var ratio = tgt != null && Rang[t.Main] > Rang[tgt] && t.Cost > 0 ? Cout(t.Main, t.Usage, tgt) / t.Cost : 1;
                apresLo += Math.Max(0, t.Cost - t.Ex150) * ratio;   // prudent : conversation plafonnée à 150k
                apresHi += Math.Max(0, t.Cost - t.Ex100) * ratio;   // standard : plafonnée à 100k
                exSomme += t.Ex100;
            }
            a.Lo = 1 - apresLo / total; a.Hi = 1 - apresHi / total; a.CtxProj = exSomme / total;

            var longs = tours.Where(t => t.Peak >= CtxAlerte).ToList();
            a.LongTurns = longs.Count;
            a.CtxPct = longs.Sum(t => t.Cost) / total;
            a.CrShare = tours.Sum(t => t.Usage.Cr * Prix[t.Main][0] * CR / 1e6) / total;

            var parSession = tours.GroupBy(t => t.Sid);
            var relus = new List<KeyValuePair<string, int>>();
            var repetes = new List<KeyValuePair<string, int>>();
            foreach (var g in parSession)
            {
                var ts = g.ToList();
                if (ts.Count >= 25 && (!clears.TryGetValue(g.Key, out var nc) || nc == 0) && ts.Max(x => x.Peak) >= CtxHaut)
                    a.Marathons.Add(ts.Count);
                foreach (var f in ts.SelectMany(t => t.FilesRead).GroupBy(x => x).Where(x => x.Count() >= 3))
                    relus.Add(new KeyValuePair<string, int>(f.Key, f.Count()));
                foreach (var cmd in ts.SelectMany(t => t.Bash).Where(x => x.Length > 8).GroupBy(x => x).Where(x => x.Count() >= 3))
                    repetes.Add(new KeyValuePair<string, int>(cmd.Key, cmd.Count()));
                foreach (var n in ts.SelectMany(t => t.Big))
                    a.Big[n] = (a.Big.TryGetValue(n, out var v) ? v : 0) + 1;
            }
            a.Reread = relus.OrderByDescending(x => x.Value).Take(5).ToList();
            a.Repeat = repetes.OrderByDescending(x => x.Value).Take(5).ToList();
            a.Fit = fit.Values.OrderByDescending(x => x.Save).ToList();
            a.Examples = exemples.OrderByDescending(x => x.Key).Take(5).Select(x => x.Value).ToList();
            return a;
        }

        // ------------------------------------------------------------ rapport HTML
        static string Esc(string s) => (s ?? "").Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
        static int Pct(double x) => (int)Math.Floor(x * 100 + 0.5);   // arrondi « classique », comme Math.round en JS
        static string Mult(double x) => (1 / Math.Max(0.05, 1 - x)).ToString("0.0", CultureInfo.InvariantCulture).Replace(".", ",");
        static string Plage(double lo, double hi, Func<double, string> f) => f(lo) == f(hi) ? f(lo) : f(lo) + " à " + f(hi);
        static string Coupe(string s, int n) => s.Length <= n ? s : s.Substring(0, n);

        static string Section(string titre, string grand, string legende, string corps, string conseil) =>
            $"<section><h2>{titre}</h2><div class=\"big\">{grand}<small>{legende}</small></div>{corps}" +
            (string.IsNullOrEmpty(conseil) ? "" : $"<p class=\"tip\">{conseil}</p>") + "</section>";

        static string Details(string label, string inner) => $"<details><summary>{label}</summary>{inner}</details>";

        static string AfficherVue(Analyse a, int n, int jours)
        {
            var savePct = Pct(a.Save / a.Total);
            var modeles = string.Concat(a.ByModel.OrderByDescending(x => x.Value).Select(kv =>
                $"<div class=\"bar\"><span>{kv.Key}</span><i style=\"width:{Math.Max(2, Pct(kv.Value / a.Total))}%\"></i><b>{Pct(kv.Value / a.Total)} %</b></div>"));
            var lignesFit = string.Concat(a.Fit.Where(f => f.Save / a.Total >= 0.01).Select(f =>
                $"<li><b>{f.N}</b> {(f.N > 1 ? Libelle[f.Kind] : LibelleSingulier[f.Kind])} {(f.N > 1 ? "faites" : "faite")} avec <b>{f.From}</b> : <b>{f.To}</b> aurait probablement suffi <em>(−{Pct(f.Save / a.Total)} %)</em></li>"));
            var exemples = string.Concat(a.Examples.Select(t =>
                $"<li>« {Esc(Coupe(t.Prompt.Replace("\n", " "), 90))} » <em>{t.Main} → {Cible[t.Kind]}</em></li>"));

            var ctx = new StringBuilder();
            if (a.LongTurns > 0) ctx.Append($"<li><b>{a.LongTurns}</b> demandes lancées avec une conversation très longue = <b>{Pct(a.CtxPct)} %</b> de ta consommation</li>");
            ctx.Append($"<li>Claude relit tout l'historique à chaque message : <b>{Pct(a.CrShare)} %</b> de la consommation</li>");
            foreach (var m in a.Marathons.Take(3)) ctx.Append($"<li>Une conversation de <b>{m}</b> demandes sans jamais repartir de zéro</li>");

            var gaspi = new List<string>();
            foreach (var kv in a.Reread) gaspi.Add($"<li>Même fichier relu <b>{kv.Value}</b> fois : <code>{Esc(Path.GetFileName(kv.Key.Replace('/', '\\')))}</code></li>");
            foreach (var kv in a.Repeat) gaspi.Add($"<li>Même commande lancée <b>{kv.Value}</b> fois : <code>{Esc(Coupe(kv.Key, 60))}</code></li>");
            foreach (var kv in a.Big) gaspi.Add($"<li><b>{kv.Value}</b> résultats « {Esc(kv.Key)} » énormes : Claude a ingéré beaucoup de texte d'un coup</li>");

            string projet;
            if (a.Hi >= 0.05)
            {
                var grand = Pct(a.Lo) == Pct(a.Hi) ? $"−{Pct(a.Lo)} %" : $"−{Pct(a.Lo)} à −{Pct(a.Hi)} %";
                var repart = Pct(a.CtxProj) > 0
                    ? $"<li>Repartir de zéro avant que la conversation gonfle : environ <b>−{Pct(a.CtxProj)} %</b></li>"
                    : "<li>Repartir de zéro : rien à gagner, tes conversations restent courtes</li>";
                projet = Section("Si tu avais suivi ces conseils", grand, "de consommation",
                    $"<p>Soit environ <b>×{Plage(a.Lo, a.Hi, Mult)}</b> de travail avant d'atteindre ta limite.</p>" +
                    $"<details><summary>Comment on l'estime</summary><ul><li>Le bon modèle pour les tâches simples : environ <b>−{savePct} %</b></li>{repart}</ul>" +
                    "<p><em>Les deux effets se recoupent, ils ne s'additionnent pas. C'est une estimation : Haiku peut faire un peu moins bien qu'Opus, repartir de zéro oblige à réexpliquer, et le lien avec ta limite d'abonnement reste approximatif.</em></p></details>", "");
            }
            else
                projet = Section("Si tu avais suivi ces conseils", "≈ 0 %", "d'écart", "<p class='ok'>Tu utilises déjà bien ton forfait : il n'y a presque rien à gagner.</p>", "");

            var sb = new StringBuilder();
            sb.Append($"<div class=\"view\" data-days=\"{jours}\"><p class=\"sub\">{n} demandes analysées sur {(jours > 1 ? $"les {jours} derniers jours" : "le dernier jour")}. Tout est resté sur ton ordinateur.</p>\n");
            sb.Append($"<section><h2>Où part ta consommation</h2>{modeles}<details><summary>Vérifier ces chiffres dans Claude Code</summary><p>Tape <code>/usage</code> (tokens par modèle) et <code>/context</code> (ce qui remplit ta conversation).</p></details></section>\n");
            sb.Append(Section("Le bon modèle pour la bonne tâche", a.Fit.Count > 0 ? $"{savePct} %" : "✓",
                a.Fit.Count > 0 ? "de ta consommation aurait pu être évitée" : "rien à signaler",
                a.Fit.Count > 0 ? Details("Voir le détail", $"<ul>{lignesFit}</ul><p><em>Exemples :</em></p><ul>{exemples}</ul>") : "<p class='ok'>Tes tâches simples ne tournent pas sur un modèle trop gros.</p>",
                a.Fit.Count > 0 ? "Un gros modèle pour une petite tâche, c'est un camion pour livrer une lettre. Choisis le modèle au début d'une session, ou juste après <code>/clear</code> (commande <code>/model</code>). Changer en pleine conversation oblige Claude à tout relire sans économie : un message cher, qui ne se rentabilise que si la conversation continue longtemps." : ""));
            sb.Append("\n");
            sb.Append(Section("Conversation trop longue", $"{Pct(a.CrShare)} %", "de la consommation sert à relire l'historique (normal)",
                (a.CtxProj >= 0.005
                    ? $"<p>Ce qui se gaspille, c'est la relecture au-delà de 100 000 tokens de contexte : environ <b>{Pct(a.CtxProj)} %</b> de ta consommation.</p>"
                    : "<p class='ok'>Aucune conversation n'a dépassé 100 000 tokens : rien à corriger de ce côté.</p>") +
                Details("Voir le détail", $"<ul>{ctx}</ul><p><b>Ce qui dure</b> (comment lancer le projet, les conventions, ce qu'il ne faut pas toucher) va dans <code>CLAUDE.md</code>. <b>L'état d'une tâche en cours</b> se range plutôt dans un petit fichier de notes à part : <code>CLAUDE.md</code> est relu à chaque message, donc chaque ligne qu'on y met coûte à chaque fois.</p>"),
                "Quand tu changes de sujet, tape <code>/clear</code> : Claude oublie la conversation (il faudra tout réexpliquer) mais ne la relit plus à chaque message. Pour continuer la même tâche, <code>/compact</code> la résume : on perd du détail, alors fais-le entre deux étapes plutôt qu'en plein milieu. Avant de nettoyer, demande à Claude : « note en dix lignes maximum ce qu'il faut retenir de ce projet dans CLAUDE.md »."));
            sb.Append("\n");
            sb.Append(Section("Actions inutiles", gaspi.Count.ToString(), "signaux repérés",
                gaspi.Count > 0 ? Details("Voir le détail", $"<ul>{string.Concat(gaspi)}</ul>") : "<p class='ok'>Rien de flagrant.</p>",
                gaspi.Count > 0 ? "Demande à Claude de chercher dans un fichier précis plutôt que de tout relire." : ""));
            sb.Append("\n").Append(projet).Append("\n</div>");
            return sb.ToString();
        }

        const string Css = @"
:root{color-scheme:dark;--marine:#0B2E4A;--deep:#082238;--sable:#FFE3A6;--ciel:#5E9BA6;--rouge:#E0262B;--muted:#A9C1D1;
--sans:""Space Grotesk"",system-ui,-apple-system,""Segoe UI"",Roboto,sans-serif;--mono:""Space Mono"",ui-monospace,""SF Mono"",Menlo,Consolas,monospace}
body{margin:0;background:var(--marine);color:#fff;font:16px/1.55 var(--sans)}
main{max-width:760px;margin:0 auto;padding:36px 20px 72px}
.logo{font:700 30px var(--sans);letter-spacing:-.5px;margin:0 0 6px}.pill{display:inline-block;margin-left:4px;padding:0 12px;background:var(--rouge);border-radius:999px}
.sub{color:var(--muted);margin:0 0 28px}
section{background:var(--deep);border:2px solid var(--deep);border-radius:12px;padding:22px 24px;margin:22px 0;box-shadow:8px 8px 0 var(--ciel)}
h2{margin:0 0 10px;font:700 12px var(--mono);letter-spacing:.08em;text-transform:uppercase;color:var(--sable)}
.big{font-size:52px;font-weight:700;line-height:1.1;color:var(--sable)}.big small{display:block;font-size:16px;font-weight:400;color:var(--muted);margin-top:4px}
ul{padding-left:20px}li{margin:7px 0}em{color:var(--muted);font-style:normal}
code{font-family:var(--mono);font-size:.9em;background:var(--marine);padding:1px 6px;border-radius:4px}
.tip{background:var(--ciel);color:#fff;border-radius:8px;padding:12px 14px;margin:14px 0 0}.tip code{background:var(--marine)}
.bar{display:flex;align-items:center;gap:10px;margin:8px 0}.bar span{width:70px;font-family:var(--mono);font-size:14px}
.bar i{display:block;height:12px;background:var(--sable);border-radius:6px}.bar b{font-weight:700;white-space:nowrap;min-width:52px;text-align:right}
.ok{color:var(--sable)}
details{margin:14px 0 0}summary{cursor:pointer;color:var(--ciel);font:700 13px var(--mono);letter-spacing:.04em}summary::marker{color:var(--ciel)}details[open] summary{margin-bottom:6px}footer{color:var(--muted);font-size:13px;margin-top:34px}
[hidden]{display:none!important}
.tabs{display:flex;gap:8px;margin:14px 0 6px}
.tabs button{font:700 13px var(--mono);letter-spacing:.04em;background:var(--deep);color:var(--muted);border:2px solid var(--ciel);border-radius:999px;padding:6px 16px;cursor:pointer}
.tabs button.on{background:var(--sable);color:var(--marine);border-color:var(--sable)}
";
        const string JsOnglets = @"document.querySelectorAll("".tabs button"").forEach(function(b){b.onclick=function(){document.querySelectorAll("".tabs button"").forEach(function(x){x.classList.toggle(""on"",x===b)});document.querySelectorAll("".view"").forEach(function(v){v.hidden=v.dataset.days!==b.dataset.d})}})";

        static string Page(List<Vue> vues)
        {
            var onglets = vues.Count > 1
                ? "<div class=\"tabs\">" + string.Concat(vues.Select((v, i) => $"<button type=\"button\" data-d=\"{v.Days}\"{(i == 0 ? " class=\"on\"" : "")}>{v.Days} jours</button>")) + "</div>"
                : "";
            var corps = string.Join("\n", vues.Select((v, i) =>
            {
                var html = AfficherVue(v.A, v.N, v.Days);
                return i == 0 ? html : html.Replace("class=\"view\"", "class=\"view\" hidden");
            }));
            return "<!doctype html><html lang=\"fr\"><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\">\n" +
                   "<title>Diagnostic Claude Code</title><style>" + Css + "</style>\n" +
                   "<main><h1 class=\"logo\">diag<span class=\"pill\">run</span></h1>" + onglets + "\n" + corps + "\n" +
                   "<footer>Diagnostic réalisé en local par Jauge Claude, d'après <a href=\"https://github.com/Francoisnollet/diagrun\" style=\"color:inherit\">diag run</a> de François Nollet (licence MIT). " +
                   "Les montants sont des estimations basées sur les tarifs publics (modifiables dans Diagnostic.cs). L'analyse de la tâche est approximative : c'est une indication, pas un verdict. " +
                   "Ce rapport peut montrer le début de tes demandes et des chemins de fichiers : relis-le avant d'en partager une capture.</footer></main>" +
                   (vues.Count > 1 ? "<script>" + JsOnglets + "</script>" : "") + "</html>";
        }

        // ------------------------------------------------------------ point d'entrée
        /// <summary>Analyse les sessions locales et écrit le rapport. Renvoie son chemin, ou null si aucune session trouvée.</summary>
        public static string Generer()
        {
            var tours = Tours(Charger(30), out var clears);
            if (tours.Count == 0) return null;
            var maintenant = DateTimeOffset.UtcNow;
            var vues = new List<Vue>();
            foreach (var jours in new[] { 7, 30 })
            {
                var coupure = maintenant.AddDays(-jours).ToUnixTimeMilliseconds();
                var sous = tours.Where(t => t.Ts >= coupure).ToList();
                if (sous.Count == 0) continue;
                if (vues.Count > 0 && vues[vues.Count - 1].N == sous.Count) continue;   // pas d'onglet en double
                vues.Add(new Vue { Days = jours, N = sous.Count, A = Analyser(sous, clears) });
            }
            var chemin = Path.Combine(Path.GetTempPath(), "JaugeClaude-diagnostic.html");
            File.WriteAllText(chemin, Page(vues), new UTF8Encoding(false));
            return chemin;
        }

        public static string DossierSessions => Dossier();
    }
}
