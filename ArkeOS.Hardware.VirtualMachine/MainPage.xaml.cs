using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using ArkeOS.Hardware.Architecture;
using ArkeOS.Hardware.Devices.ArkeIndustries;
using ArkeOS.Utilities;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace ArkeOS.Hardware.VirtualMachine {
    public partial class MainPage : Page {
        private SystemBusController system;
		private Processor processor;

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
			var interruptController = new InterruptController();
			var ram = new RandomAccessMemoryController(1 * 1024 * 1024);
			var bootManager = new BootManager(Helpers.ConvertArray((await FileIO.ReadBufferAsync(await ApplicationData.Current.LocalFolder.GetFileAsync("Boot.bin"))).ToArray()));

			this.processor = new Processor();
            this.system = new SystemBusController();

			this.system.AddDevice(interruptController);
			this.system.AddDevice(ram);
			this.system.AddDevice(bootManager);
			this.system.AddDevice(this.processor);

			this.system.Processor = this.processor;
			this.system.InterruptController = interruptController;

            var stream = (await (await ApplicationData.Current.LocalFolder.CreateFileAsync("Disk 0.bin", CreationCollisionOption.OpenIfExists)).OpenAsync(FileAccessMode.ReadWrite)).AsStream();
            stream.SetLength(8 * 1024 * 1024);
            this.system.AddDevice(new DiskDrive(stream));

            var keyboard = new Keyboard();
            this.InputTextBox.KeyDown += (ss, ee) => keyboard.TriggerKeyDown((ulong)ee.Key);
            this.InputTextBox.KeyUp += (ss, ee) => keyboard.TriggerKeyUp((ulong)ee.Key);
            this.system.AddDevice(keyboard);

			this.processor.BreakHandler = async () => await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                this.BreakButton.IsEnabled = false;
                this.ContinueButton.IsEnabled = true;
                this.StepButton.IsEnabled = true;
                this.ApplyButton.IsEnabled = true;

                this.Refresh();
            });

            this.system.Start();

            this.Refresh();

            this.StartButton.IsEnabled = false;
            this.StopButton.IsEnabled = true;
            this.BreakButton.IsEnabled = false;
            this.ContinueButton.IsEnabled = true;
            this.StepButton.IsEnabled = true;
            this.ApplyButton.IsEnabled = true;
        }

        private void StopButton_Click(object sender, RoutedEventArgs e) {
			this.Refresh();

			this.system.Stop();
            this.system = null;

            this.StartButton.IsEnabled = true;
            this.StopButton.IsEnabled = false;
            this.BreakButton.IsEnabled = false;
            this.ContinueButton.IsEnabled = false;
            this.StepButton.IsEnabled = false;
            this.ApplyButton.IsEnabled = false;
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
    }
}