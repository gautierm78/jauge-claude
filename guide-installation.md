# Jauge d'utilisation Claude – guide d'installation

## À quoi ça sert

Cette petite jauge affiche en permanence combien de votre quota Claude vous avez consommé, pour ne plus être bloqué par surprise. Elle vous prévient aussi par une notification Windows quand vous approchez de la limite.

Claude impose deux limites d'utilisation :

- **Session** : une fenêtre de 5 heures. Une fois pleine, il faut attendre sa remise à zéro.
- **Semaine** : un plafond sur 7 jours, qui repart à zéro à heure fixe.

La jauge prend la forme d'un petit cadre sombre en bas à droite de l'écran, avec deux barres : « Session » et « Semaine ». Elle s'installe en une dizaine de minutes, est gratuite et ne consomme rien de votre quota Claude.

En option, une pastille colorée dans la zone de notification Windows, près de l'horloge, peut remplacer la jauge du navigateur (voir la section **Option : l'icône Windows**). Tous les fichiers sont dans le [dépôt GitHub](https://github.com/gautierm78/jauge-claude).

## Avant de commencer

Il vous faut :

- un ordinateur avec **Google Chrome** ou **Microsoft Edge** ;
- un compte Claude (Pro, Max, Team ou Enterprise) et être **connecté à claude.ai** dans ce navigateur ;
- le droit d'installer une extension (sur un PC professionnel verrouillé, demandez à votre service informatique) ;
- environ 10 minutes.

Aucune compétence technique n'est nécessaire : il suffit de copier-coller un texte.

## Installation pas à pas

L'installation tient en cinq étapes. Faites-les dans l'ordre, sans en sauter.

1. **Installer l'extension Tampermonkey.**
   - Ouvrez le magasin d'extensions de votre navigateur : Chrome Web Store pour Chrome, « Modules complémentaires Edge » pour Edge.
   - Cherchez « Tampermonkey », choisissez celle de l'éditeur Jan Biniok, puis cliquez sur **Ajouter**.
   - Cliquez sur l'icône en forme de pièce de puzzle en haut à droite, puis sur l'épingle à côté de Tampermonkey pour garder son icône visible.
2. **Autoriser les scripts.** Sans cette étape, rien ne fonctionnera.
   - Chrome : tapez `chrome://extensions` dans la barre d'adresse, cliquez sur **Détails** sous Tampermonkey et activez **Autoriser les scripts utilisateur**.
   - Edge : tapez `edge://extensions`, ouvrez les **Détails** de Tampermonkey et activez **Autoriser les scripts utilisateur**. Si l'option n'existe pas, activez le **Mode développeur** en bas à gauche de la page.
3. **Coller le script.**
   - Cliquez sur l'icône Tampermonkey, puis sur **Créer un nouveau script**.
   - Un éditeur s'ouvre avec quelques lignes d'exemple. Sélectionnez tout (Ctrl+A) et supprimez.
   - Ouvrez [`claude-usage-badge.user.js`](https://raw.githubusercontent.com/gautierm78/jauge-claude/main/claude-usage-badge.user.js), copiez tout le texte et collez-le dans l'éditeur (Ctrl+V).
   - Enregistrez avec Ctrl+S. Le script « Claude – jauge d'utilisation » apparaît dans la liste, avec un interrupteur vert.
4. **Autoriser l'accès à claude.ai.**
   - Ouvrez [claude.ai](https://claude.ai) et rechargez la page (F5).
   - Tampermonkey peut ouvrir un nouvel onglet qui demande si le script peut accéder à claude.ai. Cliquez sur **Toujours autoriser le domaine**. Cet onglet s'ouvre parfois en arrière-plan : jetez un œil à vos onglets.
5. **Vérifier.** Rechargez claude.ai : la jauge apparaît en bas à droite avec vos deux pourcentages. Si Windows demande d'autoriser les notifications du navigateur, acceptez.

## Utilisation au quotidien

Vous n'avez rien à faire : la jauge se met à jour toute seule toutes les 5 minutes.

| Couleur | Niveau | Ce que ça veut dire |
| --- | --- | --- |
| Vert | moins de 70 % | Tout va bien. |
| Orange | de 70 à 89 % | Gardez les grosses tâches pour après la remise à zéro. |
| Rouge | 90 % et plus | Blocage proche : terminez l'essentiel. |

Une notification Windows arrive une seule fois à 70 % et une seule fois à 90 %, pour la session comme pour la semaine. Cliquer dessus ouvre la page d'utilisation de Claude.

**Les gestes utiles**

- **Survoler une barre** : affiche l'heure de remise à zéro.
- **Clic sur la jauge** : ouvre la page officielle Paramètres → Utilisation de Claude.
- **Clic droit sur la jauge** : actualise tout de suite.

La jauge n'apparaît que sur claude.ai. Le script travaille en revanche sur tous les sites : tant qu'un onglet du navigateur est ouvert, les alertes Windows et l'icône restent à jour.

## Option : l'icône Windows

Une pastille ronde près de l'horloge affiche le pourcentage le plus élevé (session ou semaine). Elle est verte sous 70 %, orange de 70 à 89 %, rouge à partir de 90 %, et grise avec un « ? » sans nouvelles depuis 20 minutes. Au survol, elle affiche le détail et les heures de remise à zéro.

Le script Tampermonkey continue de lire les chiffres et les transmet à un petit programme local. **Chrome ou Edge doit donc rester ouvert**, même réduit.

**Installation**

1. Vérifiez que le script Tampermonkey est en **version 2.1 ou plus** (colonne Version du tableau de bord). Sinon, mettez-le à jour (voir **Mettre à jour ou désinstaller**).
2. Installez **Python 3.12** depuis le Microsoft Store, s'il n'est pas déjà présent.
3. Téléchargez le dépôt (bouton **Code → Download ZIP**) et extrayez-le dans un dossier qui ne bougera plus, par exemple `C:\Outils\JaugeClaude`.
4. Double-cliquez sur `installer.bat`. Si Windows affiche « Windows a protégé votre ordinateur », cliquez sur **Informations complémentaires**, puis **Exécuter quand même**. Le programme s'installe, se lance et démarrera avec Windows.
5. Ouvrez claude.ai, puis lancez **Tester l'icône Windows** depuis le menu Tampermonkey. Si Tampermonkey demande l'accès à `127.0.0.1`, choisissez **Toujours autoriser le domaine**. La pastille prend sa couleur.
6. Si la pastille est cachée derrière la flèche ^, faites-la glisser dans la barre des tâches.

**Masquer la jauge du navigateur**

Le script continue de tourner : ne le désactivez pas, sinon la pastille repasse au gris. Pour ne plus voir le cadre dans le navigateur, utilisez une extension de blocage d'éléments ou gardez simplement la fenêtre claude.ai en arrière-plan.

**Dépannage de l'icône**

| Ce que vous voyez | Solution |
| --- | --- |
| Pastille grise « en attente du navigateur » | Lancez **Tester l'icône Windows** et lisez la notification. |
| Notification « Échec … » | Autorisez l'accès à `127.0.0.1` si Tampermonkey ou le navigateur le demande, puis refaites le test. |
| Pas de pastille du tout | Relancez `installer.bat`. Pour vérifier que le programme tourne, ouvrez `http://127.0.0.1:47321/usage` dans le navigateur : une page « 501 » signifie qu'il répond. |
| « Python n'est pas installé » | Installez Python 3.12 depuis le Microsoft Store, puis relancez `installer.bat`. |

**Désinstaller l'icône** : clic droit sur la pastille → **Quitter**. Tapez ensuite `shell:startup` dans la barre d'adresse de l'Explorateur et supprimez le raccourci « Jauge Claude ». Supprimez enfin le dossier d'installation.

**Sécurité** : le programme n'écoute que sur l'ordinateur lui-même (`127.0.0.1`), jamais sur le réseau. Il n'accepte que les messages du script : un site web ne peut pas lui envoyer de faux chiffres.

## Comment ça marche

Le script pose à claude.ai la même question que la page officielle Paramètres → Utilisation, puis affiche la réponse.

&#91;embedded content: fonctionnement de la jauge · 3 acteurs, 2 résultats\]

- **Il utilise votre connexion existante.** Il ne contient ni mot de passe ni clé : il profite de votre session claude.ai déjà ouverte dans le navigateur.
- **Il ne consomme pas votre quota.** Il lit seulement un compteur, sans faire appel à l'IA.
- **Il reste discret.** Le résultat est partagé entre tous vos onglets et n'est redemandé qu'au bout de 5 minutes, et seulement si un onglet est au premier plan.
- **Rien ne sort de votre ordinateur.** Le script ne communique qu'avec claude.ai et n'envoie aucune donnée ailleurs.
- **Si vous avez plusieurs comptes** (personnel, équipe), il choisit automatiquement celui qui a des limites.

**Sa limite** : l'adresse interrogée est celle qu'utilise le site claude.ai pour sa propre page. Elle n'est pas documentée officiellement. Si Anthropic la modifie, la jauge affichera une erreur jusqu'à ce que le script soit adapté.

## Dépannage

La plupart des problèmes viennent d'une autorisation oubliée à l'étape 2 ou 4.

| Ce que vous voyez | Solution |
| --- | --- |
| Aucune jauge sur claude.ai | Vérifiez l'étape 2 (Autoriser les scripts utilisateur), puis rechargez la page avec F5. |
| « ⚠ connecte-toi à claude.ai » | Vous êtes déconnecté : ouvrez claude.ai, connectez-vous, puis rechargez. |
| « ⚠ erreur : réseau » | L'accès à claude.ai n'a pas été autorisé : refaites l'étape 4 ou vérifiez votre connexion internet. |
| « ⚠ aucune limite trouvée » | Votre compte n'a pas de quota visible : c'est le cas d'un compte gratuit ou d'un compte uniquement API. |
| « ⚠ erreur : HTTP … », de façon durable | Claude a peut-être modifié son site, et le script doit être adapté. Prévenez la personne qui vous l'a transmis. |
| Pas de notification Windows | Paramètres Windows → Système → Notifications : autorisez Chrome ou Edge et désactivez le mode Ne pas déranger. |
| « Tampermonkey n'a pas accès à cette page » | C'est normal sur les pages internes du navigateur, comme les paramètres ou les extensions. |

En dernier recours, la page officielle [Paramètres → Utilisation](https://claude.ai/settings/usage) donne toujours les mêmes chiffres.

## Mettre à jour ou désinstaller

**Mettre à jour** quand une nouvelle version vous est transmise :

1. Cliquez sur l'icône Tampermonkey, puis sur **Tableau de bord**.
2. Cliquez sur le nom « Claude – jauge d'utilisation » pour l'ouvrir.
3. Sélectionnez tout (Ctrl+A), collez le nouveau contenu de `claude-usage-badge.user.js` et enregistrez (Ctrl+S).
4. Rechargez vos onglets ouverts.

**Mettre en pause** : dans le tableau de bord, désactivez l'interrupteur vert du script.

**Désinstaller** : dans le tableau de bord, cliquez sur l'icône de corbeille du script. Pour tout retirer, supprimez ensuite l'extension Tampermonkey depuis la page des extensions.

**Modifier les seuils d'alerte** : en haut du script, la ligne `const SEUILS = [70, 90];` fixe les pourcentages d'alerte. La ligne `INTERVALLE_MIN = 5` fixe le délai entre deux actualisations, en minutes. Changez uniquement les chiffres, puis enregistrez.
