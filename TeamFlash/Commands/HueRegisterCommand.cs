using System;
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
            IsCommand("hueregister", "Register a Philips Hue Bridge. You will need to press the link button on the bridge to register.");
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
                return 0;
            }

            //If we failed to register, attempt to initialise and see if we succeed as we may already be registered.
            hueClient.Initialize(HueBuildLight.AppKey);
            var initialised = hueClient.IsInitialized;
            Console.WriteLine(initialised ? "Already registered." : "Failed to register");
            return initialised ? 0 : 1;
        }
    }
}
