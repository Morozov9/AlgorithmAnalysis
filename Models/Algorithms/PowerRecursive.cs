namespace AlgorithmAnalysis.Models.Algorithms;

/// <summary>
/// Задание I.8 (Рис. 2) + Часть IV: Рекурсивный алгоритм возведения в степень (RecPow).
///
/// Формула:
///   x^0 = 1
///   x^n = x · (x^(n div 2))²  при нечётном n
///   x^n = (x^(n div 2))²      при чётном n
///
/// Теоретическая сложность по шагам: O(log n).
/// </summary>
public class PowerRecursive : PowerAlgorithm
{
    public override string Name => "Возведение в степень: рекурсивный (RecPow)";
    public override string TheoreticalComplexityLabel => "O(log n)";

    public override double TheoreticalComplexity(int n) => Math.Log2(n + 1);

    public override void Execute()
    {
        long steps = 0;
        _result = RecPow(BaseX, _n, ref steps);
        LastStepCount = steps;
    }

    private static double RecPow(double x, int n, ref long steps)
    {
        steps++;   // считаем каждый рекурсивный вызов

        if (n == 0) return 1.0;

        double f = RecPow(x, n / 2, ref steps);

        return (n % 2 == 1) ? f * f * x : f * f;
    }
}