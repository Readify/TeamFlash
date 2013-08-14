using System.Collections.Generic;
using System.Linq;
using TeamFlash.Hue;
using TeamFlash.TeamCity;

namespace TeamFlash.Commands
{
    class HueCommand : CommandBase
    {
        private string _ip;
        private string _lightsString;

        public HueCommand()
        {
            IsCommand("hue", "Start monitoring using a Philips Hue light");
            HasRequiredOption("ip=", "IP address to the Hue Light", o => _ip = o);
            HasOption("lights=", "Comma delimeted list of light numbers", o =>_lightsString = o);
        }

        public override int Run(string[] remainingArguments)
        {
            var lights = string.IsNullOrEmpty(_lightsString) ? new List<string>{"1"} : _lightsString.Split(',').ToList();
            BuildLight = new HueBuildLight(_ip, lights);
            return base.Run(remainingArguments);
        }

        protected override void RegisterBuildEvents(TeamCityBuildMonitor buildMonitor, int blinkInterval)
        {
        }
    }
}
