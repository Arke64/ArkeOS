using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using ArkeOS.Hardware.Architecture;
using ArkeOS.Hardware.Devices.ArkeIndustries;
using ArkeOS.Utilities;
using ArkeOS.Utilities.Extensions;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Imaging;

namespace ArkeOS.Hosts.UAP {
	public partial class MainPage : Page {
		private SystemBusController system;
		private Processor processor;
		private Display display;
		private WriteableBitmap displayBitmap;
		private DispatcherTimer displayRefreshTimer;
		private Keyboard keyboard;

		public MainPage() {
			this.InitializeComponent();
			
			this.displayBitmap = new WriteableBitmap((int)this.ScreenImage.Width, (int)this.ScreenImage.Height);
			this.displayRefreshTimer = new DispatcherTimer();
			this.displayRefreshTimer.Interval = TimeSpan.FromMilliseconds(1000 / 24);
			this.displayRefreshTimer.Tick += (s, e) => this.RefreshDisplay();

			this.ScreenImage.Source = this.displayBitmap;

			this.StartButton.IsEnabled = true;
			this.StopButton.IsEnabled = false;
			this.BreakButton.IsEnabled = false;
			this.ContinueButton.IsEnabled = false;
			this.StepButton.IsEnabled = false;
			this.RefreshButton.IsEnabled = false;
		}

		private async void StartButton_Click(object sender, RoutedEventArgs e) {
			var interruptController = new InterruptController();
			var ram = new RandomAccessMemoryController(1 * 1024 * 1024);
			var bootManager = new BootManager(Helpers.ConvertArray((await FileIO.ReadBufferAsync(await ApplicationData.Current.LocalFolder.GetFileAsync("Boot.bin"))).ToArray()));

			this.processor = new Processor();
			this.system = new SystemBusController();

			this.system.Processor = this.processor;
			this.system.InterruptController = interruptController;

			this.system.AddDevice(ram);
			this.system.AddDevice(bootManager);
			this.system.AddDevice(this.processor);
			this.system.AddDevice(interruptController);

			var stream = (await (await ApplicationData.Current.LocalFolder.CreateFileAsync("Disk 0.bin", CreationCollisionOption.OpenIfExists)).OpenAsync(FileAccessMode.ReadWrite)).AsStream();
			stream.SetLength(8 * 1024 * 1024);
			this.system.AddDevice(new DiskDrive(stream));

			this.keyboard = new Keyboard();
			this.InputTextBox.KeyDown += this.OnKeyEvent;
			this.InputTextBox.KeyUp += this.OnKeyEvent;
			this.system.AddDevice(this.keyboard);

			this.display = new Display((int)this.ScreenImage.Width, (int)this.ScreenImage.Height);
			this.system.AddDevice(this.display);

			this.processor.DebugHandler = (a, b, c) => a.Value = (ulong)DateTime.UtcNow.Ticks;

			this.processor.BreakHandler = async () => await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
				this.BreakButton.IsEnabled = false;
				this.ContinueButton.IsEnabled = true;
				this.StepButton.IsEnabled = true;

				this.RefreshDebug();
			});

			this.system.Reset();

			this.RefreshDebug();

			this.StartButton.IsEnabled = false;
			this.StopButton.IsEnabled = true;
			this.BreakButton.IsEnabled = false;
			this.ContinueButton.IsEnabled = true;
			this.StepButton.IsEnabled = true;
			this.RefreshButton.IsEnabled = true;
		}

		private void OnKeyEvent(object sender, KeyRoutedEventArgs e) {
			//https://msdn.microsoft.com/en-us/library/aa299374(v=vs.60).aspx
			if (e.KeyStatus.IsKeyReleased) {
				this.keyboard.TriggerKeyUp((ulong)e.Key);
			}
			else {
				this.keyboard.TriggerKeyDown((ulong)e.Key);
			}

			e.Handled = true;
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

			this.InputTextBox.KeyDown -= this.OnKeyEvent;
			this.InputTextBox.KeyUp -= this.OnKeyEvent;

			this.displayBitmap.Clear();
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
			var displayBase = (this.HexRadioButton.IsChecked ?? false) ? 16 : ((this.DecRadioButton.IsChecked ?? false) ? 10 : 2);

			foreach (var r in Enum.GetNames(typeof(Register))) {
				var textbox = (TextBox)this.GetType().GetField(r + "TextBox", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(this);

				this.processor.WriteRegister((Register)Enum.Parse(typeof(Register), r), Convert.ToUInt64(textbox.Text.Substring(2), displayBase));
			}
		}

		private void RefreshDebug() {
			var displayBase = (this.HexRadioButton.IsChecked ?? false) ? 16 : ((this.DecRadioButton.IsChecked ?? false) ? 10 : 2);

			foreach (var r in Enum.GetNames(typeof(Register))) {
				var textbox = (TextBox)this.GetType().GetField(r + "TextBox", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(this);

				if (textbox == null)
					return;

				textbox.Text = this.processor.ReadRegister((Register)Enum.Parse(typeof(Register), r)).ToString(displayBase);
			}

			this.CurrentInstructionLabel.Text = this.processor.CurrentInstruction.ToString(displayBase);
		}

		private void RefreshDisplay() => this.displayBitmap.FromByteArray(this.display.RawBuffer);

		private void FormatRadioButton_Checked(object sender, RoutedEventArgs e) {
			this.RefreshDebug();
		}

		private void RefreshButton_Click(object sender, RoutedEventArgs e) {
			this.RefreshDebug();
		}
	}
}