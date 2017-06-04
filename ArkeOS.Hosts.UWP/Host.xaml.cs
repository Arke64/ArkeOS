using System;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace ArkeOS.Hosts.UWP {
    public partial class Host : Page {
        private SystemHost systemHost;
        private (CoreApplicationView view, int id) debuggerView;
        private (CoreApplicationView view, int id) displayView;

        public Host() {
            this.InitializeComponent();

            this.StartButton.IsEnabled = true;
            this.StopButton.IsEnabled = false;
            this.ShowDebuggerButton.IsEnabled = false;
        }

        private async void StartButton_Click(object sender, RoutedEventArgs e) {
            var boot = (await FileIO.ReadBufferAsync(await ApplicationData.Current.LocalFolder.GetFileAsync("Boot.bin"))).AsStream();
            var app = (await (await ApplicationData.Current.LocalFolder.CreateFileAsync("Disk 0.bin", CreationCollisionOption.OpenIfExists)).OpenAsync(FileAccessMode.ReadWrite)).AsStream();

            this.systemHost = new SystemHost(boot, app, 720, 480);

            this.StartButton.IsEnabled = false;
            this.StopButton.IsEnabled = true;
            this.ShowDebuggerButton.IsEnabled = true;

            this.displayView = await this.ShowWindowAsync<Display>(this.displayView, this.systemHost);
        }

        private async void StopButton_Click(object sender, RoutedEventArgs e) {
            if (this.debuggerView.id != 0) this.debuggerView = await this.CloseWindowAsync(this.debuggerView);

            this.displayView = await this.CloseWindowAsync(this.displayView);

            this.systemHost.Dispose();
            this.systemHost = null;

            this.StartButton.IsEnabled = true;
            this.StopButton.IsEnabled = false;
            this.ShowDebuggerButton.IsEnabled = false;
        }

        private async void ShowDebuggerButton_Click(object sender, RoutedEventArgs e) => this.debuggerView = await this.ShowWindowAsync<Debugger>(this.debuggerView, this.systemHost);

        private async Task<(CoreApplicationView, int)> CloseWindowAsync((CoreApplicationView view, int id) window) {
            await window.view.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                window.view.CoreWindow.Close();
                window.view = null;
                window.id = 0;
            });

            return window;
        }

        private async Task<(CoreApplicationView, int)> ShowWindowAsync<T>((CoreApplicationView view, int id) window, object parameter) {
            if (window.view == null) {
                window.view = CoreApplication.CreateNewView();
                window.id = 0;

                await window.view.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                    var frame = new Frame();

                    frame.Navigate(typeof(T), parameter);

                    Window.Current.Content = frame;

                    Window.Current.Activate();

                    window.id = ApplicationView.GetForCurrentView().Id;
                });

                await ApplicationViewSwitcher.TryShowAsStandaloneAsync(window.id);
            }
            else {
                await ApplicationViewSwitcher.SwitchAsync(window.id);
            }

            return window;
        }
    }
}
