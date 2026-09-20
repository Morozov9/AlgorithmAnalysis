using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
    private readonly ReportService _reportService = new();
    private readonly DatabaseService _databaseService = new();
    private readonly Matrix3DReportService _matrix3DReportService = new();
    private CancellationTokenSource? _cts;

    private ScottPlot.Plot? _currentPlot;

    /// <summary>Список всех доступных алгоритмов (с чекбоксами)</summary>
    public ObservableCollection<SelectableAlgorithm> Algorithms { get; }

    public MainViewModel()
    {
        var items = AlgorithmRegistry.GetAllAlgorithms()
                                     .Select(a => new SelectableAlgorithm(a))
                                     .ToList();

        Algorithms = new ObservableCollection<SelectableAlgorithm>(items);

        // Подписка на изменение выбора каждого алгоритма — обновляет CanExecute у RunAllSelected
        foreach (var item in Algorithms)
        {
            item.SelectionChanged += (_, _) =>
            {
                RunAllSelectedCommand.NotifyCanExecuteChanged();
            };
        }
    }

    /// <summary>Выбранный алгоритм (обёртка с чекбоксом)</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunBenchmarkCommand))]
    public partial SelectableAlgorithm? SelectedAlgorithm { get; set; }

    /// <summary>Выбран ли матричный алгоритм (для показа кнопки 3D-анализа)</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunMatrix3DAnalysisCommand))]
    public partial bool IsMatrixAlgorithmSelected { get; set; }

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

    /// <summary>Текст координат при наведении</summary>
    [ObservableProperty]
    public partial string HoverCoordinatesText { get; set; } = "Наведите курсор на график для просмотра координат";

    /// <summary>Идёт ли бенчмарк или генерация отчёта</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunBenchmarkCommand))]
    [NotifyCanExecuteChangedFor(nameof(RunAllSelectedCommand))]
    [NotifyCanExecuteChangedFor(nameof(RunMatrix3DAnalysisCommand))]
    [NotifyCanExecuteChangedFor(nameof(GenerateReportCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
    public partial bool IsRunning { get; set; }

    /// <summary>Рекомендуемые размеры для выбранного алгоритма</summary>
    [ObservableProperty]
    public partial string SizesText { get; set; } = string.Empty;

    /// <summary>Принудительный пересчёт (игнорировать кэш)</summary>
    [ObservableProperty]
    public partial bool ForceRecalculate { get; set; } = false;

    /// <summary>Текст о состоянии кэша</summary>
    [ObservableProperty]
    public partial string CacheStatusText { get; set; } = string.Empty;

    /// <summary>Событие обновления данных графика</summary>
    public event EventHandler<BenchmarkResult>? BenchmarkPlotUpdated;

    partial void OnSelectedAlgorithmChanged(SelectableAlgorithm? value)
    {
        IsMatrixAlgorithmSelected = value?.Algorithm is AlgorithmAnalysis.Models.Algorithms.MatrixAlgorithm;

        if (value == null) return;

        var algo = value.Algorithm;
        var sizes = AlgorithmRegistry.GetRecommendedSizes(algo);
        SizesText = string.Join(", ", sizes);

        CacheStatusText = string.Empty;
        _ = Task.Run(async () =>
        {
            try
            {
                bool hasCached = await _databaseService.HasCachedDataAsync(algo.Name, sizes, algo.MeasureSteps);
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    CacheStatusText = hasCached
                        ? $"Кэш: данные для {sizes.Length} точек уже сохранены в БД"
                        : "Кэша нет, будет запущен полный бенчмарк";
                });
            }
            catch (Exception ex)
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    CacheStatusText = $"БД недоступна: {ex.Message}";
                });
            }
        });

        RunAllSelectedCommand.NotifyCanExecuteChanged();
    }

    // ═══════════════════════════════════════════════════════════════════
    //  ОДИНОЧНЫЙ ЗАПУСК
    // ═══════════════════════════════════════════════════════════════════

    private bool CanRunBenchmark() => SelectedAlgorithm != null && !IsRunning;

    [RelayCommand(CanExecute = nameof(CanRunBenchmark))]
    private async Task RunBenchmark(CancellationToken cancellationToken)
    {
        if (SelectedAlgorithm == null) return;

        var algo = SelectedAlgorithm.Algorithm;

        IsRunning = true;
        Progress = 0;
        Results.Clear();
        CacheStatusText = string.Empty;

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        try
        {
            var sizes = ParseSizes(SizesText);
            if (sizes.Length == 0)
            {
                StatusText = "Укажите размеры данных (через запятую или в виде 1..2000:50)";
                return;
            }

            BenchmarkResult benchmarkResult;
            bool loadedFromCache = false;

            if (!ForceRecalculate && await _databaseService.HasCachedDataAsync(algo.Name, sizes, algo.MeasureSteps))
            {
                StatusText = $"Загрузка из кэша: {algo.Name}...";

                var cached = await _databaseService.LoadCachedResultAsync(
                    algo.Name,
                    algo.TheoreticalComplexityLabel,
                    sizes,
                    n => algo.TheoreticalComplexity(n),
                    algo.MeasureSteps);

                if (cached != null)
                {
                    benchmarkResult = cached;
                    loadedFromCache = true;
                    Progress = 100;
                    CacheStatusText = $"Загружено из кэша — {benchmarkResult.Results.Count} точек";
                }
                else
                {
                    benchmarkResult = await RunFullBenchmarkAsync(algo, sizes, _cts.Token);
                }
            }
            else
            {
                if (ForceRecalculate)
                {
                    await _databaseService.DeleteRunsAsync(algo.Name);
                    CacheStatusText = "Старый кэш удален, запускаем заново...";
                }

                benchmarkResult = await RunFullBenchmarkAsync(algo, sizes, _cts.Token);
            }

            CurrentBenchmark = benchmarkResult;

            foreach (var r in benchmarkResult.Results)
                Results.Add(r);

            RenderChart(benchmarkResult);

            StatusText = loadedFromCache
                ? $"Из кэша: {algo.Name} | c = {benchmarkResult.FittedCoefficient:E3} | MSE = {benchmarkResult.MSE:E3}"
                : $"Готово: {algo.Name} | c = {benchmarkResult.FittedCoefficient:E3} | MSE = {benchmarkResult.MSE:E3}";
        }
        catch (OperationCanceledException)
        {
            StatusText = "Отменено";
            CacheStatusText = string.Empty;
        }
        catch (NotImplementedException ex)
        {
            StatusText = $"Алгоритм не реализован: {ex.Message}";
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

    // ═══════════════════════════════════════════════════════════════════
    //  ОЧЕРЕДЬ: ЗАПУСТИТЬ ВСЕ ВЫБРАННЫЕ
    // ═══════════════════════════════════════════════════════════════════

    private bool CanRunAllSelected() => !IsRunning && Algorithms.Any(a => a.IsSelected);

    [RelayCommand(CanExecute = nameof(CanRunAllSelected))]
    private async Task RunAllSelected(CancellationToken cancellationToken)
    {
        var selected = Algorithms.Where(a => a.IsSelected).Select(a => a.Algorithm).ToList();
        if (selected.Count == 0)
        {
            StatusText = "Не выбрано ни одного алгоритма";
            return;
        }

        IsRunning = true;
        Progress = 0;
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        int total = selected.Count;
        int done = 0;

        try
        {
            foreach (var algo in selected)
            {
                if (_cts.Token.IsCancellationRequested) break;

                done++;
                StatusText = $"[{done}/{total}] {algo.Name}...";

                // Синхронизируем SelectedAlgorithm с текущим алгоритмом в очереди
                var wrapper = Algorithms.FirstOrDefault(a => a.Algorithm == algo);
                if (wrapper != null)
                    SelectedAlgorithm = wrapper;

                var sizes = ParseSizes(SizesText);
                if (sizes.Length == 0)
                {
                    StatusText = $"[{done}/{total}] Некорректные размеры для {algo.Name}";
                    continue;
                }

                try
                {
                    var result = await _benchmarkService.RunBenchmarkAsync(
                        algo, sizes,
                        progress => Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                        {
                            Progress = (int)(((done - 1 + progress) / total) * 100);
                        }),
                        _cts.Token);

                    CurrentBenchmark = result;
                    Results.Clear();
                    foreach (var r in result.Results) Results.Add(r);
                    RenderChart(result);

                    StatusText = $"[{done}/{total}] {algo.Name} | c={result.FittedCoefficient:E2} | MSE={result.MSE:E2}";
                }
                catch (Exception ex)
                {
                    StatusText = $"Ошибка [{done}/{total}] {algo.Name}: {ex.Message}";
                }
            }

            StatusText = $"Готово! Обработано {done}/{total} алгоритмов";
            Progress = 100;
        }
        catch (OperationCanceledException)
        {
            StatusText = $"Отменено на {done}/{total}";
        }
        finally
        {
            IsRunning = false;
        }
    }

    private async Task<BenchmarkResult> RunFullBenchmarkAsync(
        AbstractAlgorithm algo, int[] sizes, CancellationToken token)
    {
        StatusText = $"Запуск: {algo.Name}...";

        var result = await _benchmarkService.RunBenchmarkAsync(
            algo,
            sizes,
            progress =>
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    Progress = (int)(progress * 100);
                });
            },
            token
        );

        CacheStatusText = $"Результаты сохранены в БД — {result.Results.Count} точек";
        return result;
    }

    // ═══════════════════════════════════════════════════════════════════
    //  3D-АНАЛИЗ МАТРИЦ T(n, m)
    // ═══════════════════════════════════════════════════════════════════

    private bool CanRunMatrix3DAnalysis() => !IsRunning && IsMatrixAlgorithmSelected;

    [RelayCommand(CanExecute = nameof(CanRunMatrix3DAnalysis))]
    private async Task RunMatrix3DAnalysis(CancellationToken cancellationToken)
    {
        IsRunning = true;
        Progress = 0;
        StatusText = "Запуск 3D-анализа матричного умножения A(n×m) × B(m×k)...";

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        try
        {
            string htmlPath = await _matrix3DReportService.RunAndGenerateReportAsync(
                (status, progress) =>
                {
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        StatusText = status;
                        Progress = (int)(progress * 100);
                    });
                },
                _cts.Token
            );

            StatusText = $"3D-отчёт успешно создан: {Path.GetFileName(htmlPath)}";

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = htmlPath,
                    UseShellExecute = true
                });
            }
            catch { }
        }
        catch (OperationCanceledException)
        {
            StatusText = "3D-анализ отменён";
        }
        catch (Exception ex)
        {
            StatusText = $"Ошибка 3D-анализа: {ex.Message}";
        }
        finally
        {
            IsRunning = false;
            Progress = 100;
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  ГЕНЕРАЦИЯ ОТЧЁТА
    // ═══════════════════════════════════════════════════════════════════

    private bool CanGenerateReport() => !IsRunning;

    [RelayCommand(CanExecute = nameof(CanGenerateReport))]
    private async Task GenerateReport(CancellationToken cancellationToken)
    {
        IsRunning = true;
        Progress = 0;
        StatusText = "Запуск генерации полного отчёта...";

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        try
        {
            string htmlPath = await _reportService.GenerateFullReportAsync(
                (status, progress) =>
                {
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        StatusText = status;
                        Progress = (int)(progress * 100);
                    });
                },
                _cts.Token
            );

            StatusText = $"Отчёт успешно создан: {Path.GetFileName(htmlPath)}";

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = htmlPath,
                    UseShellExecute = true
                });
            }
            catch { }
        }
        catch (OperationCanceledException)
        {
            StatusText = "Генерация отчёта отменена";
        }
        catch (Exception ex)
        {
            StatusText = $"Ошибка при генерации отчёта: {ex.Message}";
        }
        finally
        {
            IsRunning = false;
            Progress = 100;
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  ВСПОМОГАТЕЛЬНОЕ
    // ═══════════════════════════════════════════════════════════════════

    public void UpdateHoverCoordinates(double x, double y)
    {
        bool isSteps = CurrentBenchmark?.IsStepBased == true;
        string unit = isSteps ? "шагов" : "мс";
        string valPrefix = isSteps ? "S" : "T";

        if (Results.Count == 0)
        {
            HoverCoordinatesText = isSteps
                ? $"Курсор: n = {x:F0}, S = {y:F0} {unit}"
                : $"Курсор: n = {x:F0}, T = {y:F4} {unit}";
            return;
        }

        var nearest = Results.OrderBy(r => Math.Abs(r.N - x)).FirstOrDefault();
        if (nearest != null)
        {
            if (isSteps)
            {
                HoverCoordinatesText = $"Курсор: n = {x:F0}, S = {y:F0} {unit} | Ближайшая точка: n = {nearest.N} → Sэксп = {nearest.StepCount} {unit} (Sтеор = {nearest.TheoreticalTimeMs:F1} {unit})";
            }
            else
            {
                HoverCoordinatesText = $"Курсор: n = {x:F0}, T = {y:F4} {unit} | Ближайшая точка: n = {nearest.N} → Tэксп = {nearest.AverageTimeMs:F4} {unit} (Tтеор = {nearest.TheoreticalTimeMs:F4} {unit})";
            }
        }
        else
        {
            HoverCoordinatesText = $"Курсор: n = {x:F0}, {valPrefix} = {(isSteps ? y.ToString("F0") : y.ToString("F4"))} {unit}";
        }
    }

    private void RenderChart(BenchmarkResult benchmark)
    {
        var plt = new ScottPlot.Plot();

        var results = benchmark.Results;
        if (results.Count == 0) return;

        double[] xs = results.Select(r => (double)r.N).ToArray();
        double[] ysExperimental = benchmark.IsStepBased
            ? results.Select(r => (double)r.StepCount).ToArray()
            : results.Select(r => r.AverageTimeMs).ToArray();
        double[] ysTheoretical = results.Select(r => r.TheoreticalTimeMs).ToArray();

        var expPlot = plt.Add.ScatterLine(xs, ysExperimental);
        expPlot.LegendText = benchmark.IsStepBased ? "Эксперимент (шаги)" : "Эксперимент";
        expPlot.Color = ScottPlot.Color.FromHex("#2196F3");
        expPlot.LineWidth = 2;

        var theoPlot = plt.Add.ScatterLine(xs, ysTheoretical);
        theoPlot.LegendText = $"Теория {benchmark.ComplexityLabel}";
        theoPlot.Color = ScottPlot.Color.FromHex("#F44336");
        theoPlot.LineWidth = 2;
        theoPlot.LinePattern = LinePattern.Dashed;

        plt.Title(benchmark.AlgorithmName);
        plt.XLabel(benchmark.XAxisTitle);
        plt.YLabel(benchmark.YAxisTitle);
        plt.ShowLegend(Alignment.UpperLeft);

        _currentPlot = plt;

        byte[] pngBytes = plt.GetImageBytes(1100, 450, ImageFormat.Png);
        using var ms = new MemoryStream(pngBytes);
        ChartImageSource = new Bitmap(ms);

        BenchmarkPlotUpdated?.Invoke(this, benchmark);
    }

    public static int[] ParseSizes(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return [];

        var result = new HashSet<int>();
        var tokens = text.Split([',', ';', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries);

        foreach (var rawToken in tokens)
        {
            var token = rawToken.Trim();
            if (string.IsNullOrEmpty(token)) continue;

            if (token.Contains(".."))
            {
                string[] rangeParts = token.Split(':');
                string rangeSpan = rangeParts[0];
                int step = 50;

                if (rangeParts.Length > 1 && int.TryParse(rangeParts[1].Trim(), out int parsedStep) && parsedStep > 0)
                {
                    step = parsedStep;
                }

                string[] bounds = rangeSpan.Split([".."], StringSplitOptions.RemoveEmptyEntries);
                if (bounds.Length == 2 &&
                    int.TryParse(bounds[0].Trim(), out int start) &&
                    int.TryParse(bounds[1].Trim(), out int end) &&
                    start <= end)
                {
                    for (int n = start; n <= end; n += step)
                    {
                        result.Add(n);
                    }
                    if (!result.Contains(end))
                    {
                        result.Add(end);
                    }
                    continue;
                }
            }

            if (int.TryParse(token, out int singleVal) && singleVal > 0)
            {
                result.Add(singleVal);
            }
            else
            {
                var subTokens = token.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                foreach (var st in subTokens)
                {
                    if (int.TryParse(st, out int sv) && sv > 0)
                    {
                        result.Add(sv);
                    }
                }
            }
        }

        return result.OrderBy(n => n).ToArray();
    }

    [RelayCommand]
    private void SetLabSizes()
    {
        SizesText = "1..2000:50";
    }

    [RelayCommand]
    private void SetRecommendedSizes()
    {
        if (SelectedAlgorithm != null)
        {
            var sizes = AlgorithmRegistry.GetRecommendedSizes(SelectedAlgorithm.Algorithm);
            SizesText = string.Join(", ", sizes);
        }
    }

    private bool CanCancel() => IsRunning;

    [RelayCommand(CanExecute = nameof(CanCancel))]
    private void Cancel()
    {
        _cts?.Cancel();
        StatusText = "Отмена...";
    }

    public event EventHandler? ExportRequested;

    [RelayCommand]
    private void ExportChart()
    {
        ExportRequested?.Invoke(this, EventArgs.Empty);
    }

    public void SaveChartToFile(string path)
    {
        _currentPlot?.SavePng(path, 1200, 700);
    }
}