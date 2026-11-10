# Jauge d'utilisation Claude

Application Windows autonome qui affiche en permanence votre consommation Claude (session 5 h et semaine) dans la zone de notification, avec alertes Windows à 70 % et 90 %.

**Plus besoin de Tampermonkey ni de navigateur ouvert** (v3.0) : l'application embarque sa propre fenêtre claude.ai (moteur Edge WebView2, déjà présent sous Windows 10/11). Vous vous connectez une seule fois, la fenêtre se cache et la jauge se met à jour toute seule toutes les 5 minutes.

## Installation

1. Installer Python 3.12 (Microsoft Store).
2. Double-cliquer sur `installer.bat` : il installe `pystray`, `pillow` et `pywebview`, ajoute l'application au démarrage de Windows et la lance.
3. Au premier lancement, connectez-vous à claude.ai dans la fenêtre qui s'ouvre, puis fermez-la (la croix la cache, l'application continue).

Guide détaillé : [`guide-installation.md`](guide-installation.md).

## Utilisation

- Pastille : pourcentage le plus élevé (vert < 70 %, orange 70-89 %, rouge ≥ 90 %, gris si pas de chiffres depuis 20 min).
- Survol : détail session / semaine et heures de remise à zéro.
- Clic droit : ouvrir la page Utilisation, actualiser, afficher la fenêtre claude.ai (pour se reconnecter), quitter.
- Si la session expire, la fenêtre de connexion réapparaît d'elle-même.

## Limites

- L'adresse interrogée est celle que claude.ai utilise pour sa propre page d'utilisation. Elle n'est pas documentée officiellement : si elle change, il faudra adapter le programme.
- Aucun mot de passe n'est stocké par le programme : la session de la fenêtre intégrée est conservée dans `%APPDATA%\JaugeClaude`. Supprimer ce dossier = se déconnecter.

## Tests

```
python -m unittest discover tests
```

## Licence

MIT, voir [`LICENSE`](LICENSE).
