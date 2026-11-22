// Diagrun : mes tokens sous la loupe.
//
// Portage en C# de « diag run » (cli.js v0.1.1), l'outil de François Nollet :
// https://github.com/Francoisnollet/diagrun
// Copyright (c) 2026 François Nollet, licence MIT (https://github.com/Francoisnollet/diagrun/blob/main/LICENSE).
// Réutilisé avec son accord. La logique d'analyse et les textes du rapport sont les siens ;
// seul le langage change (JavaScript/Node -> C#/.NET Framework), pour ne plus dépendre de Node.
//
// Ce que fait ce fichier : lit les sessions Claude Code déjà présentes sur l'ordinateur
// (%USERPROFILE%\.claude\projects), calcule un diagnostic et l'ouvre dans le navigateur.
// Aucun appel réseau : rien n'est envoyé nulle part.
//
// Écarts volontaires par rapport à l'original :
// - le rapport est écrit dans %APPDATA%\JaugeClaude\diagnostic\rapport.html (dossier de la Jauge) ;
// - une phrase au singulier est corrigée (« 1 question courte » au lieu de « questions courte »).
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace JaugeClaude
{
    static class Diagnostic
    {
        const string Version = "0.1.1";

        // Tarifs ($/M tokens) : hypothèse. Sur abonnement, à lire comme des PARTS de consommation.
        static readonly Dictionary<string, double[]> Prices = new Dictionary<string, double[]>
        {
            { "opus", new[] { 5.0, 25.0 } }, { "sonnet", new[] { 3.0, 15.0 } }, { "haiku", new[] { 1.0, 5.0 } },
        };
        const double CW = 1.25, CR = 0.1;
        const double CtxWarn = 100000, CtxHigh = 150000, BigSize = 20000, CtxSimple = 40000;
        const int MaxDays = 30;

        static readonly HashSet<string> Edit = new HashSet<string> { "Edit", "Write", "MultiEdit", "NotebookEdit" };
        static readonly HashSet<string> Read = new HashSet<string> { "Read", "Grep", "Glob", "LS", "WebFetch", "WebSearch" };
        static readonly Regex Complex = new Regex(@"architect|refactor|redesign|migrat|investigat|root cause|pourquoi|concoi|conçoi|stratég|sécurit|security|performance|debug|race condition|from scratch", RegexOptions.IgnoreCase);
        static readonly Regex Write = new Regex(@"r[ée]dige|[ée]cris|draft|write|prospect|e-?mail|mail\b|brouillon|traduis|translate|r[ée]sume|summari[sz]e", RegexOptions.IgnoreCase);
        static readonly Regex Git = new Regex(@"\b(commit|push|pull request|pr|git (status|log|diff))\b", RegexOptions.IgnoreCase);

        static readonly Dictionary<string, string> Label = new Dictionary<string, string>
        {
            { "question courte", "questions courtes" }, { "exploration", "recherches dans le code" }, { "git/commande", "commandes git" },
            { "petite correction", "petites corrections" }, { "moyenne", "tâches moyennes" }, { "complexe", "tâches complexes" },
            { "suite de conversation", "suites de conversation" },
        };
        static readonly Dictionary<string, string> Singulier = new Dictionary<string, string>
        {
            { "question courte", "question courte" }, { "exploration", "recherche dans le code" }, { "git/commande", "commande git" },
            { "petite correction", "petite correction" }, { "moyenne", "tâche moyenne" }, { "complexe", "tâche complexe" },
            { "suite de conversation", "suite de conversation" },
        };
        static readonly Dictionary<string, string> Target = new Dictionary<string, string>
        {
            { "question courte", "haiku" }, { "exploration", "haiku" }, { "git/commande", "haiku" }, { "petite correction", "haiku" },
            { "moyenne", "sonnet" }, { "complexe", null }, { "suite de conversation", null },
        };
        static readonly Dictionary<string, int> Rank = new Dictionary<string, int> { { "haiku", 0 }, { "sonnet", 1 }, { "opus", 2 } };

        // ---------- Types ----------
        class Ev { public double T; public Dictionary<string, object> Raw; }
        class Msg { public string Model; public double In, Out, Cw, Cr; }
        class Turn
        {
            public string Sid, Prompt, Main, Kind;
            public double Ts, Cost, Peak, Ex100, Ex150;
            public Dictionary<string, Msg> Msgs = new Dictionary<string, Msg>();
            public List<string> Tools = new List<string>(), FilesRead = new List<string>(), Bash = new List<string>(), Big = new List<string>();
            public HashSet<string> Edited = new HashSet<string>();
            public Msg Usage;
            public Dictionary<string, double> Fams;
        }
        class Fit { public int N; public double Save; public string Kind, From, To; }
        class Ex { public double S; public Turn T; }
        class Analysis
        {
            public double ProjLo, ProjHi, ProjCtx, Total, Save, CtxPct, CrShare;
            public Dictionary<string, double> ByModel = new Dictionary<string, double>();
            public List<Fit> Fits; public List<Ex> Examples;
            public int LongTurns;
            public List<KeyValuePair<string, List<Turn>>> Marathons;
            public List<KeyValuePair<string, int>> Reread, Repeat;
            public Dictionary<string, int> Big = new Dictionary<string, int>();
        }

        // ---------- Aides JSON (JavaScriptSerializer renvoie Dictionary / object[]) ----------
        static object G(object o, string k)
        {
            var d = o as Dictionary<string, object>; object v;
            return d != null && d.TryGetValue(k, out v) ? v : null;
        }
        static string Str(object o) => o as string;
        static bool Vrai(object o) => o is bool && (bool)o;
        static double Num(object o)
        {
            try { return o == null ? 0 : Convert.ToDouble(o, CultureInfo.InvariantCulture); } catch { return 0; }
        }
        static IEnumerable<object> Liste(object o)
        {
            if (o == null || o is string || o is Dictionary<string, object>) return new object[0];
            var e = o as IEnumerable;
            return e == null ? new object[0] : e.Cast<object>();
        }
        static bool EstListe(object o) => o != null && !(o is string) && !(o is Dictionary<string, object>) && o is IEnumerable;

        static string TextOf(object c)
        {
            if (c is string) return (string)c;
            return string.Join("\n", Liste(c).Where(b => Str(G(b, "type")) == "text").Select(b => Str(G(b, "text")) ?? ""));
        }

        // ---------- Lecture ----------
        static string CleanPrompt(string t)
        {
            var x = Regex.Replace(t, @"<(ide_[a-z_]+|system-reminder)>[\s\S]*?</\1>", " ");
            x = Regex.Replace(x, @"<pasted_content[^>]*>[\s\S]*?</pasted_content>|<pasted_content[^>]*>[\s\S]*$", " [contenu collé] ");
            x = Regex.Replace(x, @"\s+", " ").Trim();
            return x.Length > 0 ? x : "(message sans texte)";
        }

        static string RealPrompt(Dictionary<string, object> e)
        {
            if (Str(G(e, "type")) != "user" || Vrai(G(e, "isSidechain")) || Vrai(G(e, "isMeta"))) return null;
            var c = G(G(e, "message"), "content");
            if (EstListe(c) && Liste(c).Any(b => Str(G(b, "type")) == "tool_result")) return null;
            var raw = TextOf(c).Trim();
            if (raw.Length == 0 || raw.StartsWith("<local-command") || raw.StartsWith("Caveat:") || raw.StartsWith("<system-reminder")) return null;
            return CleanPrompt(raw);
        }

        static double ResultSize(object b)
        {
            var c = G(b, "content");
            if (c is string) return ((string)c).Length;
            if (EstListe(c)) return Liste(c).Sum(x => (double)(Str(G(x, "text")) ?? "").Length);
            return 0;
        }

        static Dictionary<string, List<Ev>> Load()
        {
            var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".claude", "projects");
            var cutoffDate = DateTime.UtcNow.AddDays(-MaxDays);
            double cutoff = new DateTimeOffset(cutoffDate).ToUnixTimeMilliseconds();
            var sessions = new Dictionary<string, List<Ev>>();
            var js = new JavaScriptSerializer { MaxJsonLength = int.MaxValue, RecursionLimit = 1000 };
            IEnumerable<string> files;
            try { files = Directory.EnumerateFiles(root, "*.jsonl", SearchOption.AllDirectories).ToList(); }
            catch { return sessions; }
            foreach (var file in files)
            {
                try
                {
                    if (File.GetLastWriteTimeUtc(file) < cutoffDate) continue;   // aucun événement récent dans ce fichier
                    using (var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var sr = new StreamReader(fs, Encoding.UTF8))
                    {
                        string line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            if (line.Length == 0 || line.IndexOf("\"timestamp\"", StringComparison.Ordinal) < 0) continue;
                            Dictionary<string, object> e;
                            try { e = js.DeserializeObject(line) as Dictionary<string, object>; } catch { continue; }
                            if (e == null) continue;
                            var ts = Str(G(e, "timestamp"));
                            DateTimeOffset dto;
                            if (string.IsNullOrEmpty(ts) || !DateTimeOffset.TryParse(ts, CultureInfo.InvariantCulture, DateTimeStyles.None, out dto)) continue;
                            double t = dto.ToUnixTimeMilliseconds();
                            if (!(t >= cutoff)) continue;
                            var sid = Str(G(e, "sessionId")) ?? file;
                            List<Ev> list;
                            if (!sessions.TryGetValue(sid, out list)) sessions[sid] = list = new List<Ev>();
                            list.Add(new Ev { T = t, Raw = e });
                        }
                    }
                }
                catch { /* fichier illisible : on passe au suivant */ }
            }
            return sessions;
        }

        // ---------- Analyse ----------
        static string Family(string m)
        {
            m = (m ?? "").ToLowerInvariant();
            foreach (var f in new[] { "opus", "sonnet", "haiku" }) if (m.Contains(f)) return f;
            return null;
        }
        static double Cost(string fam, Msg u, string asFam = null)
        {
            var p = Prices[asFam ?? fam];
            return (u.In * p[0] + u.Out * p[1] + u.Cw * p[0] * CW + u.Cr * p[0] * CR) / 1e6;
        }

        static List<Turn> BuildTurns(Dictionary<string, List<Ev>> sessions, Dictionary<string, int> clears)
        {
            var turns = new List<Turn>();
            foreach (var kv in sessions)
            {
                var sid = kv.Key;
                var toolNames = new Dictionary<string, string>();
                Turn cur = null; clears[sid] = 0;
                foreach (var ev in kv.Value.OrderBy(x => x.T))
                {
                    var e = ev.Raw;
                    var p = RealPrompt(e);
                    if (p != null)
                    {
                        if (p.Contains("<command-name>/clear")) { clears[sid]++; cur = null; continue; }
                        if (p.StartsWith("<command-name>")) continue;
                        cur = new Turn { Sid = sid, Ts = ev.T, Prompt = p };
                        turns.Add(cur); continue;
                    }
                    if (cur == null) continue;
                    var msg = G(e, "message");
                    var type = Str(G(e, "type"));
                    var usage = G(msg, "usage");
                    if (type == "assistant" && usage != null)
                    {
                        var id = Str(G(msg, "id")) ?? Str(G(e, "uuid")) ?? Guid.NewGuid().ToString();
                        Msg r;
                        if (!cur.Msgs.TryGetValue(id, out r)) cur.Msgs[id] = r = new Msg { Model = Str(G(msg, "model")) };
                        r.In = Math.Max(r.In, Num(G(usage, "input_tokens")));
                        r.Out = Math.Max(r.Out, Num(G(usage, "output_tokens")));
                        r.Cw = Math.Max(r.Cw, Num(G(usage, "cache_creation_input_tokens")));
                        r.Cr = Math.Max(r.Cr, Num(G(usage, "cache_read_input_tokens")));
                        foreach (var b in Liste(G(msg, "content")))
                        {
                            var bid = Str(G(b, "id"));
                            if (Str(G(b, "type")) == "tool_use" && bid != null && !toolNames.ContainsKey(bid))
                            {
                                var name = Str(G(b, "name")) ?? "?";
                                toolNames[bid] = name;
                                var inp = G(b, "input");
                                cur.Tools.Add(name);
                                var fp = Str(G(inp, "file_path"));
                                if (name == "Read" && !string.IsNullOrEmpty(fp)) cur.FilesRead.Add(fp);
                                if (Edit.Contains(name))
                                    cur.Edited.Add(!string.IsNullOrEmpty(fp) ? fp : (Str(G(inp, "notebook_path")) ?? "?"));
                                if (name == "Bash") cur.Bash.Add((Str(G(inp, "command")) ?? "").Trim());
                            }
                        }
                    }
                    else if (type == "user" && EstListe(G(msg, "content")))
                    {
                        foreach (var b in Liste(G(msg, "content")))
                            if (Str(G(b, "type")) == "tool_result" && ResultSize(b) >= BigSize)
                            {
                                string nm; var tid = Str(G(b, "tool_use_id"));
                                cur.Big.Add(tid != null && toolNames.TryGetValue(tid, out nm) ? nm : "?");
                            }
                    }
                }
            }
            var result = new List<Turn>();
            foreach (var t in turns)
            {
                var agg = new Msg(); double total = 0, peak = 0, ex100 = 0, ex150 = 0;
                var fams = new Dictionary<string, double>();
                foreach (var m in t.Msgs.Values)
                {
                    var f = Family(m.Model); if (f == null) continue;
                    var c = Cost(f, m); total += c;
                    double old; fams.TryGetValue(f, out old); fams[f] = old + c;
                    agg.In += m.In; agg.Out += m.Out; agg.Cw += m.Cw; agg.Cr += m.Cr;
                    var ctx = m.In + m.Cw + m.Cr; peak = Math.Max(peak, ctx);
                    var unit = Prices[f][0] * CR / 1e6;   // l'excès est supposé relu depuis le cache
                    ex100 += Math.Max(0, ctx - 100000) * unit; ex150 += Math.Max(0, ctx - 150000) * unit;
                }
                if (fams.Count == 0) continue;
                t.Main = fams.OrderByDescending(x => x.Value).First().Key;
                t.Usage = agg; t.Cost = total; t.Peak = peak; t.Fams = fams; t.Ex100 = ex100; t.Ex150 = ex150;
                result.Add(t);
            }
            return result;
        }

        static string Classify(Turn t)
        {
            int nt = t.Tools.Count, ed = t.Edited.Count; double o = t.Usage.Out; var tools = new HashSet<string>(t.Tools);
            if (Complex.IsMatch(t.Prompt) || ed >= 5 || tools.Contains("Task") || o > 8000) return "complexe";
            if (nt == 0 && t.Peak >= CtxSimple) return "suite de conversation";
            if (nt == 0 && Write.IsMatch(t.Prompt)) return "moyenne";
            if (nt == 0 && o < 1500) return "question courte";
            if (ed == 0 && tools.All(x => Read.Contains(x)) && nt <= 10) return "exploration";
            if (Git.IsMatch(t.Prompt) && ed == 0) return "git/commande";
            if (ed <= 2 && o < 3000 && nt <= 8) return "petite correction";
            return "moyenne";
        }

        static Analysis Analyse(List<Turn> turns, Dictionary<string, int> clears)
        {
            var a = new Analysis();
            double total = turns.Sum(t => t.Cost); if (total == 0) total = 1e-9;
            a.Total = total;
            foreach (var t in turns)
                foreach (var f in t.Fams)
                { double o; a.ByModel.TryGetValue(f.Key, out o); a.ByModel[f.Key] = o + f.Value; }
            var fit = new Dictionary<string, Fit>(); var examples = new List<Ex>(); double save = 0;
            foreach (var t in turns)
            {
                t.Kind = Classify(t); var tgt = Target[t.Kind];
                if (tgt != null && Rank[t.Main] > Rank[tgt])
                {
                    var s = t.Cost - Cost(t.Main, t.Usage, tgt); save += s;
                    var k = t.Kind + "|" + t.Main + "|" + tgt;
                    Fit ft; if (!fit.TryGetValue(k, out ft)) fit[k] = ft = new Fit { Kind = t.Kind, From = t.Main, To = tgt };
                    ft.N++; ft.Save += s;
                    if (Regex.Replace(t.Prompt, @"\[contenu collé\]|\(message sans texte\)", "").Trim().Length > 3) examples.Add(new Ex { S = s, T = t });
                }
            }
            double afterLo = 0, afterHi = 0, exSum = 0;
            foreach (var t in turns)
            {
                var tgt = Target[t.Kind];
                double ratio = tgt != null && Rank[t.Main] > Rank[tgt] && t.Cost > 0 ? Cost(t.Main, t.Usage, tgt) / t.Cost : 1;
                afterLo += Math.Max(0, t.Cost - t.Ex150) * ratio;   // prudent : conversation plafonnée à 150k
                afterHi += Math.Max(0, t.Cost - t.Ex100) * ratio;   // standard : plafonnée à 100k
                exSum += t.Ex100;
            }
            a.ProjLo = 1 - afterLo / total; a.ProjHi = 1 - afterHi / total; a.ProjCtx = exSum / total;
            var longTurns = turns.Where(t => t.Peak >= CtxWarn).ToList();
            a.LongTurns = longTurns.Count;
            a.CtxPct = longTurns.Sum(t => t.Cost) / total;
            a.CrShare = turns.Sum(t => t.Usage.Cr * Prices[t.Main][0] * CR / 1e6) / total;
            var bySess = new Dictionary<string, List<Turn>>();
            foreach (var t in turns)
            {
                List<Turn> l; if (!bySess.TryGetValue(t.Sid, out l)) bySess[t.Sid] = l = new List<Turn>();
                l.Add(t);
            }
            a.Marathons = bySess.Where(kv =>
            {
                int c; clears.TryGetValue(kv.Key, out c);
                return kv.Value.Count >= 25 && c == 0 && kv.Value.Max(x => x.Peak) >= CtxHigh;
            }).ToList();
            var reread = new List<KeyValuePair<string, int>>(); var repeat = new List<KeyValuePair<string, int>>();
            foreach (var ts in bySess.Values)
            {
                var rc = new Dictionary<string, int>();
                foreach (var f in ts.SelectMany(t => t.FilesRead)) { int n; rc.TryGetValue(f, out n); rc[f] = n + 1; }
                reread.AddRange(rc.Where(x => x.Value >= 3));
                var cc = new Dictionary<string, int>();
                foreach (var c in ts.SelectMany(t => t.Bash)) if (c.Length > 8) { int n; cc.TryGetValue(c, out n); cc[c] = n + 1; }
                repeat.AddRange(cc.Where(x => x.Value >= 3));
                foreach (var nm in ts.SelectMany(t => t.Big)) { int n; a.Big.TryGetValue(nm, out n); a.Big[nm] = n + 1; }
            }
            a.Examples = examples.OrderByDescending(x => x.S).Take(5).ToList();
            a.Fits = fit.Values.OrderByDescending(x => x.Save).ToList();
            a.Save = save;
            a.Reread = reread.OrderByDescending(x => x.Value).Take(5).ToList();
            a.Repeat = repeat.OrderByDescending(x => x.Value).Take(5).ToList();
            return a;
        }

        // ---------- Rapport HTML ----------
        static string Esc(string s) => (s ?? "").Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
        static int Pct(double x) => (int)Math.Floor(x * 100 + 0.5);
        static string Court(string s, int n) => s.Length > n ? s.Substring(0, n) : s;
        static string NomFichier(string p)
        {
            var i = Math.Max(p.LastIndexOf('/'), p.LastIndexOf('\\'));
            return i >= 0 ? p.Substring(i + 1) : p;
        }
        static string Det(string label, string inner) => "<details><summary>" + label + "</summary>" + inner + "</details>";
        static string Card(string title, string big, string bigLabel, string body, string tip) =>
            "<section><h2>" + title + "</h2><div class='big'>" + big + "<small>" + bigLabel + "</small></div>" + body +
            (string.IsNullOrEmpty(tip) ? "" : "<p class='tip'>" + tip + "</p>") + "</section>";
        static string Mult(double x) => (1 / Math.Max(0.05, 1 - x)).ToString("0.0", CultureInfo.InvariantCulture).Replace(".", ",");
        static string Rng(double lo, double hi, Func<double, string> f) => f(lo) == f(hi) ? f(lo) : f(lo) + " à " + f(hi);

        static string View(Analysis a, int n, int days)
        {
            int savePct = Pct(a.Save / a.Total);
            var models = string.Concat(a.ByModel.OrderByDescending(kv => kv.Value).Select(kv =>
                $"<div class='bar'><span>{kv.Key}</span><i style='width:{Math.Max(2, Pct(kv.Value / a.Total))}%'></i><b>{Pct(kv.Value / a.Total)} %</b></div>"));
            var fitRows = string.Concat(a.Fits.Where(f => f.Save / a.Total >= 0.01).Select(f =>
            {
                string nom = f.N > 1 ? Label[f.Kind] : Singulier[f.Kind];
                string faites = f.N > 1 ? "faites" : "faite";
                return $"<li><b>{f.N}</b> {nom} {faites} avec <b>{f.From}</b> : <b>{f.To}</b> aurait probablement suffi <em>(−{Pct(f.Save / a.Total)} %)</em></li>";
            }));
            var ex = string.Concat(a.Examples.Select(x =>
                $"<li>« {Esc(Court(x.T.Prompt, 90))} » <em>{x.T.Main} → {Target[x.T.Kind]}</em></li>"));
            var ctxItems = new List<string>();
            if (a.LongTurns > 0) ctxItems.Add($"<li><b>{a.LongTurns}</b> demandes lancées avec une conversation très longue = <b>{Pct(a.CtxPct)} %</b> de ta consommation</li>");
            ctxItems.Add($"<li>Claude relit tout l'historique à chaque message : <b>{Pct(a.CrShare)} %</b> de la consommation</li>");
            foreach (var m in a.Marathons.Take(3)) ctxItems.Add($"<li>Une conversation de <b>{m.Value.Count}</b> demandes sans jamais repartir de zéro</li>");
            var waste = new List<string>();
            foreach (var r in a.Reread) waste.Add($"<li>Même fichier relu <b>{r.Value}</b> fois : <code>{Esc(NomFichier(r.Key))}</code></li>");
            foreach (var r in a.Repeat) waste.Add($"<li>Même commande lancée <b>{r.Value}</b> fois : <code>{Esc(Court(r.Key, 60))}</code></li>");
            foreach (var b in a.Big) waste.Add($"<li><b>{b.Value}</b> résultats « {Esc(b.Key)} » énormes : Claude a ingéré beaucoup de texte d'un coup</li>");

            string projCard;
            if (a.ProjHi >= 0.05)
            {
                string big = Pct(a.ProjLo) == Pct(a.ProjHi) ? $"−{Pct(a.ProjLo)} %" : $"−{Pct(a.ProjLo)} à −{Pct(a.ProjHi)} %";
                string ctxLi = Pct(a.ProjCtx) > 0
                    ? $"<li>Repartir de zéro avant que la conversation gonfle : environ <b>−{Pct(a.ProjCtx)} %</b></li>"
                    : "<li>Repartir de zéro : rien à gagner, tes conversations restent courtes</li>";
                string body = $"<p>Soit environ <b>×{Rng(a.ProjLo, a.ProjHi, Mult)}</b> de travail avant d'atteindre ta limite.</p>"
                    + "<details><summary>Comment on l'estime</summary><ul>"
                    + $"<li>Le bon modèle pour les tâches simples : environ <b>−{savePct} %</b></li>" + ctxLi + "</ul>"
                    + "<p><em>Les deux effets se recoupent, ils ne s'additionnent pas. C'est une estimation : Haiku peut faire un peu moins bien qu'Opus, repartir de zéro oblige à réexpliquer, et le lien avec ta limite d'abonnement reste approximatif.</em></p></details>";
                projCard = Card("Si tu avais suivi ces conseils", big, "de consommation", body, "");
            }
            else
                projCard = Card("Si tu avais suivi ces conseils", "≈ 0 %", "d'écart", "<p class='ok'>Tu utilises déjà bien ton forfait : il n'y a presque rien à gagner.</p>", "");

            string periode = days > 1 ? $"les {days} derniers jours" : "le dernier jour";
            bool aFit = a.Fits.Count > 0;
            string modelCard = Card("Le bon modèle pour la bonne tâche", aFit ? savePct + " %" : "✓",
                aFit ? "de ta consommation aurait pu être évitée" : "rien à signaler",
                aFit ? Det("Voir le détail", $"<ul>{fitRows}</ul><p><em>Exemples :</em></p><ul>{ex}</ul>") : "<p class='ok'>Tes tâches simples ne tournent pas sur un modèle trop gros.</p>",
                aFit ? "Un gros modèle pour une petite tâche, c'est un camion pour livrer une lettre. Choisis le modèle au début d'une session, ou juste après <code>/clear</code> (commande <code>/model</code>). Changer en pleine conversation oblige Claude à tout relire sans économie : un message cher, qui ne se rentabilise que si la conversation continue longtemps." : "");
            string ctxBody = (a.ProjCtx >= 0.005
                ? $"<p>Ce qui se gaspille, c'est la relecture au-delà de 100 000 tokens de contexte : environ <b>{Pct(a.ProjCtx)} %</b> de ta consommation.</p>"
                : "<p class='ok'>Aucune conversation n'a dépassé 100 000 tokens : rien à corriger de ce côté.</p>")
                + Det("Voir le détail", $"<ul>{string.Concat(ctxItems)}</ul><p><b>Ce qui dure</b> (comment lancer le projet, les conventions, ce qu'il ne faut pas toucher) va dans <code>CLAUDE.md</code>. <b>L'état d'une tâche en cours</b> se range plutôt dans un petit fichier de notes à part : <code>CLAUDE.md</code> est relu à chaque message, donc chaque ligne qu'on y met coûte à chaque fois.</p>");
            string ctxCard = Card("Conversation trop longue", Pct(a.CrShare) + " %", "de la consommation sert à relire l'historique (normal)", ctxBody,
                "Quand tu changes de sujet, tape <code>/clear</code> : Claude oublie la conversation (il faudra tout réexpliquer) mais ne la relit plus à chaque message. Pour continuer la même tâche, <code>/compact</code> la résume : on perd du détail, alors fais-le entre deux étapes plutôt qu'en plein milieu. Avant de nettoyer, demande à Claude : « note en dix lignes maximum ce qu'il faut retenir de ce projet dans CLAUDE.md ».");
            string wasteCard = Card("Actions inutiles", waste.Count.ToString(), "signaux repérés",
                waste.Count > 0 ? Det("Voir le détail", $"<ul>{string.Concat(waste)}</ul>") : "<p class='ok'>Rien de flagrant.</p>",
                waste.Count > 0 ? "Demande à Claude de chercher dans un fichier précis plutôt que de tout relire." : "");

            return $"<div class='view' data-days='{days}'><p class='sub'>{n} demandes analysées sur {periode}. Tout est resté sur ton ordinateur.</p>\n"
                + "<section><h2>Où part ta consommation</h2>" + models
                + "<details><summary>Vérifier ces chiffres dans Claude Code</summary><p>Tape <code>/usage</code> (tokens par modèle) et <code>/context</code> (ce qui remplit ta conversation).</p></details></section>\n"
                + modelCard + "\n" + ctxCard + "\n" + wasteCard + "\n" + projCard + "\n</div>";
        }

        const string Css = @":root{color-scheme:dark;--marine:#0B2E4A;--deep:#082238;--sable:#FFE3A6;--ciel:#5E9BA6;--rouge:#E0262B;--muted:#A9C1D1;
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
.tabs button.on{background:var(--sable);color:var(--marine);border-color:var(--sable)}";

        const string TabsJs = @"document.querySelectorAll("".tabs button"").forEach(function(b){b.onclick=function(){document.querySelectorAll("".tabs button"").forEach(function(x){x.classList.toggle(""on"",x===b)});document.querySelectorAll("".view"").forEach(function(v){v.hidden=v.dataset.days!==b.dataset.d})}})";

        class VueJours { public int Days, N; public Analysis A; }

        static string Page(List<VueJours> views)
        {
            var sb = new StringBuilder();
            sb.Append("<!doctype html><html lang='fr'><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'>\n");
            sb.Append("<title>Diagrun</title><style>").Append(Css).Append("</style>\n");
            sb.Append("<main><h1 class='logo'>diag<span class='pill'>run</span></h1>");
            if (views.Count > 1)
            {
                sb.Append("<div class='tabs'>");
                for (int i = 0; i < views.Count; i++)
                    sb.Append($"<button type='button' data-d='{views[i].Days}'{(i == 0 ? " class='on'" : "")}>{views[i].Days} jours</button>");
                sb.Append("</div>");
            }
            sb.Append('\n');
            for (int i = 0; i < views.Count; i++)
            {
                var v = View(views[i].A, views[i].N, views[i].Days);
                if (i > 0) v = v.Replace("class='view'", "class='view' hidden");
                sb.Append(v).Append('\n');
            }
            sb.Append($"<footer>diag run v{Version} de François Nollet (github.com/Francoisnollet/diagrun), intégré à la Jauge Claude avec son accord · Les montants sont des estimations basées sur les tarifs publics. L'analyse de la tâche est approximative : c'est une indication, pas un verdict. Ce rapport peut montrer le début de tes demandes et des chemins de fichiers : relis-le avant d'en partager une capture.</footer></main>");
            if (views.Count > 1) sb.Append("<script>").Append(TabsJs).Append("</script>");
            sb.Append("</html>");
            return sb.ToString();
        }

        // ---------- Point d'entrée (appelé depuis le menu de la Jauge) ----------
        static int enCours;

        public static void Lancer()
        {
            if (System.Threading.Interlocked.Exchange(ref enCours, 1) == 1) return;   // déjà en cours
            Task.Run(() =>
            {
                try
                {
                    var clears = new Dictionary<string, int>();
                    var turns = BuildTurns(Load(), clears);
                    if (turns.Count == 0)
                    {
                        Info("Aucune session Claude Code trouvée sur les " + MaxDays + " derniers jours.\r\nCe diagnostic analyse les sessions de Claude Code (dossier .claude\\projects), pas celles du site claude.ai.");
                        return;
                    }
                    double now = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
                    var views = new List<VueJours>();
                    foreach (var d in new[] { 7, 30 })
                    {
                        var sub = turns.Where(t => t.Ts >= now - d * 864e5).ToList();
                        if (sub.Count == 0) continue;
                        if (views.Count > 0 && views[views.Count - 1].N == sub.Count) continue;   // pas d'onglet en double
                        views.Add(new VueJours { Days = d, N = sub.Count, A = Analyse(sub, clears) });
                    }
                    var dossier = Path.Combine(Program.Dossier, "diagnostic");
                    Directory.CreateDirectory(dossier);
                    var fichier = Path.Combine(dossier, "rapport.html");
                    File.WriteAllText(fichier, Page(views), new UTF8Encoding(false));
                    System.Diagnostics.Process.Start(fichier);   // ouvre dans le navigateur par défaut
                }
                catch (Exception ex)
                {
                    Info("Le diagnostic n'a pas pu être généré.\r\n" + ex.Message);
                }
                finally { enCours = 0; }
            });
        }

        static void Info(string texte) =>
            MessageBox.Show(texte, "Diagrun", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}
