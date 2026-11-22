# Politique de confidentialité – Jauge Claude

*Dernière mise à jour : novembre 2026*

Jauge Claude est une application indépendante, non affiliée à Anthropic.

**Données collectées : aucune.** L'application n'envoie aucune donnée à son auteur ni à un tiers. Elle ne contient ni statistiques d'usage, ni publicité, ni suivi.

**Ce que l'application fait :**

- Elle affiche le site claude.ai dans une fenêtre intégrée (Microsoft Edge WebView2), où vous vous connectez vous-même. Votre identifiant et votre mot de passe sont saisis directement sur claude.ai ; l'application ne les lit pas et ne les enregistre pas.
- Elle interroge uniquement claude.ai pour lire vos pourcentages d'utilisation (session et semaine), comme le fait la page *Paramètres > Utilisation* du site.
- Elle conserve sur votre ordinateur, dans son dossier de données : la session de la fenêtre intégrée (cookies de claude.ai) et la liste des alertes déjà affichées. Rien de cela ne quitte votre ordinateur.
- Entrée optionnelle « Diagrun : mes tokens sous la loupe » : à votre demande uniquement, elle lit en local les sessions Claude Code présentes sur votre ordinateur (`%USERPROFILE%\.claude\projects`) et écrit un rapport HTML dans `%APPDATA%\JaugeClaude\diagnostic`. Aucune donnée n'est envoyée, aucun accès réseau. Outil adapté de [diag run](https://github.com/Francoisnollet/diagrun) (François Nollet, licence MIT).

**Supprimer vos données :** désinstallez l'application (Paramètres > Applications) ; pour la version téléchargée depuis GitHub, supprimez aussi le dossier `%APPDATA%\JaugeClaude`.

**Code source :** entièrement public sur https://github.com/gautierm78/jauge-claude.

**Contact :** ouvrez un ticket sur https://github.com/gautierm78/jauge-claude/issues.
