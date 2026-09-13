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
    /// Чтобы добавить новый алгоритм — просто добавьте его в этот список.
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
            
            // I.8 — Возведение в степень (наивное)
            new PowerNaive(),
            
            // I.8 — Возведение в степень (быстрое)
            new PowerFast(),
            
            // II — Матричное умножение
            new MatrixMultiplication(),
            
            // III - MergeSort
            new MergeSortAlgorithm()
        ];
    }

    /// <summary>
    /// Возвращает рекомендуемые размеры данных для алгоритма.
    /// Подобраны так, чтобы на графиках было хорошо видно кривую:
    /// — Для O(n³) и O(n²) — маленькие размеры (иначе будет очень долго)
    /// — Для O(n log n) и O(n) — можно побольше
    /// — Для O(1) и O(log n) — ещё больше
    /// </summary>
    public static int[] GetRecommendedSizes(AbstractAlgorithm algorithm)
    {
        return algorithm.TheoreticalComplexityLabel switch
        {
            "O(n³)" => BuildSizes(min: 10, max: 500, count: 20),
            "O(n²)" => BuildSizes(min: 100, max: 10000, count: 30),
            "O(n log n)" => BuildSizes(min: 100, max: 100000, count: 50),
            "O(n)" => BuildSizes(min: 100, max: 1000000, count: 1000),
            "O(log n)" => BuildSizes(min: 10, max: 10000000, count: 1000),
            "O(1)" => BuildSizes(min: 10, max: 1000000, count: 1000),
            _ => BuildSizes(min: 100, max: 50000, count: 50)
        };
    }

    private static int[] BuildSizes(int min, int max, int count)
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
