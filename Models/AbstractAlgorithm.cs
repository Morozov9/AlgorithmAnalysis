namespace AlgorithmAnalysis.Models;

/// <summary>
/// Абстрактный базовый класс для всех алгоритмов.
/// Определяет контракт: генерация данных, подготовка (срез), выполнение.
/// 
/// Паттерн работы:
/// 1. GenerateMasterData(maxN) — генерируем один большой набор данных
/// 2. PrepareData(n) — берём срез первых n элементов (копируя для деструктивных алгоритмов)
/// 3. Execute() — выполняем алгоритм над подготовленными данными
/// </summary>
public abstract class AbstractAlgorithm
{
    /// <summary>
    /// Название алгоритма для отображения в UI.
    /// Например: "Сортировка пузырьком (Bubble Sort)"
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// Подпись теоретической сложности для графика.
    /// Например: "O(n²)", "O(n log n)", "O(n)"
    /// </summary>
    public abstract string TheoreticalComplexityLabel { get; }

    /// <summary>
    /// Группа алгоритма для UI (категория в списке).
    /// Например: "Функции", "Сортировки", "Матрицы"
    /// </summary>
    public abstract string Group { get; }

    /// <summary>
    /// Теоретическая функция сложности: f(n) → ожидаемое число операций.
    /// Используется для построения теоретической кривой на графике.
    /// Например: для O(n²) вернуть n*n, для O(n log n) — n*log2(n).
    /// </summary>
    public abstract double TheoreticalComplexity(int n);

    /// <summary>
    /// Измеряется ли для данного алгоритма количество элементарных операций/шагов
    /// вместо машинного времени (для степенных алгоритмов — true).
    /// </summary>
    public virtual bool MeasureSteps => false;

    /// <summary>Подпись оси Y на графике</summary>
    public virtual string ValueAxisTitle => MeasureSteps ? "Число операций (шагов)" : "Время выполнения (мс)";

    /// <summary>Подпись оси X на графике</summary>
    public virtual string ArgumentAxisTitle => MeasureSteps ? "Показатель степени n" : "Размерность входных данных n";

    /// <summary>Краткое обозначение единицы измерения</summary>
    public virtual string UnitName => MeasureSteps ? "шагов" : "мс";

    /// <summary>
    /// Генерирует "мастер-данные" максимального размера.
    /// Вызывается один раз перед серией экспериментов.
    /// Для векторных алгоритмов — большой массив int[maxN].
    /// Для матричных — матрицы maxN×maxN.
    /// </summary>
    /// <param name="maxN">Максимальный размер данных</param>
    /// <param name="random">Генератор случайных чисел (для воспроизводимости)</param>
    public abstract void GenerateMasterData(int maxN, Random random);

    /// <summary>
    /// Подготавливает данные размера n из мастер-данных (срез).
    /// Для деструктивных алгоритмов (сортировки) — копирует данные.
    /// Вызывается перед каждым запуском Execute().
    /// </summary>
    /// <param name="n">Текущий размер данных</param>
    public abstract void PrepareData(int n);

    /// <summary>
    /// Выполняет алгоритм над подготовленными данными.
    /// Замер времени делается снаружи (BenchmarkService).
    /// </summary>
    public abstract void Execute();

    public override string ToString() => Name;
}
