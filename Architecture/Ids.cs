namespace ArkeOS.Architecture {
    public static class Ids {
        public static class Devices {
            public static ulong RandomAccessMemoryController0 => 0;
            public static ulong Processor0 => 2;
            public static ulong SystemBusController => 1;
            public static ulong BootManager => 4;
            public static ulong InterruptController => 3;
        }

        public static class ArkeIndustries {
            public static ulong VendorId => 0;

            public static class Products {
                public static ulong IC100 => 3;
                public static ulong B100 => 4;
                public static ulong HDD100 => 5;
                public static ulong KB100 => 6;
                public static ulong PROC100 => 2;
                public static ulong AB100 => 1;
                public static ulong MEM100 => 0;
            }
        }

        public static class SeymourInc {
            public static ulong VendorId => 1;

            public static class Products {

            }
        }
    }
}
