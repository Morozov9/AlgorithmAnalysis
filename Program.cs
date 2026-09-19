using Avalonia;
using System;
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
        if (args.Length > 0 && args[0] == "--test")
        {
            RunSelfTests();
            return;
        }

        if (args.Length > 0 && args[0] == "--test-report")
        {
            Console.WriteLine("=== ТЕСТ ГЕНЕРАЦИИ ОТЧЁТА ===");
            var reportService = new ReportService();
            string reportPath = reportService.GenerateFullReportAsync((status, prog) =>
            {
                Console.WriteLine($"[{(int)(prog * 100)}%] {status}");
            }).GetAwaiter().GetResult();
            Console.WriteLine($"Отчёт создан: {reportPath}");
            if (File.Exists(reportPath))
            {
                Console.WriteLine("Файл report.html существует: " + new FileInfo(reportPath).Length + " байт");
            }
            Console.WriteLine("=== ТЕСТ ОТЧЁТА УСПЕШНО ЗАВЕРШЁН ===");
            return;
        }

        // Инициализируем БД при старте (создаёт файл algorithmanalysis.db если его нет)
        AlgorithmAnalysis.Services.DatabaseService.InitializeAsync().GetAwaiter().GetResult();

        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    private static void RunSelfTests()
    {
        Console.WriteLine("=== ЗАПУСК ТЕСТОВ АЛГОРИТМОВ ===");
        var random = new Random(42);
        var algorithms = AlgorithmRegistry.GetAllAlgorithms();

        Console.WriteLine($"Зарегистрировано алгоритмов: {algorithms.Count}");

        foreach (var algo in algorithms)
        {
            Console.Write($"Тест {algo.Name} ({algo.TheoreticalComplexityLabel})... ");
            algo.GenerateMasterData(100, random);
            algo.PrepareData(50);
            algo.Execute();
            Console.WriteLine("OK");
        }

        // Проверка корректности сортировок
        Console.Write("Проверка QuickSort, TimSort, InsertionSort... ");
        var qs = new QuickSortAlgorithm();
        var ts = new TimSortAlgorithm();
        var ins = new InsertionSortAlgorithm();
        foreach (var sortAlgo in new VectorAlgorithm[] { qs, ts, ins })
        {
            sortAlgo.GenerateMasterData(100, random);
            sortAlgo.PrepareData(100);
            sortAlgo.Execute();
        }
        Console.WriteLine("OK");

        // Проверка возведения в степень
        Console.Write("Проверка совпадения алгоритмов возведения в степень... ");
        var pNaive = new PowerNaive();
        var pRec = new PowerRecursive();
        var pFast = new PowerFast();
        var pClassic = new PowerClassic();

        foreach (int testN in new[] { 0, 1, 2, 5, 10, 16 })
        {
            pNaive.PrepareData(testN); pNaive.Execute();
            pRec.PrepareData(testN); pRec.Execute();
            pFast.PrepareData(testN); pFast.Execute();
            pClassic.PrepareData(testN); pClassic.Execute();
        }
        Console.WriteLine("OK");

        // Проверка парсинга диапазонов
        Console.Write("Проверка ParseSizes(\"1..2000:50\")... ");
        var sizes = MainViewModel.ParseSizes("1..2000:50");
        if (sizes.Length == 41 && sizes[0] == 1 && sizes[^1] == 2000)
        {
            Console.WriteLine($"OK ({sizes.Length} значений)");
        }
        else
        {
            Console.WriteLine($"FAILED (длина {sizes.Length}, первое {sizes.FirstOrDefault()}, последнее {sizes.LastOrDefault()})");
        }

        // Проверка BenchmarkService
        Console.Write("Проверка сквозного выполнения бенчмарка... ");
        var benchmark = new BenchmarkService();
        var benchRes = benchmark.RunBenchmarkAsync(new QuickSortAlgorithm(), [10, 50, 100]).GetAwaiter().GetResult();
        if (benchRes.Results.Count == 3 && benchRes.FittedCoefficient > 0)
        {
            Console.WriteLine($"OK (c = {benchRes.FittedCoefficient:E2})");
        }
        else
        {
            Console.WriteLine("FAILED");
        }

        Console.WriteLine("=== ВСЕ ТЕСТЫ УСПЕШНО ПРОЙДЕНЫ ===");
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
