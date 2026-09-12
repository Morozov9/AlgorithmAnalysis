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
            "O(n³)" => [10, 25, 50, 75, 100, 150, 200, 250, 300, 400, 500],
            "O(n²)" => [100, 250, 500, 1000, 2000, 3000, 5000, 7000, 10000],
            "O(n log n)" => [100, 500, 1000, 2000, 5000, 10000, 20000, 50000, 100000],
            "O(n)" => [100, 500, 1000, 5000, 10000, 50000, 100000, 500000, 1000000],
            "O(log n)" => [10, 100, 1000, 10000, 100000, 1000000, 10000000],
            "O(1)" => [10, 100, 1000, 10000, 100000, 1000000],
            _ => [100, 500, 1000, 2000, 5000, 10000, 20000, 50000]
        };
    }
}
