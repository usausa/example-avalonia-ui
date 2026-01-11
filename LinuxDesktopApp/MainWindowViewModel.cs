namespace LinuxDesktopApp;

using Avalonia.Threading;

using LinuxDesktopApp.Settings;
using LinuxDesktopApp.Shell;
using LinuxDesktopApp.Views;

using LinuxDotNet.GameInput;

[ObservableGeneratorOption(Reactive = true, ViewModel = true)]
public class MainWindowViewModel : ExtendViewModelBase
{
    private static readonly ViewId[] Views =
    [
        ViewId.Dashboard,
        ViewId.Typography,
        ViewId.Barcode,
        ViewId.Camera,
        ViewId.Printer,
        ViewId.Controller,
        ViewId.Nfc
    ];

    private readonly IDispatcher dispatcher;

    private readonly GameController controller;

    public Navigator Navigator { get; set; }

    public IObserveCommand ForwardCommand { get; }

    public MainWindowViewModel(
        IDispatcher dispatcher,
        ControllerSetting controllerSetting,
        Navigator navigator,
        GameController controller)
    {
        this.dispatcher = dispatcher;
        Navigator = navigator;

        ForwardCommand = MakeDelegateCommand<ViewId>(x => Navigator.Forward(x));

        this.controller = controller;
        controller.ButtonChanged += ControllerOnButtonChanged;
        if (controllerSetting.UseJoystick)
        {
            controller.Start();
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            controller.ButtonChanged -= ControllerOnButtonChanged;
        }
    }

    private void ControllerOnButtonChanged(byte key, bool pressed)
    {
        if (pressed)
        {
            switch (key)
            {
                case 0:
                    NotifyTrigger(ShellEvent.Trigger1);
                    break;
                case 1:
                    NotifyTrigger(ShellEvent.Trigger2);
                    break;
                case 2:
                    NotifyTrigger(ShellEvent.Trigger3);
                    break;
                case 3:
                    NotifyTrigger(ShellEvent.Trigger4);
                    break;
                case 4:
                    SwitchView(false);
                    break;
                case 5:
                    SwitchView(true);
                    break;
            }
        }
    }

    private void SwitchView(bool right)
    {
        dispatcher.Post(() =>
        {
            if (Navigator.CurrentViewId is not ViewId viewId)
            {
                return;
            }

            var index = Array.IndexOf(Views, viewId);
            index = index < 0 ? 0 : (index + (right ? 1 : -1));
            index = (index + Views.Length) % Views.Length;
            Navigator.Forward(Views[index]);
        });
    }

    private void NotifyTrigger(ShellEvent ev)
    {
        dispatcher.Post(() =>
        {
            Navigator.NotifyAsync(ev);
        });
    }
}
