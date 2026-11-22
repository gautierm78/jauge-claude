# Jauge d'utilisation Claude

Petite application Windows qui affiche en permanence votre consommation Claude (session 5 h et semaine) près de l'horloge, avec une notification à 50 % et 85 %.

- **Un seul fichier** `JaugeClaude.exe` (moins de 1 Mo), sans installation ni dépendance : Windows 10/11 contient déjà tout (.NET Framework 4.8 et Edge WebView2).
- **Code source complet et lisible** dans [`csharp/Program.cs`](csharp/Program.cs) (environ 450 lignes, commenté en français) et [`csharp/Diagnostic.cs`](csharp/Diagnostic.cs) (le diagnostic, voir plus bas).
- **Construit en public** par GitHub Actions : vous pouvez vérifier que le `.exe` téléchargé vient bien de ce code (voir plus bas).

## Télécharger

Page [**Releases**](https://github.com/gautierm78/jauge-claude/releases/latest) → `JaugeClaude.exe`.

Mode d'emploi complet : [`guide-installation.md`](guide-installation.md).

## Fonctionnement

1. Au premier lancement, une fenêtre claude.ai s'ouvre (moteur Edge WebView2, comme un navigateur). Connectez-vous une fois, puis fermez-la : elle se cache, l'application continue.
2. Toutes les 5 minutes, l'application lit votre utilisation sur claude.ai, comme le fait la page *Paramètres > Utilisation*.
3. L'étoile affiche le pourcentage le plus élevé : vert < 50 %, orange 50-84 %, rouge ≥ 85 %, gris si pas de chiffres depuis 20 min.
4. Clic droit : actualiser, **diagnostic de consommation**, afficher la fenêtre claude.ai, lancer au démarrage de Windows, quitter.

## Diagnostic : où partent mes tokens ?

Clic droit > **Diagnostic : où partent mes tokens ?** : la jauge lit les sessions Claude Code enregistrées sur votre ordinateur (`%USERPROFILE%\.claude\projects`), puis ouvre dans votre navigateur un rapport en langage simple : tâches faites avec un modèle plus gros que nécessaire, coût des conversations trop longues, fichiers relus ou commandes répétées, économie estimée si vous suivez les conseils.

- **Uniquement quand vous cliquez.** Rien n'est lu en arrière-plan.
- **100 % local** : rien n'est envoyé, le rapport est un fichier HTML temporaire (`%TEMP%\JaugeClaude-diagnostic.html`). Il peut montrer le début de vos demandes et des chemins de fichiers : relisez-le avant d'en partager une capture.
- Ne voit que les sessions que Claude Code a conservées (30 jours par défaut).
- Sessions Claude Code uniquement (pas claude.ai dans le navigateur).
- Portage en C# de [diag run](https://github.com/Francoisnollet/diagrun) de François Nollet (licence MIT), voir [`csharp/Diagnostic.cs`](csharp/Diagnostic.cs).

## Ce que fait le programme, et ce qu'il ne fait pas

Tout est vérifiable dans [`csharp/Program.cs`](csharp/Program.cs).

| Il fait | Il ne fait pas |
| --- | --- |
| Ouvre `https://claude.ai/settings/usage` dans sa fenêtre intégrée | Ne contacte aucun autre site (pas de statistiques, pas de serveur tiers) |
| Lit `/api/organizations` et `/api/organizations/<id>/usage` sur claude.ai | Ne lit ni vos conversations claude.ai ni vos fichiers, sauf le diagnostic ci-dessous (lecture locale, à votre demande) |
| Garde la session de sa fenêtre et ses réglages dans `%APPDATA%\JaugeClaude` | Ne stocke aucun mot de passe lui-même, n'ouvre aucun port réseau |
| Ajoute une ligne « JaugeClaude » dans `HKCU\...\CurrentVersion\Run` (démarrage auto, désactivable) | Ne demande pas de droits administrateur, ne modifie rien d'autre |
| Sur demande (menu Diagnostic), lit `%USERPROFILE%\.claude\projects\**\*.jsonl` et écrit un rapport dans `%TEMP%` | Ne l'envoie nulle part |
| Extrait `WebView2Loader.dll` (composant officiel Microsoft) dans `%APPDATA%\JaugeClaude\bin` | N'installe ni service ni pilote |

Les seuls composants externes sont ceux de Microsoft : le paquet NuGet officiel [`Microsoft.Web.WebView2`](https://www.nuget.org/packages/Microsoft.Web.WebView2), embarqué dans le `.exe` pour qu'il reste un seul fichier.

## Vérifier le fichier téléchargé

1. **Empreinte** : dans PowerShell, à côté du fichier :
   ```
   Get-FileHash .\JaugeClaude.exe -Algorithm SHA256
   ```
   Le résultat doit être identique à celui affiché sur la page Release et dans le journal de construction (onglet [Actions](https://github.com/gautierm78/jauge-claude/actions), étape « Empreinte SHA-256 »).
2. **Construire vous-même** : installez le [SDK .NET](https://dotnet.microsoft.com/download), puis :
   ```
   git clone https://github.com/gautierm78/jauge-claude
   cd jauge-claude
   dotnet build csharp/JaugeClaude.csproj -c Release
   ```
   Le programme est dans `csharp\bin\Release\net48\JaugeClaude.exe`.
3. **Analyse antivirus** : vous pouvez soumettre le fichier à [VirusTotal](https://www.virustotal.com).

## Blocage par Windows

Le fichier n'est pas signé numériquement (une signature est payante). Selon votre PC :

- **« Windows a protégé votre ordinateur »** (SmartScreen) : *Informations complémentaires* > *Exécuter quand même*.
- **« Le Contrôle intelligent des applications a bloqué… »** (Windows 11) : ce filtre refuse tout programme non signé, il n'y a pas de contournement simple. Seule une version signée (Microsoft Store ou certificat) passera. Voir le guide.

## Limites

L'adresse lue est celle qu'utilise la page Utilisation de claude.ai. Elle n'est pas documentée officiellement : si elle change, l'application devra être adaptée.

## Publier une nouvelle version

1. Sur GitHub : **Releases** > **Draft a new release**.
2. **Choose a tag** : tapez le numéro (par exemple `v3.1.0`) puis **Create new tag**. Titre et description au choix, **sans joindre de fichier**.
3. **Publish release**. En 2 à 3 minutes, GitHub construit l'application et ajoute lui-même `JaugeClaude.exe` et `JaugeClaude.exe.sha256` à la version.

## Microsoft Store

Le paquet pour le Store (`JaugeClaude-Store.msix`) est construit automatiquement (onglet **Actions** > **Artifacts**). La procédure de publication est dans [`store/README.md`](store/README.md). Politique de confidentialité : [`CONFIDENTIALITE.md`](CONFIDENTIALITE.md).

## Licence

MIT, voir [`LICENSE`](LICENSE).
