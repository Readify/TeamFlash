using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading;
using Mono.Options;

namespace TeamFlash
{
    class Program
    {
        static void Main(string[] args)
        {
			var help = false;
			var serverUrl = string.Empty;
			var username = string.Empty;
			var password = string.Empty;
			var guestAuth = false;
			var specificProject = string.Empty;

            bool failOnFirstFailed = false;

            string buildLies = string.Empty;

            var options = new OptionSet()
					.Add("?|help|h", "Output options", option => help = option != null)
					.Add("s=|url=|server=", "TeamCity URL", option => serverUrl = option)
					.Add("u|user=|username=", "Username", option => username = option)
					.Add("p|password=","Password", option => password = option)
					.Add("g|guest|guestauth", "Connect using anonymous guestAuth", option => guestAuth = option != null)
					.Add("sp|specificproject=","Constrain to a specific project", option => specificProject = option)
                    .Add("f|failonfirstfailed", "Check until finding the first failed", option => failOnFirstFailed = option != null)
                    .Add("l|lies=","Lie for these builds, say they are green", option => buildLies = option);

			try
			{
				options.Parse(args);
			}
			catch (OptionException)
			{
				OutputFailureAndExit(options, "Incorrect arguments, usage is: ");
			}

			if (help)
			{
				Console.WriteLine(options);
				Environment.Exit(0);
			}

			if (string.IsNullOrEmpty(serverUrl))
				OutputFailureAndExit(options, "Must have a Server URL provided");

			if (!guestAuth && string.IsNullOrEmpty(username))
				OutputFailureAndExit(options, "Either provide username/password or use guestAuth = true");


            var monitor = new Monitor();
            TurnOffLights(monitor);

            TestLights(monitor);
            try
            {
                while (!Console.KeyAvailable)
                {

                    //TurnOnBuildCheckLight(monitor);

                    List<string> failingBuildNames;
                    var lies = new List<String>(buildLies.ToLowerInvariant().Split(','));
                    var lastBuildStatus = RetrieveBuildStatus(
                        serverUrl,
                        username,
                        password,
                        specificProject,
                        guestAuth,
                        failOnFirstFailed,
                        lies,
                        out failingBuildNames);
                    switch (lastBuildStatus)
                    {
                        case BuildStatus.Unavailable:
                            TurnOffLights(monitor);
                            Console.WriteLine(DateTime.Now.ToShortTimeString() + " Server unavailable");
                            break;
                        case BuildStatus.Passed:
                            TurnOnSuccessLight(monitor);
                            Console.WriteLine(DateTime.Now.ToShortTimeString() + " Passed");
                            break;
                        case BuildStatus.Investigating:
                            TurnOnWarningLight(monitor);
                            Console.WriteLine(DateTime.Now.ToShortTimeString() + " Investigating");
                            break;
                        case BuildStatus.Failed:
                            TurnOnFailLight(monitor);
                            Console.WriteLine(DateTime.Now.ToShortTimeString() + " Failed");
                            foreach (var failingBuildName in failingBuildNames)
                            {
                                Console.WriteLine(string.Format("{0}", failingBuildName).PadLeft(20, ' '));
                            }
                            break;
                    }

                    Wait();
                }
            }
            finally
            {
                TurnOffLights(monitor);                
            }

            TurnOffLights(monitor);

        }

        static void Wait()
        {
            var delayCount = 0;
            while (delayCount < 30 &&
                !Console.KeyAvailable)
            {
                delayCount++;
                Thread.Sleep(1000);
            }
        }

        static void TestLights(Monitor monitor)
        {
            var i = 0;

            while (i < 1 &&
                !Console.KeyAvailable)
            {
                i++;
                TurnOnFailLight(monitor);
                Thread.Sleep(800);
                TurnOffLights(monitor);
                Thread.Sleep(200);
                TurnOnWarningLight(monitor);
                Thread.Sleep(800);
                TurnOffLights(monitor);
                Thread.Sleep(200);
                TurnOnSuccessLight(monitor);
                Thread.Sleep(800);
                TurnOffLights(monitor);
                Thread.Sleep(200);
            }
            
        }

        static void TurnOnBuildCheckLight(Monitor monitor)
        {
            monitor.SetLed(DelcomBuildIndicator.REDLED, false, false);
            monitor.SetLed(DelcomBuildIndicator.GREENLED, true, true);
            monitor.SetLed(DelcomBuildIndicator.BLUELED, false, false);
        }

        
        static void TurnOnSuccessLight(Monitor monitor)
        {
            monitor.SetLed(DelcomBuildIndicator.REDLED, false, false);
            monitor.SetLed(DelcomBuildIndicator.GREENLED, true, false);
            monitor.SetLed(DelcomBuildIndicator.BLUELED, false, false);
        }

        static void TurnOnWarningLight(Monitor monitor)
        {
            monitor.SetLed(DelcomBuildIndicator.REDLED, false, false);
            monitor.SetLed(DelcomBuildIndicator.GREENLED, false, false);
            monitor.SetLed(DelcomBuildIndicator.BLUELED, true, false);
        }

        static void TurnOnFailLight(Monitor monitor)
        {
            monitor.SetLed(DelcomBuildIndicator.REDLED, true, false);
            monitor.SetLed(DelcomBuildIndicator.GREENLED, false, false);
            monitor.SetLed(DelcomBuildIndicator.BLUELED, false, false);
        }

        static void TurnOffLights(Monitor monitor)
        {
            monitor.SetLed(DelcomBuildIndicator.REDLED, false, false);
            monitor.SetLed(DelcomBuildIndicator.GREENLED, false, false);
            monitor.SetLed(DelcomBuildIndicator.BLUELED, false, false);
        }

        static BuildStatus RetrieveBuildStatus(string serverUrl, string username, string password, string specificProject, bool guestAuth, bool failFast, List<string> buildLies, out List<string> buildTypeNames)
        {
            var api = new TeamCityApi(serverUrl);

            buildTypeNames = new List<string>();

            var buildStatus = BuildStatus.Passed;

            try
            {
                List<BuildType> buildTypes = string.IsNullOrEmpty(specificProject) ? api.GetBuildTypes() : api.GetBuildTypesByProjectName(specificProject);
                foreach (var buildType in buildTypes)
                {
                    if (buildLies.Contains(buildType.Name.ToLowerInvariant()))
                        continue;
                    var details = api.GetBuildTypeDetailsById(buildType.Id);

                    if (details.Paused)
                        continue;

                    var latestBuild = api.GetLatestBuildByBuildType(buildType.Id);
                    if (latestBuild == null)
                        continue;


                    if ("success".Equals(latestBuild.Status, StringComparison.CurrentCultureIgnoreCase))
                        continue;


                    //var isUnstableBuild = false;
                    //foreach (var property in latestBuild.Properties)
                    //{
                    //    if ("system.BuildState".Equals(property.Name, StringComparison.CurrentCultureIgnoreCase) &&
                    //        "unstable".Equals(property.Value, StringComparison.CurrentCultureIgnoreCase))
                    //    {
                    //        isUnstableBuild = true;
                    //    }

                    //    if ("BuildState".Equals(property.Name, StringComparison.CurrentCultureIgnoreCase) &&
                    //        "unstable".Equals(property.Value, StringComparison.CurrentCultureIgnoreCase))
                    //    {
                    //        isUnstableBuild = true;

                    //    }
                    //}
                    //if (isUnstableBuild)
                    //{
                    //    continue;
                    //}

                    buildStatus = BuildStatus.Failed;
                    buildTypeNames.Add(buildType.Name);
                    if (failFast)
                        return buildStatus;
                    //foreach (var investigation in buildType.Investigations)
                    //{
                    //    var investigationState = investigation.State;
                    //    if ("taken".Equals(investigationState, StringComparison.CurrentCultureIgnoreCase) ||
                    //        "fixed".Equals(investigationState, StringComparison.CurrentCultureIgnoreCase))
                    //    {
                    //        buildStatus = BuildStatus.Investigating;
                    //    }
                    //}
                }
            }
            catch (Exception)
            {
                return BuildStatus.Unavailable;
            }

            return buildStatus;
        }

		static OptionSet OutputFailureAndExit(OptionSet options, string message)
		{
			Console.WriteLine(message);
			Console.WriteLine("teamflash.exe /s[erver] VALUE /u[sername] VALUE /p[assword] VALUE /g[uestauth] /sp[ecificproject] VALUE");
			options.WriteOptionDescriptions(Console.Error);
			System.Environment.Exit(1);
			return options;
		}
    }
}
