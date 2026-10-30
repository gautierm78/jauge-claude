# Jauge d'utilisation Claude

Affiche en permanence votre consommation Claude (session 5 h et semaine), avec alertes Windows à 70 % et 90 %.

Deux parties, indépendantes :

- **`claude-usage-badge.user.js`** : script Tampermonkey (Chrome ou Edge). Affiche la jauge en bas à droite de claude.ai et envoie les chiffres à l'icône Windows.
- **`jauge_claude.pyw`** + **`installer.bat`** (optionnel) : pastille colorée dans la zone de notification Windows, près de l'horloge.

## Installation

Le guide pas à pas est dans [`guide-installation.md`](guide-installation.md).

1. Installer Tampermonkey, autoriser les scripts utilisateur, coller `claude-usage-badge.user.js`.
2. Pour l'icône Windows : installer Python 3.12 (Microsoft Store), puis lancer `installer.bat`.

## Limites

- L'adresse interrogée est celle que claude.ai utilise pour sa propre page d'utilisation. Elle n'est pas documentée officiellement : si elle change, il faudra adapter le script.
- Le script ne contient aucun mot de passe ni clé. Il utilise votre session claude.ai déjà ouverte.
- Le programme Windows n'écoute que sur `127.0.0.1`.

## Licence

MIT, voir [`LICENSE`](LICENSE).
