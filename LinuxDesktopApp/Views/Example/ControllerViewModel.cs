namespace LinuxDesktopApp.Views.Example;

using System.Diagnostics;

using Avalonia.Threading;

using LinuxDesktopApp.Components.Motor;
using LinuxDesktopApp.Settings;
using LinuxDesktopApp.Views;

using LinuxDotNet.GameInput;

public sealed partial class ControllerViewModel : AppViewModelBase
{
    private static readonly (byte R, byte G, byte B)[] ShiftColors =
    [
        (255, 0, 0),       // Red (Low speed)
        (255, 128, 0),     // Orange
        (255, 255, 0),     // Yellow
        (0, 255, 0),       // Green
        (0, 255, 255),     // Cyan
        (0, 0, 255),       // Blue
        (255, 0, 255)      // Magenta (High speed)
    ];

    private readonly IDispatcher dispatcher;

    private readonly GameController gamepad;

    private readonly MotorController motor;

    private readonly PeriodicTimer timer;
    private readonly CancellationTokenSource cancellationTokenSource;

    [ObservableProperty]
    public partial int Fps { get; set; }

    [ObservableProperty]
    public partial int Speed { get; set; }

    [ObservableProperty]
    public partial int Angle { get; set; }

    [ObservableProperty]
    public partial bool Accel { get; set; }

    [ObservableProperty]
    public partial bool Brake { get; set; }

    [ObservableProperty]
    public partial bool ShiftDown { get; set; }

    [ObservableProperty]
    public partial bool ShiftUp { get; set; }

    public ControllerViewModel(
        IDispatcher dispatcher,
        MotorSetting motorSetting,
        GameController gamepad)
    {
        this.dispatcher = dispatcher;
        this.gamepad = gamepad;
        motor = new MotorController(motorSetting.Port);

        timer = new PeriodicTimer(TimeSpan.FromMilliseconds(1000d / 60));
        cancellationTokenSource = new CancellationTokenSource();

        _ = StartTimerAsync();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();
            timer.Dispose();
            motor.Dispose();
        }
    }

    private async Task StartTimerAsync()
    {
        try
        {
            var fps = 0;
            var model = new GameModel();

            motor.Open();
            gamepad.Start();

            // Set initial
            var (r, g, b) = ShiftColors[model.ShiftValue];
            motor.SetLed(r, g, b);
            motor.SetServo(ServoChannel.Servo1, 90);
            motor.SetServo(ServoChannel.Servo2, 20);

            var watch = Stopwatch.StartNew();
            while (await timer.WaitForNextTickAsync(cancellationTokenSource.Token).ConfigureAwait(false))
            {
                var accel = gamepad.GetButtonPressed(0); // A
                var brake = gamepad.GetButtonPressed(1); // B
                var shiftDown = gamepad.GetButtonPressed(2); // X
                var shiftUp = gamepad.GetButtonPressed(3); // Y
                var axis = gamepad.GetAxisValue(0);

                model.Update(accel, brake, shiftDown, shiftUp, axis);

                if (model.IsUpdated)
                {
                    var speed = model.Speed;
                    var angle = model.SteeringAngle;
                    var accelButton = model.Accel;
                    var brakeButton = model.Brake;
                    var shiftDownButton = model.ShiftDown;
                    var shiftUpButton = model.ShiftUp;

                    dispatcher.Post(() =>
                    {
                        Speed = speed;
                        Angle = angle;
                        Accel = accelButton;
                        Brake = brakeButton;
                        ShiftDown = shiftDownButton;
                        ShiftUp = shiftUpButton;
                    });

                    if (model.SteeringChanged)
                    {
                        motor.SetServo(ServoChannel.Servo1, model.SteeringAngle);
                    }

                    if (model.ThrottleChanged)
                    {
                        motor.SetServo(ServoChannel.Servo2, model.ThrottleAngle);
                    }

                    if (model.ShiftChanged)
                    {
                        (r, g, b) = ShiftColors[model.ShiftValue];
                        motor.SetLed(r, g, b);
                    }
                }

                // FPS
                fps++;
                if (watch.ElapsedMilliseconds > 1000)
                {
                    var fpsValue = fps;
                    dispatcher.Post(() => { Fps = fpsValue; });

                    fps = 0;
                    watch.Restart();
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Ignore
        }
        finally
        {
            motor.SetLed(0, 0, 0);
            motor.SetServo(ServoChannel.Servo1, 90);
            motor.SetServo(ServoChannel.Servo2, 20);
            motor.Close();
        }
    }

    private sealed class GameModel
    {
        private const double AccelVelocity1 = 64d / 60;
        private const double AccelVelocity2 = 48d / 60;
        private const double AccelVelocity3 = 32d / 60;
        private const double AccelVelocity4 = 16d / 60;
        private const double BrakeVelocity = 96d / 60;
        private const double DefaultVelocity = 32d / 60;

        private double speed;
        private int prevSpeed;
        private int prevThrottleAngle = -1;
        private int prevSteeringAngle = -1;

        private bool prevAccel;
        private bool prevBrake;
        private bool prevShiftDown;
        private bool prevShiftUp;

        public int Speed { get; private set; }
        public int ThrottleAngle { get; private set; }
        public int SteeringAngle { get; private set; }
        public int ShiftValue { get; private set; }

        public bool Accel { get; private set; }
        public bool Brake { get; private set; }
        public bool ShiftDown { get; private set; }
        public bool ShiftUp { get; private set; }

        public bool IsUpdated { get; private set; }
        public bool SteeringChanged { get; private set; }
        public bool ThrottleChanged { get; private set; }
        public bool ShiftChanged { get; private set; }

        public void Update(bool accel, bool brake, bool shiftDown, bool shiftUp, short axis)
        {
            // Reset flags
            IsUpdated = false;
            SteeringChanged = false;
            ThrottleChanged = false;
            ShiftChanged = false;

            // Update speed
            if (brake)
            {
                speed = Math.Max(0, speed - BrakeVelocity);
            }
            else if (accel)
            {
                var velocity = speed switch
                {
                    < 128 => AccelVelocity1,
                    < 192 => AccelVelocity2,
                    < 224 => AccelVelocity3,
                    _ => AccelVelocity4
                };
                speed = Math.Min(255, speed + velocity);
            }
            else
            {
                speed = Math.Max(0, speed - DefaultVelocity);
            }

            var currentSpeed = (int)speed;

            // Convert speed (0-255) to throttle angle (90-180)
            var throttleAngle = 90 + (currentSpeed * 90 / 255);
            // Convert axis (-32768 to 32767) to steering angle (0-180)
            var steeringAngle = (int)((axis + 32768) * 180.0 / 65535.0);

            // Shift value change
            if (shiftDown && !prevShiftDown)
            {
                if (ShiftValue > 0)
                {
                    ShiftValue--;
                    ShiftChanged = true;
                }
            }
            if (shiftUp && !prevShiftUp)
            {
                if (ShiftValue < ShiftColors.Length - 1)
                {
                    ShiftValue++;
                    ShiftChanged = true;
                }
            }

            // Check if any property changed
            if ((currentSpeed != prevSpeed) ||
                (steeringAngle != prevSteeringAngle) ||
                (accel != prevAccel) ||
                (brake != prevBrake) ||
                (shiftDown != prevShiftDown) ||
                (shiftUp != prevShiftUp))
            {
                IsUpdated = true;

                Speed = currentSpeed;
                Accel = accel;
                Brake = brake;
                ShiftDown = shiftDown;
                ShiftUp = shiftUp;

                if (steeringAngle != prevSteeringAngle)
                {
                    SteeringChanged = true;
                    SteeringAngle = steeringAngle;
                    prevSteeringAngle = steeringAngle;
                }

                if (throttleAngle != prevThrottleAngle)
                {
                    ThrottleChanged = true;
                    ThrottleAngle = throttleAngle;
                    prevThrottleAngle = throttleAngle;
                }

                prevSpeed = currentSpeed;
                prevAccel = accel;
                prevBrake = brake;
                prevShiftDown = shiftDown;
                prevShiftUp = shiftUp;
            }
        }
    }
}
