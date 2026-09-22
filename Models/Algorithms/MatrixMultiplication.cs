namespace AlgorithmAnalysis.Models.Algorithms;

/// <summary>
/// Задание II: Обычное матричное умножение A×B, матрицы n×n.
/// C[i, j] = Σ(k=0..n-1) A[i, k] * B[k, j]
/// Теоретическая сложность: O(n³)
/// </summary>
public class MatrixMultiplication : MatrixAlgorithm
{
    public override string Name => "Матричное умножение (n×n)";
    public override string TheoreticalComplexityLabel => "O(n³)";

    public override double TheoreticalComplexity(int n) => (double)n * n * n;

    public override void Execute()
    {
        int n = CurrentN;
        int m = CurrentM;
        int k = CurrentK;

        for (int i = 0; i < n; i++)
        {
            ThrowIfCancellationRequested();
            for (int j = 0; j < k; j++)
            {
                double sum = 0;
                for (int p = 0; p < m; p++)
                {
                    sum += _masterA[i, p] * _masterB[p, j];
                }
                _result[i, j] = sum;
            }
        }
    }
}
