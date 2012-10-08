using System;
using System.Collections.Generic;
using System.Threading;
using Mono.Options;

namespace TeamFlash
{
    class Program
    {
        static void Main(string[] args)
        {
			bool help = false;
			string serverUrl = string.Empty;
			string username = string.Empty;
			string password = string.Empty;
			bool guestAuth = false;
			string specificProject = string.Empty;

			var options = new OptionSet()
					.Add("?|help|h", "Output options", option => help = option != null)
					.Add("s=|url=|server=", "TeamCity URL", option => serverUrl = option)
					.Add("u=|user=|username=", "Username", option => username = option)
					.Add("p=|password=","Password", option => password = option)
					.Add("g=|guest=|guestauth=", "Connect using anonymous guestAuth", option => guestAuth = option != null)
					.Add("sp=|specificproject=","Constrain to a specific project", option => specificProject = option);

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
				System.Environment.Exit(0);
			}

			if (string.IsNullOrEmpty(serverUrl))
				OutputFailureAndExit(options, "Must have a Server URL provided");

			if (!guestAuth && string.IsNullOrEmpty(username))
				OutputFailureAndExit(options, "Either provide username/password or use guestAuth = true");


            var monitor = new Monitor();
            TurnOffLights(monitor);

            while (!Console.KeyAvailable)
            {
                List<string> failingBuildNames;
                var lastBuildStatus = RetrieveBuildStatus(
                    serverUrl,
                    username,
                    password,
					specificProject,
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
                        break;
                }

                Wait();
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

        static BuildStatus RetrieveBuildStatus(
            string serverUrl, string username, string password, string specificProject,
            out List<string> buildTypeNames)
        {
            dynamic query = new Query(serverUrl, username, password);
            buildTypeNames = null;

            var buildStatus = BuildStatus.Passed;

            try
            {
                foreach (var project in query.Projects)
                {
					if (!string.IsNullOrEmpty(specificProject) && !project.Name.Equals(specificProject))
						continue;

                    if (!project.BuildTypesExists)
                    {
                        continue;
                    }
                    foreach (var buildType in project.BuildTypes)
                    {
                        if ("true".Equals(buildType.Paused, StringComparison.CurrentCultureIgnoreCase))
                        {
                            continue;
                        }
                        var builds = buildType.Builds;
                        var latestBuild = builds.First;
                        if (latestBuild == null)
                        {
                            continue;
                        }

                        if ("success".Equals(latestBuild.Status, StringComparison.CurrentCultureIgnoreCase))
                        {
                            continue;
                        }

                        var isUnstableBuild = false;
                        foreach (var property in latestBuild.Properties)
                        {
                            if ("system.BuildState".Equals(property.Name, StringComparison.CurrentCultureIgnoreCase) &&
                            "unstable".Equals(property.Value, StringComparison.CurrentCultureIgnoreCase))
                            {
                                isUnstableBuild = true;
                            }

                            if ("BuildState".Equals(property.Name, StringComparison.CurrentCultureIgnoreCase) &&
                            "unstable".Equals(property.Value, StringComparison.CurrentCultureIgnoreCase))
                            {
                                isUnstableBuild = true;

                            }
                        }

                        if (isUnstableBuild)
                        {
                            continue;
                        }

                        var buildId = buildType.Id;
                        dynamic investigationQuery = new Query(serverUrl, username, password);
                        investigationQuery.RestBasePath = @"/httpAuth/app/rest/buildTypes/id:" + buildId +@"/";
                        buildStatus = BuildStatus.Failed;
             
                        foreach (var investigation in investigationQuery.Investigations)
                        {
                            var investigationState = investigation.State;
                            if ("taken".Equals(investigationState, StringComparison.CurrentCultureIgnoreCase) ||
                                "fixed".Equals(investigationState, StringComparison.CurrentCultureIgnoreCase))
                            {
                                buildStatus = BuildStatus.Investigating;
                            }
                        }

                        if (buildStatus == BuildStatus.Failed)
                        {
                            return buildStatus;
                        }
                    }

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
