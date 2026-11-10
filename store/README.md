# Publier la Jauge Claude sur le Microsoft Store

Le Store signe lui-même l'application : plus de blocage par SmartScreen ni par le Contrôle intelligent des applications, et l'application apparaît dans *Paramètres > Applications* avec un bouton Désinstaller.

GitHub Actions construit déjà le paquet `JaugeClaude-Store.msix` à chaque modification (onglet **Actions** > dernière construction > **Artifacts**). Il reste à faire, une seule fois :

## 1. Créer le compte développeur

1. Allez sur https://storedeveloper.microsoft.com et choisissez un compte **Particulier** (Individual).
2. Connectez-vous avec un compte Microsoft, puis suivez la vérification d'identité (pièce d'identité + selfie). Comptez de quelques heures à quelques jours.

## 2. Réserver le nom

Dans l'Espace partenaires (https://partner.microsoft.com/dashboard) : **Applications et jeux** > **Nouveau produit** > **Application MSIX ou PWA** > nom souhaité.

Microsoft peut refuser un nom contenant une marque (« Claude » appartient à Anthropic). Si « Jauge Claude » est refusé, essayez par exemple « Jauge d'utilisation pour Claude ».

## 3. Recopier l'identité du paquet dans GitHub

Dans l'Espace partenaires : votre application > **Identité du produit**. Trois valeurs y figurent :

| Espace partenaires | Variable GitHub à créer |
| --- | --- |
| Package/Identity/Name | `MSIX_NAME` |
| Package/Identity/Publisher (commence par `CN=`) | `MSIX_PUBLISHER` |
| Package/Properties/PublisherDisplayName | `MSIX_PUBLISHER_DISPLAY` |

Sur GitHub : **Settings** > **Secrets and variables** > **Actions** > onglet **Variables** > **New repository variable**, une fois pour chacune.

Relancez ensuite la construction (**Actions** > *Construire JaugeClaude.exe* > **Run workflow**) et téléchargez `JaugeClaude-Store.msix` dans les **Artifacts**.

## 4. Soumettre

Dans l'Espace partenaires, **Démarrer la soumission** :

- **Tarification et disponibilité** : Gratuit.
- **Propriétés** : catégorie *Productivité*. Politique de confidentialité : `https://github.com/gautierm78/jauge-claude/blob/main/CONFIDENTIALITE.md`.
- **Classification par âge** : questionnaire (aucun contenu sensible).
- **Packages** : glissez `JaugeClaude-Store.msix`.
- **Fiche du Store** (Français) : textes ci-dessous, plus au moins une capture d'écran (1366×768 ou plus) montrant la pastille et son infobulle.
- **Notes pour la certification** : texte ci-dessous.

Envoyez. La certification prend en général de quelques heures à 3 jours.

### Description

> Jauge Claude affiche en permanence votre utilisation de Claude (claude.ai) dans la zone de notification, près de l'horloge.
>
> • Pastille colorée avec le pourcentage le plus élevé : vert sous 70 %, orange de 70 à 89 %, rouge à partir de 90 %.
> • Survol : détail de la session (5 heures) et de la semaine, avec les heures de remise à zéro.
> • Notification Windows à 70 % et à 90 %.
> • Se lance avec Windows, se met à jour toute seule toutes les 5 minutes.
>
> Connectez-vous une fois à claude.ai dans la fenêtre de l'application ; ensuite, tout est automatique.
>
> Application indépendante et gratuite, non affiliée à Anthropic. Aucune donnée collectée. Code source public : https://github.com/gautierm78/jauge-claude

### Notes pour la certification

> The app shows the user's own Claude (claude.ai) usage limits in the notification area. On first launch, a WebView2 window opens claude.ai so the user can sign in with their own account; the window can then be closed (it hides; the app keeps running in the tray). A Claude account (free or paid) is required to see figures; without signing in, the tray icon stays grey with "?". The app only contacts claude.ai. runFullTrust is required because it is a WinForms desktop app (NotifyIcon). Source code: https://github.com/gautierm78/jauge-claude

## Mises à jour

Augmentez le numéro de version (en créant une nouvelle version GitHub `vX.Y.Z`, voir le README), téléchargez le nouveau `JaugeClaude-Store.msix` et soumettez-le dans une nouvelle soumission. Les utilisateurs du Store reçoivent la mise à jour automatiquement.
