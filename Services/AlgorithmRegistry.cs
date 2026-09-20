using AlgorithmAnalysis.Models;
using AlgorithmAnalysis.Models.Algorithms;

namespace AlgorithmAnalysis.Services;

/// <summary>
/// Реестр доступных алгоритмов.
/// Здесь регистрируются все алгоритмы и определяются рекомендуемые размеры данных.
/// </summary>
public static class AlgorithmRegistry
{
    /// <summary>
    /// Возвращает список всех доступных алгоритмов.
    /// Чтобы добавить новый — просто добавь его в этот список.
    /// </summary>
    public static List<AbstractAlgorithm> GetAllAlgorithms()
    {
        return
        [
            // I.1 — Постоянная функция
            new ConstantFunction(),

            // I.2 — Сумма элементов
            new SumFunction(),

            // I.3 — Произведение элементов
            new ProductFunction(),

            // I.4a — Полином (наивно)
            new PolynomialNaive(),

            // I.4b — Полином (Горнер)
            new PolynomialHorner(),

            // I.5 — Пузырьковая сортировка
            new BubbleSortAlgorithm(),

            // I.6 — Быстрая сортировка
            new QuickSortAlgorithm(),

            // I.7 — Timsort
            new TimSortAlgorithm(),

            // I.8a — Возведение в степень: простой (Pow)
            new PowerNaive(),

            // I.8b — Возведение в степень: рекурсивный (RecPow)
            new PowerRecursive(),

            // I.8c — Возведение в степень: быстрый (QuickPow)
            new PowerFast(),

            // I.8d — Возведение в степень: классический быстрый (QuickPow1)
            new PowerClassic(),

            // II — Матричное умножение
            new MatrixMultiplication(),

            // III — Сортировка слиянием
            new MergeSortAlgorithm(),

            // III — Сортировка вставками
            new InsertionSortAlgorithm(),

            // IV — Числа Фибоначчи (рекурсивный)
            new FibonacciRecursive()
        ];
    }

    /// <summary>
    /// Возвращает рекомендуемые размеры данных для алгоритма.
    /// Подобраны так, чтобы последняя точка была достаточно заметной
    /// (не микросекунды), но программа не зависала.
    /// </summary>
    public static int[] GetRecommendedSizes(AbstractAlgorithm algorithm)
    {
        if (algorithm is PowerAlgorithm)
        {
            return algorithm.TheoreticalComplexityLabel == "O(n)"
                ? BuildLinearSizes(min: 10, max: 2000, step: 50)
                : BuildSizes(min: 2, max: 100000, count: 25);
        }

        return algorithm.TheoreticalComplexityLabel switch
        {
            // O(n³) — очень тяжёлый: до 500 (~секунды)
            "O(n³)" => BuildLinearSizes(min: 10, max: 500, step: 25),

            // O(n²) — тяжёлый: до 5000 (~5 сек на последней точке)
            "O(n²)" => BuildLinearSizes(min: 50, max: 5000, step: 100),

            // O(n log n) — средний: до 100000
            "O(n log n)" => BuildLinearSizes(min: 100, max: 100000, step: 2000),

            // O(n) — быстрый: до 1 000 000
            "O(n)" => BuildLinearSizes(min: 1000, max: 1000000, step: 20000),

            // O(log n) — очень быстрый: до 10 000 000 (геометрически)
            "O(log n)" => BuildSizes(min: 10, max: 10000000, count: 40),

            // O(1) — мгновенный: до 1 000 000
            "O(1)" => BuildLinearSizes(min: 1000, max: 1000000, step: 20000),

            // O(2^n) — только малые значения! F(35) ~ 1 сек, F(40) — минуты
            "O(2^n)" => BuildLinearSizes(min: 1, max: 35, step: 1),

            _ => BuildLinearSizes(min: 50, max: 2000, step: 50)
        };
    }

    /// <summary>
    /// Массив размеров с равномерным линейным шагом.
    /// Гарантирует, что max попадёт в результат.
    /// </summary>
    public static int[] BuildLinearSizes(int min, int max, int step)
    {
        var list = new List<int>();
        for (int val = min; val <= max; val += step)
        {
            list.Add(val);
        }
        if (list.Count == 0 || list[^1] != max)
        {
            list.Add(max);
        }
        return [.. list.Distinct().OrderBy(x => x)];
    }

    /// <summary>
    /// Массив размеров в геометрической прогрессии (count точек от min до max).
    /// Полезно для алгоритмов с огромным диапазоном (O(log n), O(n log n)).
    /// </summary>
    public static int[] BuildSizes(int min, int max, int count)
    {
        if (count < 2) return [min];

        var sizes = new int[count];
        double ratio = Math.Pow((double)max / min, 1.0 / (count - 1));

        double current = min;
        for (int i = 0; i < count; i++)
        {
            sizes[i] = (int)Math.Round(current);
            current *= ratio;
        }

        return sizes.Distinct().OrderBy(x => x).ToArray();
    }
}