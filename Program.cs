using Avalonia;
using System;
using System.IO;
using System.Linq;
using AlgorithmAnalysis.Services;
using AlgorithmAnalysis.Models.Algorithms;
using AlgorithmAnalysis.ViewModels;

namespace AlgorithmAnalysis;

sealed class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Инициализируем БД при старте (создаёт файл algorithmanalysis.db)
        try
        {
            DatabaseService.InitializeAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WARN] Ошибка инициализации БД: {ex.Message}");
            Console.WriteLine("Приложение продолжит работу без кэша.");
        }

        if (args.Contains("--test-all"))
        {
            RunVerificationTests();
            return;
        }

        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    private static void RunVerificationTests()
    {
        Console.WriteLine("=== ЗАПУСК ПРОВЕРОЧНЫХ ТЕСТОВ ===");

        var benchService = new BenchmarkService { RunsPerSize = 3 };

        // 1. Тест степенных алгоритмов и подсчёта шагов
        Console.WriteLine("\n[1/3] Проверка степенных алгоритмов (шаги)...");
        var powerNaive = new PowerNaive();
        int[] sizes = [10, 50, 100];
        var resNaive = benchService.RunBenchmarkAsync(powerNaive, sizes).GetAwaiter().GetResult();

        if (!resNaive.IsStepBased) throw new Exception("PowerNaive должно быть IsStepBased == true");
        if (resNaive.Results[0].StepCount != 10 || resNaive.Results[1].StepCount != 50 || resNaive.Results[2].StepCount != 100)
            throw new Exception($"Неверный подсчёт шагов: {resNaive.Results[0].StepCount}, {resNaive.Results[1].StepCount}, {resNaive.Results[2].StepCount}");
        if (Math.Abs(resNaive.FittedCoefficient - 1.0) > 0.001)
            throw new Exception($"Коэффициент c для PowerNaive должен быть ~1.0, получено: {resNaive.FittedCoefficient}");

        Console.WriteLine($"  PowerNaive: шаги=[{string.Join(", ", resNaive.Results.Select(r => r.StepCount))}], c={resNaive.FittedCoefficient:F4}, MSE={resNaive.MSE:E3}");

        var powerFast = new PowerFast();
        var resFast = benchService.RunBenchmarkAsync(powerFast, [16, 64, 256]).GetAwaiter().GetResult();
        if (!resFast.IsStepBased || resFast.Results[0].StepCount == 0)
            throw new Exception("PowerFast шаги не подсчитаны");
        Console.WriteLine($"  PowerFast: шаги=[{string.Join(", ", resFast.Results.Select(r => r.StepCount))}], c={resFast.FittedCoefficient:F4}, MSE={resFast.MSE:E3}");

        // 2. Тест 3D-отчёта матриц Plotly
        Console.WriteLine("\n[2/3] Проверка генерации 3D-графика матриц...");
        var matrixService = new Matrix3DReportService();
        string matrix3DHtml = matrixService.RunAndGenerateReportAsync((msg, p) =>
        {
            Console.Write($"\r  {msg} ({p * 100:F0}%)    ");
        }).GetAwaiter().GetResult();
        Console.WriteLine();

        if (!File.Exists(matrix3DHtml) || new FileInfo(matrix3DHtml).Length < 1000)
            throw new Exception("3D-отчёт матриц не сформирован или слишком мал");

        string html3DContent = File.ReadAllText(matrix3DHtml);
        if (!html3DContent.Contains("plotly-2.35.2.min.js") || !html3DContent.Contains("traceExp") || !html3DContent.Contains("traceTheo"))
            throw new Exception("HTML 3D-отчёта не содержит необходимых структур Plotly.js");

        Console.WriteLine($"  3D-график создан: {Path.GetFileName(matrix3DHtml)} ({new FileInfo(matrix3DHtml).Length / 1024} КБ)");

        // 3. Тест отчёта ReportService
        Console.WriteLine("\n[3/3] Проверка генератора отчёта (ReportService)...");
        var reportService = new ReportService();
        string reportHtml = reportService.GenerateFullReportAsync((msg, p) =>
        {
            Console.Write($"\r  {msg} ({p * 100:F0}%)    ");
        }).GetAwaiter().GetResult();
        Console.WriteLine();

        if (!File.Exists(reportHtml))
            throw new Exception("report.html не сформирован");

        string reportHtmlContent = File.ReadAllText(reportHtml);
        if (!reportHtmlContent.Contains("tab-summary") || !reportHtmlContent.Contains("badge-step") || !reportHtmlContent.Contains("@media print"))
            throw new Exception("report.html не содержит интерактивной вёрстки или поддержки печати");

        Console.WriteLine($"  Интерактивный отчёт создан: {reportHtml}");
        Console.WriteLine("\n=== ВСЕ ТЕСТЫ УСПЕШНО ПРОЙДЕНЫ ===");
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}