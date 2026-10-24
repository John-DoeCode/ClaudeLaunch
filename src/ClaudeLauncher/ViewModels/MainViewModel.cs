using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using ClaudeLauncher.Models;
using ClaudeLauncher.Services;
using ClaudeLauncher.Views;

namespace ClaudeLauncher.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private const int MaxHistoryEntries = 30;

    private readonly StorageService _storage = new();
    private readonly LauncherService _launcher = new();
    private readonly ClaudeConfigService _claudeConfig = new();

    private ProjectEntry? _selectedProject;
    private PromptTemplate? _selectedTemplate;
    private string _templateContent = string.Empty;
    private bool _templateDirty;

    public MainViewModel()
    {
        Settings = _storage.LoadSettings();
        History = new ObservableCollection<ProjectEntry>(_storage.LoadHistory());
        Templates = new ObservableCollection<PromptTemplate>(_storage.LoadTemplates());

        SelectedProject = History.FirstOrDefault();
        SelectedTemplate = Templates.FirstOrDefault(t => t.Id == Settings.LastTemplateId)
            ?? Templates.FirstOrDefault();

        BrowseCommand = new RelayCommand(_ => Browse());
        LaunchCommand = new RelayCommand(_ => Launch(), _ => CanLaunch);
        OpenInExplorerCommand = new RelayCommand(_ => OpenInExplorer(), _ => SelectedProject?.Exists == true);
        RemoveCommand = new RelayCommand(_ => RemoveSelected(), _ => SelectedProject is not null);
        NewTemplateCommand = new RelayCommand(_ => NewTemplate());
        SaveTemplateCommand = new RelayCommand(_ => SaveTemplate(), _ => TemplateContent.Length > 0);
        DeleteTemplateCommand = new RelayCommand(_ => DeleteTemplate(), _ => SelectedTemplate is not null);
    }

    public AppSettings Settings { get; }

    public ObservableCollection<ProjectEntry> History { get; }

    public ObservableCollection<PromptTemplate> Templates { get; }

    public ICommand BrowseCommand { get; }

    public ICommand LaunchCommand { get; }

    public ICommand OpenInExplorerCommand { get; }

    public ICommand RemoveCommand { get; }

    public ICommand NewTemplateCommand { get; }

    public ICommand SaveTemplateCommand { get; }

    public ICommand DeleteTemplateCommand { get; }

    /// <summary>Raised once every action succeeded so the window can close itself.</summary>
    public event EventHandler? LaunchCompleted;

    public ProjectEntry? SelectedProject
    {
        get => _selectedProject;
        set
        {
            if (_selectedProject == value)
            {
                return;
            }

            _selectedProject = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedPath));
            OnPropertyChanged(nameof(HasSelection));
            OnPropertyChanged(nameof(CanLaunch));
            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(StatusIsWarning));
        }
    }

    public PromptTemplate? SelectedTemplate
    {
        get => _selectedTemplate;
        set
        {
            if (_selectedTemplate == value)
            {
                return;
            }

            _selectedTemplate = value;
            TemplateContent = value?.Content ?? string.Empty;
            _templateDirty = false;

            Settings.LastTemplateId = value?.Id;
            SaveSettings();

            OnPropertyChanged();
            OnPropertyChanged(nameof(TemplateStatus));
        }
    }

    /// <summary>Text of the editor, used as-is when generating CLAUDE.md.</summary>
    public string TemplateContent
    {
        get => _templateContent;
        set
        {
            if (_templateContent == value)
            {
                return;
            }

            _templateContent = value;
            _templateDirty = SelectedTemplate is null || SelectedTemplate.Content != value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(TemplateStatus));
        }
    }

    public string TemplateStatus
    {
        get
        {
            if (SelectedTemplate is null)
            {
                return TemplateContent.Length > 0
                    ? "Texte non enregistré : utilisez « Enregistrer » pour lui donner un nom."
                    : "Aucun modèle : créez un texte qui servira de CLAUDE.md.";
            }

            return _templateDirty
                ? $"« {SelectedTemplate.Name} » modifié (non enregistré)."
                : $"« {SelectedTemplate.Name} » — modifié le {SelectedTemplate.UpdatedAt:dd/MM/yyyy HH:mm}.";
        }
    }

    public bool HasSelection => SelectedProject is not null;

    public string SelectedPath => SelectedProject?.Path ?? "Aucun dossier sélectionné";

    public bool CanLaunch => SelectedProject?.Exists == true;

    /// <summary>Short description of what the launch will do for the selected folder.</summary>
    public string StatusText
    {
        get
        {
            if (SelectedProject is null)
            {
                return "Choisissez un dossier de projet pour commencer.";
            }

            if (!SelectedProject.Exists)
            {
                return "Dossier introuvable : il a été déplacé ou supprimé.";
            }

            return SelectedProject.HasClaudeMd
                ? "CLAUDE.md présent : démarrage direct, aucune génération."
                : "Aucun CLAUDE.md : il sera généré à partir du modèle sélectionné avant l'ouverture du terminal.";
        }
    }

    public bool StatusIsWarning => SelectedProject is not null && !SelectedProject.Exists;

    /// <summary>Checked runs "claude --rc", unchecked runs "claude".</summary>
    public bool UseRemoteMode
    {
        get => Settings.UseRemoteMode;
        set
        {
            Settings.UseRemoteMode = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CommandPreview));
            SaveSettings();
        }
    }

    public string CommandPreview => $"Commande : {Settings.Command}";

    private void Browse()
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Choisir le dossier du projet",
            Multiselect = false
        };

        if (SelectedProject?.Exists == true)
        {
            dialog.InitialDirectory = SelectedProject.Path;
        }

        if (dialog.ShowDialog() == true)
        {
            SelectFolder(dialog.FolderName);
        }
    }

    /// <summary>Adds the folder to the history (or moves it to the top) and selects it.</summary>
    public void SelectFolder(string folder)
    {
        var normalized = Path.GetFullPath(folder).TrimEnd(Path.DirectorySeparatorChar);
        var existing = History.FirstOrDefault(e =>
            string.Equals(e.Path.TrimEnd(Path.DirectorySeparatorChar), normalized, StringComparison.OrdinalIgnoreCase));

        if (existing is null)
        {
            existing = new ProjectEntry { Path = normalized, LastOpened = DateTime.Now };
            History.Insert(0, existing);
        }
        else
        {
            History.Move(History.IndexOf(existing), 0);
        }

        TrimHistory();
        SelectedProject = existing;
        Persist();
    }

    private void Launch()
    {
        var project = SelectedProject;
        if (project is null || !project.Exists)
        {
            return;
        }

        // A missing CLAUDE.md is generated from the selected template before the terminal
        // opens, so Claude Code starts with the project already described.
        if (project.ClaudeMdPath is null && !TryGenerateClaudeMd(project))
        {
            return;
        }

        // The CLI refuses to start in a folder whose trust dialog was never accepted.
        if (!_claudeConfig.IsWorkspaceTrusted(project.Path) && !_claudeConfig.TrustWorkspace(project.Path))
        {
            ShowError("Approbation impossible",
                "Le dossier n'a pas pu être approuvé dans la configuration de Claude Code "
                + $"({_claudeConfig.ConfigPath}). Lancez « claude » une fois dans ce dossier "
                + "pour accepter la fenêtre de confiance.");
            return;
        }

        var result = _launcher.Launch(project.Path, Settings);
        if (result.Error is not null)
        {
            ShowError("Lancement impossible", result.Error);
            return;
        }

        project.LastOpened = DateTime.Now;
        project.LaunchCount++;

        var index = History.IndexOf(project);
        if (index > 0)
        {
            History.Move(index, 0);
        }

        Persist();
        LaunchCompleted?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Creates CLAUDE.md from the editor text. Returns false when the user has no template
    /// selected, which cancels the launch.
    /// </summary>
    private bool TryGenerateClaudeMd(ProjectEntry project)
    {
        if (SelectedTemplate is null || string.IsNullOrWhiteSpace(TemplateContent))
        {
            ShowError("Modèle requis",
                "Ce dossier ne contient pas de CLAUDE.md. Choisissez un modèle de texte dans "
                + "la liste (ou créez-en un) avant de lancer Claude Code.");
            return false;
        }

        try
        {
            LauncherService.CreateClaudeMd(project.Path, TemplateContent);
            OnPropertyChanged(nameof(StatusText));
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ShowError("Écriture impossible",
                $"CLAUDE.md n'a pas pu être écrit dans le dossier :{Environment.NewLine}{ex.Message}");
            return false;
        }
    }

    private void NewTemplate()
    {
        SelectedTemplate = null;
        TemplateContent = string.Empty;
    }

    /// <summary>
    /// Saves the editor text: overwrites the selected template, or asks for a name when the
    /// text does not belong to a template yet.
    /// </summary>
    private void SaveTemplate()
    {
        var owner = System.Windows.Application.Current?.MainWindow;
        var target = SelectedTemplate;

        if (target is null)
        {
            var name = InputDialog.AskText(owner, "Enregistrer le modèle",
                "Nom du modèle :", $"Modèle {Templates.Count + 1}");

            if (name is null)
            {
                return;
            }

            var duplicate = Templates.FirstOrDefault(t =>
                string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase));

            if (duplicate is not null)
            {
                if (!InputDialog.Confirm(owner, "Nom déjà utilisé",
                        $"Un modèle nommé « {name} » existe déjà. Le remplacer ?", "Remplacer"))
                {
                    return;
                }

                target = duplicate;
            }
            else
            {
                target = new PromptTemplate { Name = name };
                Templates.Insert(0, target);
            }
        }

        target.Content = TemplateContent;
        target.UpdatedAt = DateTime.Now;
        _storage.SaveTemplates(Templates);

        _selectedTemplate = target;
        _templateDirty = false;
        Settings.LastTemplateId = target.Id;
        SaveSettings();

        OnPropertyChanged(nameof(SelectedTemplate));
        OnPropertyChanged(nameof(TemplateStatus));
    }

    private void DeleteTemplate()
    {
        var template = SelectedTemplate;
        if (template is null)
        {
            return;
        }

        var owner = System.Windows.Application.Current?.MainWindow;
        if (!InputDialog.Confirm(owner, "Supprimer le modèle",
                $"Supprimer définitivement « {template.Name} » ?", "Supprimer"))
        {
            return;
        }

        var index = Templates.IndexOf(template);
        Templates.Remove(template);
        _storage.SaveTemplates(Templates);

        SelectedTemplate = Templates.Count == 0
            ? null
            : Templates[Math.Min(index, Templates.Count - 1)];
    }

    private void OpenInExplorer()
    {
        if (SelectedProject?.Exists != true)
        {
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = $"\"{SelectedProject.Path}\"",
            UseShellExecute = true
        });
    }

    private void RemoveSelected()
    {
        if (SelectedProject is null)
        {
            return;
        }

        var index = History.IndexOf(SelectedProject);
        History.Remove(SelectedProject);
        SelectedProject = History.Count == 0
            ? null
            : History[Math.Min(index, History.Count - 1)];
        Persist();
    }

    private void TrimHistory()
    {
        while (History.Count > MaxHistoryEntries)
        {
            History.RemoveAt(History.Count - 1);
        }
    }

    private static void ShowError(string title, string message) =>
        InputDialog.ShowMessage(System.Windows.Application.Current?.MainWindow, title, message);

    private void Persist() => _storage.SaveHistory(History);

    private void SaveSettings() => _storage.SaveSettings(Settings);

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
