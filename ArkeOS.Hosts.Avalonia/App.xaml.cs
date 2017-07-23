using Avalonia;
using Avalonia.Markup.Xaml;

namespace ArkeOS.Hosts.Avalonia {
    public class App : Application {
        public override void Initialize() {
            AvaloniaXamlLoader.Load(this);
        }
    }
}