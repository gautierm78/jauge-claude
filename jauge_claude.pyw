"""
Jauge Claude - icône dans la zone de notification Windows.

Reçoit les chiffres envoyés par le script Tampermonkey « Claude – jauge d'utilisation »
(v2.0 ou plus) et les affiche dans une icône près de l'horloge.

- L'icône montre le pourcentage le plus élevé (session ou semaine) et sa couleur :
  vert < 70 %, orange 70-89 %, rouge >= 90 %, gris si plus de nouvelles depuis 20 min.
- Survol : détail session / semaine et heures de remise à zéro.
- Clic droit : menu (ouvrir la page Utilisation, quitter).

Écoute uniquement sur 127.0.0.1 (rien n'est accessible depuis le réseau).
Dépendances : pip install pystray pillow
"""
import json
import threading
import time
import webbrowser
from datetime import datetime, timezone
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer

import pystray
from PIL import Image, ImageDraw, ImageFont

PORT = 47321
OBSOLETE_APRES = 20 * 60          # secondes sans nouvelles avant de griser l'icône
URL_USAGE = "https://claude.ai/settings/usage"

VERT, ORANGE, ROUGE, GRIS = (46, 158, 91), (224, 161, 0), (214, 69, 69), (130, 130, 130)

etat = {"session": None, "semaine": None, "recu": 0.0}
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
        s, w, recu = etat["session"], etat["semaine"], etat["recu"]
    if not recu:
        return "Jauge Claude : en attente du navigateur"
    txt = f"{ligne('Session', s)}\n{ligne('Semaine', w)}"
    if time.time() - recu > OBSOLETE_APRES:
        txt += f"\nPas de nouvelles depuis {int((time.time() - recu) // 60)} min"
    return txt[:127]  # limite Windows


# ---------------------------------------------------------------- icône
def rafraichir(icone):
    with verrou:
        vals = [v["pct"] for v in (etat["session"], etat["semaine"]) if v]
        recu = etat["recu"]
    pct = max(vals) if vals else None
    obsolete = not recu or time.time() - recu > OBSOLETE_APRES
    icone.icon = dessiner(pct, obsolete)
    icone.title = infobulle()


def boucle(icone):
    icone.visible = True
    while True:  # met à jour les « dans x min » et l'état obsolète
        rafraichir(icone)
        time.sleep(30)


# ---------------------------------------------------------------- serveur local
def lire(v):
    if not isinstance(v, dict) or not isinstance(v.get("pct"), (int, float)):
        return None
    return {"pct": max(0, min(int(round(v["pct"])), 999)), "reset": str(v.get("reset") or "")[:40]}


class Recepteur(BaseHTTPRequestHandler):
    def do_POST(self):
        # En-tête personnalisé obligatoire : une page web ordinaire ne peut pas l'envoyer
        # vers 127.0.0.1 sans autorisation CORS, que ce serveur ne donne jamais.
        if self.path != "/usage" or self.headers.get("X-Jauge-Claude") != "1":
            self.send_error(403)
            return
        try:
            n = min(int(self.headers.get("Content-Length", 0)), 4096)
            data = json.loads(self.rfile.read(n))
        except (ValueError, json.JSONDecodeError):
            self.send_error(400)
            return
        with verrou:
            etat["session"], etat["semaine"], etat["recu"] = lire(data.get("session")), lire(data.get("semaine")), time.time()
        rafraichir(ICONE)
        self.send_response(204)
        self.end_headers()

    def log_message(self, *args):  # silencieux
        pass


def ouvrir_serveur():
    """Réserve le port. Échec = une autre jauge tourne déjà : on s'arrête pour éviter une icône muette."""
    try:
        return ThreadingHTTPServer(("127.0.0.1", PORT), Recepteur)
    except OSError:
        try:
            import ctypes
            ctypes.windll.user32.MessageBoxW(
                None,
                "La Jauge Claude tourne déjà (regardez derrière la flèche ^ près de l'horloge).\n"
                "Pour la relancer : clic droit sur la pastille > Quitter, puis relancez.",
                "Jauge Claude", 0x40)
        except Exception:
            pass
        raise SystemExit(0)


# ---------------------------------------------------------------- démarrage
def quitter(icone, _):
    icone.stop()


ICONE = pystray.Icon(
    "jauge_claude",
    dessiner(None, True),
    "Jauge Claude : en attente du navigateur",
    menu=pystray.Menu(
        pystray.MenuItem(lambda _: ligne("Session", etat["session"]), None, enabled=False),
        pystray.MenuItem(lambda _: ligne("Semaine", etat["semaine"]), None, enabled=False),
        pystray.Menu.SEPARATOR,
        pystray.MenuItem("Ouvrir la page Utilisation", lambda *_: webbrowser.open(URL_USAGE), default=True),
        pystray.MenuItem("Quitter", quitter),
    ),
)

if __name__ == "__main__":
    srv = ouvrir_serveur()
    threading.Thread(target=srv.serve_forever, daemon=True).start()
    ICONE.run(setup=boucle)
