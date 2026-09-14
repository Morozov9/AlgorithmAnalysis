namespace AlgorithmAnalysis.Models.Algorithms;

/// <summary>
/// Задание I.8 (Рис. 1): Возведение в степень (простой итеративный алгоритм).
/// По формуле f = 1; while (k < n) { f = f * x; k++; }
/// Теоретическая сложность: O(n)
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

        while (k < _n)
        {
            f *= BaseX;
            k++;
        }

        _result = f;
    }
}
