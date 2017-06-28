using System;
using System.IO;
using Hdw = ArkeOS.Hardware.ArkeIndustries;

namespace ArkeOS.Hosts.UWP {
    public sealed class SystemHost : IDisposable {
        public Hdw.SystemBusController SystemBusController { get; private set; }
        public Hdw.Processor Processor { get; }
        public Hdw.Display Display { get; }
        public Hdw.Keyboard Keyboard { get; }

        public SystemHost(Stream bootImage, Stream applicationImage, ulong displayWidth, ulong displayHeight) {
            var interruptController = new Hdw.InterruptController();
            var ram = new Hdw.RandomAccessMemoryController(1 * 1024 * 1024);
            var bootManager = new Hdw.BootManager(bootImage);

            this.Processor = new Hdw.Processor();

            this.SystemBusController = new Hdw.SystemBusController() {
                Processor = this.Processor,
                InterruptController = interruptController
            };

            this.SystemBusController.AddDevice(ram);
            this.SystemBusController.AddDevice(bootManager);
            this.SystemBusController.AddDevice(this.Processor);
            this.SystemBusController.AddDevice(interruptController);

            applicationImage.SetLength(8 * 1024 * 1024);

            this.SystemBusController.AddDevice(new Hdw.DiskDrive(applicationImage));

            this.Keyboard = new Hdw.Keyboard();
            this.SystemBusController.AddDevice(this.Keyboard);

            this.Display = new Hdw.Display(displayWidth, displayHeight);
            this.SystemBusController.AddDevice(this.Display);

            this.Processor.DebugHandler = (a, b, c) => a.Value = (ulong)DateTime.UtcNow.Ticks;

            this.SystemBusController.Start();
        }

        public void Dispose() {
            this.SystemBusController.Stop();
            this.SystemBusController.Dispose();
            this.SystemBusController = null;
        }
    }
}
