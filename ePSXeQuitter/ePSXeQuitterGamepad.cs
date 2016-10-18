using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

using XInputDotNetPure;
using XInputReporter;

namespace ePSXeQuitter
{
    public delegate void GamepadEventHandler(object sender, EventArgs e);
    enum GamePadButtonsAndDpad
    {
        Start, Back, LeftStick, RightStick, LeftShoulder, RightShoulder, Guide, A, B, X, Y,
        Up, Down, Left, Right, LeftTrigger, RightTrigger,
        Select = 1,
        L1 = 4,
        R1 = 5,
        PS = 6,
        Playstation = 6,
        Circle = 8,
        Cross = 7,
        Square = 9,
        Triangle = 10,
        L2 = 15,
        R2 = 16,
    }

    class ePSXeQuitterGamepad
    {
        // 
        private ReporterState reporterState;
        private List<int> hotkeys = new List<int>();
        DispatcherTimer dispatcherTimer;

        // 
        public event GamepadEventHandler gamepadHotkeyDownEvent;
        public event GamepadEventHandler gamepadHotkeyUpEvent;
        public event GamepadEventHandler gamepadGuideButtonDownEvent;
        public event GamepadEventHandler gamepadGuideButtonUpEvent;
        public event GamepadEventHandler gamepadAButtonDownEvent;
        public event GamepadEventHandler gamepadAButtonUpEvent;
        public event GamepadEventHandler gamepadBButtonDownEvent;
        public event GamepadEventHandler gamepadBButtonUpEvent;
        public event GamepadEventHandler gamepadUpButtonDownEvent;
        public event GamepadEventHandler gamepadUpButtonUpEvent;
        public event GamepadEventHandler gamepadDownButtonDownEvent;
        public event GamepadEventHandler gamepadDownButtonUpEvent;
        public event GamepadEventHandler gamepadLeftStickUpEvent;
        public event GamepadEventHandler gamepadLeftStickDownEvent;
        public event GamepadEventHandler gamepadLeftStickNeutralEvent;
        public event GamepadEventHandler gamepadLeftTriggerDownEvent;
        public event GamepadEventHandler gamepadLeftTriggerUpEvent;
        public event GamepadEventHandler gamepadRightTriggerDownEvent;
        public event GamepadEventHandler gamepadRightTriggerUpEvent;

        public ePSXeQuitterGamepad()
        {
            ePSXeQuitterSettings settings = new ePSXeQuitterSettings();

            reporterState = new ReporterState();
            if (!reporterState.LastActiveState.IsConnected)
            {
                // should be checked DirectInput
            }

            var hotkeysString = settings.Hotkeys.Split(',').Select(x => x.Trim()).Where(x => !string.IsNullOrWhiteSpace(x));
            foreach (string hotkeyString in hotkeysString)
            {
                try
                {
                    hotkeys.Add((int)Enum.Parse(typeof(GamePadButtonsAndDpad), hotkeyString, true));
                }
                catch { }
            }

            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 1000 / 60);
            dispatcherTimer.Start();
        }

        void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            if (reporterState.Poll())
            {
                GamepadUpdateState();
            }
            else
            {
                // should be checked DirectInput
            }
        }

        private void GamepadUpdateState()
        {
            GamePadState lastActiveState = reporterState.LastActiveState;

            bool hotkeysCalled = true;
            foreach (int hotkey in hotkeys)
            {
                if (hotkey <= 10)
                {
                    hotkeysCalled = hotkeysCalled && ((ButtonState)(lastActiveState.Buttons.GetType().GetProperties())[hotkey].GetValue(lastActiveState.Buttons) == ButtonState.Pressed);
                }
                else if (hotkey <= 14)
                {
                    hotkeysCalled = hotkeysCalled && ((ButtonState)(lastActiveState.DPad.GetType().GetProperties())[hotkey - 11].GetValue(lastActiveState.DPad) == ButtonState.Pressed);
                }
                else if (hotkey <= 16)
                {
                    hotkeysCalled = hotkeysCalled && ((float)(lastActiveState.Triggers.GetType().GetProperties())[hotkey - 15].GetValue(lastActiveState.Triggers) >= 0.4);
                }
            }

            if (hotkeysCalled)
            {
                gamepadHotkeyDownEvent?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                gamepadHotkeyUpEvent?.Invoke(this, EventArgs.Empty);
            }

            if (lastActiveState.Buttons.Guide == ButtonState.Pressed)
            {
                gamepadGuideButtonDownEvent?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                gamepadGuideButtonUpEvent?.Invoke(this, EventArgs.Empty);
            }

            if (lastActiveState.Buttons.A == ButtonState.Pressed)
            {
                gamepadAButtonDownEvent?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                gamepadAButtonUpEvent?.Invoke(this, EventArgs.Empty);
            }

            if (lastActiveState.Buttons.B == ButtonState.Pressed)
            {
                gamepadBButtonDownEvent?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                gamepadBButtonUpEvent?.Invoke(this, EventArgs.Empty);
            }

            if (lastActiveState.DPad.Up == ButtonState.Pressed)
            {
                gamepadUpButtonDownEvent?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                gamepadUpButtonUpEvent?.Invoke(this, EventArgs.Empty);
            }

            if (lastActiveState.DPad.Down == ButtonState.Pressed)
            {
                gamepadDownButtonDownEvent?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                gamepadDownButtonUpEvent?.Invoke(this, EventArgs.Empty);
            }

            if (lastActiveState.ThumbSticks.Left.Y >= 0.6)
            {
                gamepadLeftStickUpEvent?.Invoke(this, EventArgs.Empty);
            }
            else if (lastActiveState.ThumbSticks.Left.Y <= -0.8)
            {
                gamepadLeftStickDownEvent?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                gamepadLeftStickNeutralEvent?.Invoke(this, EventArgs.Empty);
            }

            if (lastActiveState.Triggers.Left >= 0.6)
            {
                gamepadLeftTriggerDownEvent?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                gamepadLeftTriggerUpEvent?.Invoke(this, EventArgs.Empty);
            }

            if (lastActiveState.Triggers.Right >= 0.6)
            {
                gamepadRightTriggerDownEvent?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                gamepadRightTriggerUpEvent?.Invoke(this, EventArgs.Empty);
            }
        }

        public void Timer_Stop()
        {
            dispatcherTimer.Stop();
        }
    }
}
