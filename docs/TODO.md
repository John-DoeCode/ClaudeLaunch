# À faire / à vérifier — Claude Launcher

## En attente du retour de test

- [x] `Error: Workspace not trusted` sur un dossier neuf : corrigé par l'approbation
      automatique dans `~/.claude.json` (case « Approuver le dossier »).
- [x] Confirmation avant fermeture supprimée.

- [ ] Lancement réel en mode remote (`claude --rc`) sur un dossier neuf : approbation,
      génération du CLAUDE.md, démarrage du CLI.
- [ ] Lancement réel en mode non remote (`claude` seul).
- [ ] Lancement en administrateur : invite UAC, Windows Terminal élevé dans le bon dossier ;
      message d'erreur si l'UAC est refusée.
- [ ] Vérifier le comportement quand Windows Terminal ouvre un nouvel onglet dans une fenêtre
      existante plutôt qu'une nouvelle fenêtre (résolution de la fenêtre cible).
- [ ] Vérifier la génération de `CLAUDE.md` sur un dossier réel et la pertinence du modèle
      par défaut.

## Améliorations possibles

- [ ] Renommer un modèle sans passer par une suppression / recréation.
- [ ] Icône d'application (`.ico`) et raccourci `--auto` prêt à l'emploi sur le bureau.
- [ ] Publication en exécutable autonome documentée dans le README (`dotnet publish`).
- [ ] Alias de projet (le champ `Alias` existe dans `ProjectEntry` mais n'est pas exposé).

## Rejeté volontairement

- Envoi de `/init` en frappes clavier : remplacé par l'écriture directe de `CLAUDE.md`.
- Envoi automatique du `y` pour la question du contrôle à distance : retiré, inutile.
- Journal dans l'interface : retiré ; seules les erreurs s'affichent, en boîte de dialogue.
- Options avancées dans l'interface (commande libre, délais, garder le terminal ouvert,
  réduire après lancement) : retirées pour simplifier ; les valeurs restent en dur ou dans
  `settings.json`.
