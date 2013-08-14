using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ManyConsole;
using Q42.HueApi;
using TeamFlash.Hue;

namespace TeamFlash.Commands
{
    class HueRegisterCommand : ConsoleCommand
    {
        private string _ip;

        public HueRegisterCommand()
        {
            IsCommand("hueregister", "Register a Philips Hue Bridge");
            HasRequiredOption("ip=", "IP address for the bridge", o => _ip = o);
            SkipsCommandSummaryBeforeRunning();
        }
        public override int Run(string[] remainingArguments)
        {
            var hueClient = new HueClient(_ip);
            var result = hueClient.RegisterAsync(HueBuildLight.AppName, HueBuildLight.AppKey).Result;
            if (result)
            {
                Console.WriteLine("Registered correctly.");
            }
            else
            {
                Console.WriteLine("Failed to register.");
            }
            return result ? 0 : 1;
        }
    }
}
