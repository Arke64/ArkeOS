using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using ArkeOS.Executable;
using ArkeOS.Hardware;

namespace ArkeOS.VirtualMachine {
	public partial class MainWindow : Window {
		private MemoryManager memory;
		private Processor processor;

		public MainWindow() {
			this.InitializeComponent();

			this.memory = new MemoryManager(1 * 1024 * 1024);
			this.processor = new Processor(this.memory);
		}

		private void RunButton_Click(object sender, RoutedEventArgs e) {
			if (File.Exists(this.FileNameTextBox.Text)) {
                this.processor.LoadBootImage(new MemoryStream(new Image(File.OpenRead(this.FileNameTextBox.Text)).Sections.First().Data));

				Task.Run((Action)this.processor.Run);
			}
		}
	}
}