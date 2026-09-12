using Avalonia.Controls;
using Avalonia.Platform.Storage;
using AlgorithmAnalysis.ViewModels;

namespace AlgorithmAnalysis.Views;

public partial class MainWindow : Window
{
    private MainViewModel? _vm;

    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (_vm != null)
            _vm.ExportRequested -= OnExportRequested;

        _vm = DataContext as MainViewModel;

        if (_vm != null)
            _vm.ExportRequested += OnExportRequested;
    }

    private async void OnExportRequested(object? sender, EventArgs e)
    {
        if (_vm == null) return;

        var topLevel = GetTopLevel(this);
        if (topLevel == null) return;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Сохранить график",
            DefaultExtension = "png",
            SuggestedFileName = $"chart_{_vm.SelectedAlgorithm?.Name ?? "algorithm"}",
            FileTypeChoices =
            [
                new FilePickerFileType("PNG Image") { Patterns = ["*.png"] },
            ]
        });

        if (file != null)
        {
            var path = file.Path.LocalPath;
            _vm.SaveChartToFile(path);
            _vm.StatusText = $"График сохранён: {path}";
        }
    }
}