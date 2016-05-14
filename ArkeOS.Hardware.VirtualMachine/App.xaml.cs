using System;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace ArkeOS.Hardware.VirtualMachine {
	public sealed partial class App : Application {
		public App() {
			this.InitializeComponent();
			this.Suspending += this.OnSuspending;
		}

		protected override void OnLaunched(LaunchActivatedEventArgs e) {
			var rootFrame = Window.Current.Content as Frame;

			if (rootFrame == null) {
				rootFrame = new Frame();

				rootFrame.NavigationFailed += this.OnNavigationFailed;

				Window.Current.Content = rootFrame;
			}

			if (!e.PrelaunchActivated) {
				if (rootFrame.Content == null)
					rootFrame.Navigate(typeof(MainPage), e.Arguments);

				Window.Current.Activate();
			}
		}

		private void OnNavigationFailed(object sender, NavigationFailedEventArgs e) {
			throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
		}

		private void OnSuspending(object sender, SuspendingEventArgs e) {
			e.SuspendingOperation.GetDeferral().Complete();
		}
	}
}
