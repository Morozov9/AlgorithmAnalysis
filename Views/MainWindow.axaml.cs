using System;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using AlgorithmAnalysis.ViewModels;
using AlgorithmAnalysis.Models;
using ScottPlot;

namespace AlgorithmAnalysis.Views;

public partial class MainWindow : Window
{
    private MainViewModel? _vm;
    private ScottPlot.Plot? _currentPlot;
    private bool _isPanning;
    private Point? _lastPanPoint;

    public MainWindow()
    {
        InitializeComponent();

        ChartContainer.PointerWheelChanged += OnChartPointerWheelChanged;
        ChartContainer.PointerPressed += OnChartPointerPressed;
        ChartContainer.PointerMoved += OnChartPointerMoved;
        ChartContainer.PointerReleased += OnChartPointerReleased;
        ChartContainer.DoubleTapped += OnChartDoubleTapped;
        ChartContainer.SizeChanged += (s, e) => RenderPlot();

        Loaded += (s, e) => InitializePlaceholderPlot();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (_vm != null)
        {
            _vm.ExportRequested -= OnExportRequested;
            _vm.BenchmarkPlotUpdated -= OnBenchmarkPlotUpdated;
        }

        _vm = DataContext as MainViewModel;

        if (_vm != null)
        {
            _vm.ExportRequested += OnExportRequested;
            _vm.BenchmarkPlotUpdated += OnBenchmarkPlotUpdated;
        }
    }

    private void InitializePlaceholderPlot()
    {
        if (_currentPlot != null) return;

        var plt = new ScottPlot.Plot();
        ApplyTheme(plt);
        plt.Title("Выберите алгоритм и нажмите «Запустить анализ»");
        plt.XLabel("Размерность входных данных n");
        plt.YLabel("Время выполнения (мс)");
        plt.Axes.SetLimits(0, 2000, 0, 10);

        _currentPlot = plt;
        RenderPlot();
    }

    private void ApplyTheme(ScottPlot.Plot plt)
    {
        bool isDark = ActualThemeVariant == ThemeVariant.Dark;

        if (isDark)
        {
            plt.FigureBackground.Color = ScottPlot.Color.FromHex("#1E1E1E");
            plt.DataBackground.Color = ScottPlot.Color.FromHex("#252526");
            plt.Axes.Color(ScottPlot.Color.FromHex("#D4D4D4"));
            plt.Grid.MajorLineColor = ScottPlot.Color.FromHex("#3A3A3D");
            plt.Legend.BackgroundColor = ScottPlot.Color.FromHex("#2D2D30");
            plt.Legend.FontColor = ScottPlot.Color.FromHex("#E0E0E0");
            plt.Legend.OutlineColor = ScottPlot.Color.FromHex("#454545");
        }
        else
        {
            plt.FigureBackground.Color = ScottPlot.Color.FromHex("#FFFFFF");
            plt.DataBackground.Color = ScottPlot.Color.FromHex("#FAFAFA");
            plt.Axes.Color(ScottPlot.Color.FromHex("#333333"));
            plt.Grid.MajorLineColor = ScottPlot.Color.FromHex("#E0E0E0");
            plt.Legend.BackgroundColor = ScottPlot.Color.FromHex("#FFFFFF");
            plt.Legend.FontColor = ScottPlot.Color.FromHex("#333333");
            plt.Legend.OutlineColor = ScottPlot.Color.FromHex("#CCCCCC");
        }
    }

    private void OnBenchmarkPlotUpdated(object? sender, BenchmarkResult benchmark)
    {
        var plt = new ScottPlot.Plot();
        ApplyTheme(plt);

        var results = benchmark.Results;

        if (results.Count > 0)
        {
            double[] xs = results.Select(r => (double)r.N).ToArray();
            double[] ysExp = benchmark.IsStepBased
                ? results.Select(r => (double)r.StepCount).ToArray()
                : results.Select(r => r.AverageTimeMs).ToArray();
            double[] ysTheo = results.Select(r => r.TheoreticalTimeMs).ToArray();

            var expPlot = plt.Add.ScatterLine(xs, ysExp);
            expPlot.LegendText = benchmark.IsStepBased ? "Эксперимент (шаги)" : "Эксперимент";
            expPlot.Color = ScottPlot.Color.FromHex("#29B6F6");
            expPlot.LineWidth = 2.5f;

            var theoPlot = plt.Add.ScatterLine(xs, ysTheo);
            theoPlot.LegendText = $"Теория {benchmark.ComplexityLabel}";
            theoPlot.Color = ScottPlot.Color.FromHex("#EF5350");
            theoPlot.LineWidth = 2;
            theoPlot.LinePattern = LinePattern.Dashed;

            plt.Title($"{benchmark.AlgorithmName}  |  c = {benchmark.FittedCoefficient:E2}  |  MSE = {benchmark.MSE:E2}");
            plt.XLabel(benchmark.XAxisTitle);
            plt.YLabel(benchmark.YAxisTitle);
            plt.ShowLegend(Alignment.UpperLeft);

            if (benchmark.IsStepBased)
            {
                double maxY = Math.Max(ysExp.Max(), ysTheo.Max()) * 1.1;
                if (maxY <= 0) maxY = 10;
                double maxX = xs.Max() * 1.05;
                plt.Axes.SetLimits(0, maxX, 0, maxY);
            }
            else
            {
                // Отсекаем выбросы: ограничиваем ось Y по 99-му перцентилю
                var sortedY = ysExp
                    .Where(y => !double.IsNaN(y) && !double.IsInfinity(y))
                    .OrderBy(y => y)
                    .ToArray();

                double maxY;
                if (sortedY.Length > 0)
                {
                    int idx99 = (int)(sortedY.Length * 0.99);
                    if (idx99 >= sortedY.Length) idx99 = sortedY.Length - 1;
                    double p99 = sortedY[idx99];

                    double actualMax = sortedY[^1];
                    maxY = p99 * 1.2;
                    if (maxY < actualMax * 0.3)
                    {
                        maxY = actualMax * 0.5;
                    }
                }
                else
                {
                    maxY = 1;
                }

                double maxX = xs.Max() * 1.05;
                plt.Axes.SetLimits(0, maxX, 0, maxY);
            }
        }

        _currentPlot = plt;
        RenderPlot();
    }

    private void RenderPlot()
    {
        if (_currentPlot == null || _vm == null) return;

        int width = Math.Max(300, (int)ChartContainer.Bounds.Width);
        int height = Math.Max(200, (int)ChartContainer.Bounds.Height);

        byte[] pngBytes = _currentPlot.GetImageBytes(width, height, ImageFormat.Png);
        using var ms = new MemoryStream(pngBytes);
        _vm.ChartImageSource = new Bitmap(ms);
    }

    private void OnResetViewClick(object? sender, RoutedEventArgs e)
    {
        ResetPlotView();
    }

    private void OnChartDoubleTapped(object? sender, TappedEventArgs e)
    {
        ResetPlotView();
    }

    private void ResetPlotView()
    {
        if (_currentPlot == null) return;
        _currentPlot.Axes.AutoScale();
        RenderPlot();
    }

    private void OnChartPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (_currentPlot == null) return;

        double factor = e.Delta.Y > 0 ? 1.25 : 0.8;
        var limits = _currentPlot.Axes.GetLimits();
        double spanX = limits.Right - limits.Left;
        double spanY = limits.Top - limits.Bottom;

        if (factor < 1.0 && (spanX > 50_000 || spanY > 10_000))
        {
            return;
        }

        var pos = e.GetPosition(ChartContainer);
        int w = Math.Max(100, (int)ChartContainer.Bounds.Width);
        int h = Math.Max(100, (int)ChartContainer.Bounds.Height);

        double fracX = Math.Clamp(pos.X / w, 0.0, 1.0);
        double fracY = Math.Clamp(1.0 - (pos.Y / h), 0.0, 1.0);

        double mouseX = limits.Left + fracX * spanX;
        double mouseY = limits.Bottom + fracY * spanY;

        double newSpanX = spanX / factor;
        double newSpanY = spanY / factor;

        double newLeft = mouseX - fracX * newSpanX;
        double newRight = newLeft + newSpanX;
        double newBottom = mouseY - fracY * newSpanY;
        double newTop = newBottom + newSpanY;

        if (newLeft < -spanX * 0.2)
        {
            double corr = -spanX * 0.2 - newLeft;
            newLeft += corr;
            newRight += corr;
        }
        if (newBottom < -spanY * 0.2)
        {
            double corr = -spanY * 0.2 - newBottom;
            newBottom += corr;
            newTop += corr;
        }

        _currentPlot.Axes.SetLimits(newLeft, newRight, newBottom, newTop);
        RenderPlot();
    }

    private void OnChartPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(ChartContainer).Properties.IsLeftButtonPressed)
        {
            _isPanning = true;
            _lastPanPoint = e.GetPosition(ChartContainer);
        }
    }

    private void OnChartPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _isPanning = false;
        _lastPanPoint = null;
    }

    private void OnChartPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_currentPlot == null || _vm == null) return;

        var pos = e.GetPosition(ChartContainer);
        int w = Math.Max(100, (int)ChartContainer.Bounds.Width);
        int h = Math.Max(100, (int)ChartContainer.Bounds.Height);

        if (_isPanning && _lastPanPoint.HasValue)
        {
            double dx = pos.X - _lastPanPoint.Value.X;
            double dy = pos.Y - _lastPanPoint.Value.Y;
            _lastPanPoint = pos;

            var limits = _currentPlot.Axes.GetLimits();
            double spanX = limits.Right - limits.Left;
            double spanY = limits.Top - limits.Bottom;

            double deltaX = -dx * (spanX / w);
            double deltaY = dy * (spanY / h);

            double newLeft = limits.Left + deltaX;
            double newRight = limits.Right + deltaX;
            double newBottom = limits.Bottom + deltaY;
            double newTop = limits.Top + deltaY;

            if (newLeft < -spanX)
            {
                double diff = -spanX - newLeft;
                newLeft += diff;
                newRight += diff;
            }
            if (newBottom < -spanY)
            {
                double diff = -spanY - newBottom;
                newBottom += diff;
                newTop += diff;
            }

            _currentPlot.Axes.SetLimits(newLeft, newRight, newBottom, newTop);
            RenderPlot();
            return;
        }

        var coords = _currentPlot.GetCoordinates(new Pixel((float)pos.X, (float)pos.Y));
        _vm.UpdateHoverCoordinates(coords.X, coords.Y);
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