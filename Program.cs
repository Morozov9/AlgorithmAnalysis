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

        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
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