namespace AlgorithmAnalysis.Models.Algorithms;

/// <summary>
/// Задание I.8 (Рис. 1) + Часть IV: Простой итеративный алгоритм возведения в степень (Pow).
/// f = 1; while (k < n) { f = f * x; k++; }
///
/// Теоретическая сложность по шагам: O(n) — ровно n умножений.
/// </summary>
public class PowerNaive : PowerAlgorithm
{
    public override string Name => "Возведение в степень: простой (Pow)";
    public override string TheoreticalComplexityLabel => "O(n)";

    public override double TheoreticalComplexity(int n) => n;

    public override void Execute()
    {
        double f = 1.0;
        int k = 0;
        long steps = 0;

        while (k < _n)
        {
            f *= BaseX;
            k++;
            steps++;   // считаем каждое умножение
        }

        _result = f;
        LastStepCount = steps;
    }
}