using System.Diagnostics;
using System.IO;
using System.Text;
using AlgorithmAnalysis.Models;
using AlgorithmAnalysis.Models.Algorithms;
using ScottPlot;

namespace AlgorithmAnalysis.Services;

/// <summary>
/// Сервис автоматической генерации отчёта по лабораторной работе №1.
/// Запускает серию экспериментов для всех алгоритмов, строит графики (включая сводный),
/// и формирует готовые файлы отчёта (HTML для печати в PDF и Markdown).
/// </summary>
public class ReportService
{
    private readonly BenchmarkService _benchmarkService = new() { RunsPerSize = 5 };

    public async Task<string> GenerateFullReportAsync(
        Action<string, double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string reportsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports", $"Report_{timestamp}");
        string chartsDir = Path.Combine(reportsDir, "charts");

        Directory.CreateDirectory(chartsDir);

        var algorithms = AlgorithmRegistry.GetAllAlgorithms();
        var benchmarkResults = new List<(AbstractAlgorithm Algo, BenchmarkResult Result, string ChartPath)>();

        int total = algorithms.Count;

        for (int i = 0; i < total; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var algo = algorithms[i];

            progress?.Invoke($"Тестирование: {algo.Name} ({i + 1}/{total})", (double)i / (total + 1));

            int[] sizes = GetReportSizes(algo);

            var benchResult = await _benchmarkService.RunBenchmarkAsync(algo, sizes, cancellationToken: cancellationToken);

            string safeName = MakeSafeFileName(algo.Name);
            string chartFileName = $"chart_{i + 1:D2}_{safeName}.png";
            string chartPath = Path.Combine(chartsDir, chartFileName);

            SaveAlgorithmChart(algo, benchResult, chartPath);

            benchmarkResults.Add((algo, benchResult, Path.Combine("charts", chartFileName)));
        }

        // Сводный график сортировок
        progress?.Invoke("Построение сводного графика сортировок...", (double)total / (total + 1));
        string combinedSortChartName = "chart_combined_sorting.png";
        string combinedSortChartPath = Path.Combine(chartsDir, combinedSortChartName);
        SaveCombinedSortingChart(benchmarkResults.Where(b => b.Algo.Group == "Сортировки").ToList(), combinedSortChartPath);

        progress?.Invoke("Формирование HTML и Markdown отчётов...", 0.95);

        string htmlPath = Path.Combine(reportsDir, "report.html");
        string mdPath = Path.Combine(reportsDir, "report.md");

        string htmlContent = BuildHtmlReport(benchmarkResults, Path.Combine("charts", combinedSortChartName));
        await File.WriteAllTextAsync(htmlPath, htmlContent, Encoding.UTF8, cancellationToken);

        string mdContent = BuildMarkdownReport(benchmarkResults, Path.Combine("charts", combinedSortChartName));
        await File.WriteAllTextAsync(mdPath, mdContent, Encoding.UTF8, cancellationToken);

        progress?.Invoke("Отчёт успешно сформирован!", 1.0);

        return htmlPath;
    }

    /// <summary>
    /// Размеры данных для отчёта — быстро и наглядно (15–25 точек).
    /// </summary>
    private static int[] GetReportSizes(AbstractAlgorithm algo)
    {
        return algo.TheoreticalComplexityLabel switch
        {
            "O(n³)" => AlgorithmRegistry.BuildLinearSizes(10, 250, 15),
            "O(n²)" => AlgorithmRegistry.BuildLinearSizes(50, 2000, 100),
            "O(n log n)" => AlgorithmRegistry.BuildLinearSizes(50, 2000, 100),
            "O(n)" => AlgorithmRegistry.BuildLinearSizes(50, 2000, 100),
            "O(log n)" => AlgorithmRegistry.BuildSizes(10, 1000000, 25),
            "O(1)" => AlgorithmRegistry.BuildLinearSizes(50, 2000, 100),
            "O(2^n)" => AlgorithmRegistry.BuildLinearSizes(1, 30, 1),
            _ => AlgorithmRegistry.BuildLinearSizes(50, 2000, 100)
        };
    }

    private static void SaveAlgorithmChart(AbstractAlgorithm algo, BenchmarkResult bench, string filePath)
    {
        var plt = new Plot();
        var results = bench.Results;
        if (results.Count == 0) return;

        double[] xs = results.Select(r => (double)r.N).ToArray();
        double[] ysExp = results.Select(r => r.AverageTimeMs).ToArray();
        double[] ysTheo = results.Select(r => r.TheoreticalTimeMs).ToArray();

        var exp = plt.Add.ScatterLine(xs, ysExp);
        exp.LegendText = "Экспериментальные замеры (T среднее)";
        exp.Color = Color.FromHex("#1976D2");
        exp.LineWidth = 2;

        var theo = plt.Add.ScatterLine(xs, ysTheo);
        theo.LegendText = $"Аппроксимация ({bench.ComplexityLabel}, c={bench.FittedCoefficient:E2}, MSE={bench.MSE:E2})";
        theo.Color = Color.FromHex("#D32F2F");
        theo.LineWidth = 2;
        theo.LinePattern = LinePattern.Dashed;

        plt.Title($"{algo.Name}");
        plt.XLabel("Размерность входных данных n");
        plt.YLabel("Время исполнения (мс)");
        plt.ShowLegend(Alignment.UpperLeft);

        plt.SavePng(filePath, 1100, 550);
    }

    private static void SaveCombinedSortingChart(
        List<(AbstractAlgorithm Algo, BenchmarkResult Result, string ChartPath)> sortResults,
        string filePath)
    {
        var plt = new Plot();
        var colors = new[] { "#E53935", "#8E24AA", "#1E88E5", "#43A047", "#FB8C00" };
        int colorIdx = 0;

        foreach (var (algo, bench, _) in sortResults)
        {
            var results = bench.Results;
            if (results.Count == 0) continue;

            double[] xs = results.Select(r => (double)r.N).ToArray();
            double[] ys = results.Select(r => r.AverageTimeMs).ToArray();

            var line = plt.Add.ScatterLine(xs, ys);
            line.LegendText = $"{algo.Name} [{algo.TheoreticalComplexityLabel}]";
            line.Color = Color.FromHex(colors[colorIdx % colors.Length]);
            line.LineWidth = 2.5f;
            colorIdx++;
        }

        plt.Title("Сравнительный анализ алгоритмов сортировки");
        plt.XLabel("Размер массива n");
        plt.YLabel("Время выполнения (мс)");
        plt.ShowLegend(Alignment.UpperLeft);

        plt.SavePng(filePath, 1200, 600);
    }

    private static string BuildHtmlReport(
        List<(AbstractAlgorithm Algo, BenchmarkResult Result, string ChartPath)> results,
        string combinedSortChartPath)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"ru\">");
        sb.AppendLine("<head>");
        sb.AppendLine("  <meta charset=\"UTF-8\">");
        sb.AppendLine("  <title>Отчёт по лабораторной работе №1</title>");
        sb.AppendLine("  <style>");
        sb.AppendLine("    body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; line-height: 1.6; color: #24292e; max-width: 1100px; margin: 0 auto; padding: 25px; background: #fdfdfd; }");
        sb.AppendLine("    h1, h2, h3 { color: #0d47a1; border-bottom: 1px solid #e0e0e0; padding-bottom: 6px; margin-top: 30px; }");
        sb.AppendLine("    .header-box { background: #e3f2fd; padding: 20px 25px; border-radius: 8px; border-left: 6px solid #1976d2; margin-bottom: 30px; }");
        sb.AppendLine("    .algo-card { background: #ffffff; border: 1px solid #e1e4e8; border-radius: 8px; padding: 20px; margin-bottom: 30px; box-shadow: 0 2px 5px rgba(0,0,0,0.04); }");
        sb.AppendLine("    .chart-img { max-width: 100%; height: auto; border: 1px solid #ddd; border-radius: 6px; display: block; margin: 15px auto; }");
        sb.AppendLine("    table { width: 100%; border-collapse: collapse; margin: 15px 0; font-size: 13px; }");
        sb.AppendLine("    th, td { border: 1px solid #e0e0e0; padding: 8px 12px; text-align: right; }");
        sb.AppendLine("    th { background: #f5f5f5; text-align: center; }");
        sb.AppendLine("    td:first-child { text-align: center; }");
        sb.AppendLine("    .badge { display: inline-block; padding: 3px 8px; font-weight: bold; border-radius: 4px; font-size: 12px; background: #e0f2f1; color: #00695c; }");
        sb.AppendLine("    .print-btn { background: #1976d2; color: white; border: none; padding: 10px 20px; font-size: 15px; border-radius: 6px; cursor: pointer; float: right; }");
        sb.AppendLine("    .print-btn:hover { background: #0d47a1; }");
        sb.AppendLine("    @media print { .print-btn { display: none; } body { max-width: 100%; padding: 0; } .algo-card { page-break-inside: avoid; box-shadow: none; border: 1px solid #ccc; } }");
        sb.AppendLine("  </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");

        sb.AppendLine("  <button class=\"print-btn\" onclick=\"window.print()\">🖨 Печать в PDF</button>");
        sb.AppendLine("  <div class=\"header-box\">");
        sb.AppendLine("    <h2>Лабораторная работа №1</h2>");
        sb.AppendLine("    <p><b>Тема:</b> Эмпирический анализ временной сложности алгоритмов</p>");
        sb.AppendLine($"    <p><b>Дата формирования:</b> {DateTime.Now:dd.MM.yyyy HH:mm}</p>");
        sb.AppendLine("    <p><b>Цель:</b> Практическое исследование зависимости времени работы алгоритмов от объёма входных данных, сопоставление экспериментальных кривых с теоретическими классами сложности Big-O, аппроксимация методом наименьших квадратов, оценка MSE.</p>");
        sb.AppendLine("  </div>");

        // Сводный раздел сортировок
        sb.AppendLine("  <h2>Сравнительный анализ алгоритмов сортировки</h2>");
        sb.AppendLine("  <div class=\"algo-card\">");
        sb.AppendLine($"    <img class=\"chart-img\" src=\"{combinedSortChartPath}\" alt=\"Сравнение сортировок\">");
        sb.AppendLine("    <p><b>Анализ:</b> Из графика видно преимущество алгоритмов <code>O(n log n)</code> (QuickSort, Timsort, MergeSort) над квадратичными <code>O(n²)</code> (BubbleSort, InsertionSort). При росте n до 2000 элементов время квадратичных сортировок растёт круто по параболе.</p>");
        sb.AppendLine("  </div>");

        // Результаты по алгоритмам
        sb.AppendLine("  <h2>Подробные результаты по алгоритмам</h2>");

        foreach (var (algo, bench, chartPath) in results)
        {
            sb.AppendLine("  <div class=\"algo-card\">");
            sb.AppendLine($"    <h3>{algo.Name} <span class=\"badge\">{algo.TheoreticalComplexityLabel}</span></h3>");
            sb.AppendLine($"    <p><b>Группа:</b> {algo.Group} | <b>c:</b> <code>{bench.FittedCoefficient:E3}</code> | <b>MSE:</b> <code>{bench.MSE:E3}</code></p>");
            sb.AppendLine($"    <img class=\"chart-img\" src=\"{chartPath}\" alt=\"{algo.Name}\">");

            sb.AppendLine("    <table>");
            sb.AppendLine("      <thead><tr><th>n</th><th>T эксп. (мс)</th><th>T теор. (мс)</th><th>Все 5 замеров (мс)</th></tr></thead>");
            sb.AppendLine("      <tbody>");

            foreach (var r in bench.Results)
            {
                string runsStr = string.Join(", ", r.AllRunsMs.Select(x => x.ToString("F4")));
                sb.AppendLine($"        <tr><td>{r.N}</td><td>{r.AverageTimeMs:F4}</td><td>{r.TheoreticalTimeMs:F4}</td><td style=\"font-size:11px;color:#555;\">{runsStr}</td></tr>");
            }

            sb.AppendLine("      </tbody>");
            sb.AppendLine("    </table>");
            sb.AppendLine("  </div>");
        }

        // Выводы
        sb.AppendLine("  <h2>Выводы</h2>");
        sb.AppendLine("  <div class=\"algo-card\">");
        sb.AppendLine("    <ol>");
        sb.AppendLine("      <li><b>Соответствие теории и практики:</b> Экспериментальные замеры подтверждают теоретические классы сложности.</li>");
        sb.AppendLine("      <li><b>MSE:</b> Чем меньше MSE, тем лучше теоретическая модель описывает эксперимент. Для чистых функций MSE минимальна.</li>");
        sb.AppendLine("      <li><b>Факторы погрешности:</b> Колебания эмпирической кривой объясняются системными процессами ОС и кэшированием процессора.</li>");
        sb.AppendLine("    </ol>");
        sb.AppendLine("  </div>");

        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    private static string BuildMarkdownReport(
        List<(AbstractAlgorithm Algo, BenchmarkResult Result, string ChartPath)> results,
        string combinedSortChartPath)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Лабораторная работа №1: Эмпирический анализ временной сложности алгоритмов\n");
        sb.AppendLine($"*Дата формирования:* {DateTime.Now:dd.MM.yyyy HH:mm}\n");
        sb.AppendLine("## 1. Цель работы\n");
        sb.AppendLine("Эмпирический анализ временной сложности алгоритмов, сопоставление теоретических оценок Big-O с экспериментальными замерами и аппроксимация методом наименьших квадратов.\n");

        sb.AppendLine("## 2. Сравнительный анализ алгоритмов сортировки\n");
        sb.AppendLine($"![Сравнение сортировок]({combinedSortChartPath})\n");

        sb.AppendLine("## 3. Результаты экспериментов\n");

        foreach (var (algo, bench, chartPath) in results)
        {
            sb.AppendLine($"### {algo.Name} ({algo.TheoreticalComplexityLabel})\n");
            sb.AppendLine($"- **Группа:** {algo.Group}");
            sb.AppendLine($"- **Коэффициент c:** `{bench.FittedCoefficient:E3}`");
            sb.AppendLine($"- **MSE:** `{bench.MSE:E3}`\n");
            sb.AppendLine($"![{algo.Name}]({chartPath})\n");

            sb.AppendLine("| n | T эксп. (мс) | T теор. (мс) |");
            sb.AppendLine("|---|---|---|");
            foreach (var r in bench.Results)
            {
                sb.AppendLine($"| {r.N} | {r.AverageTimeMs:F4} | {r.TheoreticalTimeMs:F4} |");
            }
            sb.AppendLine();
        }

        sb.AppendLine("## 4. Сводная таблица алгоритмов\n");
        sb.AppendLine("| Алгоритм | Класс | c | MSE |");
        sb.AppendLine("|---|---|---|---|");
        foreach (var (algo, bench, _) in results)
        {
            sb.AppendLine($"| {algo.Name} | {algo.TheoreticalComplexityLabel} | `{bench.FittedCoefficient:E2}` | `{bench.MSE:E2}` |");
        }
        sb.AppendLine();

        sb.AppendLine("## 5. Выводы\n");
        sb.AppendLine("1. Экспериментальные данные подтверждают теоретические оценки.");
        sb.AppendLine("2. MSE показывает качество аппроксимации: чем меньше, тем точнее теория.");
        sb.AppendLine("3. Преимущество эффективных алгоритмов: Горнер O(n) над наивным O(n²), QuickSort O(n log n) над BubbleSort O(n²), бинарное возведение в степень O(log n) над наивным O(n).");

        return sb.ToString();
    }

    private static string MakeSafeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var safe = new string(name.Select(ch => invalid.Contains(ch) || ch == ' ' || ch == '(' || ch == ')' || ch == '=' ? '_' : ch).ToArray());
        return safe.Trim('_');
    }
}