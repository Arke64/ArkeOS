using ArkeOS.Hardware.Architecture;
using ArkeOS.Hardware.ArkeIndustries;
using ArkeOS.Utilities.Extensions;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.IO;

namespace ArkeOS.Hosts.Avalonia {
    public partial class MainPage : UserControl {
        private SystemBusController system;
        private Processor processor;
        private Display display;
        private WritableBitmap displayBitmap;
        private DispatcherTimer displayRefreshTimer;
        private Keyboard keyboard;
        private HashSet<Key> currentPressedKeys;

        private Image ScreenImage;
        private Button StopButton;
        private Button BreakButton;
        private Button StartButton;
        private Button RefreshButton;
        private Button ContinueButton;
        private Button StepButton;
        private TextBox InputTextBox;
        private Dispatcher Dispatcher;
        private RadioButton HexRadioButton;
        private RadioButton DecRadioButton;
        private RadioButton BinRadioButton;
        private TextBlock CurrentInstructionLabel;

        public MainPage() {
            this.InitializeComponent();

            this.currentPressedKeys = new HashSet<Key>();
            this.displayRefreshTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1000 / 24) };
            this.displayRefreshTimer.Tick += (s, e) => this.RefreshDisplay();


            this.displayBitmap = new WritableBitmap(160, 120, PixelFormat.Rgba8888);
            this.ClearDisplay();

            this.ScreenImage.Source = this.displayBitmap;

            this.StartButton.IsEnabled = true;
            this.StopButton.IsEnabled = false;
            this.BreakButton.IsEnabled = false;
            this.ContinueButton.IsEnabled = false;
            this.StepButton.IsEnabled = false;
            this.RefreshButton.IsEnabled = false;
        }

        private void RawSetPixel(ILockedFramebuffer fb, int y, int x, byte red, byte green, byte blue, byte alpha) {
            int addr = y * fb.RowBytes + x * 4;
            unsafe
            {
                byte* bitmap = (byte*)fb.Address;
                bitmap[addr] = red;
                bitmap[addr + 1] = green;
                bitmap[addr + 2] = blue;
                bitmap[addr + 3] = alpha;
            }
        }

        private void ClearDisplay() {
            using (var fb = this.displayBitmap.Lock()) {
                for (var y = 0; y < fb.Height; y++) {
                    for (var x = 0; x < fb.Width; x++) {
                        RawSetPixel(fb, y, x, 0, 0, 0, 255);
                    }
                }
            }
        }

        private void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);

            this.Dispatcher = Dispatcher.UIThread;

            this.ScreenImage = this.Get<Image>("ScreenImage");
            this.StopButton = this.Get<Button>("StopButton");
            this.StopButton.Click += this.StopButton_Click;
            this.BreakButton = this.Get<Button>("BreakButton");
            this.BreakButton.Click += this.BreakButton_Click;
            this.ContinueButton = this.Get<Button>("ContinueButton");
            this.StopButton.Click += this.StopButton_Click;
            this.RefreshButton = this.Get<Button>("RefreshButton");
            this.RefreshButton.Click += this.RefreshButton_Click;
            this.StartButton = this.Get<Button>("StartButton");
            this.StartButton.Click += this.StartButton_Click;
            this.InputTextBox = this.Get<TextBox>("InputTextBox");
            this.StepButton = this.Get<Button>("StepButton");
            this.StepButton.Click += this.StepButton_Click;
            this.ContinueButton = this.Get<Button>("ContinueButton");
            this.ContinueButton.Click += this.ContinueButton_Click;
            this.HexRadioButton = this.Get<RadioButton>("HexRadioButton");
            this.DecRadioButton = this.Get<RadioButton>("DecRadioButton");
            this.BinRadioButton = this.Get<RadioButton>("BinRadioButton");
            this.HexRadioButton.Click += this.FormatRadioButton_Checked;
            this.DecRadioButton.Click += this.FormatRadioButton_Checked;
            this.BinRadioButton.Click += this.FormatRadioButton_Checked;
            this.CurrentInstructionLabel = this.Get<TextBlock>("CurrentInstructionLabel");
        }

        private static string ApplicationDirectory() => Directory.GetCurrentDirectory();

        private static Stream DiskImage() {
            var dir = MainPage.ApplicationDirectory();
            return File.Open(dir + "/../Images/Fib.bin", FileMode.OpenOrCreate, FileAccess.ReadWrite);
        }

        private static Stream BootImage() {
            var dir = MainPage.ApplicationDirectory();
            return File.Open(dir + "/../Images/BootK.bin", FileMode.Open, FileAccess.Read);
        }

        private void StartButton_Click(object sender, RoutedEventArgs e) {
            var interruptController = new InterruptController();
            var ram = new RandomAccessMemoryController(1 * 1024 * 1024);
            var bootManager = new BootManager(MainPage.BootImage());

            this.processor = new Processor();

            this.system = new SystemBusController() {
                Processor = this.processor,
                InterruptController = interruptController
            };

            this.system.AddDevice(ram);
            this.system.AddDevice(bootManager);
            this.system.AddDevice(this.processor);
            this.system.AddDevice(interruptController);

            var stream = MainPage.DiskImage();
            stream.SetLength(8 * 1024 * 1024);
            this.system.AddDevice(new DiskDrive(stream));

            this.keyboard = new Keyboard();
            this.InputTextBox.KeyDown += this.OnKeyDown;
            this.InputTextBox.KeyUp += this.OnKeyUp;
            this.system.AddDevice(this.keyboard);

            this.display = new Display((ulong)this.ScreenImage.Width, (ulong)this.ScreenImage.Height);
            this.system.AddDevice(this.display);

            this.processor.DebugHandler = (a, b, c) => a.Value = (ulong)DateTime.UtcNow.Ticks;

            this.processor.BreakHandler = async () => await this.Dispatcher.InvokeTaskAsync(() => {
                this.BreakButton.IsEnabled = false;
                this.ContinueButton.IsEnabled = true;
                this.StepButton.IsEnabled = true;

                this.RefreshDebug();
            });

            this.system.Start();

            this.RefreshDebug();

            this.StartButton.IsEnabled = false;
            this.StopButton.IsEnabled = true;
            this.BreakButton.IsEnabled = false;
            this.ContinueButton.IsEnabled = true;
            this.StepButton.IsEnabled = true;
            this.RefreshButton.IsEnabled = true;
        }

        private void OnKeyUp(object sender, KeyEventArgs e) {
            e.Handled = true;

            this.currentPressedKeys.Remove(e.Key);
            this.keyboard.TriggerKeyUp(AvaloniaHelpers.ConvertFromAvaloniaKey(e.Key));
        }

        private void OnKeyDown(object sender, KeyEventArgs e) {
            e.Handled = true;
            if (this.currentPressedKeys.Contains(e.Key))
                return;

            this.currentPressedKeys.Remove(e.Key);

            this.keyboard.TriggerKeyDown(AvaloniaHelpers.ConvertFromAvaloniaKey(e.Key));
        }

        private void StopButton_Click(object sender, RoutedEventArgs e) {
            this.displayRefreshTimer.Stop();

            this.RefreshDebug();

            this.system.Dispose();
            this.system = null;
            this.processor = null;

            this.StartButton.IsEnabled = true;
            this.StopButton.IsEnabled = false;
            this.BreakButton.IsEnabled = false;
            this.ContinueButton.IsEnabled = false;
            this.StepButton.IsEnabled = false;
            this.RefreshButton.IsEnabled = false;

            this.InputTextBox.KeyDown -= this.OnKeyUp;
            this.InputTextBox.KeyUp -= this.OnKeyDown;


            this.currentPressedKeys.Clear();
        }

        private void BreakButton_Click(object sender, RoutedEventArgs e) {
            this.displayRefreshTimer.Stop();

            this.processor.Break();

            this.BreakButton.IsEnabled = false;
            this.ContinueButton.IsEnabled = true;
            this.StepButton.IsEnabled = true;

            this.RefreshDebug();
        }

        private void ContinueButton_Click(object sender, RoutedEventArgs e) {
            this.displayRefreshTimer.Start();

            this.Apply();

            this.processor.Continue();

            this.BreakButton.IsEnabled = true;
            this.ContinueButton.IsEnabled = false;
            this.StepButton.IsEnabled = false;
        }

        private void StepButton_Click(object sender, RoutedEventArgs e) {
            this.Apply();

            this.processor.Step();

            this.RefreshDebug();
            this.RefreshDisplay();
        }

        private void Apply() {
            var displayBase = (this.HexRadioButton.IsChecked) ? 16 : ((this.DecRadioButton.IsChecked) ? 10 : 2);

            foreach (var r in Enum.GetNames(typeof(Register))) {
                var textbox = this.Get<TextBox>(r + "TextBox");

                this.processor.WriteRegister((Register)Enum.Parse(typeof(Register), r), Convert.ToUInt64(textbox.Text.Substring(2).Replace("_", ""), displayBase));
            }
        }

        private void RefreshDebug() {
            var displayBase = (this.HexRadioButton.IsChecked) ? 16 : ((this.DecRadioButton.IsChecked) ? 10 : 2);

            foreach (var r in Enum.GetNames(typeof(Register))) {
                var textbox = this.Get<TextBox>(r + "TextBox");

                if (textbox == null)
                    return;

                textbox.Text = this.processor.ReadRegister((Register)Enum.Parse(typeof(Register), r)).ToString(displayBase);
            }

            this.CurrentInstructionLabel.Text = this.processor.CurrentInstruction.ToString(displayBase);
        }

        private void RefreshDisplay() {
            var buf = this.display.RawBuffer;
            using (var fb = this.displayBitmap.Lock()) {
                for (var y = 0; y < fb.Height; y++) {
                    for (var x = 0; x < fb.Width; x++) {
                        var st = ((y * fb.Width) + x) * 4;
                        RawSetPixel(fb, y, x, buf[st], buf[st + 1], buf[st + 2], buf[st + 3]);
                    }
                }
            }
        }

        private void FormatRadioButton_Checked(object sender, RoutedEventArgs e) => this.RefreshDebug();

        private void RefreshButton_Click(object sender, RoutedEventArgs e) => this.RefreshDebug();
    }
}
