using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WindowsInput;
using WindowsInput.Native;

namespace ePSXeQuitter
{
    public partial class MainWindow : Window
    {
        public ePSXeQuitterNotifyIcon notifyIconWrapper;
        private ePSXeQuitterSettings settings;
        private GameTitle gameTitle;
        private Process ePSXeProcess;
        private ePSXeQuitterGamepad gamepad;
        private Dictionary<string, int> gamepadCount = new Dictionary<string, int>();

        private InputSimulator inputSimulator;

        public MainWindow()
        {
            InitializeComponent();

            // avoid activate window
            WindowStyle = WindowStyle.None;
            Topmost = true;
            AllowsTransparency = true;
            ShowInTaskbar = false;

            settings = new ePSXeQuitterSettings();

            BuildMainMenu();

            notifyIconWrapper = new ePSXeQuitterNotifyIcon();

            gameTitle = new GameTitle();

            gamepad = new ePSXeQuitterGamepad();
            SetGamePadEvent();

            inputSimulator = new InputSimulator();

            List<string> args = new List<string>(Environment.GetCommandLineArgs());
            args.RemoveAt(0);
            ePSXeProcess = ExecuteEPSXe(settings.ePSXePath, args);
        }

        private void listBox_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var menuItem = (MenuItem)listBox.SelectedItem;
            if (menuItem != null)
            {
                menuItem.FireCommand();
            }
            else
            {
                // 
            }
        }

        public void BuildMainMenu()
        {
            var menuItems = ((ePSXeQuitterViewModel)DataContext).MenuItems;
            menuItems.Clear();

            MenuItem menuItem1 = new MenuItem();
            menuItem1.Content = "Save State";
            menuItem1.MouseLeftButtonDownEvent += (sender, e) => {
                BuildStateMenu(StateMenuType.Save);
            };
            menuItems.Add(menuItem1);

            MenuItem menuItem2 = new MenuItem();
            menuItem2.Content = "Load State";
            menuItem2.MouseLeftButtonDownEvent += (sender, e) => {
                BuildStateMenu(StateMenuType.Load);
            };
            menuItems.Add(menuItem2);

            MenuItem menuItem3 = new MenuItem();
            menuItem3.Content = "Save State & Exit ePSXe";
            menuItem3.MouseLeftButtonDownEvent += (sender, e) => {
                BuildStateMenu(StateMenuType.SaveAndClose);
            };
            menuItems.Add(menuItem3);

            MenuItem menuItem4 = new MenuItem();
            menuItem4.Content = "Exit ePSXe without saving";
            menuItem4.MouseLeftButtonDownEvent += (sender, e) => {
                gameTitle.ClearStateBackupFolder();
                TryClosingEPSXe();
                Application.Current.Shutdown();
            };
            menuItems.Add(menuItem4);

            MenuItem menuItem5 = new MenuItem();
            menuItem5.Content = "Cancel";
            menuItem5.MouseLeftButtonDownEvent += (sender, e) => {
                gameTitle.ClearStateBackupFolder();
                Hide();
            };
            menuItems.Add(menuItem5);

            listBox.SelectedIndex = 0;
        }

        private enum StateMenuType
        {
            Save,
            Load,
            SaveAndClose
        }
        private void BuildStateMenu(StateMenuType stateMenuType)
        {
            var menuItems = ((ePSXeQuitterViewModel)DataContext).MenuItems;
            menuItems.Clear();

            MenuItem menuItemBack = new MenuItem();
            menuItemBack.Content = "Back";
            menuItemBack.MouseLeftButtonDownEvent += (sender, e) => { BuildMainMenu(); };
            menuItems.Add(menuItemBack);

            var stateFilesPath = gameTitle.GetStateFiles();
            if (stateMenuType == StateMenuType.Load)
            {
                foreach (string stateFilePath in stateFilesPath) {
                    MenuItem menuItem = new MenuItem();
                    menuItem.Path = stateFilePath;
                    menuItem.Content = Path.GetExtension(stateFilePath) + ": " + File.GetLastWriteTime(stateFilePath).ToString();
                    menuItem.MouseLeftButtonDownEvent += (sender, e) => {
                        Hide();
                        gameTitle.PseudoLoadState(((MenuItem)listBox.SelectedItem).Path);
                    };
                    menuItems.Add(menuItem);
                }
            }
            else // save or "save and close"
            {
                for (int i = 0; i < settings.StateCount; i++)
                {
                    string checkExtention = ".00" + i;
                    var stateFilePath = stateFilesPath.FirstOrDefault(x => Path.GetExtension(x) == checkExtention);

                    if (stateFilePath != null)
                    {
                        MenuItem menuItem = new MenuItem();
                        menuItem.Path = stateFilePath;
                        menuItem.Content = Path.GetExtension(stateFilePath) + ": " + File.GetLastWriteTime(stateFilePath).ToString();
                        menuItem.MouseLeftButtonDownEvent += (sender, e) => {
                            Hide();
                            if (stateMenuType == StateMenuType.Save)
                            {
                                gameTitle.PseudoSaveState(listBox.SelectedIndex - 1);
                            }
                            else if (stateMenuType == StateMenuType.Load)
                            {
                                gameTitle.PseudoLoadState(((MenuItem)listBox.SelectedItem).Path);
                            }
                            else if (stateMenuType == StateMenuType.SaveAndClose)
                            {
                                gameTitle.PseudoSaveState(listBox.SelectedIndex - 1);
                                TryClosingEPSXe();
                                Application.Current.Shutdown();
                            }
                        };
                        menuItems.Add(menuItem);
                    }
                    else
                    {
                        MenuItem menuItem = new MenuItem();
                        menuItem.Content = checkExtention + ": (none)";
                        menuItem.MouseLeftButtonDownEvent += (sender, e) => {
                            Hide();
                            gameTitle.PseudoSaveState(listBox.SelectedIndex - 1);
                            if (stateMenuType == StateMenuType.SaveAndClose)
                            {
                                TryClosingEPSXe();
                                Application.Current.Shutdown();
                            }
                        };
                        menuItems.Add(menuItem);
                    }
                }
            }
            listBox.SelectedIndex = 0;
        }

        private void SetGamePadEvent()
        {
            gamepad.gamepadHotkeyDownEvent += (sender, e) =>
            {
                if (gamepadCount["Hotkey"] == 0)
                {
                    if (ePSXeProcess != null)
                    {
                        if (ePSXeProcess.HasExited)
                        {
                            gameTitle.ClearStateBackupFolder();
                            TryClosingEPSXe();
                            Application.Current.Shutdown();
                        }
                        else if (Visibility == Visibility.Visible)
                        {
                            gameTitle.ClearStateBackupFolder();
                            Hide();
                        }
                        else if (ePSXeProcess.Id == GetActiveProcess().Id)
                        {
                            if (settings.StateSaveType == StateSaveTypes.Cyclic)
                            {
                                gameTitle.PreviewPseudoSaveState();
                            }
                            BuildMainMenu();
                            Show();
                        }
                        else
                        {
                            // what should be done?
                        }
                    }
                    else // ePSXeProcess == null
                    {
                        gameTitle.ClearStateBackupFolder();
                        Application.Current.Shutdown();
                    }
                    gamepadCount["Hotkey"]++;
                }
            };

            gamepad.gamepadHotkeyUpEvent += (sender, e) =>
            {
                gamepadCount["Hotkey"] = 0;
            };

            gamepad.gamepadAButtonDownEvent += (sender, e) =>
            {
                if (ePSXeQuitterSettings.Region == Regions.JP)
                {
                    if (Visibility == Visibility.Visible)
                    {
                        if (gamepadCount["A"] == 0)
                        {
                            var menuItems = ((ePSXeQuitterViewModel)DataContext).MenuItems;
                            var menuItemBack = menuItems.FirstOrDefault(x => x.Content == "Back");
                            if (menuItemBack != null)
                            {
                                menuItemBack.FireCommand();
                            }
                        }
                        gamepadCount["A"]++;
                    }
                }
                else
                {
                    if (Visibility == Visibility.Visible)
                    {
                        if (gamepadCount["A"] == 0)
                        {
                            listBox.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, System.Windows.Input.MouseButton.Left) { RoutedEvent = MouseLeftButtonDownEvent });
                        }
                        gamepadCount["A"]++;
                    }
                }
            };

            gamepad.gamepadAButtonUpEvent += (sender, e) =>
            {
                gamepadCount["A"] = 0;
            };

            gamepad.gamepadBButtonDownEvent += (sender, e) =>
            {
                if (ePSXeQuitterSettings.Region == Regions.JP)
                {
                    if (Visibility == Visibility.Visible)
                    {
                        if (gamepadCount["B"] == 0)
                        {
                            listBox.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, System.Windows.Input.MouseButton.Left) { RoutedEvent = MouseLeftButtonDownEvent });
                        }
                        gamepadCount["B"]++;
                    }
                }
                else
                {
                    if (Visibility == Visibility.Visible)
                    {
                        if (gamepadCount["B"] == 0)
                        {
                            var menuItems = ((ePSXeQuitterViewModel)DataContext).MenuItems;
                            var menuItemBack = menuItems.FirstOrDefault(x => x.Content == "Back");
                            if (menuItemBack != null)
                            {
                                menuItemBack.FireCommand();
                            }
                        }
                        gamepadCount["B"]++;
                    }
                }
            };

            gamepad.gamepadBButtonUpEvent += (sender, e) =>
            {
                gamepadCount["B"] = 0;
            };

            gamepad.gamepadUpButtonDownEvent += (sender, e) =>
            {
                if (Visibility == Visibility.Visible)
                {
                    if (gamepadCount["Up"] % 5 == 0)
                    {
                        if (listBox.SelectedIndex > 0)
                        {
                            listBox.SelectedIndex--;
                        }
                    }
                    gamepadCount["Up"]++;
                }
            };

            gamepad.gamepadUpButtonUpEvent += (sender, e) =>
            {
                gamepadCount["Up"] = 0;
            };

            gamepad.gamepadDownButtonDownEvent += (sender, e) =>
            {
                if (Visibility == Visibility.Visible)
                {
                    if (gamepadCount["Down"] % 5 == 0)
                    {
                        if (listBox.SelectedIndex < listBox.Items.Count)
                        {
                            listBox.SelectedIndex++;
                        }
                    }
                    gamepadCount["Down"]++;
                }
            };

            gamepad.gamepadDownButtonUpEvent += (sender, e) =>
            {
                gamepadCount["Down"] = 0;
            };

            gamepad.gamepadLeftStickUpEvent += (sender, e) =>
            {
                if (Visibility == Visibility.Visible)
                {
                    if (gamepadCount["LeftStick"] <= 0 || (gamepadCount["LeftStick"] > 0 && gamepadCount["LeftStick"] % 5 == 0))
                    {
                        if (listBox.SelectedIndex > 0)
                        {
                            listBox.SelectedIndex--;
                        }
                        gamepadCount["LeftStick"] = 0;
                    }
                    gamepadCount["LeftStick"]++;
                }
            };

            gamepad.gamepadLeftStickDownEvent += (sender, e) =>
            {
                if (Visibility == Visibility.Visible)
                {
                    if (gamepadCount["LeftStick"] >= 0 || (gamepadCount["LeftStick"] < 0 && gamepadCount["LeftStick"] % 5 == 0))
                    {
                        if (listBox.SelectedIndex < listBox.Items.Count)
                        {
                            listBox.SelectedIndex++;
                        }
                        gamepadCount["LeftStick"] = 0;
                    }
                    gamepadCount["LeftStick"]--;
                }
            };

            gamepad.gamepadLeftStickNeutralEvent += (sender, e) =>
            {
                gamepadCount["LeftStick"] = 0;
            };
        }

        public Process GetActiveProcess()
        {
            int processId;
            IntPtr hWnd = GetForegroundWindow();
            GetWindowThreadProcessId(hWnd, out processId);
            Process process = Process.GetProcessById(processId);

            return process;
        }

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private Process ExecuteEPSXe(string ePSXePath, List<string> args)
        {
            if (args.Count() <= 1)
            {
                return null;
            }

            Process p = new Process();
            if (!File.Exists(ePSXePath)) {
                string appendText = DateTime.Now.ToString() + "\t" + "Couldn't find ePSXe: " + ePSXePath + Environment.NewLine;
                File.AppendAllText(@"log.txt", appendText);
                Application.Current.Shutdown();
            }
            p.StartInfo.FileName = ePSXePath;
            p.StartInfo.WorkingDirectory = Path.GetDirectoryName(ePSXePath);
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardInput = false;

            string optionPattern = "^-";
            List<string> _args = new List<string>();
            foreach (string arg in args)
            {
                if (Regex.IsMatch(arg, optionPattern))
                {
                    _args.Add(arg);
                }
                else
                {
                    _args.Add(@"""" + arg + @"""");
                }
            }

            p.StartInfo.Arguments = string.Join(" ", _args); // set args
            p.Start();

            return p;
        }

        private void TryClosingEPSXe()
        {
            Hide();

            inputSimulator.Keyboard.ModifiedKeyStroke(VirtualKeyCode.MENU, VirtualKeyCode.F4);
            inputSimulator.Keyboard.Sleep(500);
            inputSimulator.Keyboard.KeyPress(VirtualKeyCode.RETURN);
            /*
            // Though I know it's the right way to close ePSXe using Esc, it doesn' roll back the display resolution.
            inputSimulator.Keyboard.KeyPress(VirtualKeyCode.ESCAPE);
            */
            Thread.Sleep(1500);
            if (ePSXeProcess != null)
            {
                try
                {
                    ePSXeProcess.Kill();
                    ePSXeProcess.Close();
                    ePSXeProcess.Dispose();
                }
                catch { }
            }
        }

        // avoid activate window
        public const int WS_EX_TRANSPARENT = 0x00000020;
        public const int WS_EX_NOACTIVATE = 0x08000000;
        public const int GWL_EXSTYLE = (-20);

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var hwnd = new WindowInteropHelper(this).Handle;
            SetWindowExTransparent(hwnd);
        }

        public static void SetWindowExTransparent(IntPtr hwnd)
        {
            var extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT | WS_EX_NOACTIVATE);
        }

        [DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);
    }
}
