using System;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using ArkeOS.Architecture;
using ArkeOS.Hardware;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace ArkeOS.VirtualMachine {
	public partial class MainPage : Page {
		private SystemBusController memoryController;
		private InterruptController interruptController;
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
			this.memoryController = new SystemBusController(10 * 1024 * 1024);
			this.interruptController = new InterruptController();
			this.processor = new Processor(this.memoryController, this.interruptController);
			this.processor.ExecutionPaused += async (ss, ee) => await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => this.BreakButton_Click(null, null));

			var picker = new FileOpenPicker();
			picker.ViewMode = PickerViewMode.List;
			picker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
			picker.FileTypeFilter.Add(".bin");

			var file = await picker.PickSingleFileAsync();
			var buffer = await FileIO.ReadBufferAsync(file);

			this.processor.LoadStartupImage(buffer.ToArray());

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
			this.memoryController = null;
			this.interruptController = null;
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

				this.processor.Registers[(Register)Enum.Parse(typeof(Register), r)] = Convert.ToUInt64(textbox.Text.Substring(2), 16);
			}
		}

		private void Refresh() {
			foreach (var r in Enum.GetNames(typeof(Register))) {
				var textbox = (TextBox)this.GetType().GetField(r + "TextBox", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(this);

				textbox.Text = "0x" + this.processor.Registers[(Register)Enum.Parse(typeof(Register), r)].ToString("X8");
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