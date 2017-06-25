using ArkeOS.Hardware.Architecture;
using ArkeOS.Utilities.Extensions;
using System;
using System.Reflection;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace ArkeOS.Hosts.UWP {
    public partial class Debugger : Page {
        private SystemHost host;
        private DispatcherTimer refreshTimer;

        public Debugger() {
            this.InitializeComponent();

            this.BreakButton.IsEnabled = false;
            this.ContinueButton.IsEnabled = false;
            this.StepButton.IsEnabled = false;
            this.RefreshButton.IsEnabled = true;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e) {
            this.host = (SystemHost)e.Parameter;
            this.refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            this.refreshTimer.Tick += (s, ee) => this.RefreshDebug();

            this.host.Processor.BreakHandler = async () => await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                this.BreakButton.IsEnabled = false;
                this.ContinueButton.IsEnabled = true;
                this.StepButton.IsEnabled = true;

                this.refreshTimer.Stop();

                this.RefreshDebug();
            });

            var running = this.host.Processor.IsRunning;

            this.BreakButton.IsEnabled = running;
            this.ContinueButton.IsEnabled = !running;
            this.StepButton.IsEnabled = !running;

            if (running)
                this.refreshTimer.Start();

            this.RefreshDebug();
        }

        private void BreakButton_Click(object sender, RoutedEventArgs e) {
            this.refreshTimer.Stop();

            this.host.Processor.Break();

            this.BreakButton.IsEnabled = false;
            this.ContinueButton.IsEnabled = true;
            this.StepButton.IsEnabled = true;

            this.RefreshDebug();
        }

        private void ContinueButton_Click(object sender, RoutedEventArgs e) {
            this.refreshTimer.Start();

            this.Apply();

            this.host.Processor.Continue();

            this.BreakButton.IsEnabled = true;
            this.ContinueButton.IsEnabled = false;
            this.StepButton.IsEnabled = false;
        }

        private void StepButton_Click(object sender, RoutedEventArgs e) {
            this.Apply();

            this.host.Processor.Step();

            this.RefreshDebug();
        }

        private void Apply() {
            var displayBase = (this.HexRadioButton.IsChecked ?? false) ? 16 : ((this.DecRadioButton.IsChecked ?? false) ? 10 : 2);

            foreach (var r in Enum.GetNames(typeof(Register))) {
                var textbox = (TextBox)this.GetType().GetField(r + "TextBox", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(this);

                this.host.Processor.WriteRegister((Register)Enum.Parse(typeof(Register), r), (ulong)Convert.ToInt64(textbox.Text.Replace("_", "").Replace(",", "").Replace("0b", "").Replace("0x", "").Replace("0d", ""), displayBase));
            }
        }

        private void RefreshDebug() {
            var displayBase = (this.HexRadioButton.IsChecked ?? false) ? 16 : ((this.DecRadioButton.IsChecked ?? false) ? 10 : 2);

            foreach (var r in Enum.GetNames(typeof(Register))) {
                var textbox = (TextBox)this.GetType().GetField(r + "TextBox", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(this);

                if (textbox == null)
                    return;

                textbox.Text = this.host.Processor.ReadRegister((Register)Enum.Parse(typeof(Register), r)).ToString(displayBase);
            }

            this.CurrentInstructionLabel.Text = this.host.Processor.CurrentInstruction.ToString(displayBase);
        }

        private void FormatRadioButton_Checked(object sender, RoutedEventArgs e) => this.RefreshDebug();

        private void RefreshButton_Click(object sender, RoutedEventArgs e) => this.RefreshDebug();
    }
}
