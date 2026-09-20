namespace AlgorithmAnalysis.Models.Algorithms;

/// <summary>
/// Задание I.8 (Рис. 4) + Часть IV: Классический быстрый алгоритм возведения в степень (QuickPow1).
///
/// Теоретическая сложность по шагам: O(log n).
/// </summary>
public class PowerClassic : PowerAlgorithm
{
    public override string Name => "Возведение в степень: классический быстрый (QuickPow1)";
    public override string TheoreticalComplexityLabel => "O(log n)";

    public override double TheoreticalComplexity(int n) => Math.Log2(n + 1);

    public override void Execute()
    {
        double c = BaseX;
        double f = 1.0;
        int k = _n;
        long steps = 0;

        while (k != 0)
        {
            if (k % 2 == 0)
            {
                c *= c;
                steps++;   // возведение в квадрат
                k /= 2;
            }
            else
            {
                f *= c;
                steps++;   // умножение результата
                k -= 1;
            }
        }

        _result = f;
        LastStepCount = steps;
    }
}