# Jauge d'utilisation Claude

Petite application Windows qui affiche en permanence votre consommation Claude (session 5 h et semaine) près de l'horloge, avec une notification à 70 % et 90 %.

- **Un seul fichier** `JaugeClaude.exe` (moins de 1 Mo), sans installation ni dépendance : Windows 10/11 contient déjà tout (.NET Framework 4.8 et Edge WebView2).
- **Code source complet et lisible** dans [`csharp/Program.cs`](csharp/Program.cs) (un seul fichier d'environ 400 lignes, commenté en français).
- **Construit en public** par GitHub Actions : vous pouvez vérifier que le `.exe` téléchargé vient bien de ce code (voir plus bas).

## Télécharger

Page [**Releases**](https://github.com/gautierm78/jauge-claude/releases/latest) → `JaugeClaude.exe`.

Mode d'emploi complet : [`guide-installation.md`](guide-installation.md).

## Fonctionnement

1. Au premier lancement, une fenêtre claude.ai s'ouvre (moteur Edge WebView2, comme un navigateur). Connectez-vous une fois, puis fermez-la : elle se cache, l'application continue.
2. Toutes les 5 minutes, l'application lit votre utilisation sur claude.ai, comme le fait la page *Paramètres > Utilisation*.
3. La pastille affiche le pourcentage le plus élevé : vert < 70 %, orange 70-89 %, rouge ≥ 90 %, gris si pas de chiffres depuis 20 min.
4. Clic droit : actualiser, afficher la fenêtre claude.ai, lancer au démarrage de Windows, quitter.

## Ce que fait le programme, et ce qu'il ne fait pas

Tout est vérifiable dans [`csharp/Program.cs`](csharp/Program.cs).

| Il fait | Il ne fait pas |
| --- | --- |
| Ouvre `https://claude.ai/settings/usage` dans sa fenêtre intégrée | Ne contacte aucun autre site (pas de statistiques, pas de serveur tiers) |
| Lit `/api/organizations` et `/api/organizations/<id>/usage` sur claude.ai | Ne lit ni vos conversations ni vos fichiers |
| Garde la session de sa fenêtre et ses réglages dans `%APPDATA%\JaugeClaude` | Ne stocke aucun mot de passe lui-même, n'ouvre aucun port réseau |
| Ajoute une ligne « JaugeClaude » dans `HKCU\...\CurrentVersion\Run` (démarrage auto, désactivable) | Ne demande pas de droits administrateur, ne modifie rien d'autre |
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

## Licence

MIT, voir [`LICENSE`](LICENSE).
