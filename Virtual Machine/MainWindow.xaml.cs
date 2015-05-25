using System;
using System.IO;
using System.Linq;
using System.Reflection;
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
			this.PauseButton.IsEnabled = false;
			this.UnpauseButton.IsEnabled = false;
		}

		private void StartButton_Click(object sender, RoutedEventArgs e) {
			if (File.Exists(this.BootImageTextBox.Text)) {
				this.memoryController = new MemoryController(1 * 1024 * 1024);
				this.interruptController = new InterruptController();
				this.processor = new Processor(this.memoryController, this.interruptController);

				this.processor.LoadBootImage(new MemoryStream(new Executable.Image(File.OpenRead(this.BootImageTextBox.Text)).Sections.First().Data));

				this.processor.Start();

				this.StartButton.IsEnabled = false;
				this.StopButton.IsEnabled = true;
				this.PauseButton.IsEnabled = true;
				this.UnpauseButton.IsEnabled = false;
			}
		}

		private void StopButton_Click(object sender, RoutedEventArgs e) {
			this.processor.Stop();

			this.StartButton.IsEnabled = true;
			this.StopButton.IsEnabled = false;
			this.PauseButton.IsEnabled = false;
			this.UnpauseButton.IsEnabled = false;

			this.RefreshRegisters();
		}

		private void PauseButton_Click(object sender, RoutedEventArgs e) {
			this.processor.Pause();

			this.StartButton.IsEnabled = false;
			this.StopButton.IsEnabled = true;
			this.PauseButton.IsEnabled = false;
			this.UnpauseButton.IsEnabled = true;

			this.RefreshRegisters();
		}

		private void UnpauseButton_Click(object sender, RoutedEventArgs e) {
			this.processor.Unpause();

			this.StartButton.IsEnabled = false;
			this.StopButton.IsEnabled = true;
			this.PauseButton.IsEnabled = true;
			this.UnpauseButton.IsEnabled = false;
		}

		private void RefreshRegisters() {
			foreach (var r in Enum.GetNames(typeof(Register))) {
				var textbox = (TextBox)this.GetType().GetField(r + "TextBox", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(this);
					
				textbox.Text = this.processor.Registers[(Register)Enum.Parse(typeof(Register), r)].ToString();
			}
		}
	}
}