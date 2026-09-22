namespace AlgorithmAnalysis.Models.Algorithms;

/// <summary>
/// Рекурсивное вычисление числа Фибоначчи F(n).
///
/// Алгоритм:
///   F(0) = 0; F(1) = 1;
///   F(n) = F(n-1) + F(n-2) при n >= 2.
///
/// Теоретическая сложность: O(2^n) — дерево рекурсии ~2^n узлов.
/// Диапазон n ограничен ~35: F(40) ≈ 10^9 вызовов — десятки секунд.
/// </summary>
public class FibonacciRecursive : AbstractAlgorithm
{
    public override string Name => "Числа Фибоначчи: рекурсивный F(n)";
    public override string TheoreticalComplexityLabel => "O(2^n)";
    public override string Group => "Рекурсия";

    public override double TheoreticalComplexity(int n) => Math.Pow(2, n);

    private int _n;
    private long _result;

    /// <summary>Количество рекурсивных вызовов последнего запуска</summary>
    public long LastStepCount { get; private set; }

    public override void GenerateMasterData(int maxN, Random random)
    {
        // Фибоначчи не работает с массивами — ничего не генерируем
    }

    public override void PrepareData(int n)
    {
        _n = n;
        LastStepCount = 0;
    }

    public override void Execute()
    {
        _result = Fib(_n);
    }

    private long Fib(int n)
    {
        LastStepCount++;   // считаем каждый рекурсивный вызов
        if ((LastStepCount & 0x3FF) == 0)
        {
            ThrowIfCancellationRequested();
        }

        if (n <= 0) return 0;
        if (n == 1) return 1;
        return Fib(n - 1) + Fib(n - 2);
    }
}