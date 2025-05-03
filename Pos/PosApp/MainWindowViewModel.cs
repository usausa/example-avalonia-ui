namespace PosApp;

using Smart.Avalonia.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public Navigator Navigator { get; set; }

    public MainWindowViewModel(Navigator navigator)
    {
        Navigator = navigator;
    }
}
