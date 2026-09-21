using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AlgorithmAnalysis.Models;
using AlgorithmAnalysis.Models.Algorithms;
using ScottPlot;

namespace AlgorithmAnalysis.Services;

/// <summary>
/// Сервис автоматической генерации отчёта по лабораторной работе №1.
/// Запускает серию экспериментов для всех алгоритмов, строит графики (включая сводный),
/// и формирует готовые файлы отчёта (интерактивный HTML для печати в PDF и Markdown).
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
        if (algo is PowerAlgorithm)
        {
            return algo.TheoreticalComplexityLabel == "O(n)"
                ? AlgorithmRegistry.BuildLinearSizes(10, 2000, 100)
                : AlgorithmRegistry.BuildSizes(2, 50000, 25);
        }

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
        double[] ysExp = bench.IsStepBased
            ? results.Select(r => (double)r.StepCount).ToArray()
            : results.Select(r => r.AverageTimeMs).ToArray();
        double[] ysTheo = results.Select(r => r.TheoreticalTimeMs).ToArray();

        var exp = plt.Add.ScatterLine(xs, ysExp);
        exp.LegendText = bench.IsStepBased ? "Эксперимент (число шагов)" : "Экспериментальные замеры (T среднее)";
        exp.Color = Color.FromHex("#1976D2");
        exp.LineWidth = 2;

        var theo = plt.Add.ScatterLine(xs, ysTheo);
        theo.LegendText = $"Аппроксимация ({bench.ComplexityLabel}, c={bench.FittedCoefficient:E2}, MSE={bench.MSE:E2})";
        theo.Color = Color.FromHex("#D32F2F");
        theo.LineWidth = 2;
        theo.LinePattern = LinePattern.Dashed;

        plt.Title($"{algo.Name}");
        plt.XLabel(bench.XAxisTitle);
        plt.YLabel(bench.YAxisTitle);
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
        var palette = new[]
        {
            "#2563eb", "#dc2626", "#16a34a", "#9333ea",
            "#d97706", "#0891b2", "#e11d48", "#4f46e5",
            "#059669", "#7c3aed", "#c026d3", "#ca8a04",
            "#0284c7", "#b91c1c", "#15803d", "#6d28d9"
        };

        var algosDataList = results.Select((r, idx) => new
        {
            id = idx,
            name = r.Algo.Name,
            group = r.Algo.Group,
            complexity = r.Algo.TheoreticalComplexityLabel,
            isStepBased = r.Result.IsStepBased,
            unit = r.Result.UnitLabel,
            color = palette[idx % palette.Length],
            points = r.Result.Results.Select(pt => new
            {
                n = pt.N,
                y = r.Result.IsStepBased ? (double)pt.StepCount : pt.AverageTimeMs
            }).ToList()
        }).ToList();
        string algosJson = System.Text.Json.JsonSerializer.Serialize(algosDataList);

        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"ru\">");
        sb.AppendLine("<head>");
        sb.AppendLine("  <meta charset=\"UTF-8\">");
        sb.AppendLine("  <title>Отчёт по лабораторной работе №1 — Анализ алгоритмов</title>");
        sb.AppendLine("  <style>");
        sb.AppendLine("    * { box-sizing: border-box; }");
        sb.AppendLine("    body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; line-height: 1.6; color: #1e293b; background: #f8fafc; margin: 0; padding: 0; }");
        sb.AppendLine("    .app-layout { display: flex; min-height: 100vh; width: 100%; }");
        sb.AppendLine("");
        sb.AppendLine("    /* Сайдбар */");
        sb.AppendLine("    .sidebar { width: 320px; min-width: 320px; background: #ffffff; color: #334155; padding: 24px 16px; flex-shrink: 0; border-right: 1px solid #e2e8f0; height: 100vh; position: sticky; top: 0; overflow-y: auto; }");
        sb.AppendLine("    .sidebar-header { padding-bottom: 16px; margin-bottom: 16px; border-bottom: 1px solid #e2e8f0; }");
        sb.AppendLine("    .sidebar-title { font-size: 16px; font-weight: 700; color: #0f172a; margin: 0 0 4px 0; }");
        sb.AppendLine("    .sidebar-sub { font-size: 12px; color: #64748b; margin: 0; }");
        sb.AppendLine("    .search-input { width: 100%; padding: 8px 12px; background: #f8fafc; border: 1px solid #cbd5e1; border-radius: 6px; color: #0f172a; font-size: 13px; margin-bottom: 14px; }");
        sb.AppendLine("    .search-input:focus { outline: none; border-color: #2563eb; background: #ffffff; box-shadow: 0 0 0 2px rgba(37,99,235,0.1); }");
        sb.AppendLine("    .nav-btn { display: flex; align-items: center; justify-content: space-between; width: 100%; padding: 8px 12px; background: transparent; border: none; border-radius: 6px; color: #334155; font-size: 13px; text-align: left; cursor: pointer; transition: all 0.15s ease; margin-bottom: 3px; }");
        sb.AppendLine("    .nav-btn:hover { background: #f1f5f9; color: #0f172a; }");
        sb.AppendLine("    .nav-btn.active { background: #eff6ff; color: #1d4ed8; font-weight: 600; }");
        sb.AppendLine("    .nav-group-title { font-size: 11px; font-weight: 700; text-transform: uppercase; letter-spacing: 0.05em; color: #94a3b8; margin: 16px 8px 6px 8px; }");
        sb.AppendLine("    .action-btn { font-weight: 500; margin-bottom: 6px; justify-content: center; font-size: 13px; }");
        sb.AppendLine("    .print-btn { background: #2563eb; color: #ffffff; border: 1px solid #1d4ed8; }");
        sb.AppendLine("    .print-btn:hover { background: #1d4ed8; }");
        sb.AppendLine("    .view-all-btn { background: #ffffff; color: #334155; border: 1px solid #cbd5e1; }");
        sb.AppendLine("    .view-all-btn:hover { background: #f1f5f9; color: #0f172a; }");
        sb.AppendLine("");
        sb.AppendLine("    /* Основная область контента */");
        sb.AppendLine("    .main-content { flex: 1; min-width: 0; width: 100%; padding: 32px 48px; background: #f8fafc; }");
        sb.AppendLine("    .header-box { background: #ffffff; padding: 24px 30px; border-radius: 8px; border: 1px solid #e2e8f0; border-left: 4px solid #2563eb; margin-bottom: 24px; box-shadow: 0 1px 3px rgba(0,0,0,0.03); }");
        sb.AppendLine("    .header-box h2 { margin: 0 0 10px 0; color: #0f172a; font-size: 20px; font-weight: 700; }");
        sb.AppendLine("    .header-box p { margin: 4px 0; font-size: 14px; color: #475569; }");
        sb.AppendLine("    .algo-card { background: #ffffff; border: 1px solid #e2e8f0; border-radius: 8px; padding: 26px 30px; margin-bottom: 24px; box-shadow: 0 1px 3px rgba(0,0,0,0.03); width: 100%; }");
        sb.AppendLine("    .algo-card h3 { margin: 0 0 12px 0; color: #0f172a; font-size: 18px; font-weight: 600; display: flex; align-items: center; justify-content: space-between; }");
        sb.AppendLine("    .meta-line { font-size: 13px; color: #64748b; margin-bottom: 16px; padding-bottom: 10px; border-bottom: 1px solid #f1f5f9; }");
        sb.AppendLine("    .meta-line code { background: #f1f5f9; border: 1px solid #e2e8f0; padding: 2px 6px; border-radius: 4px; color: #0f172a; font-weight: 600; font-size: 12px; }");
        sb.AppendLine("    .chart-img { max-width: 100%; height: auto; border: 1px solid #e2e8f0; border-radius: 8px; display: block; margin: 20px auto; box-shadow: 0 1px 4px rgba(0,0,0,0.04); }");
        sb.AppendLine("    table { width: 100%; border-collapse: collapse; margin: 18px 0; font-size: 13px; }");
        sb.AppendLine("    th, td { border: 1px solid #e2e8f0; padding: 9px 12px; text-align: right; }");
        sb.AppendLine("    th { background: #f8fafc; color: #334155; font-weight: 600; text-align: center; }");
        sb.AppendLine("    td:first-child { text-align: left; font-weight: 500; }");
        sb.AppendLine("    tr:hover td { background: #f8fafc; }");
        sb.AppendLine("    .badge { display: inline-block; padding: 3px 8px; font-weight: 600; border-radius: 4px; font-size: 12px; background: #f1f5f9; color: #334155; border: 1px solid #e2e8f0; }");
        sb.AppendLine("    .badge-sm { font-size: 11px; padding: 2px 6px; }");
        sb.AppendLine("    .badge-step { background: #eff6ff; color: #1d4ed8; border: 1px solid #bfdbfe; }");
        sb.AppendLine("    .tip-box { background: #f8fafc; border: 1px solid #cbd5e1; border-left: 4px solid #2563eb; color: #1e293b; padding: 12px 16px; border-radius: 6px; font-size: 13px; margin: 16px 0; }");
        sb.AppendLine("    .tab-pane { display: none; }");
        sb.AppendLine("    .tab-pane.active { display: block; animation: fadeIn 0.2s ease-in-out; }");
        sb.AppendLine("    @keyframes fadeIn { from { opacity: 0; transform: translateY(4px); } to { opacity: 1; transform: translateY(0); } }");
        sb.AppendLine("");
        sb.AppendLine("    /* Стили для печати в PDF */");
        sb.AppendLine("    @media print {");
        sb.AppendLine("      .sidebar, .action-btn, .search-input { display: none !important; }");
        sb.AppendLine("      .app-layout { display: block !important; }");
        sb.AppendLine("      .main-content { width: 100% !important; max-width: 100% !important; padding: 0 !important; background: #ffffff !important; }");
        sb.AppendLine("      .tab-pane { display: block !important; }");
        sb.AppendLine("      .algo-card { break-inside: avoid !important; page-break-inside: avoid !important; box-shadow: none !important; border: 1px solid #cbd5e1 !important; margin-bottom: 25px !important; }");
        sb.AppendLine("      .summary-section { break-after: page !important; page-break-after: always !important; }");
        sb.AppendLine("      body { background: #ffffff !important; color: #000000 !important; }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    @media (max-width: 900px) {");
        sb.AppendLine("      .app-layout { flex-direction: column; }");
        sb.AppendLine("      .sidebar { width: 100%; height: auto; position: static; }");
        sb.AppendLine("      .main-content { padding: 16px; }");
        sb.AppendLine("    }");
        sb.AppendLine("  </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");

        sb.AppendLine("  <div class=\"app-layout\">");

        // Генерация сайдбара
        sb.AppendLine("    <div class=\"sidebar\">");
        sb.AppendLine("      <div class=\"sidebar-header\">");
        sb.AppendLine("        <div class=\"sidebar-title\">Лабораторная работа №1</div>");
        sb.AppendLine("        <div class=\"sidebar-sub\">Эмпирический анализ алгоритмов</div>");
        sb.AppendLine("      </div>");

        sb.AppendLine("      <input type=\"text\" id=\"algoSearch\" class=\"search-input\" placeholder=\"Поиск алгоритма...\" oninput=\"filterAlgos()\">");

        sb.AppendLine("      <button class=\"nav-btn action-btn active\" id=\"btn-summary\" onclick=\"selectTab('summary')\">");
        sb.AppendLine("        <span>Сводный обзор</span>");
        sb.AppendLine("      </button>");

        sb.AppendLine("      <button class=\"nav-btn action-btn\" id=\"btn-compare\" onclick=\"selectTab('compare')\">");
        sb.AppendLine("        <span>Сравнение графиков</span>");
        sb.AppendLine("      </button>");

        sb.AppendLine("      <button class=\"nav-btn action-btn print-btn\" onclick=\"showAllAndPrint()\">");
        sb.AppendLine("        <span>Печать в PDF</span>");
        sb.AppendLine("      </button>");

        sb.AppendLine("      <button class=\"nav-btn action-btn view-all-btn\" id=\"btn-show-all\" onclick=\"toggleShowAll()\">");
        sb.AppendLine("        <span>Показать все</span>");
        sb.AppendLine("      </button>");

        // Группировка алгоритмов в сайдбаре
        var groups = results.GroupBy(r => r.Algo.Group).ToList();

        for (int gIdx = 0; gIdx < groups.Count; gIdx++)
        {
            var grp = groups[gIdx];
            sb.AppendLine($"      <div class=\"nav-group-title\">{WebUtility.HtmlEncode(grp.Key)}</div>");

            foreach (var item in grp)
            {
                int itemIndex = results.IndexOf(item);
                string encodedName = WebUtility.HtmlEncode(item.Algo.Name);
                string encodedLabel = WebUtility.HtmlEncode(item.Algo.TheoreticalComplexityLabel);
                string badgeClass = item.Algo.MeasureSteps ? "badge badge-sm badge-step" : "badge badge-sm";

                sb.AppendLine($"      <button class=\"nav-btn algo-btn\" id=\"btn-algo-{itemIndex}\" onclick=\"selectTab('algo-{itemIndex}')\">");
                sb.AppendLine($"        <span style=\"text-overflow:ellipsis; overflow:hidden; white-space:nowrap; max-width:200px;\">{encodedName}</span>");
                sb.AppendLine($"        <span class=\"{badgeClass}\">{encodedLabel}</span>");
                sb.AppendLine("      </button>");
            }
        }

        sb.AppendLine("    </div>"); // Конец sidebar

        // Главная область контента
        sb.AppendLine("    <div class=\"main-content\">");

        sb.AppendLine("      <div class=\"header-box\">");
        sb.AppendLine("        <h2>Лабораторная работа №1: Эмпирический анализ сложности алгоритмов</h2>");
        sb.AppendLine("        <p><b>Тема:</b> Эмпирический анализ временной сложности алгоритмов, Big-O аппроксимация, подсчёт шагов</p>");
        sb.AppendLine($"        <p><b>Дата формирования:</b> {DateTime.Now:dd.MM.yyyy HH:mm}</p>");
        sb.AppendLine("        <p><b>Цель:</b> Практическое исследование зависимости времени работы и количества элементарных операций от объёма входных данных, сопоставление экспериментальных кривых с теоретическими функциями сложности, аппроксимация МНК и оценка MSE.</p>");
        sb.AppendLine("      </div>");

        // 1. Вкладка Сводного обзора
        sb.AppendLine("      <div id=\"tab-summary\" class=\"tab-pane active summary-section\">");

        sb.AppendLine("        <div class=\"algo-card\">");
        sb.AppendLine("          <h3>Сравнительный анализ алгоритмов сортировки</h3>");
        sb.AppendLine($"          <img class=\"chart-img\" src=\"{combinedSortChartPath}\" alt=\"Сравнение сортировок\">");
        sb.AppendLine("          <p><b>Анализ сортировок:</b> Из графиков отчётливо видно преимущество логарифмических сортировок <code>O(n log n)</code> (QuickSort, Timsort, MergeSort) над квадратичными <code>O(n²)</code> (BubbleSort, InsertionSort). При росте размера массива n до 2000 элементов квадратичные сортировки демонстрируют крутой параболический рост времени исполнения.</p>");
        sb.AppendLine("        </div>");

        sb.AppendLine("        <div class=\"algo-card\">");
        sb.AppendLine("          <h3>Сводная таблица результатов всех алгоритмов</h3>");
        sb.AppendLine("          <table>");
        sb.AppendLine("            <thead>");
        sb.AppendLine("              <tr><th>Алгоритм</th><th>Группа</th><th>Класс сложности</th><th>Коэффициент c</th><th>MSE</th><th>Тип замера</th></tr>");
        sb.AppendLine("            </thead>");
        sb.AppendLine("            <tbody>");

        foreach (var (algo, bench, _) in results)
        {
            string encodedName = WebUtility.HtmlEncode(algo.Name);
            string encodedGroup = WebUtility.HtmlEncode(algo.Group);
            string encodedClass = WebUtility.HtmlEncode(algo.TheoreticalComplexityLabel);
            string typeLabel = bench.IsStepBased ? "Число шагов" : "Время (мс)";
            string badgeClass = bench.IsStepBased ? "badge badge-step" : "badge";

            sb.AppendLine($"              <tr>");
            sb.AppendLine($"                <td style=\"text-align:left;\"><b>{encodedName}</b></td>");
            sb.AppendLine($"                <td style=\"text-align:center;\">{encodedGroup}</td>");
            sb.AppendLine($"                <td style=\"text-align:center;\"><span class=\"{badgeClass}\">{encodedClass}</span></td>");
            sb.AppendLine($"                <td><code>{bench.FittedCoefficient:E3}</code></td>");
            sb.AppendLine($"                <td><code>{bench.MSE:E3}</code></td>");
            sb.AppendLine($"                <td style=\"text-align:center;\">{typeLabel}</td>");
            sb.AppendLine($"              </tr>");
        }

        sb.AppendLine("            </tbody>");
        sb.AppendLine("          </table>");
        sb.AppendLine("        </div>");

        sb.AppendLine("        <div class=\"algo-card\">");
        sb.AppendLine("          <h3>Теоретические выводы</h3>");
        sb.AppendLine("          <ol>");
        sb.AppendLine("            <li><b>Соответствие теории и практики:</b> Экспериментальные кривые подтверждают теоретические оценки Big-O для всех групп алгоритмов.</li>");
        sb.AppendLine("            <li><b>Метрика MSE:</b> Минимальная среднеквадратичная ошибка подтверждает адекватность выбранной теоретической модели. Для детерминированных алгоритмов шагов MSE практически равна нулю.</li>");
        sb.AppendLine("            <li><b>Возведение в степень:</b> Подсчёт точного числа шагов (умножений) наглядно демонстрирует переход от линейного $O(n)$ к логарифмическому $O(\\log n)$ числу операций в бинарных алгоритмах.</li>");
        sb.AppendLine("          </ol>");
        sb.AppendLine("        </div>");

        sb.AppendLine("      </div>"); // Конец tab-summary

        // 1.5. Вкладка Сравнения и наложения графиков
        sb.AppendLine("      <div id=\"tab-compare\" class=\"tab-pane\">");
        sb.AppendLine("        <div class=\"algo-card\">");
        sb.AppendLine("          <h3>Интерактивное сравнение и наложение графиков</h3>");
        sb.AppendLine("          <p>Отметьте интересующие алгоритмы чекбоксами для одновременного наложения их экспериментальных кривых на единый график.</p>");
        sb.AppendLine("");
        sb.AppendLine("          <div style=\"display:flex; gap:8px; flex-wrap:wrap; margin:16px 0;\">");
        sb.AppendLine("            <button type=\"button\" class=\"nav-btn\" style=\"width:auto; background:#eff6ff; color:#1d4ed8; border:1px solid #bfdbfe; font-weight:600; padding:6px 14px;\" onclick=\"selectGroupForCompare('Сортировки')\">Все сортировки</button>");
        sb.AppendLine("            <button type=\"button\" class=\"nav-btn\" style=\"width:auto; background:#eff6ff; color:#1d4ed8; border:1px solid #bfdbfe; font-weight:600; padding:6px 14px;\" onclick=\"selectGroupForCompare('Возведение в степень')\">Все степени</button>");
        sb.AppendLine("            <button type=\"button\" class=\"nav-btn\" style=\"width:auto; background:#f1f5f9; color:#334155; border:1px solid #cbd5e1; padding:6px 14px;\" onclick=\"selectAllForCompare(false)\">Сбросить выбор</button>");
        sb.AppendLine("          </div>");
        sb.AppendLine("");
        sb.AppendLine("          <div id=\"compareWarning\" style=\"display:none; background:#fffbeb; border:1px solid #fde68a; color:#92400e; padding:10px 14px; border-radius:6px; font-size:13px; margin-bottom:14px;\">Внимание: выбраны алгоритмы с разными единицами измерения (мс и шаги). Рекомендуется сравнивать отдельно время или отдельно шаги.</div>");
        sb.AppendLine("");
        sb.AppendLine("          <div style=\"display:grid; grid-template-columns:repeat(auto-fill, minmax(260px, 1fr)); gap:8px; margin-bottom:20px; padding:14px; background:#f8fafc; border:1px solid #e2e8f0; border-radius:8px;\">");

        for (int i = 0; i < results.Count; i++)
        {
            var item = results[i];
            string cColor = palette[i % palette.Length];
            string encName = WebUtility.HtmlEncode(item.Algo.Name);
            string encClass = WebUtility.HtmlEncode(item.Algo.TheoreticalComplexityLabel);
            bool defaultChecked = item.Algo.Group == "Сортировки";

            sb.AppendLine("            <label style=\"display:flex; align-items:center; gap:8px; font-size:13px; cursor:pointer; user-select:none;\">");
            sb.AppendLine($"              <input type=\"checkbox\" id=\"chk-cmp-{i}\" class=\"cmp-checkbox\" data-id=\"{i}\" data-group=\"{WebUtility.HtmlEncode(item.Algo.Group)}\" {(defaultChecked ? "checked" : "")} onchange=\"onCompareCheckboxChanged()\">");
            sb.AppendLine($"              <span style=\"width:10px; height:10px; border-radius:50%; background:{cColor}; display:inline-block; flex-shrink:0;\"></span>");
            sb.AppendLine($"              <span style=\"flex:1; overflow:hidden; text-overflow:ellipsis; white-space:nowrap;\">{encName}</span>");
            sb.AppendLine($"              <span class=\"badge badge-sm\" style=\"font-size:10px;\">{encClass}</span>");
            sb.AppendLine("            </label>");
        }

        sb.AppendLine("          </div>");
        sb.AppendLine("");
        sb.AppendLine("          <div id=\"compareSvgContainer\" style=\"background:#ffffff; border:1px solid #e2e8f0; border-radius:8px; padding:16px; position:relative;\">");
        sb.AppendLine("            <svg id=\"compareSvg\" viewBox=\"0 0 1000 480\" style=\"width:100%; height:auto; display:block;\"></svg>");
        sb.AppendLine("            <div id=\"compareTooltip\" style=\"position:absolute; display:none; background:rgba(15,23,42,0.92); color:#ffffff; padding:6px 10px; border-radius:6px; font-size:12px; pointer-events:none; z-index:10; box-shadow:0 4px 12px rgba(0,0,0,0.15);\"></div>");
        sb.AppendLine("          </div>");
        sb.AppendLine("");
        sb.AppendLine("          <div id=\"compareLegend\" style=\"display:flex; flex-wrap:wrap; gap:12px; margin-top:16px;\"></div>");
        sb.AppendLine("        </div>");
        sb.AppendLine("      </div>"); // Конец tab-compare

        // 2. Вкладки для каждого отдельного алгоритма
        for (int i = 0; i < results.Count; i++)
        {
            var (algo, bench, chartPath) = results[i];
            string encodedName = WebUtility.HtmlEncode(algo.Name);
            string encodedGroup = WebUtility.HtmlEncode(algo.Group);
            string encodedClass = WebUtility.HtmlEncode(algo.TheoreticalComplexityLabel);
            string badgeClass = bench.IsStepBased ? "badge badge-step" : "badge";
            string unitName = bench.UnitLabel;

            sb.AppendLine($"      <div id=\"tab-algo-{i}\" class=\"tab-pane\">");
            sb.AppendLine("        <div class=\"algo-card\">");
            sb.AppendLine($"          <h3>{encodedName} <span class=\"{badgeClass}\">{encodedClass}</span></h3>");
            sb.AppendLine($"          <div class=\"meta-line\">Группа: <b>{encodedGroup}</b> | Коэффициент c: <code>{bench.FittedCoefficient:E3}</code> | MSE: <code>{bench.MSE:E3}</code> | Измерение: <b>{unitName}</b></div>");

            if (algo.Group == "Матрицы")
            {
                sb.AppendLine("          <div class=\"tip-box\"><b>3D-анализ матриц:</b> В программе доступен запуск 3D-эксперимента матричного умножения A(n×m) × B(m×k) с интерактивной 3D-поверхностью на Plotly.js!</div>");
            }

            sb.AppendLine($"          <img class=\"chart-img\" src=\"{chartPath}\" alt=\"{encodedName}\">");

            sb.AppendLine("          <table>");
            if (bench.IsStepBased)
            {
                sb.AppendLine("            <thead><tr><th>n</th><th>Шаги эксп.</th><th>Шаги теор.</th><th>Время вып. (мс)</th></tr></thead>");
                sb.AppendLine("            <tbody>");
                foreach (var r in bench.Results)
                {
                    sb.AppendLine($"              <tr><td>{r.N}</td><td><b>{r.StepCount}</b></td><td>{r.TheoreticalTimeMs:F1}</td><td style=\"font-size:12px;color:#64748b;\">{r.AverageTimeMs:F4}</td></tr>");
                }
            }
            else
            {
                sb.AppendLine("            <thead><tr><th>n</th><th>T эксп. (мс)</th><th>T теор. (мс)</th><th>Все 5 замеров (мс)</th></tr></thead>");
                sb.AppendLine("            <tbody>");
                foreach (var r in bench.Results)
                {
                    string runsStr = string.Join(", ", r.AllRunsMs.Select(x => x.ToString("F4")));
                    sb.AppendLine($"              <tr><td>{r.N}</td><td><b>{r.AverageTimeMs:F4}</b></td><td>{r.TheoreticalTimeMs:F4}</td><td style=\"font-size:11px;color:#64748b;\">{runsStr}</td></tr>");
                }
            }
            sb.AppendLine("            </tbody>");
            sb.AppendLine("          </table>");

            sb.AppendLine("        </div>");
            sb.AppendLine("      </div>");
        }

        sb.AppendLine("    </div>"); // Конец main-content
        sb.AppendLine("  </div>"); // Конец app-layout

        // Скрипт переключения вкладок и интерактивного наложения графиков (Vanilla JS, 100% офлайн)
        sb.AppendLine("  <script>");
        sb.AppendLine($"    const algosData = {algosJson};");
        sb.AppendLine("    let activeCompareIds = new Set(algosData.filter(a => a.group === 'Сортировки').map(a => a.id));");
        sb.AppendLine("    let showAllMode = false;");
        sb.AppendLine("");
        sb.AppendLine("    function selectTab(tabId) {");
        sb.AppendLine("      showAllMode = false;");
        sb.AppendLine("      const btnShowAll = document.getElementById('btn-show-all');");
        sb.AppendLine("      if (btnShowAll) btnShowAll.innerHTML = '<span>Показать все</span>';");
        sb.AppendLine("");
        sb.AppendLine("      document.querySelectorAll('.tab-pane').forEach(el => el.classList.remove('active'));");
        sb.AppendLine("      document.querySelectorAll('.nav-btn').forEach(el => el.classList.remove('active'));");
        sb.AppendLine("");
        sb.AppendLine("      const activeTab = document.getElementById('tab-' + tabId);");
        sb.AppendLine("      if (activeTab) activeTab.classList.add('active');");
        sb.AppendLine("");
        sb.AppendLine("      const activeBtn = document.getElementById('btn-' + tabId);");
        sb.AppendLine("      if (activeBtn) activeBtn.classList.add('active');");
        sb.AppendLine("");
        sb.AppendLine("      if (tabId === 'compare') { renderCompareChart(); }");
        sb.AppendLine("");
        sb.AppendLine("      window.scrollTo({ top: 0, behavior: 'smooth' });");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    function toggleShowAll() {");
        sb.AppendLine("      showAllMode = !showAllMode;");
        sb.AppendLine("      const btn = document.getElementById('btn-show-all');");
        sb.AppendLine("      if (showAllMode) {");
        sb.AppendLine("        document.querySelectorAll('.tab-pane').forEach(el => el.classList.add('active'));");
        sb.AppendLine("        document.querySelectorAll('.nav-btn').forEach(el => el.classList.remove('active'));");
        sb.AppendLine("        if (btn) { btn.classList.add('active'); btn.innerHTML = '<span>Скрыть остальные</span>'; }");
        sb.AppendLine("        renderCompareChart();");
        sb.AppendLine("      } else {");
        sb.AppendLine("        selectTab('summary');");
        sb.AppendLine("      }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    function showAllAndPrint() {");
        sb.AppendLine("      document.querySelectorAll('.tab-pane').forEach(el => el.classList.add('active'));");
        sb.AppendLine("      renderCompareChart();");
        sb.AppendLine("      setTimeout(() => { window.print(); }, 150);");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    function filterAlgos() {");
        sb.AppendLine("      const q = document.getElementById('algoSearch').value.toLowerCase().trim();");
        sb.AppendLine("      document.querySelectorAll('.algo-btn').forEach(btn => {");
        sb.AppendLine("        const text = btn.textContent.toLowerCase();");
        sb.AppendLine("        btn.style.display = text.includes(q) ? 'flex' : 'none';");
        sb.AppendLine("      });");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    function onCompareCheckboxChanged() {");
        sb.AppendLine("      activeCompareIds.clear();");
        sb.AppendLine("      document.querySelectorAll('.cmp-checkbox:checked').forEach(chk => {");
        sb.AppendLine("        activeCompareIds.add(parseInt(chk.dataset.id));");
        sb.AppendLine("      });");
        sb.AppendLine("      renderCompareChart();");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    function selectGroupForCompare(groupName) {");
        sb.AppendLine("      document.querySelectorAll('.cmp-checkbox').forEach(chk => {");
        sb.AppendLine("        chk.checked = chk.dataset.group === groupName;");
        sb.AppendLine("      });");
        sb.AppendLine("      onCompareCheckboxChanged();");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    function selectAllForCompare(checked) {");
        sb.AppendLine("      document.querySelectorAll('.cmp-checkbox').forEach(chk => {");
        sb.AppendLine("        chk.checked = checked;");
        sb.AppendLine("      });");
        sb.AppendLine("      onCompareCheckboxChanged();");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    function renderCompareChart() {");
        sb.AppendLine("      const svg = document.getElementById('compareSvg');");
        sb.AppendLine("      const legend = document.getElementById('compareLegend');");
        sb.AppendLine("      const warning = document.getElementById('compareWarning');");
        sb.AppendLine("      if (!svg) return;");
        sb.AppendLine("");
        sb.AppendLine("      const selected = algosData.filter(a => activeCompareIds.has(a.id));");
        sb.AppendLine("      const hasSteps = selected.some(a => a.isStepBased);");
        sb.AppendLine("      const hasTime = selected.some(a => !a.isStepBased);");
        sb.AppendLine("      if (warning) warning.style.display = (hasSteps && hasTime) ? 'block' : 'none';");
        sb.AppendLine("");
        sb.AppendLine("      if (selected.length === 0) {");
        sb.AppendLine("        svg.innerHTML = '<text x=\"500\" y=\"240\" text-anchor=\"middle\" fill=\"#64748b\" font-size=\"15\">Выберите хотя бы один алгоритм чекбоксами выше</text>';");
        sb.AppendLine("        if (legend) legend.innerHTML = '';");
        sb.AppendLine("        return;");
        sb.AppendLine("      }");
        sb.AppendLine("");
        sb.AppendLine("      let maxX = 0;");
        sb.AppendLine("      let maxY = 0;");
        sb.AppendLine("      selected.forEach(a => {");
        sb.AppendLine("        a.points.forEach(p => {");
        sb.AppendLine("          if (p.n > maxX) maxX = p.n;");
        sb.AppendLine("          if (p.y > maxY) maxY = p.y;");
        sb.AppendLine("        });");
        sb.AppendLine("      });");
        sb.AppendLine("      if (maxX <= 0) maxX = 100;");
        sb.AppendLine("      if (maxY <= 0) maxY = 1;");
        sb.AppendLine("");
        sb.AppendLine("      const padLeft = 80;");
        sb.AppendLine("      const padRight = 40;");
        sb.AppendLine("      const padTop = 30;");
        sb.AppendLine("      const padBottom = 50;");
        sb.AppendLine("      const pw = 1000 - padLeft - padRight;");
        sb.AppendLine("      const ph = 480 - padTop - padBottom;");
        sb.AppendLine("      const mapX = (x) => padLeft + (x / maxX) * pw;");
        sb.AppendLine("      const mapY = (y) => padTop + ph - (y / maxY) * ph;");
        sb.AppendLine("");
        sb.AppendLine("      let s = '';");
        sb.AppendLine("      const gridSteps = 5;");
        sb.AppendLine("      for (let i = 0; i <= gridSteps; i++) {");
        sb.AppendLine("        const yVal = (maxY / gridSteps) * i;");
        sb.AppendLine("        const py = mapY(yVal);");
        sb.AppendLine("        s += `<line x1=\"${padLeft}\" y1=\"${py}\" x2=\"${padLeft + pw}\" y2=\"${py}\" stroke=\"#e2e8f0\" stroke-width=\"1\" stroke-dasharray=\"3 3\"/>`;");
        sb.AppendLine("        const yLabel = yVal < 0.1 && yVal > 0 ? yVal.toExponential(1) : (yVal >= 10 ? yVal.toFixed(0) : yVal.toFixed(2));");
        sb.AppendLine("        s += `<text x=\"${padLeft - 10}\" y=\"${py + 4}\" text-anchor=\"end\" fill=\"#64748b\" font-size=\"11\">${yLabel}</text>`;");
        sb.AppendLine("      }");
        sb.AppendLine("");
        sb.AppendLine("      for (let i = 0; i <= gridSteps; i++) {");
        sb.AppendLine("        const xVal = (maxX / gridSteps) * i;");
        sb.AppendLine("        const px = mapX(xVal);");
        sb.AppendLine("        s += `<line x1=\"${px}\" y1=\"${padTop}\" x2=\"${px}\" y2=\"${padTop + ph}\" stroke=\"#e2e8f0\" stroke-width=\"1\" stroke-dasharray=\"3 3\"/>`;");
        sb.AppendLine("        s += `<text x=\"${px}\" y=\"${padTop + ph + 20}\" text-anchor=\"middle\" fill=\"#64748b\" font-size=\"11\">${Math.round(xVal)}</text>`;");
        sb.AppendLine("      }");
        sb.AppendLine("");
        sb.AppendLine("      s += `<line x1=\"${padLeft}\" y1=\"${padTop}\" x2=\"${padLeft}\" y2=\"${padTop + ph}\" stroke=\"#94a3b8\" stroke-width=\"1.5\"/>`;");
        sb.AppendLine("      s += `<line x1=\"${padLeft}\" y1=\"${padTop + ph}\" x2=\"${padLeft + pw}\" y2=\"${padTop + ph}\" stroke=\"#94a3b8\" stroke-width=\"1.5\"/>`;");
        sb.AppendLine("");
        sb.AppendLine("      const yAxisTitle = hasSteps ? (hasTime ? 'Значение (мс / шаги)' : 'Число операций (шагов)') : 'Время выполнения (мс)';");
        sb.AppendLine("      s += `<text x=\"${padLeft + pw / 2}\" y=\"${padTop + ph + 42}\" text-anchor=\"middle\" fill=\"#334155\" font-size=\"12\" font-weight=\"600\">Размер входных данных n</text>`;");
        sb.AppendLine("      s += `<text x=\"20\" y=\"${padTop + ph / 2}\" text-anchor=\"middle\" transform=\"rotate(-90 20 ${padTop + ph / 2})\" fill=\"#334155\" font-size=\"12\" font-weight=\"600\">${yAxisTitle}</text>`;");
        sb.AppendLine("");
        sb.AppendLine("      selected.forEach(a => {");
        sb.AppendLine("        if (a.points.length === 0) return;");
        sb.AppendLine("        const ptsStr = a.points.map(p => `${mapX(p.n)},${mapY(p.y)}`).join(' ');");
        sb.AppendLine("        s += `<polyline points=\"${ptsStr}\" fill=\"none\" stroke=\"${a.color}\" stroke-width=\"2.5\" stroke-linejoin=\"round\"/>`;");
        sb.AppendLine("        a.points.forEach(p => {");
        sb.AppendLine("          const cx = mapX(p.n);");
        sb.AppendLine("          const cy = mapY(p.y);");
        sb.AppendLine("          const encA = a.name.replace(/\"/g, '&quot;');");
        sb.AppendLine("          s += `<circle cx=\"${cx}\" cy=\"${cy}\" r=\"3.5\" fill=\"${a.color}\" stroke=\"#ffffff\" stroke-width=\"1\" style=\"cursor:pointer;\" onmouseenter=\"showCmpTooltip(event, '${encA}', ${p.n}, ${p.y}, '${a.unit}')\" onmouseleave=\"hideCmpTooltip()\"/>`;");
        sb.AppendLine("        });");
        sb.AppendLine("      });");
        sb.AppendLine("");
        sb.AppendLine("      svg.innerHTML = s;");
        sb.AppendLine("");
        sb.AppendLine("      if (legend) {");
        sb.AppendLine("        legend.innerHTML = selected.map(a => `");
        sb.AppendLine("          <div style=\"display:flex; align-items:center; gap:6px; font-size:12px; color:#334155;\">");
        sb.AppendLine("            <span style=\"width:12px; height:12px; border-radius:3px; background:${a.color}; display:inline-block;\"></span>");
        sb.AppendLine("            <b>${a.name}</b>");
        sb.AppendLine("            <span class=\"badge badge-sm\" style=\"font-size:10px;\">${a.complexity}</span>");
        sb.AppendLine("          </div>");
        sb.AppendLine("        `).join('');");
        sb.AppendLine("      }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    function showCmpTooltip(e, name, n, y, unit) {");
        sb.AppendLine("      const tt = document.getElementById('compareTooltip');");
        sb.AppendLine("      if (!tt) return;");
        sb.AppendLine("      const container = document.getElementById('compareSvgContainer');");
        sb.AppendLine("      const rect = container ? container.getBoundingClientRect() : { left: 0, top: 0 };");
        sb.AppendLine("      const yFormatted = y >= 1 ? y.toFixed(2) : y.toFixed(4);");
        sb.AppendLine("      tt.innerHTML = `<b>${name}</b><br>n = ${n}<br>Значение = ${yFormatted} ${unit}`;");
        sb.AppendLine("      tt.style.left = (e.clientX - rect.left + 12) + 'px';");
        sb.AppendLine("      tt.style.top = (e.clientY - rect.top - 20) + 'px';");
        sb.AppendLine("      tt.style.display = 'block';");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    function hideCmpTooltip() {");
        sb.AppendLine("      const tt = document.getElementById('compareTooltip');");
        sb.AppendLine("      if (tt) tt.style.display = 'none';");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    // Инициализация сравнительного графика");
        sb.AppendLine("    renderCompareChart();");
        sb.AppendLine("  </script>");

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
            sb.AppendLine($"- **MSE:** `{bench.MSE:E3}`");
            sb.AppendLine($"- **Единица измерения:** {bench.UnitLabel}\n");
            sb.AppendLine($"![{algo.Name}]({chartPath})\n");

            if (bench.IsStepBased)
            {
                sb.AppendLine("| n | Шаги эксп. | Шаги теор. | Время (мс) |");
                sb.AppendLine("|---|---|---|---|");
                foreach (var r in bench.Results)
                {
                    sb.AppendLine($"| {r.N} | {r.StepCount} | {r.TheoreticalTimeMs:F1} | {r.AverageTimeMs:F4} |");
                }
            }
            else
            {
                sb.AppendLine("| n | T эксп. (мс) | T теор. (мс) |");
                sb.AppendLine("|---|---|---|");
                foreach (var r in bench.Results)
                {
                    sb.AppendLine($"| {r.N} | {r.AverageTimeMs:F4} | {r.TheoreticalTimeMs:F4} |");
                }
            }
            sb.AppendLine();
        }

        sb.AppendLine("## 4. Сводная таблица алгоритмов\n");
        sb.AppendLine("| Алгоритм | Класс | c | MSE | Измерение |");
        sb.AppendLine("|---|---|---|---|---|");
        foreach (var (algo, bench, _) in results)
        {
            sb.AppendLine($"| {algo.Name} | {algo.TheoreticalComplexityLabel} | `{bench.FittedCoefficient:E2}` | `{bench.MSE:E2}` | {bench.UnitLabel} |");
        }
        sb.AppendLine();

        sb.AppendLine("## 5. Выводы\n");
        sb.AppendLine("1. Экспериментальные данные подтверждают теоретические оценки сложности.");
        sb.AppendLine("2. MSE показывает качество аппроксимации: чем меньше, тем точнее теория описывает процесс.");
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