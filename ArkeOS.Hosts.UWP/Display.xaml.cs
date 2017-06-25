using ArkeOS.Utilities;
using System;
using System.Collections.Generic;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

namespace ArkeOS.Hosts.UWP {
    public partial class Display : Page {
        private WriteableBitmap displayBitmap;
        private DispatcherTimer refreshTimer;
        private HashSet<ulong> currentPressedKeys;
        private SystemHost host;

        public Display() => this.InitializeComponent();

        protected override void OnNavigatedTo(NavigationEventArgs e) {
            this.host = (SystemHost)e.Parameter;
            this.currentPressedKeys = new HashSet<ulong>();
            this.refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1000 / 24) };

            this.refreshTimer.Tick += (s, ee) => this.displayBitmap.FromByteArray(this.host.Display.RawBuffer);
            this.displayBitmap = new WriteableBitmap((int)this.host.Display.Width, (int)this.host.Display.Height);
            this.DisplayImage.Width = (int)this.host.Display.Width;
            this.DisplayImage.Height = (int)this.host.Display.Height;
            this.DisplayImage.Source = this.displayBitmap;

            this.refreshTimer.Start();

            Window.Current.CoreWindow.KeyUp += this.OnKeyEvent;
            Window.Current.CoreWindow.KeyDown += this.OnKeyEvent;

            this.host.Processor.Continue();
        }

        private void OnKeyEvent(CoreWindow sender, KeyEventArgs e) {
            var scanCode = Helpers.ConvertFromWindowsScanCode(e.KeyStatus.IsExtendedKey, e.KeyStatus.ScanCode);

            e.Handled = true;

            if (e.KeyStatus.IsKeyReleased) {
                this.currentPressedKeys.Remove(scanCode);

                this.host.Keyboard.TriggerKeyUp(scanCode);
            }
            else {
                if (this.currentPressedKeys.Contains(scanCode))
                    return;

                this.currentPressedKeys.Remove(scanCode);

                this.host.Keyboard.TriggerKeyDown(scanCode);
            }
        }
    }
}
