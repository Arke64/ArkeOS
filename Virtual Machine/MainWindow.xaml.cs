using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ArkeOS.Hardware;
using ArkeOS.ISA;

namespace ArkeOS.VirtualMachine {
	public partial class MainWindow : Window {
		private MemoryController memoryController;
		private InterruptController interruptController;
		private Processor processor;

		public MainWindow() {
			this.InitializeComponent();

			this.StartButton.IsEnabled = true;
			this.StopButton.IsEnabled = false;
			this.BreakButton.IsEnabled = false;
			this.ContinueButton.IsEnabled = false;
			this.StepButton.IsEnabled = false;
		}

		private void StartButton_Click(object sender, RoutedEventArgs e) {
			this.memoryController = new MemoryController(10 * 1024 * 1024);
			this.interruptController = new InterruptController();
			this.processor = new Processor(this.memoryController, this.interruptController);

			this.processor.LoadBootImage(new MemoryStream(new Executable.Image(File.OpenRead(this.BootImageTextBox.Text)).Sections.First().Data));

			this.processor.Start();

			this.Refresh();

			this.StartButton.IsEnabled = false;
			this.StopButton.IsEnabled = true;
			this.BreakButton.IsEnabled = false;
			this.ContinueButton.IsEnabled = true;
			this.StepButton.IsEnabled = true;
		}

		private void StopButton_Click(object sender, RoutedEventArgs e) {
			this.processor.Stop();

			this.StartButton.IsEnabled = true;
			this.StopButton.IsEnabled = false;
			this.BreakButton.IsEnabled = false;
			this.ContinueButton.IsEnabled = false;
			this.StepButton.IsEnabled = false;

			this.Refresh();

			this.processor = null;
			this.memoryController = null;
			this.interruptController = null;
		}

		private void BreakButton_Click(object sender, RoutedEventArgs e) {
			this.processor.Break();

			this.BreakButton.IsEnabled = false;
			this.ContinueButton.IsEnabled = true;
			this.StepButton.IsEnabled = true;

			this.Refresh();
		}

		private void ContinueButton_Click(object sender, RoutedEventArgs e) {
			this.Apply();

			this.processor.Continue();

			this.BreakButton.IsEnabled = true;
			this.ContinueButton.IsEnabled = false;
			this.StepButton.IsEnabled = false;
		}

		private void StepButton_Click(object sender, RoutedEventArgs e) {
			this.Apply();

			this.processor.Step();

			this.Refresh();
		}

		private async void Refresh() {
			await Task.Delay(250);

			foreach (var r in Enum.GetNames(typeof(Register))) {
				var textbox = (TextBox)this.GetType().GetField(r + "TextBox", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(this);

				textbox.Text = "0x" + this.processor.Registers[(Register)Enum.Parse(typeof(Register), r)].ToString("X8");
			}

			this.CurrentInstructionLabel.Content = this.processor.CurrentInstruction.ToString();
		}

		private void Apply() {
			foreach (var r in Enum.GetNames(typeof(Register))) {
				var textbox = (TextBox)this.GetType().GetField(r + "TextBox", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(this);

				this.processor.Registers[(Register)Enum.Parse(typeof(Register), r)] = Convert.ToUInt64(textbox.Text.Substring(2), 16);
			}
		}
	}
}