"""
Jauge Claude - application Windows autonome (sans Tampermonkey).

- Une fenêtre claude.ai intégrée (moteur Edge WebView2) garde votre connexion :
  on se connecte une seule fois, ensuite elle reste cachée.
- Toutes les 5 minutes, l'application lit votre utilisation (session 5 h et semaine).
- Pastille dans la zone de notification : pourcentage le plus élevé et sa couleur
  (vert < 70 %, orange 70-89 %, rouge >= 90 %, gris si pas de chiffres depuis 20 min).
- Notification Windows à 70 % et 90 % (une seule fois par seuil et par période).
- Clic droit sur la pastille : actualiser, afficher la fenêtre claude.ai, quitter.

Aucun mot de passe n'est stocké par ce programme : la session est celle de la fenêtre
intégrée, conservée dans %APPDATA%\\JaugeClaude.
Dépendances : pip install pystray pillow pywebview
"""
import json
import os
import threading
import time
import webbrowser
from datetime import datetime, timezone
from pathlib import Path

import pystray
from PIL import Image, ImageDraw, ImageFont

SEUILS = (70, 90)
INTERVALLE = 5 * 60               # secondes entre deux relevés
OBSOLETE_APRES = 20 * 60          # secondes sans chiffres avant de griser l'icône
URL_USAGE = "https://claude.ai/settings/usage"
DOSSIER = Path(os.environ.get("APPDATA") or Path.home()) / "JaugeClaude"
FICHIER_ALERTES = DOSSIER / "alertes.json"

VERT, ORANGE, ROUGE, GRIS = (46, 158, 91), (224, 161, 0), (214, 69, 69), (130, 130, 130)

etat = {"session": None, "semaine": None, "recu": 0.0, "erreur": None}
verrou = threading.Lock()


# ---------------------------------------------------------------- dessin de l'icône
def _police(taille):
    for nom in ("segoeuib.ttf", "arialbd.ttf", "DejaVuSans-Bold.ttf"):
        try:
            return ImageFont.truetype(nom, taille)
        except OSError:
            continue
    return ImageFont.load_default()


def couleur(pct):
    if pct is None:
        return GRIS
    return ROUGE if pct >= 90 else ORANGE if pct >= 70 else VERT


def dessiner(pct, obsolete):
    """Pastille ronde colorée avec le pourcentage au centre (64x64, réduit par Windows)."""
    img = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    fond = GRIS if obsolete else couleur(pct)
    d.ellipse((0, 0, 63, 63), fill=fond)
    texte = "?" if pct is None else ("!" if pct >= 100 else str(pct))
    police = _police(38 if len(texte) <= 1 else 32)
    l, t, r, b = d.textbbox((0, 0), texte, font=police)
    d.text(((64 - (r - l)) / 2 - l, (64 - (b - t)) / 2 - t), texte, font=police, fill="white")
    return img


# ---------------------------------------------------------------- textes
def quand(iso):
    if not iso:
        return "?"
    try:
        dt = datetime.fromisoformat(iso.replace("Z", "+00:00"))
    except ValueError:
        return "?"
    minutes = int((dt - datetime.now(timezone.utc)).total_seconds() // 60)
    if minutes < 60:
        return f"dans {max(minutes, 0)} min"
    if minutes < 24 * 60:
        return f"dans {minutes // 60} h {minutes % 60:02d}"
    jours = ["lun.", "mar.", "mer.", "jeu.", "ven.", "sam.", "dim."]
    local = dt.astimezone()
    return f"{jours[local.weekday()]} {local:%H:%M}"


def ligne(nom, v):
    return f"{nom} : {v['pct']} % (reset {quand(v.get('reset'))})" if v else f"{nom} : —"


def infobulle():
    with verrou:
        s, w, recu, err = etat["session"], etat["semaine"], etat["recu"], etat["erreur"]
    if err in ("HTTP 401", "HTTP 403"):
        return "Jauge Claude : connectez-vous (clic droit > Afficher claude.ai)"
    if not recu:
        return "Jauge Claude : " + (f"erreur ({err})" if err else "premier relevé en cours…")
    txt = f"{ligne('Session', s)}\n{ligne('Semaine', w)}"
    if time.time() - recu > OBSOLETE_APRES:
        txt += f"\nPas de nouvelles depuis {int((time.time() - recu) // 60)} min"
    return txt[:127]  # limite Windows


# ---------------------------------------------------------------- données
def lire(v):
    if not isinstance(v, dict) or not isinstance(v.get("pct"), (int, float)):
        return None
    return {"pct": max(0, min(int(round(v["pct"])), 999)), "reset": str(v.get("reset") or "")[:40]}


def convertir(usage):
    """Réponse brute de claude.ai (five_hour / seven_day) -> {session, semaine}."""
    def un(x):
        return lire({"pct": x.get("utilization"), "reset": x.get("resets_at")}) if isinstance(x, dict) else None
    return {"session": un(usage.get("five_hour")), "semaine": un(usage.get("seven_day"))}


def alertes_a_envoyer(donnees, deja, maintenant=None):
    """Renvoie (liste de (titre, texte), dictionnaire « déjà alerté » mis à jour)."""
    maintenant = maintenant or time.time()
    deja = {k: t for k, t in deja.items() if maintenant - t < 8 * 86400}  # ménage
    a_envoyer = []
    for nom, v in (("Session", donnees.get("session")), ("Semaine", donnees.get("semaine"))):
        if not v:
            continue
        try:  # l'heure de remise à zéro varie de quelques µs : arrondie à 10 min
            periode = round(datetime.fromisoformat(v["reset"].replace("Z", "+00:00")).timestamp() / 600)
        except (ValueError, AttributeError):
            periode = "?"
        for s in SEUILS:
            cle = f"{nom}|{periode}|{s}"
            if v["pct"] >= s and cle not in deja:
                deja[cle] = maintenant
                a_envoyer.append((f"Claude : {nom.lower()} à {v['pct']} %",
                                  f"Seuil de {s} % dépassé. Remise à zéro {quand(v['reset'])}."))
    return a_envoyer, deja


def charger_alertes():
    try:
        return json.loads(FICHIER_ALERTES.read_text(encoding="utf-8"))
    except (OSError, ValueError):
        return {}


def sauver_alertes(deja):
    try:
        DOSSIER.mkdir(parents=True, exist_ok=True)
        FICHIER_ALERTES.write_text(json.dumps(deja), encoding="utf-8")
    except OSError:
        pass


# Exécuté dans la fenêtre claude.ai : même session, mêmes cookies que le site.
JS_RELEVE = r"""
(async () => {
  for (let i = 0; i < 50 && !(window.pywebview && window.pywebview.api && window.pywebview.api.recevoir); i++)
    await new Promise(r => setTimeout(r, 200));   // le pont Python arrive juste après le chargement
  const api = window.pywebview && window.pywebview.api;
  if (!api) return;
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
    api.recevoir(JSON.stringify(u));
  } catch (e) { api.erreur(String(e.message || e)); }
})();
"""


# ---------------------------------------------------------------- application
class Pont:
    """Méthodes appelées depuis la fenêtre claude.ai (window.pywebview.api)."""

    def recevoir(self, brut):
        try:
            d = convertir(json.loads(brut))
        except (ValueError, TypeError, AttributeError):
            return self.erreur("réponse illisible")
        with verrou:
            etat.update(session=d["session"], semaine=d["semaine"], recu=time.time(), erreur=None)
        envois, deja = alertes_a_envoyer(d, charger_alertes())
        sauver_alertes(deja)
        for titre, texte in envois:
            try:
                ICONE.notify(texte, titre)
            except Exception:
                pass
        rafraichir()

    def erreur(self, msg):
        with verrou:
            etat["erreur"] = msg
        rafraichir()
        if msg in ("HTTP 401", "HTTP 403") and FENETRE:
            FENETRE.show()  # il faut se (re)connecter


FENETRE = None
reveil = threading.Event()


def rafraichir():
    with verrou:
        vals = [v["pct"] for v in (etat["session"], etat["semaine"]) if v]
        recu = etat["recu"]
    pct = max(vals) if vals else None
    obsolete = not recu or time.time() - recu > OBSOLETE_APRES
    ICONE.icon = dessiner(pct, obsolete)
    ICONE.title = infobulle()
    ICONE.update_menu()


def releve():
    try:
        if FENETRE and (FENETRE.get_current_url() or "").startswith("https://claude.ai"):
            FENETRE.evaluate_js(JS_RELEVE)
        elif FENETRE:
            FENETRE.load_url(URL_USAGE)  # le relevé repartira à la fin du chargement
    except Exception as e:
        Pont().erreur(str(e))


def boucle():
    dernier = 0.0
    while True:
        if reveil.wait(30) or time.time() - dernier >= INTERVALLE:
            reveil.clear()
            dernier = time.time()
            releve()
        rafraichir()  # met à jour les « dans x min » et l'état obsolète


def quitter(icone, _):
    icone.stop()
    if FENETRE:
        FENETRE.destroy()
    os._exit(0)


ICONE = pystray.Icon(
    "jauge_claude",
    dessiner(None, True),
    "Jauge Claude : démarrage…",
    menu=pystray.Menu(
        pystray.MenuItem(lambda _: ligne("Session", etat["session"]), None, enabled=False),
        pystray.MenuItem(lambda _: ligne("Semaine", etat["semaine"]), None, enabled=False),
        pystray.Menu.SEPARATOR,
        pystray.MenuItem("Ouvrir la page Utilisation", lambda *_: webbrowser.open(URL_USAGE), default=True),
        pystray.MenuItem("Actualiser maintenant", lambda *_: reveil.set()),
        pystray.MenuItem("Afficher la fenêtre claude.ai (connexion)", lambda *_: FENETRE and FENETRE.show()),
        pystray.Menu.SEPARATOR,
        pystray.MenuItem("Quitter", quitter),
    ),
)


def deja_lancee():
    """Un seul exemplaire à la fois (verrou nommé Windows)."""
    try:
        import ctypes
        ctypes.windll.kernel32.CreateMutexW(None, False, "JaugeClaudeApp")
        if ctypes.windll.kernel32.GetLastError() == 183:  # ERROR_ALREADY_EXISTS
            ctypes.windll.user32.MessageBoxW(
                None, "La Jauge Claude tourne déjà (regardez derrière la flèche ^ près de l'horloge).",
                "Jauge Claude", 0x40)
            return True
    except Exception:
        pass
    return False


def main():
    global FENETRE
    import webview

    if deja_lancee():
        return
    DOSSIER.mkdir(parents=True, exist_ok=True)
    FENETRE = webview.create_window("Jauge Claude – connexion à claude.ai", URL_USAGE,
                                    js_api=Pont(), width=1000, height=760, hidden=True)

    def fermeture():  # la croix cache la fenêtre au lieu de quitter
        FENETRE.hide()
        return False

    FENETRE.events.closing += fermeture
    FENETRE.events.loaded += lambda *_: reveil.set()

    ICONE.run_detached()
    threading.Thread(target=boucle, daemon=True).start()
    # private_mode=False + storage_path : cookies conservés entre deux démarrages.
    webview.start(private_mode=False, storage_path=str(DOSSIER / "navigateur"))


if __name__ == "__main__":
    try:
        main()
    except Exception:  # pythonw n'a pas de console : on garde la trace et on prévient
        import traceback
        DOSSIER.mkdir(parents=True, exist_ok=True)
        (DOSSIER / "erreur.txt").write_text(traceback.format_exc(), encoding="utf-8")
        try:
            import ctypes
            ctypes.windll.user32.MessageBoxW(
                None, f"La Jauge Claude n'a pas pu démarrer.\nDétail : {DOSSIER / 'erreur.txt'}", "Jauge Claude", 0x10)
        except Exception:
            pass
        raise
