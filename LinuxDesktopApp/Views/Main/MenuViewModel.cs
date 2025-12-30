namespace LinuxDesktopApp.Views.Main;

public sealed partial class MenuViewModel : AppViewModelBase
{
    [ObservableProperty]
    public partial string Message { get; set; }

    public ICommand NavigateCommand { get; }

    public MenuViewModel()
    {
        Message = "Hello from MenuViewModel!";
        NavigateCommand = MakeDelegateCommand(() =>
        {
            Navigator.Forward(ViewId.Sub);
        });
    }
}
