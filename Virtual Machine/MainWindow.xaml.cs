using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using ArkeOS.Executable;
using ArkeOS.Hardware;

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

				this.processor.LoadBootImage(new MemoryStream(new Image(File.OpenRead(this.BootImageTextBox.Text)).Sections.First().Data));

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
		}

		private void PauseButton_Click(object sender, RoutedEventArgs e) {
			this.processor.Pause();

			this.StartButton.IsEnabled = false;
			this.StopButton.IsEnabled = true;
			this.PauseButton.IsEnabled = false;
			this.UnpauseButton.IsEnabled = true;
		}

		private void UnpauseButton_Click(object sender, RoutedEventArgs e) {
			this.processor.Unpause();

			this.StartButton.IsEnabled = false;
			this.StopButton.IsEnabled = true;
			this.PauseButton.IsEnabled = true;
			this.UnpauseButton.IsEnabled = false;
		}
	}
}