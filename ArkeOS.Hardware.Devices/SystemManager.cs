using System.Collections.Generic;

namespace ArkeOS.Hardware.Devices {
    public class SystemManager {
        private List<SystemBusDevice> peripherals;

        public Processor Processor { get; private set; }
        public BootManager BootManager { get; private set; }
        public SystemBusController SystemBusController { get; private set; }
        public InterruptController InterruptController { get; private set; }
        public RandomAccessMemoryController RandomAccessMemoryController { get; private set; }

        public IReadOnlyList<SystemBusDevice> Peripherals => this.peripherals;

        public ulong[] BootImage { get; set; }
        public ulong PhysicalMemorySize { get; set; }

        public SystemManager() {
            this.peripherals = new List<SystemBusDevice>();

            this.Processor = new Processor();
            this.BootManager = new BootManager();
            this.InterruptController = new InterruptController();
            this.RandomAccessMemoryController = new RandomAccessMemoryController();

            this.SystemBusController = new SystemBusController();
            this.SystemBusController.AddDevice(Architecture.Ids.Devices.Processor0, this.Processor);
            this.SystemBusController.AddDevice(Architecture.Ids.Devices.BootManager, this.BootManager);
            this.SystemBusController.AddDevice(Architecture.Ids.Devices.InterruptController, this.InterruptController);
            this.SystemBusController.AddDevice(Architecture.Ids.Devices.RandomAccessMemoryController0, this.RandomAccessMemoryController);
        }

        public void AddPeripheral(SystemBusDevice device) {
            this.peripherals.Add(device);

            this.SystemBusController.AddDevice(device);
        }

        public void Start() {
            this.Processor.InterruptController = this.InterruptController;
            this.BootManager.BootImage = this.BootImage;
            this.RandomAccessMemoryController.Size = this.PhysicalMemorySize;

            this.SystemBusController.Start();
        }

        public void Stop() {
            this.SystemBusController.Stop();
        }
    }
}