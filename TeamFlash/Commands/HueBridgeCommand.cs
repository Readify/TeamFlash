using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using ManyConsole;
using Q42.HueApi;

namespace TeamFlash.Commands
{
    class HueBridgeCommand : ConsoleCommand
    {
        private string _timeoutString;
        private bool _scan;

        public HueBridgeCommand()
        {
            IsCommand("huebridge", "Attempts to discover any Hue bridges on the network, and outputs their IP.");
            HasOption("t=|timeout=", "Maximum length of time in seconds to search for the bridge (default 15 seconds)", o => _timeoutString = o);
            HasOption("s|scan", "Use IP address range scanning to find the bridge", o => _scan = o != null);
            SkipsCommandSummaryBeforeRunning();
        }
        public override int Run(string[] remainingArguments)
        {
            if (_scan)
            {
                //TODO: Cant really assume /24 subnet...
                var ip = String.Join(".",GetIpAddresss().Split('.').Take(3));
                Parallel.ForEach(Enumerable.Range(0, 254), (lastByte, state) =>
                    {
                        try
                        {
                            var testHost = string.Format("{0}.{1}", ip, lastByte);
                            var hostResource = String.Format("http://{0}/debug/clip.html", testHost);
                            using (var client = new WebClient())
                            {
                                client.DownloadString(hostResource);
                                Console.WriteLine("Found host at: {0}", testHost);
                                state.Break();
                            }
                        }
                        catch (WebException)
                        {
                        }
                    });
                Console.WriteLine("Failed to find any bridges via scan.  Ensure bridge is switched on and discoverable on your network segment.");
                return 1;
            }

            try
            {
                var locator = new HttpBridgeLocator();
                var result = locator.LocateBridgesAsync(new TimeSpan(0, 0, GetTimeoutValue())).Result.ToList();
                if (result.Any())
                {
                    Console.WriteLine("Found {0} bridges:", result.Count);
                    Console.WriteLine();
                    foreach (var bridge in result)
                    {
                        Console.WriteLine("\t{0}", bridge);
                    }
                    return 0;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Could not locate bridges due to error: {0} : {1}", e.Message, e.InnerException != null ? e.InnerException.Message : "");
                throw;
            }
            Console.WriteLine("Failed to find any bridges.  Ensure bridge Has internet access to register.");
            return 1;
        }

        private string GetIpAddresss()
        {
            var localIp = string.Empty;
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList.Where(ip => ip.AddressFamily == AddressFamily.InterNetwork))
            {
                localIp = ip.ToString();
            }
            return localIp;
        }

        private int GetTimeoutValue()
        {
            int timeout;
            var result = Int32.TryParse(_timeoutString, out timeout);
            return result ? timeout : 15;
        }
    }
}
