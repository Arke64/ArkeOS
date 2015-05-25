using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace ArkeOS.VirtualMachine {
	public partial class MainWindow : Window {
		private Interpreter.Interpreter interpreter;

		public MainWindow() {
			this.InitializeComponent();

			this.interpreter = new Interpreter.Interpreter();
		}

		private void RunButton_Click(object sender, RoutedEventArgs e) {
			if (File.Exists(this.FileNameTextBox.Text)) {
				var file = File.ReadAllBytes(this.FileNameTextBox.Text);

				try {
					this.interpreter.Load(file);
				}
				catch (Interpreter.InvalidProgramFormatException) {
					MessageBox.Show("Invalid program format.");
				}

				Task.Run((Action)this.interpreter.Run);
			}
		}
	}
}
