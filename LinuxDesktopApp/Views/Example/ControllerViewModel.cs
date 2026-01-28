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
        (0, 0, 255),       // Blue (Low speed)
        (0, 255, 255),     // Cyan
        (0, 255, 0),       // Green
        (255, 255, 0),     // Yellow
        (255, 128, 0),     // Orange
        (255, 0, 0)        // Red (High speed)
    ];

    private readonly IDispatcher dispatcher;

    private readonly GameController gamepad;

    private readonly MotorController motor;

    private readonly PeriodicTimer timer;
    private readonly CancellationTokenSource cancellationTokenSource;

    [ObservableProperty]
    public partial int Fps { get; set; }

    [ObservableProperty]
    public partial int Shift { get; set; }

    [ObservableProperty]
    public partial int Speed { get; set; }

    [ObservableProperty]
    public partial int SteeringAngle { get; set; }

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
            motor.SetServo(ServoChannel.Servo2, 90);

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
                    var shift = model.ShiftValue + 1;
                    var speed = model.Speed;
                    var steeringAngle = model.SteeringAngle;
                    var accelButton = model.Accel;
                    var brakeButton = model.Brake;
                    var shiftDownButton = model.ShiftDown;
                    var shiftUpButton = model.ShiftUp;

                    dispatcher.Post(() =>
                    {
                        Shift = shift;
                        Speed = speed;
                        SteeringAngle = steeringAngle;
                        Accel = accelButton;
                        Brake = brakeButton;
                        ShiftDown = shiftDownButton;
                        ShiftUp = shiftUpButton;
                    });

                    if (model.ShiftChanged)
                    {
                        (r, g, b) = ShiftColors[model.ShiftValue];
                        motor.SetLed(r, g, b);
                    }
                    if (model.ThrottleChanged)
                    {
                        motor.SetServo(ServoChannel.Servo2, model.ThrottleAngle);
                    }
                    if (model.SteeringChanged)
                    {
                        motor.SetServo(ServoChannel.Servo1, model.SteeringAngle);
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
            motor.SetServo(ServoChannel.Servo2, 90);
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

        private double rawSpeed;

        public int ShiftValue { get; private set; }
        public int Speed { get; private set; }

        public int ThrottleAngle { get; private set; } = 90;
        public int SteeringAngle { get; private set; } = 90;

        public bool Accel { get; private set; }
        public bool Brake { get; private set; }
        public bool ShiftDown { get; private set; }
        public bool ShiftUp { get; private set; }

        public bool IsUpdated { get; private set; }
        public bool ShiftChanged { get; private set; }
        public bool SteeringChanged { get; private set; }
        public bool ThrottleChanged { get; private set; }

        public void Update(bool accel, bool brake, bool shiftDown, bool shiftUp, short axis)
        {
            // Store previous values
            var prevShiftValue = ShiftValue;
            var prevSpeed = Speed;
            var prevThrottleAngle = ThrottleAngle;
            var prevSteeringAngle = SteeringAngle;
            var prevAccel = Accel;
            var prevBrake = Brake;
            var prevShiftDown = ShiftDown;
            var prevShiftUp = ShiftUp;

            // Update speed
            rawSpeed = CalculateSpeed(accel, brake);
            var currentSpeed = (int)rawSpeed;

            // Convert speed (0-255) to throttle angle (90-180)
            var throttleAngle = 90 + (currentSpeed * 90 / 255);
            // Convert axis (-32768 to 32767) to steering angle (0-180)
            var steeringAngle = (int)((axis + 32768) * 180.0 / 65535.0);

            // Update shift value
            if (shiftDown && !prevShiftDown && ShiftValue > 0)
            {
                ShiftValue--;
            }
            if (shiftUp && !prevShiftUp && ShiftValue < ShiftColors.Length - 1)
            {
                ShiftValue++;
            }

            // Detect changes
            ShiftChanged = ShiftValue != prevShiftValue;
            SteeringChanged = steeringAngle != prevSteeringAngle;
            ThrottleChanged = throttleAngle != prevThrottleAngle;

            IsUpdated = (currentSpeed != prevSpeed) ||
                        (steeringAngle != prevSteeringAngle) ||
                        (accel != prevAccel) ||
                        (brake != prevBrake) ||
                        (shiftDown != prevShiftDown) ||
                        (shiftUp != prevShiftUp);

            // Update properties
            Speed = currentSpeed;
            SteeringAngle = steeringAngle;
            ThrottleAngle = throttleAngle;
            Accel = accel;
            Brake = brake;
            ShiftDown = shiftDown;
            ShiftUp = shiftUp;
        }

        private double CalculateSpeed(bool accel, bool brake)
        {
            if (brake)
            {
                return Math.Max(0, rawSpeed - BrakeVelocity);
            }
            if (accel)
            {
                var velocity = CalculateAccelerationVelocity();
                return Math.Min(255, rawSpeed + velocity);
            }
            return Math.Max(0, rawSpeed - DefaultVelocity);
        }

        private double CalculateAccelerationVelocity()
        {
            return rawSpeed switch
            {
                < 128 => AccelVelocity1,
                < 192 => AccelVelocity2,
                < 224 => AccelVelocity3,
                _ => AccelVelocity4
            };
        }
    }
}
