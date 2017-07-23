using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Themes.Default;

namespace ArkeOS.Hosts.Avalonia {
    public class MainWindow : Window {
        public MainWindow() {
            this.InitializeComponent();
            this.AttachDevTools();
        }

        private void InitializeComponent() {
            var theme = new DefaultTheme();
            theme.FindResource("Button");
            AvaloniaXamlLoader.Load(this);
        }
    }
}