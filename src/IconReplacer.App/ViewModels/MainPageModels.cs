using System.Windows.Input;
using Microsoft.UI.Xaml;

namespace IconReplacer.App.ViewModels;

public sealed record MetricTileViewModel(
    string Label,
    string Value,
    string Detail);

public sealed record ContentRowViewModel(
    string Title,
    string Detail,
    string Status,
    string? ActionLabel = null,
    ICommand? ActionCommand = null,
    bool IsActionEnabled = true)
{
    public Visibility ActionVisibility =>
        string.IsNullOrWhiteSpace(ActionLabel) ? Visibility.Collapsed : Visibility.Visible;
}
