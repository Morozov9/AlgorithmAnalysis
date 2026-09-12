namespace AlgorithmAnalysis.Models.Algorithms;

/// <summary>
/// Задание II: Обычное матричное умножение A×B, матрицы n×n.
/// Теоретическая сложность: O(n³)
/// </summary>
public class MatrixMultiplication : MatrixAlgorithm
{
    public override string Name => "Матричное умножение (n×n)";
    public override string TheoreticalComplexityLabel => "O(n³)";

    public override double TheoreticalComplexity(int n) => (double)n * n * n;

    public override void Execute()
    {
        // TODO: Реализовать стандартное умножение матриц
        // _result[i,j] = Σ(k=0..n-1) _workingA[i,k] * _workingB[k,j]
        throw new NotImplementedException("Реализуйте матричное умножение");
    }
}
