using System;
using System.Linq;
using ManyConsole;
using Q42.HueApi;

namespace TeamFlash.Commands
{
    class HueBridgeCommand : ConsoleCommand
    {
        private string _timeoutString;

        public HueBridgeCommand()
        {
            IsCommand("huebridge", "Attempts to discover any Hue bridges on the network, and outputs their IP.");
            HasOption("t=|timeout=", "Maximum length of time in seconds to search for the bridge (default 15 seconds)", o => _timeoutString = o);
            SkipsCommandSummaryBeforeRunning();
        }
        public override int Run(string[] remainingArguments)
        {
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

            Console.WriteLine("Failed to find any bridges.  Ensure bridge is switched on and discoverable on your network segment.");
            return 1;
        }

        private int GetTimeoutValue()
        {
            int timeout;
            var result = Int32.TryParse(_timeoutString, out timeout);
            return result ? timeout : 15;
        }
    }
}
