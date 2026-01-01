namespace LinuxDesktopApp.Views.Example;

using LinuxDesktopApp.Views;

public sealed partial class TypographyViewModel : AppViewModelBase
{
    [ObservableProperty]
    public partial string Message { get; set; }

    public ICommand NavigateCommand { get; }

    public TypographyViewModel()
    {
        Message = "Hello from TypographyViewModel!";
        NavigateCommand = MakeDelegateCommand(() =>
        {
            Navigator.Forward(ViewId.Menu);
        });
    }
}
