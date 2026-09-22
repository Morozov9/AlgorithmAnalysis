namespace AlgorithmAnalysis.Models.Algorithms;

/// Сортировка слиянием (Merge Sort).
/// Теоретическая сложность: O(n log n)
public class MergeSortAlgorithm : VectorAlgorithm
{
    public override string Name => "Сортировка слиянием (Merge Sort)";
    public override string TheoreticalComplexityLabel => "O(n log n)";
    public override string Group => "Сортировки";

    protected override bool IsDestructive => true;

    public override double TheoreticalComplexity(int n) => n * Math.Log2(n > 0 ? n : 1);

    public override void Execute()
    {
        MergeSort(_workingData, 0, _workingData.Length - 1);
    }

    private void MergeSort(int[] arr, int left, int right)
    {
        if (left >= right) return;
        ThrowIfCancellationRequested();

        int mid = (left + right) / 2;

        MergeSort(arr, left, mid);
        MergeSort(arr, mid + 1, right);

        Merge(arr, left, mid, right);
    }

    private void Merge(int[] arr, int left, int mid, int right)
    {
        int n1 = mid - left + 1;
        int n2 = right - mid;

        int[] leftArr = new int[n1];
        int[] rightArr = new int[n2];

        Array.Copy(arr, left, leftArr, 0, n1);
        Array.Copy(arr, mid + 1, rightArr, 0, n2);

        int i = 0, j = 0, k = left;

        while (i < n1 && j < n2)
        {
            if (leftArr[i] <= rightArr[j])
                arr[k++] = leftArr[i++];
            else
                arr[k++] = rightArr[j++];
        }

        while (i < n1) arr[k++] = leftArr[i++];
        while (j < n2) arr[k++] = rightArr[j++];
    }
}