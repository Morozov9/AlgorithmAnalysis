using CommunityToolkit.Mvvm.ComponentModel;
using AlgorithmAnalysis.Models;

namespace AlgorithmAnalysis.ViewModels;

/// <summary>
/// Обёртка над алгоритмом для отображения в списке с чекбоксом.
/// При изменении IsSelected уведомляет родительскую ViewModel.
/// </summary>
public partial class SelectableAlgorithm : ObservableObject
{
    public AbstractAlgorithm Algorithm { get; }

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasResult))]
    public partial BenchmarkResult? LastResult { get; set; }

    public bool HasResult => LastResult != null;

    /// <summary>Событие для родительской VM: изменение выбора</summary>
    public event EventHandler? SelectionChanged;

    public SelectableAlgorithm(AbstractAlgorithm algorithm)
    {
        Algorithm = algorithm;
        IsSelected = false;
    }

    public string Name => Algorithm.Name;
    public string TheoreticalComplexityLabel => Algorithm.TheoreticalComplexityLabel;
    public string Group => Algorithm.Group;

    partial void OnIsSelectedChanged(bool value)
    {
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }
}