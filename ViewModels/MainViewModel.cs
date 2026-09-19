using System.Collections.ObjectModel;
using System.Diagnostics;
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
    private readonly ReportService _reportService = new();
    private readonly DatabaseService _databaseService = new();
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

    /// <summary>Изображение графика для отображения в Image (обратная совместимость)</summary>
    [ObservableProperty]
    public partial Bitmap? ChartImageSource { get; set; }

    /// <summary>Прогресс выполнения (0..100)</summary>
    [ObservableProperty]
    public partial int Progress { get; set; }

    /// <summary>Текст статуса</summary>
    [ObservableProperty]
    public partial string StatusText { get; set; } = "Выберите алгоритм и нажмите «Запустить»";

    /// <summary>Текст координат и ближайшей точки при наведении курсора на график</summary>
    [ObservableProperty]
    public partial string HoverCoordinatesText { get; set; } = "Наведите курсор на график для просмотра координат";

    /// <summary>Идёт ли бенчмарк или генерация отчёта</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunBenchmarkCommand))]
    [NotifyCanExecuteChangedFor(nameof(GenerateReportCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
    public partial bool IsRunning { get; set; }

    /// <summary>Рекомендуемые размеры для выбранного алгоритма</summary>
    [ObservableProperty]
    public partial string SizesText { get; set; } = string.Empty;

    /// <summary>
    /// Если true — игнорировать кэш и запустить бенчмарк заново, даже если данные уже есть в БД.
    /// Если false (по умолчанию) — загружать результаты из кэша, если они там есть.
    /// </summary>
    [ObservableProperty]
    public partial bool ForceRecalculate { get; set; } = false;

    /// <summary>Текст о состоянии кэша — показывается под чекбоксом</summary>
    [ObservableProperty]
    public partial string CacheStatusText { get; set; } = string.Empty;

    /// <summary>Событие обновления данных графика для интерактивного AvaPlot</summary>
    public event EventHandler<BenchmarkResult>? BenchmarkPlotUpdated;

    partial void OnSelectedAlgorithmChanged(AbstractAlgorithm? value)
    {
        if (value == null) return;

        var sizes = AlgorithmRegistry.GetRecommendedSizes(value);
        SizesText = string.Join(", ", sizes);

        // Проверяем кэш в фоне и обновляем подсказку
        CacheStatusText = string.Empty;
        _ = Task.Run(async () =>
        {
            bool hasCached = await _databaseService.HasCachedDataAsync(value.Name, sizes);
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                CacheStatusText = hasCached
                    ? $"✅ Кэш: данные для {sizes.Length} точек уже сохранены в БД"
                    : "ℹ️ Кэша нет — будет запущен полный бенчмарк";
            });
        });
    }

    private bool CanRunBenchmark() => SelectedAlgorithm != null && !IsRunning;

    [RelayCommand(CanExecute = nameof(CanRunBenchmark))]
    private async Task RunBenchmark(CancellationToken cancellationToken)
    {
        if (SelectedAlgorithm == null) return;

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
                StatusText = "⚠ Укажите размеры данных (через запятую или в виде 1..2000:50)";
                return;
            }

            var algo = SelectedAlgorithm;
            BenchmarkResult benchmarkResult;
            bool loadedFromCache = false;

            // ── Пробуем загрузить из кэша ───────────────────────────────────────
            if (!ForceRecalculate && await _databaseService.HasCachedDataAsync(algo.Name, sizes))
            {
                StatusText = $"⏳ Загрузка из кэша: {algo.Name}...";

                var cached = await _databaseService.LoadCachedResultAsync(
                    algo.Name,
                    algo.TheoreticalComplexityLabel,
                    sizes,
                    n => algo.TheoreticalComplexity(n));

                if (cached != null)
                {
                    benchmarkResult = cached;
                    loadedFromCache = true;
                    Progress = 100;
                    CacheStatusText = $"✅ Загружено из кэша — {benchmarkResult.Results.Count} точек";
                }
                else
                {
                    // Кэш есть в БД, но данные не удалось восстановить — запускаем полный бенчмарк
                    benchmarkResult = await RunFullBenchmarkAsync(algo, sizes, _cts.Token);
                }
            }
            else
            {
                // ── Принудительный пересчёт или кэша нет ───────────────────────
                if (ForceRecalculate)
                {
                    await _databaseService.DeleteRunsAsync(algo.Name);
                    CacheStatusText = "🗑 Старый кэш удалён, запускаем заново...";
                }

                benchmarkResult = await RunFullBenchmarkAsync(algo, sizes, _cts.Token);
            }

            CurrentBenchmark = benchmarkResult;

            foreach (var r in benchmarkResult.Results)
                Results.Add(r);

            // Строим график
            RenderChart(benchmarkResult);

            StatusText = loadedFromCache
                ? $"Из кэша: {algo.Name} | Коэффициент c = {benchmarkResult.FittedCoefficient:E3}"
                : $"Готово: {algo.Name} | Коэффициент c = {benchmarkResult.FittedCoefficient:E3}";
        }
        catch (OperationCanceledException)
        {
            StatusText = "Отменено";
            CacheStatusText = string.Empty;
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
    /// Вспомогательный метод: запускает полный бенчмарк и обновляет статус кэша.
    /// </summary>
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

        CacheStatusText = $"💾 Результаты сохранены в БД — {result.Results.Count} точек";
        return result;
    }

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

            // Открываем созданный HTML-отчёт в браузере по умолчанию
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = htmlPath,
                    UseShellExecute = true
                });
            }
            catch
            {
                // Игнорируем ошибку запуска внешнего процесса
            }
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

    /// <summary>
    /// Обновляет отображение координат под курсором и находит ближайшую точку замера.
    /// </summary>
    public void UpdateHoverCoordinates(double x, double y)
    {
        if (Results.Count == 0)
        {
            HoverCoordinatesText = $"Курсор: n = {x:F0}, T = {y:F4} мс";
            return;
        }

        var nearest = Results.OrderBy(r => Math.Abs(r.N - x)).FirstOrDefault();
        if (nearest != null)
        {
            HoverCoordinatesText = $"Курсор: n = {x:F0}, T = {y:F4} мс | Ближайшая точка: n = {nearest.N} → Tэксп = {nearest.AverageTimeMs:F4} мс (Tтеор = {nearest.TheoreticalTimeMs:F4} мс)";
        }
        else
        {
            HoverCoordinatesText = $"Курсор: n = {x:F0}, T = {y:F4} мс";
        }
    }

    /// <summary>
    /// Рендерит ScottPlot график в Avalonia Bitmap и уведомляет интерактивный контрол.
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

        // Уведомляем интерактивный график AvaPlot
        BenchmarkPlotUpdated?.Invoke(this, benchmark);
    }

    /// <summary>
    /// Парсит строку размеров: поддерживает как числа через запятую/пробел,
    /// так и диапазоны вида start..end:step (например, 1..2000:50).
    /// </summary>
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
            var sizes = AlgorithmRegistry.GetRecommendedSizes(SelectedAlgorithm);
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
