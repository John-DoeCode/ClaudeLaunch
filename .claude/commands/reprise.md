---
description: Reprendre le travail sur Claude Launcher à partir des fichiers de suivi
allowed-tools: Read, Glob, Grep, Bash(dotnet build:*), Bash(git status:*), Bash(git log:*)
---

Reprise de session sur le projet **Claude Launcher** (application WPF .NET 8).

Fais ceci dans l'ordre, sans poser de question préalable :

1. Lis `CLAUDE.md`, `docs/ETAT.md` et `docs/TODO.md` à la racine du projet.
2. Compile le projet (`dotnet build`) pour vérifier que l'état du dépôt est sain, et signale
   toute erreur ou tout avertissement.
3. Résume en quelques lignes : ce qui est fait, ce qui est en attente de test, et les points
   ouverts prioritaires de `docs/TODO.md`.
4. Prends en compte ce que l'utilisateur ajoute après la commande (`$ARGUMENTS`) : c'est en
   général le retour de son test manuel, un bug constaté ou la prochaine tâche voulue. S'il
   n'a rien ajouté, demande-lui le résultat de son test avant de modifier quoi que ce soit.
5. Une fois le travail de la session effectué, mets à jour `docs/ETAT.md` (état, décisions,
   ce qui reste non testé) et `docs/TODO.md` (cases cochées, nouvelles entrées) avant de
   conclure.

Rappels utiles :

- Les textes d'interface sont en français, le code et ses commentaires en anglais.
- Ne jamais lancer un test qui démarre une vraie session `claude --rc` sans prévenir
  l'utilisateur.
- `ClaudeConfigService` écrit dans `~/.claude.json` : tester uniquement sur une copie.
- Données utilisateur : `%APPDATA%\ClaudeLauncher\` (`history.json`, `templates.json`,
  `settings.json`) ; restaurer ces fichiers après un test qui les modifie.

$ARGUMENTS
