namespace AlgorithmAnalysis.Models.Algorithms;

/// <summary>
/// Задание I.6: Быстрая сортировка (Quick Sort).
/// Теоретическая сложность: O(n log n) — средний случай, O(n²) — худший.
/// </summary>
public class QuickSortAlgorithm : VectorAlgorithm
{
    public override string Name => "Быстрая сортировка (Quick Sort)";
    public override string TheoreticalComplexityLabel => "O(n log n)";
    public override string Group => "Сортировки";

    protected override bool IsDestructive => true;

    public override double TheoreticalComplexity(int n) => n * Math.Log2(n > 0 ? n : 1);

    public override void Execute()
    {
        if (_workingData.Length > 1)
        {
            QuickSort(_workingData, 0, _workingData.Length - 1);
        }
    }

    private void QuickSort(int[] arr, int low, int high)
    {
        if (low < high)
        {
            ThrowIfCancellationRequested();
            int p = Partition(arr, low, high);
            QuickSort(arr, low, p);
            QuickSort(arr, p + 1, high);
        }
    }

    private static int Partition(int[] arr, int low, int high)
    {
        // Опорный элемент — средний (устойчиво к частично отсортированным данным)
        int pivot = arr[low + (high - low) / 2];
        int i = low - 1;
        int j = high + 1;

        while (true)
        {
            do { i++; } while (arr[i] < pivot);
            do { j--; } while (arr[j] > pivot);

            if (i >= j)
                return j;

            (arr[i], arr[j]) = (arr[j], arr[i]);
        }
    }
}