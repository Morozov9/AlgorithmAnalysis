using System.Collections.ObjectModel;
using System.IO;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AlgorithmAnalysis.Models;
using AlgorithmAnalysis.Services;
using ScottPlot;

namespace AlgorithmAnalysis.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly BenchmarkService _benchmarkService = new();
    private CancellationTokenSource? _cts;

    // Храним ScottPlot.Plot для экспорта в файл
    private ScottPlot.Plot? _currentPlot;

    /// <summary>Список всех доступных алгоритмов</summary>
    public ObservableCollection<AbstractAlgorithm> Algorithms { get; } = new(AlgorithmRegistry.GetAllAlgorithms());

    /// <summary>Выбранный алгоритм</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunBenchmarkCommand))]
    public partial AbstractAlgorithm? SelectedAlgorithm { get; set; }

    /// <summary>Результаты экспериментов (для DataGrid)</summary>
    public ObservableCollection<ExperimentResult> Results { get; } = [];

    /// <summary>Текущий результат бенчмарка</summary>
    [ObservableProperty]
    public partial BenchmarkResult? CurrentBenchmark { get; set; }

    /// <summary>Изображение графика для отображения в Image</summary>
    [ObservableProperty]
    public partial Bitmap? ChartImageSource { get; set; }

    /// <summary>Прогресс выполнения (0..100)</summary>
    [ObservableProperty]
    public partial int Progress { get; set; }

    /// <summary>Текст статуса</summary>
    [ObservableProperty]
    public partial string StatusText { get; set; } = "Выберите алгоритм и нажмите «Запустить»";

    /// <summary>Идёт ли бенчмарк</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunBenchmarkCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
    public partial bool IsRunning { get; set; }

    /// <summary>Рекомендуемые размеры для выбранного алгоритма</summary>
    [ObservableProperty]
    public partial string SizesText { get; set; } = string.Empty;

    partial void OnSelectedAlgorithmChanged(AbstractAlgorithm? value)
    {
        if (value != null)
        {
            var sizes = AlgorithmRegistry.GetRecommendedSizes(value);
            SizesText = string.Join(", ", sizes);
        }
    }

    private bool CanRunBenchmark() => SelectedAlgorithm != null && !IsRunning;

    [RelayCommand(CanExecute = nameof(CanRunBenchmark))]
    private async Task RunBenchmark(CancellationToken cancellationToken)
    {
        if (SelectedAlgorithm == null) return;

        IsRunning = true;
        Progress = 0;
        Results.Clear();
        StatusText = $"Запуск: {SelectedAlgorithm.Name}...";

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        try
        {
            var sizes = ParseSizes(SizesText);
            if (sizes.Length == 0)
            {
                StatusText = "⚠ Укажите размеры данных (через запятую)";
                return;
            }

            var benchmarkResult = await _benchmarkService.RunBenchmarkAsync(
                SelectedAlgorithm,
                sizes,
                progress =>
                {
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        Progress = (int)(progress * 100);
                    });
                },
                _cts.Token
            );

            CurrentBenchmark = benchmarkResult;

            foreach (var r in benchmarkResult.Results)
            {
                Results.Add(r);
            }

            // Строим график
            RenderChart(benchmarkResult);

            StatusText = $"Готово: {SelectedAlgorithm.Name} | " +
                         $"Коэффициент c = {benchmarkResult.FittedCoefficient:E3}";
        }
        catch (OperationCanceledException)
        {
            StatusText = "Отменено";
        }
        catch (NotImplementedException ex)
        {
            StatusText = $"⚠ Алгоритм не реализован: {ex.Message}";
        }
        catch (Exception ex)
        {
            StatusText = $"Ошибка: {ex.Message}";
        }
        finally
        {
            IsRunning = false;
            Progress = 100;
        }
    }

    /// <summary>
    /// Рендерит ScottPlot график в Avalonia Bitmap.
    /// </summary>
    private void RenderChart(BenchmarkResult benchmark)
    {
        var plt = new ScottPlot.Plot();

        var results = benchmark.Results;
        if (results.Count == 0) return;

        double[] xs = results.Select(r => (double)r.N).ToArray();
        double[] ysExperimental = results.Select(r => r.AverageTimeMs).ToArray();
        double[] ysTheoretical = results.Select(r => r.TheoreticalTimeMs).ToArray();

        // Экспериментальная кривая (синяя)
        var expPlot = plt.Add.ScatterLine(xs, ysExperimental);
        expPlot.LegendText = "Эксперимент";
        expPlot.Color = ScottPlot.Color.FromHex("#2196F3");
        expPlot.LineWidth = 2;

        // Теоретическая кривая (красная, пунктир)
        var theoPlot = plt.Add.ScatterLine(xs, ysTheoretical);
        theoPlot.LegendText = $"Теория {benchmark.ComplexityLabel}";
        theoPlot.Color = ScottPlot.Color.FromHex("#F44336");
        theoPlot.LineWidth = 2;
        theoPlot.LinePattern = LinePattern.Dashed;

        plt.Title(benchmark.AlgorithmName);
        plt.XLabel("Размерность n");
        plt.YLabel("Время (мс)");
        plt.ShowLegend(Alignment.UpperLeft);

        _currentPlot = plt;

        // Рендерим в PNG → MemoryStream → Avalonia Bitmap
        byte[] pngBytes = plt.GetImageBytes(1100, 450, ImageFormat.Png);
        using var ms = new MemoryStream(pngBytes);
        ChartImageSource = new Bitmap(ms);
    }

    private static int[] ParseSizes(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return [];

        return text.Split([',', ' ', ';', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries)
                   .Select(s => s.Trim())
                   .Where(s => int.TryParse(s, out _))
                   .Select(int.Parse)
                   .OrderBy(n => n)
                   .ToArray();
    }

    private bool CanCancel() => IsRunning;

    [RelayCommand(CanExecute = nameof(CanCancel))]
    private void Cancel()
    {
        _cts?.Cancel();
        StatusText = "Отмена...";
    }

    /// <summary>Событие для запроса экспорта (обрабатывается во View для SaveFileDialog)</summary>
    public event EventHandler? ExportRequested;

    [RelayCommand]
    private void ExportChart()
    {
        ExportRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Сохраняет текущий график в файл</summary>
    public void SaveChartToFile(string path)
    {
        _currentPlot?.SavePng(path, 1200, 700);
    }
}
