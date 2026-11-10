# Jauge Claude – mode d'emploi (v3)

## À quoi ça sert

Claude impose deux limites d'utilisation : une **session** de 5 heures et un plafond sur **7 jours**. La Jauge Claude affiche en permanence où vous en êtes, sous forme d'une étoile colorée près de l'horloge, et vous prévient à 50 % et 85 %.

## Installer

1. Téléchargez **`JaugeClaude.exe`** sur la page [Releases](https://github.com/gautierm78/jauge-claude/releases/latest).
2. Rangez-le dans un dossier où il restera (par exemple `Documents`). Le démarrage automatique pointe vers cet emplacement ; si vous le déplacez, relancez-le une fois depuis son nouvel emplacement.
3. Double-cliquez dessus.
   - Si Windows affiche **« Windows a protégé votre ordinateur »** : cliquez sur *Informations complémentaires*, puis *Exécuter quand même* (une seule fois).
   - Si Windows affiche **« Le Contrôle intelligent des applications a bloqué… »** : voir la section *Blocage* plus bas.
4. Une fenêtre **« Jauge Claude – connexion à claude.ai »** s'ouvre : connectez-vous comme d'habitude, puis fermez-la avec la croix. Elle se cache, l'application continue.
5. La pastille apparaît près de l'horloge (cliquez sur la flèche **^** si elle est masquée ; vous pouvez la glisser à côté de l'horloge pour la garder visible).

L'application se lance ensuite toute seule à chaque démarrage de Windows.

## Au quotidien

| Couleur | Niveau | Ce que ça veut dire |
| --- | --- | --- |
| Vert | moins de 50 % | Tout va bien. |
| Orange | 50 à 84 % | Gardez les grosses tâches pour après la remise à zéro. |
| Rouge | 85 % et plus | Blocage proche. |
| Gris | — | Pas de chiffres depuis 20 min (pas de réseau, ou déconnecté). |

- **Survol** : détail session / semaine et heures de remise à zéro.
- **Double-clic** : ouvre la page Utilisation de claude.ai dans votre navigateur.
- **Clic droit** : *Actualiser maintenant*, *Afficher la fenêtre claude.ai* (pour se reconnecter), *Lancer au démarrage de Windows* (case à cocher), *Quitter*.

Une notification arrive une seule fois à 50 % et une seule fois à 85 %, pour la session comme pour la semaine.

## Mettre à jour

1. Clic droit sur la pastille > **Quitter**.
2. Remplacez `JaugeClaude.exe` par la nouvelle version, au même endroit, et lancez-la. Vous restez connecté.

## Désinstaller

L'application n'est pas « installée » : elle n'apparaît donc pas dans *Paramètres > Applications*.

1. Clic droit sur la pastille : décochez **Lancer au démarrage de Windows**, puis **Quitter**.
2. Supprimez `JaugeClaude.exe`.
3. Supprimez le dossier `%APPDATA%\JaugeClaude` (collez ce chemin dans la barre d'adresse de l'Explorateur). Cela efface aussi la connexion à claude.ai.

## En cas de souci

| Problème | Solution |
| --- | --- |
| Pastille grise, « connectez-vous » | La session claude.ai a expiré : la fenêtre de connexion se réaffiche, reconnectez-vous. |
| « La Jauge Claude tourne déjà » | Elle est déjà lancée : regardez derrière la flèche ^. |
| « Microsoft Edge WebView2 est introuvable » | Acceptez d'ouvrir la page Microsoft et installez « WebView2 Runtime ». |
| Autre erreur au démarrage | Le détail est dans `%APPDATA%\JaugeClaude\erreur.txt`. |

## Blocage par le Contrôle intelligent des applications (Windows 11)

Ce filtre de Windows 11 refuse tout programme qui n'est pas signé numériquement. Cocher *Débloquer* dans les propriétés du fichier ne suffit pas, et il n'y a pas de bouton *Exécuter quand même*.

Solutions :

- **Attendre une version signée** (Microsoft Store ou certificat) : c'est la seule solution qui garde la protection active.
- **Désactiver le filtre** : *Sécurité Windows > Contrôle des applications et du navigateur > Paramètres du Contrôle intelligent des applications > Désactivé*. Attention : il ne peut plus être réactivé sans réinstaller Windows. Déconseillé sur un PC professionnel.

## Sécurité

Le code source complet est public ([`csharp/Program.cs`](csharp/Program.cs)) et le `.exe` est construit en public par GitHub. Le [README](README.md#ce-que-fait-le-programme-et-ce-quil-ne-fait-pas) détaille ce que fait le programme et comment vérifier le fichier téléchargé.
