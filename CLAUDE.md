# Claude Launcher

Application WPF (.NET 8, Windows) qui sert d'assistant de démarrage au CLI Claude Code :
sélection d'un dossier de projet, génération du `CLAUDE.md` depuis un modèle si le dossier
n'en contient pas, approbation du dossier dans `~/.claude.json` si nécessaire, puis
ouverture d'un terminal dans ce dossier qui exécute `claude --rc` (ou `claude`).

## Reprise de session

Lire `docs/ETAT.md` (état courant, décisions, procédure de test) puis `docs/TODO.md`
(travaux en attente). La commande `/reprise` fait ce chargement automatiquement.
Après chaque changement notable, mettre ces deux fichiers à jour.

## Compiler / lancer

```powershell
dotnet build
dotnet run --project src/ClaudeLauncher
```

L'exécutable de développement est `src/ClaudeLauncher/bin/Debug/net8.0-windows/ClaudeLauncher.exe`.
Arguments : `<dossier>` présélectionne un dossier, `--auto` lance immédiatement.

## Structure

```
src/ClaudeLauncher/
  Models/      ProjectEntry, AppSettings, PromptTemplate
  Services/    StorageService (JSON), LauncherService (CLAUDE.md + terminal),
               ClaudeConfigService (~/.claude.json), RelayCommand
  ViewModels/  MainViewModel (toute la logique applicative)
  Views/       MainWindow, InputDialog
  Themes/      Theme.xaml (thème sombre, styles de tous les contrôles)
```

Données utilisateur : `%APPDATA%\ClaudeLauncher\` (`history.json`, `templates.json`,
`settings.json`).

## Conventions

- Code, commentaires et documentation en anglais dans les fichiers `.cs`, textes
  d'interface en français.
- Aucune dépendance NuGet ni référence hors WPF.
- Pas de framework MVVM : `MainViewModel` implémente `INotifyPropertyChanged` à la main et
  `RelayCommand` fournit `ICommand`.
- Les fichiers `.cs` contenant des accents sont enregistrés en UTF-8 **avec BOM**, les
  `.xaml` en UTF-8 sans BOM.
- Ne jamais écraser un `CLAUDE.md` existant dans un dossier cible.

## Pièges connus

- Un `TextBox` avec une `Height` fixe et le style maison est tronqué verticalement : laisser
  la hauteur automatique et utiliser `Padding` (voir `Views/InputDialog.xaml`).
- Tester un lancement réel démarre une vraie session `claude --rc` : utiliser une commande
  neutre et un dossier temporaire pour les essais automatisés.
- `ClaudeConfigService` réécrit `~/.claude.json` : ne jamais tester l'écriture sur le vrai
  fichier, passer le chemin d'une copie au constructeur.
