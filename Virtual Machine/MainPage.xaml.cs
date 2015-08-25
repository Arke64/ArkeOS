using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using ArkeOS.Architecture;
using ArkeOS.Hardware;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace ArkeOS.VirtualMachine {
    public partial class MainPage : Page {
        private SystemBusController systemBusController;
        private MemoryManager memoryManager;
        private DiskDrive diskDrive;
        private Processor processor;
        private Keyboard keyboard;
        private BootManager bootManager;

        private Stream stream;

        public MainPage() {
            this.InitializeComponent();

            this.StartButton.IsEnabled = true;
            this.StopButton.IsEnabled = false;
            this.BreakButton.IsEnabled = false;
            this.ContinueButton.IsEnabled = false;
            this.StepButton.IsEnabled = false;
            this.ApplyButton.IsEnabled = false;
        }

        private async void StartButton_Click(object sender, RoutedEventArgs e) {
            this.stream = (await (await ApplicationData.Current.LocalFolder.CreateFileAsync("Disk 0.bin", CreationCollisionOption.OpenIfExists)).OpenAsync(FileAccessMode.ReadWrite)).AsStream();

            this.memoryManager = new MemoryManager(1 * 1024 * 1024);
            this.systemBusController = new SystemBusController();
            this.diskDrive = new DiskDrive(10 * 1024 * 1024, this.stream);
            this.keyboard = new Keyboard();
            this.bootManager = new BootManager(Helpers.ConvertArray((await FileIO.ReadBufferAsync(await ApplicationData.Current.LocalFolder.GetFileAsync("Boot.bin"))).ToArray()));

            this.InputTextBox.KeyDown += (ss, ee) => this.keyboard.TriggerKeyDown((ulong)ee.Key);
            this.InputTextBox.KeyUp += (ss, ee) => this.keyboard.TriggerKeyUp((ulong)ee.Key);

            this.systemBusController.AddDevice(SystemBusController.RandomAccessMemoryDeviceId, this.memoryManager);
            this.systemBusController.AddDevice(SystemBusController.BootManagerDeviceId, this.bootManager);
            this.systemBusController.AddDevice(this.diskDrive);
            this.systemBusController.AddDevice(this.keyboard);

            this.processor = new Processor(this.systemBusController);
            this.processor.ExecutionPaused += async (ss, ee) => await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => this.BreakButton_Click(null, null));

            this.processor.Start();

            this.Refresh();

            this.StartButton.IsEnabled = false;
            this.StopButton.IsEnabled = true;
            this.BreakButton.IsEnabled = false;
            this.ContinueButton.IsEnabled = true;
            this.StepButton.IsEnabled = true;
            this.ApplyButton.IsEnabled = true;
        }

        private void StopButton_Click(object sender, RoutedEventArgs e) {
            this.processor.Stop();

            this.StartButton.IsEnabled = true;
            this.StopButton.IsEnabled = false;
            this.BreakButton.IsEnabled = false;
            this.ContinueButton.IsEnabled = false;
            this.StepButton.IsEnabled = false;
            this.ApplyButton.IsEnabled = false;

            this.Clear();

            this.processor = null;
            this.memoryManager = null;
            this.systemBusController = null;
            this.diskDrive = null;
            this.keyboard = null;

            this.stream.Flush();
            this.stream.Dispose();
            this.stream = null;
        }

        private void BreakButton_Click(object sender, RoutedEventArgs e) {
            this.processor.Break();

            this.BreakButton.IsEnabled = false;
            this.ContinueButton.IsEnabled = true;
            this.StepButton.IsEnabled = true;
            this.ApplyButton.IsEnabled = true;

            this.Refresh();
        }

        private void ContinueButton_Click(object sender, RoutedEventArgs e) {
            this.processor.Continue();

            this.BreakButton.IsEnabled = true;
            this.ContinueButton.IsEnabled = false;
            this.StepButton.IsEnabled = false;
            this.ApplyButton.IsEnabled = false;
        }

        private void StepButton_Click(object sender, RoutedEventArgs e) {
            this.processor.Step();

            this.Refresh();
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e) {
            foreach (var r in Enum.GetNames(typeof(Register))) {
                var textbox = (TextBox)this.GetType().GetField(r + "TextBox", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(this);

                this.processor.WriteRegister((Register)Enum.Parse(typeof(Register), r), Convert.ToUInt64(textbox.Text.Substring(2), 16));
            }
        }

        private void Refresh() {
            foreach (var r in Enum.GetNames(typeof(Register))) {
                var textbox = (TextBox)this.GetType().GetField(r + "TextBox", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(this);

                textbox.Text = "0x" + this.processor.ReadRegister((Register)Enum.Parse(typeof(Register), r)).ToString("X8");
            }

            this.CurrentInstructionLabel.Text = this.processor.CurrentInstruction.ToString();
        }

        private void Clear() {
            foreach (var r in Enum.GetNames(typeof(Register))) {
                var textbox = (TextBox)this.GetType().GetField(r + "TextBox", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(this);

                textbox.Text = "0x" + 0.ToString("X8");
            }

            this.CurrentInstructionLabel.Text = string.Empty;
        }
    }
}