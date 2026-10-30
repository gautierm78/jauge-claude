"""Tests des fonctions pures de l'icône. Lancer : python -m unittest discover tests"""
import pathlib
import sys
import types
import unittest
from datetime import datetime, timedelta, timezone

# pystray et pillow ne sont pas nécessaires pour tester la logique : on les simule si absents.
for nom in ("pystray", "PIL"):
    try:
        __import__(nom)
    except ImportError:
        sys.modules[nom] = types.ModuleType(nom)
if "PIL.Image" not in sys.modules:
    try:
        import PIL.Image  # noqa: F401
    except ImportError:
        for sous in ("Image", "ImageDraw", "ImageFont"):
            sys.modules[f"PIL.{sous}"] = types.ModuleType(f"PIL.{sous}")
            setattr(sys.modules["PIL"], sous, sys.modules[f"PIL.{sous}"])

CHEMIN = pathlib.Path(__file__).resolve().parent.parent / "jauge_claude.pyw"
jauge = types.ModuleType("jauge_claude")
# On n'exécute que la partie définitions : la création de l'icône (pystray) est laissée de côté.
definitions = CHEMIN.read_text(encoding="utf-8").split("ICONE = pystray.Icon(")[0]
exec(compile(definitions, str(CHEMIN), "exec"), jauge.__dict__)


class Couleur(unittest.TestCase):
    def test_seuils(self):
        self.assertEqual(jauge.couleur(None), jauge.GRIS)
        self.assertEqual(jauge.couleur(69), jauge.VERT)
        self.assertEqual(jauge.couleur(70), jauge.ORANGE)
        self.assertEqual(jauge.couleur(89), jauge.ORANGE)
        self.assertEqual(jauge.couleur(90), jauge.ROUGE)


class Lire(unittest.TestCase):
    def test_valeur_valide(self):
        self.assertEqual(jauge.lire({"pct": 42.6, "reset": "x"}), {"pct": 43, "reset": "x"})

    def test_valeurs_invalides(self):
        for v in (None, "42", {}, {"pct": "a"}):
            self.assertIsNone(jauge.lire(v))

    def test_bornes(self):
        self.assertEqual(jauge.lire({"pct": -5})["pct"], 0)
        self.assertEqual(jauge.lire({"pct": 5000})["pct"], 999)


class Quand(unittest.TestCase):
    def test_vide_ou_invalide(self):
        self.assertEqual(jauge.quand(""), "?")
        self.assertEqual(jauge.quand("pas une date"), "?")

    def test_minutes(self):
        iso = (datetime.now(timezone.utc) + timedelta(minutes=30, seconds=30)).isoformat()
        self.assertEqual(jauge.quand(iso), "dans 30 min")

    def test_heures(self):
        iso = (datetime.now(timezone.utc) + timedelta(hours=3, minutes=5, seconds=30)).isoformat()
        self.assertEqual(jauge.quand(iso), "dans 3 h 05")


if __name__ == "__main__":
    unittest.main()
