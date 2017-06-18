using System;
using System.IO;
using Hdw = ArkeOS.Hardware.ArkeIndustries;

namespace ArkeOS.Hosts.UWP {
    public sealed class SystemHost : IDisposable {
        private Hdw.SystemBusController system;

        public Hdw.Processor Processor { get; }
        public Hdw.Display Display { get; }
        public Hdw.Keyboard Keyboard { get; }

        public SystemHost(Stream bootImage, Stream applicationImage, ulong displayWidth, ulong displayHeight) {
            var interruptController = new Hdw.InterruptController();
            var ram = new Hdw.RandomAccessMemoryController(1 * 1024 * 1024);
            var bootManager = new Hdw.BootManager(bootImage);

            this.Processor = new Hdw.Processor();

            this.system = new Hdw.SystemBusController() {
                Processor = this.Processor,
                InterruptController = interruptController
            };

            this.system.AddDevice(ram);
            this.system.AddDevice(bootManager);
            this.system.AddDevice(this.Processor);
            this.system.AddDevice(interruptController);

            applicationImage.SetLength(8 * 1024 * 1024);

            this.system.AddDevice(new Hdw.DiskDrive(applicationImage));

            this.Keyboard = new Hdw.Keyboard();
            this.system.AddDevice(this.Keyboard);

            this.Display = new Hdw.Display(displayWidth, displayHeight);
            this.system.AddDevice(this.Display);

            this.Processor.DebugHandler = (a, b, c) => a.Value = (ulong)DateTime.UtcNow.Ticks;

            this.system.Start();
        }

        public void Dispose() {
            this.system.Stop();
            this.system.Dispose();
            this.system = null;
        }
    }
}
