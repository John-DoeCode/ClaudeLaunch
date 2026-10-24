# État du projet — Claude Launcher

Dernière mise à jour : 2026-09-06 (après le premier test utilisateur)

## Où en est le programme

Application complète et fonctionnelle, compilée sans erreur ni avertissement
(`dotnet build`, .NET 8, `net8.0-windows`). L'interface a été vérifiée par captures d'écran ;
la génération de `CLAUDE.md` et l'ouverture du terminal ont été testées sur un dossier
temporaire avec une commande neutre.

### Fonctionnalités en place

1. **Sélection du dossier** : liste déroulante d'historique (dernier dossier en tête et
   présélectionné), bouton **Parcourir…** (`Microsoft.Win32.OpenFolderDialog`),
   **Explorateur**, **Retirer**, glisser-déposer d'un dossier sur la fenêtre.
2. **Modèles de CLAUDE.md** : liste déroulante des textes enregistrés (dernier utilisé
   présélectionné), éditeur multiligne, boutons **Nouveau**, **Enregistrer**, **Supprimer**.
   Le nom est demandé lorsque le texte n'appartient à aucun modèle ; un nom déjà pris demande
   confirmation avant remplacement. Un modèle par défaut est créé au premier lancement.
3. **Génération** : si le dossier ne contient pas de `CLAUDE.md`, il est écrit **avant**
   l'ouverture du terminal à partir du texte affiché dans l'éditeur, avec substitution de
   `{{PROJECT_NAME}}`, `{{PROJECT_PATH}}` et `{{DATE}}`. Sans modèle sélectionné, une fenêtre
   demande d'en choisir un et le lancement est annulé. Un `CLAUDE.md` existant n'est jamais
   touché.
4. **Lancement** : toujours **Windows Terminal en administrateur** (`wt.exe`,
   `Verb = "runas"`, invite UAC), sans choix possible :
   `wt.exe -d "<dossier>" powershell.exe -NoExit -Command "claude --rc"` si la case
   **Mode remote** est cochée, sinon `claude`. Aucune frappe clavier n'est envoyée. Un
   refus de l'UAC affiche « L'élévation en administrateur a été refusée » ; sans `wt.exe`,
   le lancement est refusé (« Windows Terminal (wt.exe) est introuvable »). Le dossier
   passe par `-d` car un processus élevé ignore le dossier de travail et démarre dans
   `System32`. Lancer PowerShell directement en administrateur ouvrait une console
   classique (Windows ne confie pas une console élevée au terminal par défaut), d'où l'appel
   explicite à `wt.exe`. Testé sans élévation depuis `System32` sur un chemin avec espaces
   et apostrophes ; l'élévation réelle (clic UAC) reste à valider.
5. **Approbation du dossier** : le CLI refuse de démarrer dans un dossier dont la fenêtre de
   confiance n'a jamais été acceptée (`Error: Workspace not trusted`). Avant le lancement, le
   programme lit `~/.claude.json` et écrit `hasTrustDialogAccepted: true` pour ce dossier si
   nécessaire. Comportement systématique, non désactivable.
6. **Fin de lancement** : le dossier remonte en tête de l'historique et le lanceur se ferme
   immédiatement, sans question.
7. **Persistance** dans `%APPDATA%\ClaudeLauncher\` : `history.json` (dossiers, date, nombre
   de lancements), `templates.json` (modèles), `settings.json` (mode remote,
   dernier modèle ; l'ancienne clé `Terminal` est ignorée à la lecture).
8. **Ligne de commande** : `ClaudeLauncher.exe "<dossier>"` présélectionne, `--auto` lance
   immédiatement.

## Session du 2026-09-06 (test utilisateur)

Premier essai réel (avec l'ancienne commande `claude remote`) : le CLI s'est arrêté sur
`Error: Workspace not trusted. Please run 'claude' in <dossier> first`, et le `y` envoyé
ensuite a atterri dans PowerShell (`Le terme «y» n'est pas reconnu`). Deux corrections :

- `Services/ClaudeConfigService.cs` lit et met à jour `~/.claude.json` (clé
  `projects["<chemin en slashs>"].hasTrustDialogAccepted`). Le fichier est réécrit de façon
  atomique et seulement après une lecture JSON réussie ; testé sur une copie : les autres
  entrées restent identiques.
- La confirmation avant fermeture a été supprimée : le lanceur se ferme dès que le lancement
  est terminé.
- La commande du mode remote est passée de `claude remote` à `claude --rc`.
- Suppressions demandées ensuite : injection du `y` (et tout son outillage Win32/WinForms),
  case à cocher d'approbation (comportement désormais systématique), carte JOURNAL. Les
  erreurs passent maintenant par une boîte de dialogue.

## Décisions structurantes

- **Génération de fichier plutôt que `/init`** : la première version envoyait `/init` en
  frappes clavier après le démarrage du CLI. Remplacé par l'écriture directe de `CLAUDE.md`
  avant l'ouverture du terminal, à la demande de l'utilisateur.
- **Aucune frappe clavier** : l'envoi automatique de `y` (question du contrôle à distance) a
  été retiré à la demande de l'utilisateur, avec toute la mécanique associée
  (`NativeMethods`, `SendKeys`, référence WinForms, délai `RemotePromptDelayMs`).
- **Interface non repliable** : les sections OPTIONS et MODÈLE CLAUDE.md sont toujours
  visibles, la fenêtre s'ouvre à sa taille complète (`SizeToContent="Height"`,
  `ResizeMode="NoResize"`, aucun `ScrollViewer` de page).
- **Options minimales** : seule la case Mode remote est exposée (la liste de choix du
  terminal a été supprimée : toujours Windows Terminal en administrateur). Les
  anciens réglages (commande libre, délais, garder le terminal ouvert, réduire après
  lancement, approbation du dossier) ont été retirés ; le terminal reste ouvert, la
  génération et l'approbation restent actives en dur.
- **Approbation automatique du dossier** : écrire `hasTrustDialogAccepted` revient à
  accepter à la place de l'utilisateur une fenêtre de sécurité du CLI. Choix assumé et
  systématique (le dossier est celui que l'utilisateur vient de sélectionner) ; l'écriture
  est atomique et ne touche pas le reste de `~/.claude.json`.
- **Barre de titre maison** : une seule croix, dessinée en `Path` vectoriel (la police
  d'icônes affichait un carré vide).

## Comment tester

### Test manuel complet (non encore réalisé)

1. Lancer `src/ClaudeLauncher/bin/Debug/net8.0-windows/ClaudeLauncher.exe`.
2. Choisir un dossier de projet sans `CLAUDE.md`, vérifier le message d'état.
3. Vérifier que le modèle voulu est sélectionné, puis **Lancer Claude Code**.
4. Contrôler : `CLAUDE.md` créé, dossier approuvé si nécessaire, terminal ouvert dans le bon
   dossier, `claude --rc` exécuté, fermeture immédiate du lanceur.
5. Recommencer avec **Mode remote** décoché : la commande doit être `claude`.

### Test automatisé sans effet de bord

Une session précédente a piloté l'interface via UI Automation (PowerShell,
`UIAutomationClient`) pour ouvrir les dialogues et prendre des captures. Pour tester un
lancement sans démarrer de vraie session distante : écrire temporairement dans
`%APPDATA%\ClaudeLauncher\settings.json` un mode non remote, utiliser un dossier temporaire,
puis restaurer le fichier. Pour `ClaudeConfigService`, passer au constructeur le chemin
d'une **copie** de `~/.claude.json` (voir le harnais `dotnet run` utilisé lors de la session
du 2026-09-06).

## Ce qui n'a jamais été exécuté

Le cycle réel complet avec `claude --rc` : ouverture du terminal, approbation du dossier et
démarrage du CLI.
