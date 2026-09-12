namespace AlgorithmAnalysis.Models.Algorithms;

/// <summary>
/// Базовый класс для алгоритмов, работающих с матрицами n×n.
/// Генерирует две мастер-матрицы A и B, обрезает до n×n.
/// </summary>
public abstract class MatrixAlgorithm : AbstractAlgorithm
{
    /// <summary>Мастер-матрица A максимального размера</summary>
    protected double[,] _masterA = new double[0, 0];

    /// <summary>Мастер-матрица B максимального размера</summary>
    protected double[,] _masterB = new double[0, 0];

    /// <summary>Рабочая матрица A (n×n срез)</summary>
    protected double[,] _workingA = new double[0, 0];

    /// <summary>Рабочая матрица B (n×n срез)</summary>
    protected double[,] _workingB = new double[0, 0];

    /// <summary>Результат умножения</summary>
    protected double[,] _result = new double[0, 0];

    public override string Group => "Матрицы";

    public override void GenerateMasterData(int maxN, Random random)
    {
        _masterA = new double[maxN, maxN];
        _masterB = new double[maxN, maxN];

        for (int i = 0; i < maxN; i++)
        {
            for (int j = 0; j < maxN; j++)
            {
                _masterA[i, j] = random.Next(0, 100);
                _masterB[i, j] = random.Next(0, 100);
            }
        }
    }

    public override void PrepareData(int n)
    {
        _workingA = new double[n, n];
        _workingB = new double[n, n];
        _result = new double[n, n];

        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
            {
                _workingA[i, j] = _masterA[i, j];
                _workingB[i, j] = _masterB[i, j];
            }
        }
    }
}
