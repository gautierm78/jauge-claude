# Jauge d'utilisation Claude – guide d'installation (v3.0)

## À quoi ça sert

Cette petite application affiche en permanence combien de votre quota Claude vous avez consommé, pour ne plus être bloqué par surprise. Elle prévient par une notification Windows quand vous approchez de la limite.

Claude impose deux limites :

- **Session** : une fenêtre de 5 heures.
- **Semaine** : un plafond sur 7 jours.

La jauge prend la forme d'une pastille colorée dans la zone de notification Windows, près de l'horloge. Elle fonctionne **sans Tampermonkey et sans navigateur ouvert** : l'application a sa propre fenêtre claude.ai, cachée.

## Avant de commencer

- Windows 10 ou 11 (le moteur Edge WebView2 est déjà installé ; sinon, Windows le propose).
- Un compte Claude (Pro, Max, Team ou Enterprise).
- Environ 5 minutes.

## Installation pas à pas

1. **Installer Python.** Ouvrez le Microsoft Store, cherchez « Python 3.12 », cliquez sur **Obtenir**.
2. **Télécharger la jauge.** Sur la [page GitHub](https://github.com/gautierm78/jauge-claude), bouton vert **Code** > **Download ZIP**. Décompressez le dossier à un endroit où il restera (par exemple `Documents\jauge-claude`) : le démarrage automatique pointe vers ce dossier.
3. **Lancer `installer.bat`** (double-clic). Il installe les composants, ajoute la jauge au démarrage de Windows et la lance.
4. **Se connecter une fois.** Une fenêtre « Jauge Claude – connexion à claude.ai » s'ouvre au premier lancement (si elle ne s'ouvre pas : clic droit sur la pastille > **Afficher la fenêtre claude.ai**). Connectez-vous comme d'habitude, puis fermez la fenêtre avec la croix : elle se cache, l'application continue.
5. **Vérifier.** Après quelques secondes, la pastille prend sa couleur et affiche votre pourcentage. Cliquez sur la flèche ^ près de l'horloge si elle est masquée ; vous pouvez la glisser à côté de l'horloge pour la garder visible.

## Utilisation au quotidien

Rien à faire : la jauge se met à jour toutes les 5 minutes.

| Couleur | Niveau | Ce que ça veut dire |
| --- | --- | --- |
| Vert | moins de 70 % | Tout va bien. |
| Orange | de 70 à 89 % | Gardez les grosses tâches pour après la remise à zéro. |
| Rouge | 90 % et plus | Blocage proche : terminez l'essentiel. |
| Gris | — | Pas de chiffres depuis 20 min (pas de réseau, ou déconnecté). |

- **Survol** : détail session / semaine et heures de remise à zéro.
- **Double-clic** : ouvre la page Utilisation de claude.ai.
- **Clic droit** : actualiser maintenant, afficher la fenêtre claude.ai, quitter.

Une notification arrive une seule fois à 70 % et une seule fois à 90 %, pour la session comme pour la semaine.

## En cas de souci

| Problème | Solution |
| --- | --- |
| La pastille reste grise avec « connectez-vous » | Votre session claude.ai a expiré : la fenêtre de connexion se réaffiche, reconnectez-vous. |
| Message « La Jauge Claude tourne déjà » | Elle est déjà lancée : regardez derrière la flèche ^. |
| « Python n'est pas installé » | Installez Python 3.12 depuis le Microsoft Store, puis relancez `installer.bat`. |
| La fenêtre de connexion reste blanche | Installez « Microsoft Edge WebView2 Runtime » depuis le site de Microsoft. |
| Je veux me déconnecter | Quittez la jauge et supprimez le dossier `%APPDATA%\JaugeClaude`. |

## Désinstaller

1. Clic droit sur la pastille > **Quitter**.
2. Touche Windows + R, tapez `shell:startup`, supprimez le raccourci « Jauge Claude ».
3. Supprimez le dossier `%APPDATA%\JaugeClaude` et le dossier de la jauge.

## Sécurité

- Le programme ne stocke aucun mot de passe : il utilise la session de sa fenêtre claude.ai, conservée dans `%APPDATA%\JaugeClaude`, comme le ferait un navigateur.
- Il n'ouvre aucun port réseau et ne contacte que claude.ai.
- L'adresse lue est celle de la page Utilisation de claude.ai, non documentée officiellement : si elle change, la jauge devra être adaptée.

Note : le PDF `docs/mode-emploi-jauge-claude.pdf` décrit encore l'ancienne version Tampermonkey (v2.1).
