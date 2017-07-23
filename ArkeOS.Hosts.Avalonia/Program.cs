using System;
using Avalonia;

namespace ArkeOS.Hosts.Avalonia {
    class Program {
        static void Main(string[] args) {
            Console.WriteLine("Hello World!");

            AppBuilder.Configure<App>().UsePlatformDetect().Start<MainWindow>();
        }
    }
}
