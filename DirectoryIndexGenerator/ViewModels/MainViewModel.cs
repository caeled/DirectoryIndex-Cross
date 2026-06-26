using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using DirectoryIndexGenerator.Services;

namespace DirectoryIndexGenerator.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    // ── Drop zone brushes ─────────────────────────────────────────────
    private static readonly ISolidColorBrush NormalBorder     = new SolidColorBrush(Color.Parse("#31314A"));
    private static readonly ISolidColorBrush DragBorder       = new SolidColorBrush(Color.Parse("#7C3AED"));
    private static readonly ISolidColorBrush NormalBackground = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
    private static readonly ISolidColorBrush DragBackground   = new SolidColorBrush(Color.FromArgb(0x18, 0x7C, 0x3A, 0xED));

    private Window? _window;

    // ── Backing fields ────────────────────────────────────────────────
    private string _pageTitle            = "Folder Index";
    private string _authorName           = "";
    private string _headerImagePath      = "";
    private string _selectedImageStyle   = "Cover";
    private string _imageHeight          = "220";
    private string _imageOverlayText     = "";
    private string _selectedFolderPath   = "";
    private string _statusMessage        = "Drop a folder to get started.";
    private string _fileCountMessage     = "";
    private bool   _isBusy;
    private bool   _isDragOver;
    private bool   _hasOutput;

    public string PageTitle
    {
        get => _pageTitle;
        set { _pageTitle = value; OnPropertyChanged(); }
    }
    public string AuthorName
    {
        get => _authorName;
        set { _authorName = value; OnPropertyChanged(); }
    }
    public string HeaderImagePath
    {
        get => _headerImagePath;
        set { _headerImagePath = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasHeaderImage)); }
    }
    public bool HasHeaderImage => !string.IsNullOrWhiteSpace(_headerImagePath);

    public string[] ImageStyles { get; } = ["Cover", "Contain", "Tile", "Banner"];

    public string SelectedImageStyle
    {
        get => _selectedImageStyle;
        set { _selectedImageStyle = value; OnPropertyChanged(); }
    }
    public string ImageHeight
    {
        get => _imageHeight;
        set { _imageHeight = value; OnPropertyChanged(); }
    }
    public string ImageOverlayText
    {
        get => _imageOverlayText;
        set { _imageOverlayText = value; OnPropertyChanged(); }
    }
    public string SelectedFolderPath
    {
        get => _selectedFolderPath;
        private set
        {
            _selectedFolderPath = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedFolderName));
            OnPropertyChanged(nameof(HasFolder));
            (GenerateCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        }
    }
    public string SelectedFolderName =>
        Path.GetFileName(_selectedFolderPath.TrimEnd('/', '\\')) ?? _selectedFolderPath;
    public bool HasFolder => !string.IsNullOrWhiteSpace(_selectedFolderPath);

    public string StatusMessage
    {
        get => _statusMessage;
        set { _statusMessage = value; OnPropertyChanged(); }
    }
    public string FileCountMessage
    {
        get => _fileCountMessage;
        set { _fileCountMessage = value; OnPropertyChanged(); }
    }
    public bool IsBusy
    {
        get => _isBusy;
        set { _isBusy = value; OnPropertyChanged(); }
    }
    public bool IsDragOver
    {
        get => _isDragOver;
        set
        {
            _isDragOver = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DropZoneBorderBrush));
            OnPropertyChanged(nameof(DropZoneBackgroundBrush));
        }
    }
    public bool HasOutput
    {
        get => _hasOutput;
        set { _hasOutput = value; OnPropertyChanged(); (OpenOutputCommand as RelayCommand)?.RaiseCanExecuteChanged(); }
    }

    public IBrush DropZoneBorderBrush     => _isDragOver ? DragBorder     : NormalBorder;
    public IBrush DropZoneBackgroundBrush => _isDragOver ? DragBackground : NormalBackground;

    // ── Commands ──────────────────────────────────────────────────────
    public ICommand BrowseFolderCommand { get; }
    public ICommand BrowseImageCommand  { get; }
    public ICommand GenerateCommand     { get; }
    public ICommand OpenOutputCommand   { get; }
    public ICommand HowToUseCommand     { get; }

    public MainViewModel()
    {
        BrowseFolderCommand = new AsyncRelayCommand(BrowseFolderAsync);
        BrowseImageCommand  = new AsyncRelayCommand(BrowseImageAsync);
        GenerateCommand     = new AsyncRelayCommand(GenerateAsync, () => HasFolder && !IsBusy);
        OpenOutputCommand   = new RelayCommand(OpenOutput, () => HasOutput);
        HowToUseCommand     = new RelayCommand(ShowHelp);
    }

    public void SetWindow(Window window) => _window = window;

    public void SetFolder(string path)
    {
        SelectedFolderPath = path;
        HasOutput = false;
        StatusMessage = $"Ready — {path}";
        UpdateFileCount(path);
    }

    private void UpdateFileCount(string path)
    {
        try
        {
            var count = Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories).Count();
            FileCountMessage = $"{count:N0} files";
        }
        catch { FileCountMessage = ""; }
    }

    private async Task BrowseFolderAsync()
    {
        if (_window is null) return;
        var result = await _window.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title         = "Select folder to index",
            AllowMultiple = false,
        });
        if (result.Count > 0)
        {
            var path = result[0].TryGetLocalPath();
            if (path != null) SetFolder(path);
        }
    }

    private async Task BrowseImageAsync()
    {
        if (_window is null) return;
        var result = await _window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title         = "Select header image",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Image files")
                {
                    Patterns = ["*.jpg", "*.jpeg", "*.png", "*.webp", "*.gif"]
                }
            ]
        });
        if (result.Count > 0)
        {
            var path = result[0].TryGetLocalPath();
            if (path != null) HeaderImagePath = path;
        }
    }

    private async Task GenerateAsync()
    {
        if (!HasFolder) return;
        IsBusy = true;
        HasOutput = false;
        StatusMessage = "Scanning directory…";

        try
        {
            var options = new GeneratorOptions
            {
                FolderPath       = _selectedFolderPath,
                PageTitle        = _pageTitle,
                AuthorName       = _authorName,
                HeaderImagePath  = _headerImagePath,
                ImageStyle       = _selectedImageStyle,
                ImageHeight      = int.TryParse(_imageHeight, out var h) ? h : 220,
                ImageOverlayText = _imageOverlayText,
            };

            var progress = new Progress<string>(msg => StatusMessage = msg);
            await Task.Run(() => HtmlGenerator.Generate(options, progress));

            HasOutput = true;
            StatusMessage = "index.html generated successfully.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void OpenOutput()
    {
        var outputPath = Path.Combine(_selectedFolderPath, "index.html");
        if (File.Exists(outputPath))
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName  = outputPath,
                UseShellExecute = true
            });
    }

    private void ShowHelp()
    {
        if (_window is null) return;
        new HelpWindow { DataContext = null }.ShowDialog(_window);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
