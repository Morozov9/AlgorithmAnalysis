namespace AlgorithmAnalysis.Models.Algorithms;

/// <summary>
/// Задание I.8 (Рис. 2): Возведение в степень (рекурсивный алгоритм RecPow).
/// Формула (3):
/// x^n = 1 при n = 0;
/// x * (x^(n div 2))^2 при n нечетном;
/// (x^(n div 2))^2 при n четном.
/// Теоретическая сложность: O(log n)
/// </summary>
public class PowerRecursive : PowerAlgorithm
{
    public override string Name => "Возведение в степень: рекурсивный (RecPow)";
    public override string TheoreticalComplexityLabel => "O(log n)";

    public override double TheoreticalComplexity(int n) => Math.Log2(n > 0 ? n : 1);

    public override void Execute()
    {
        _result = RecPow(BaseX, _n);
    }

    private static double RecPow(double x, int n)
    {
        if (n == 0) return 1.0;

        double f = RecPow(x, n / 2);

        if (n % 2 == 1)
        {
            return f * f * x;
        }

        return f * f;
    }
}
