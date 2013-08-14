using System;
using System.Collections.Generic;
using ManyConsole;
using TeamFlash.TeamCity;

namespace TeamFlash.Commands
{
    internal class CommandBase : ConsoleCommand
    {
        private bool _help = false;
        private string _serverUrl = String.Empty;
        private string _username = String.Empty;
        private string _password = String.Empty;
        private bool _guestAuth = false;
        private string _specificProject = String.Empty;
        private bool _failOnFirstFailed = false;
        private string _buildLies = String.Empty;
        private double _pollInterval = 60000;
        protected IBuildLight buildLight;

        protected CommandBase()
        {
            HasRequiredOption("s=|url=|server=", "TeamCity URL", option => _serverUrl = option);
            SkipsCommandSummaryBeforeRunning();
            HasOption("u|user=|username=", "Username", option => _username = option);
            HasOption("p|password=", "Password", option => _password = option);
            HasOption("g|guest|guestauth", "Connect using anonymous guestAuth", option => _guestAuth = option != null);
            HasOption("sp|specificproject=", "Constrain to a specific project", option => _specificProject = option);
            HasOption("f|failonfirstfailed", "Check until finding the first failed", option => _failOnFirstFailed = option != null);
            HasOption("l|lies=", "Lie for these builds, say they are green", option => _buildLies = option);
            HasOption("i|interval", "Time interval in seconds to poll server.", option => _pollInterval = option != null ? Convert.ToDouble(option) : 60000);
        }

        public override int Run(string[] remainingArguments)
        {
            buildLight.TurnOffLights();
            AppDomain.CurrentDomain.ProcessExit += (sender, eventArgs) => buildLight.TurnOffLights();

            buildLight.TestLights();
            buildLight.Disco(2);
            buildLight.TurnOffLights();
            TeamCityBuildMonitor buildMonitor = null;
            try
            {
                var lies = new List<string>(_buildLies.ToLowerInvariant().Split(';'));
                ITeamCityApi api = new TeamCityApi(_serverUrl);
                buildMonitor = new TeamCityBuildMonitor(api, _specificProject, _failOnFirstFailed, lies, _pollInterval);
                buildMonitor.CheckFailed += (sender, eventArgs) =>
                {
                    buildLight.TurnOnFailLight();
                    Console.WriteLine(DateTime.Now.ToShortTimeString() + " Failed");
                };
                buildMonitor.BuildChecked += (sender, eventArgs) => buildLight.Blink();
                const int blinkInterval = 30;
                buildMonitor.BuildPaused += (sender, eventArgs) => buildLight.BlinkThenRevert(LightColour.Yellow, blinkInterval);
                buildMonitor.BuildSkipped += (sender, eventArgs) => buildLight.BlinkThenRevert(LightColour.Purple, blinkInterval);
                buildMonitor.BuildSuccess += (sender, eventArgs) => buildLight.BlinkThenRevert(LightColour.Green, blinkInterval);
                buildMonitor.BuildFail += (sender, eventArgs) => buildLight.BlinkThenRevert(LightColour.Red, blinkInterval);
                buildMonitor.BuildUnknown += (sender, eventArgs) => buildLight.BlinkThenRevert(LightColour.Yellow, blinkInterval);
                buildMonitor.CheckSuccessfull += (sender, eventArgs) =>
                {
                    buildLight.TurnOnSuccessLight();
                    Console.WriteLine(DateTime.Now.ToShortTimeString() + " Passed");
                };
                buildMonitor.ServerCheckException += (sender, eventArgs) => Console.WriteLine(DateTime.Now.ToShortTimeString() + " Server unavailable");
                buildMonitor.Start();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            while (!Console.KeyAvailable)
            {
            }
            if (buildMonitor != null) buildMonitor.Stop();
            buildLight.TurnOffLights();
            return 0;
        }
    }
}
