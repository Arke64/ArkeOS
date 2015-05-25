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

			this.memoryController = new MemoryController(1 * 1024 * 1024);
			this.interruptController = new InterruptController();
			this.processor = new Processor(this.memoryController, this.interruptController);
		}

		private void RunButton_Click(object sender, RoutedEventArgs e) {
			if (File.Exists(this.FileNameTextBox.Text)) {
                this.processor.LoadBootImage(new MemoryStream(new Image(File.OpenRead(this.FileNameTextBox.Text)).Sections.First().Data));

				Task.Run((Action)this.processor.Run);
			}
		}
	}
}