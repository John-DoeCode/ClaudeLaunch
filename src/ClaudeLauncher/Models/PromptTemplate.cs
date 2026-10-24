namespace ClaudeLauncher.Models;

/// <summary>
/// A named text used to generate the CLAUDE.md file of a project.
/// </summary>
public sealed class PromptTemplate
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Name { get; set; } = "Sans nom";

    public string Content { get; set; } = string.Empty;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public override string ToString() => Name;

    /// <summary>Template proposed on first run so the list is never empty.</summary>
    public static PromptTemplate CreateDefault() => new()
    {
        Name = "Modèle par défaut",
        Content = """
                  # {{PROJECT_NAME}}

                  ## Contexte

                  Décrire ici le projet, son but et son état actuel.

                  ## Stack

                  - Langage :
                  - Framework :
                  - Outils :

                  ## Commandes

                  ```bash
                  # build
                  # test
                  # run
                  ```

                  ## Conventions

                  - Style de code :
                  - Nommage :
                  - Tests :

                  ## Consignes pour Claude

                  - Répondre en français.
                  - Ne pas modifier de fichier hors du périmètre demandé.
                  """
    };
}
