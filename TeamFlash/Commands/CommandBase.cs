using System;
using System.Collections.Generic;
using System.Linq;
using ManyConsole;
using TeamFlash.TeamCity;

namespace TeamFlash.Commands
{
    abstract class CommandBase : ConsoleCommand
    {
        private string _serverUrl = String.Empty;
        private string _username = String.Empty;
        private string _password = String.Empty;
        private bool _guestAuth;
        private string _specificProject = String.Empty;
        private bool _failOnFirstFailed;
        private string _buildLies = String.Empty;
        private Int64 _pollInterval = 60000;
        private List<string> _buildTypeIds;
        protected IBuildLight BuildLight;

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
            HasOption("i|interval", "Time interval in milliseconds to poll server (default 60000, or 1 minute).", option => _pollInterval = option != null ? Convert.ToInt64(option) : 60000);
            HasOption("b|biuldId=","Build Type Ids, comma delimited", option => _buildTypeIds = option.Split(';').ToList());
}

        public override int Run(string[] remainingArguments)
        {
            BuildLight.TurnOffLights();
            AppDomain.CurrentDomain.ProcessExit += (sender, eventArgs) => BuildLight.TurnOffLights();

            BuildLight.TestLights();
            BuildLight.Disco(2);
            BuildLight.TurnOffLights();
            TeamCityBuildMonitor buildMonitor = null;
            try
            {
                var lies = new List<string>(_buildLies.ToLowerInvariant().Split(';'));
                ITeamCityApi api = new TeamCityApi(_serverUrl);
                buildMonitor = new TeamCityBuildMonitor(api, _specificProject, _failOnFirstFailed, lies, _pollInterval, _buildTypeIds);
                const int blinkInterval = 30;
                buildMonitor.CheckFailed += (sender, eventArgs) =>
                {
                    BuildLight.TurnOnFailLight();
                    Console.WriteLine(DateTime.Now.ToShortTimeString() + " Failed");
                };
                RegisterBuildEvents(buildMonitor, blinkInterval);
                buildMonitor.CheckSuccessfull += (sender, eventArgs) =>
                {
                    BuildLight.TurnOnSuccessLight();
                    Console.WriteLine(DateTime.Now.ToShortTimeString() + " Passed");
                };
                buildMonitor.ServerCheckException += (sender, eventArgs) => Console.WriteLine(DateTime.Now.ToShortTimeString() + " Server unavailable");
                buildMonitor.Start();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            Console.ReadKey();

            if (buildMonitor != null) buildMonitor.Stop();
            BuildLight.TurnOffLights();
            return 0;
        }

        protected abstract void RegisterBuildEvents(TeamCityBuildMonitor buildMonitor, int blinkInterval);
    }
}
