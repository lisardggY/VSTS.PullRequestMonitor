using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace PullRequestMonitor
{
    public static class Program
    {
        static async Task Main(string[] args)
        {
            #if DEBUG
              AllocConsole();
            #endif

            Console.WriteLine($"VSTSMonitor v{Assembly.GetExecutingAssembly().GetName().Version.ToString(3)}");

            await Monitor.ConnectAndMonitor();

        }


        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AllocConsole();
    }
}
