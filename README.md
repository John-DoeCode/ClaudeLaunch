# Claude Launcher

Assistant de démarrage WPF pour le CLI **Claude Code**. Il choisit un dossier de projet,
génère son `CLAUDE.md` à partir d'un modèle de texte quand il n'en existe pas, ouvre un
approuve le dossier dans la configuration du CLI si nécessaire, puis ouvre un terminal
dedans qui lance `claude --rc` (ou `claude` si le mode remote est décoché).

## Fonctionnement

1. Sélection du dossier : liste déroulante d'historique (dernier dossier utilisé en tête),
   bouton **Parcourir…** ou glisser-déposer d'un dossier sur la fenêtre.
2. Modèle de `CLAUDE.md` : liste déroulante des textes enregistrés (dernier utilisé
   présélectionné), éditeur, boutons **Nouveau**, **Enregistrer** (le nom est demandé pour
   un nouveau texte), **Supprimer**.
3. Clic sur **Lancer Claude Code** :
   - si le dossier contient déjà un `CLAUDE.md`, il n'est pas touché ;
   - sinon, `CLAUDE.md` est écrit **avant** l'ouverture du terminal à partir du texte
     affiché dans l'éditeur ; si aucun modèle n'est sélectionné, une fenêtre demande d'en
     choisir un et le lancement est annulé ;
   - si le dossier n'a jamais été approuvé dans Claude Code, `hasTrustDialogAccepted` est
     écrit pour lui dans `~/.claude.json` (le CLI refuse sinon de démarrer) ;
   - un terminal s'ouvre avec le dossier comme répertoire courant ;
   - la commande est exécutée : `claude --rc` si **Mode remote** est coché, sinon `claude`.
4. Le dossier remonte en tête de l'historique et le lanceur se ferme.

## Modèles de CLAUDE.md

Chaque modèle est un texte nommé, réutilisable d'un projet à l'autre. Variables remplacées
au moment de la génération :

| Variable | Valeur |
|----------|--------|
| `{{PROJECT_NAME}}` | Nom du dossier |
| `{{PROJECT_PATH}}` | Chemin complet du dossier |
| `{{DATE}}` | Date du jour (`yyyy-MM-dd`) |

Le texte utilisé est celui affiché dans l'éditeur, modifications non enregistrées comprises.
Le fichier est écrit en UTF-8 et n'écrase jamais un `CLAUDE.md` existant.

## Prérequis

- Windows 10 / 11
- .NET 8 Desktop Runtime (ou plus récent avec un `TargetFramework` adapté)
- `claude` accessible dans le `PATH`

## Compiler et lancer

```powershell
dotnet build
dotnet run --project src/ClaudeLauncher
```

Exécutable autonome (dossier `publish/`) :

```powershell
dotnet publish src/ClaudeLauncher -c Release -r win-x64 --self-contained false -o publish
```

## Arguments de ligne de commande

| Argument   | Effet                                                      |
|------------|------------------------------------------------------------|
| `<dossier>`| Sélectionne ce dossier au démarrage                         |
| `--auto`   | Lance immédiatement le projet sélectionné (démarrage zéro clic) |

Un raccourci `ClaudeLauncher.exe --auto` reprend donc le dernier dossier utilisé et
démarre Claude Code sans interaction (le lanceur demande ensuite confirmation avant de se
fermer).

## Options (persistées)

| Réglage | Défaut | Rôle |
|---------|--------|------|
| Terminal | Windows Terminal | `wt.exe`, PowerShell ou `cmd` (repli automatique sur PowerShell si `wt.exe` est absent) |
| Mode remote | coché | Coché : `claude --rc`. Décoché : `claude` |

Les deux réglages, comme le dernier dossier et le dernier modèle, sont restaurés au
démarrage suivant.

## Stockage

`%APPDATA%\ClaudeLauncher\`

- `history.json` : dossiers, date de dernière ouverture, nombre de lancements
- `templates.json` : modèles de `CLAUDE.md` (nom, contenu, date de modification)
- `settings.json` : terminal, mode remote, dernier modèle utilisé

## Approbation des dossiers

Claude Code refuse de démarrer dans un dossier dont la fenêtre de confiance n'a jamais été
acceptée (`Error: Workspace not trusted`). Le lanceur écrit donc `hasTrustDialogAccepted`
pour le dossier sélectionné dans `~/.claude.json`, de façon atomique et seulement après une
lecture JSON réussie ; le reste de la configuration n'est pas modifié.

## Structure

```
src/ClaudeLauncher/
  Models/        ProjectEntry, AppSettings, PromptTemplate
  Services/      StorageService (JSON), LauncherService (CLAUDE.md + terminal), ClaudeConfigService, RelayCommand
  ViewModels/    MainViewModel
  Views/         MainWindow, InputDialog
  Themes/        Theme.xaml (thème sombre)
```
