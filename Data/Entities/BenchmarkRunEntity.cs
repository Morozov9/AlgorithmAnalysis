namespace AlgorithmAnalysis.Data.Entities;

/// <summary>
/// Сущность EF Core — один запуск алгоритма на конкретном размере данных.
/// Отображается на таблицу BenchmarkRuns в SQLite.
/// </summary>
public class BenchmarkRunEntity
{
    /// <summary>Первичный ключ (автоинкремент)</summary>
    public int Id { get; set; }

    /// <summary>Название алгоритма (например "Сортировка пузырьком")</summary>
    public string AlgorithmName { get; set; } = string.Empty;

    /// <summary>Размер входных данных n</summary>
    public int N { get; set; }

    /// <summary>
    /// Второй размер m — используется только для матричных алгоритмов (n x m).
    /// Для всех остальных алгоритмов — null.
    /// </summary>
    public int? M { get; set; }

    /// <summary>Порядковый номер запуска (1..RunsPerSize)</summary>
    public int RunIndex { get; set; }

    /// <summary>Затраченное время в миллисекундах</summary>
    public double ElapsedMs { get; set; }

    /// <summary>
    /// Количество шагов алгоритма (если алгоритм поддерживает счётчик шагов).
    /// Для алгоритмов без счётчика — null.
    /// </summary>
    public long? StepCount { get; set; }

    /// <summary>Дата и время эксперимента в UTC</summary>
    public DateTime ExperimentDate { get; set; }

    /// <summary>Seed генератора случайных чисел (для воспроизводимости)</summary>
    public int RandomSeed { get; set; }
}
