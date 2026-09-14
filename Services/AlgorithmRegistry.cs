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
            
            // I.8a — Возведение в степень: простой итеративный (Рис. 1, Pow)
            new PowerNaive(),

            // I.8b — Возведение в степень: рекурсивный (Рис. 2, RecPow)
            new PowerRecursive(),
            
            // I.8c — Возведение в степень: быстрый (Рис. 3, QuickPow)
            new PowerFast(),

            // I.8d — Возведение в степень: классический быстрый (Рис. 4, QuickPow1)
            new PowerClassic(),
            
            // II — Матричное умножение
            new MatrixMultiplication(),
            
            // III — Сортировка слиянием
            new MergeSortAlgorithm(),

            // III — Сортировка вставками (Insertion Sort)
            new InsertionSortAlgorithm()
        ];
    }

    /// <summary>
    /// Возвращает рекомендуемые размеры данных для алгоритма.
    /// Подобраны с учётом задания (n от 1 до 2000) и сложности алгоритма,
    /// чтобы графики строились быстро и наглядно отражали теоретические кривые.
    /// </summary>
    public static int[] GetRecommendedSizes(AbstractAlgorithm algorithm)
    {
        return algorithm.TheoreticalComplexityLabel switch
        {
            // Для O(n³) ограничено 300, чтобы избежать зависания (2000³ операций = 8 млрд)
            "O(n³)" => BuildLinearSizes(min: 10, max: 300, step: 20),

            // Для O(n²) диапазон в точности соответствует лабораторной работе (до 2000)
            "O(n²)" => BuildLinearSizes(min: 50, max: 2000, step: 50),

            // Для O(n log n) диапазон до 2000 с шагом 50
            "O(n log n)" => BuildLinearSizes(min: 50, max: 2000, step: 50),

            // Для O(n) диапазон до 2000 с шагом 50
            "O(n)" => BuildLinearSizes(min: 50, max: 2000, step: 50),

            // Для O(log n) логарифмическая шкала для лучшей наглядности кривой
            "O(log n)" => BuildSizes(min: 10, max: 2000000, count: 40),

            // Для O(1) диапазон до 2000 с шагом 50
            "O(1)" => BuildLinearSizes(min: 50, max: 2000, step: 50),

            _ => BuildLinearSizes(min: 50, max: 2000, step: 50)
        };
    }

    /// <summary>
    /// Создает массив размеров с равномерным линейным шагом.
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
        return [.. list.Distinct()];
    }

    /// <summary>
    /// Создает массив размеров в геометрической прогрессии.
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

        return sizes.Distinct().ToArray();
    }
}
