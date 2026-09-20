namespace AlgorithmAnalysis.Models.Algorithms;

/// <summary>
/// Базовый класс для алгоритмов, работающих с матрицами n×n.
/// Генерирует две мастер-матрицы A и B, обрезает до n×n.
/// </summary>
public abstract class MatrixAlgorithm : AbstractAlgorithm
{
    /// <summary>Мастер-матрица A (n × m)</summary>
    protected double[,] _masterA = new double[0, 0];

    /// <summary>Мастер-матрица B (m × k)</summary>
    protected double[,] _masterB = new double[0, 0];

    /// <summary>Матрица результата C (n × k)</summary>
    protected double[,] _result = new double[0, 0];

    public int CurrentN { get; protected set; }
    public int CurrentM { get; protected set; }
    public int CurrentK { get; protected set; }

    public override string Group => "Матрицы";

    public override void GenerateMasterData(int maxN, Random random)
    {
        GenerateMasterData(maxN, maxN, maxN, random);
    }

    public virtual void GenerateMasterData(int maxN, int maxM, int maxK, Random random)
    {
        _masterA = new double[maxN, maxM];
        _masterB = new double[maxM, maxK];
        _result = new double[maxN, maxK];

        for (int i = 0; i < maxN; i++)
        {
            for (int j = 0; j < maxM; j++)
            {
                _masterA[i, j] = random.Next(0, 100);
            }
        }

        for (int i = 0; i < maxM; i++)
        {
            for (int j = 0; j < maxK; j++)
            {
                _masterB[i, j] = random.Next(0, 100);
            }
        }
    }

    public override void PrepareData(int n)
    {
        PrepareData(n, n, n);
    }

    public virtual void PrepareData(int n, int m, int k = -1)
    {
        CurrentN = n;
        CurrentM = m;
        CurrentK = k > 0 ? k : n;
    }
}
