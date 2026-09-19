namespace AlgorithmAnalysis.Models.Algorithms;

/// <summary>
/// Рекурсивное вычисление числа Фибоначчи F(n).
///
/// Алгоритм:
///   F(0) = 0; F(1) = 1;
///   F(n) = F(n-1) + F(n-2) при n >= 2.
///
/// Теоретическая сложность: O(2^n) — каждый вызов порождает два подвызова,
/// дерево рекурсии имеет ~2^n узлов.
///
/// Из-за экспоненциального роста диапазон n ограничен значениями до ~35,
/// иначе время выполнения становится неприемлемым (F(40) ≈ 10^9 вызовов).
/// </summary>
public class FibonacciRecursive : AbstractAlgorithm
{
    public override string Name => "Числа Фибоначчи: рекурсивный F(n)";
    public override string TheoreticalComplexityLabel => "O(2^n)";
    public override string Group => "Рекурсия";

    /// <summary>
    /// Теоретическая функция сложности O(2^n).
    /// Возвращает 2^n — количество рекурсивных вызовов.
    /// </summary>
    public override double TheoreticalComplexity(int n) => Math.Pow(2, n);

    // Текущий n (индекс числа Фибоначчи для вычисления)
    private int _n;

    // Результат (для предотвращения оптимизации компилятором)
    private long _result;

    /// <summary>
    /// Для Фибоначчи "мастер-данные" не нужны — алгоритм работает только с числом n.
    /// Метод ничего не делает (данные не нужны).
    /// </summary>
    public override void GenerateMasterData(int maxN, Random random)
    {
        // Фибоначчи не работает с массивами — ничего не генерируем
    }

    /// <summary>
    /// Устанавливает текущий n для вычисления F(n).
    /// </summary>
    public override void PrepareData(int n)
    {
        _n = n;
    }

    /// <summary>
    /// Вычисляет F(n) рекурсивно без мемоизации.
    /// </summary>
    public override void Execute()
    {
        _result = Fib(_n);
    }

    private static long Fib(int n)
    {
        if (n <= 0) return 0;
        if (n == 1) return 1;
        return Fib(n - 1) + Fib(n - 2);
    }
}
