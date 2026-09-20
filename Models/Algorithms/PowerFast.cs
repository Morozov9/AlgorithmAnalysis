namespace AlgorithmAnalysis.Models.Algorithms;

/// <summary>
/// Задание I.8 (Рис. 3) + Часть IV: Быстрый алгоритм возведения в степень (QuickPow).
/// Итеративный алгоритм на основе двоичного разложения.
///
/// Теоретическая сложность по шагам: O(log n).
/// </summary>
public class PowerFast : PowerAlgorithm
{
    public override string Name => "Возведение в степень: быстрый (QuickPow)";
    public override string TheoreticalComplexityLabel => "O(log n)";

    public override double TheoreticalComplexity(int n) => Math.Log2(n + 1);

    public override void Execute()
    {
        if (_n == 0)
        {
            _result = 1.0;
            LastStepCount = 0;
            return;
        }

        double c = BaseX;
        int k = _n;
        double f = (k % 2 == 1) ? c : 1.0;
        long steps = (k % 2 == 1) ? 1 : 0;

        while (true)
        {
            k /= 2;
            c *= c;
            steps++;   // возведение c в квадрат

            if (k % 2 == 1)
            {
                f *= c;
                steps++;   // умножение f на c
            }

            if (k == 0)
                break;
        }

        _result = f;
        LastStepCount = steps;
    }
}