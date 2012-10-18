using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace TeamFlash
{
    class Program
    {
        static void Main()
        {
            Console.Write("TeamCity URL:");
            var serverUrl = Console.ReadLine();
            Console.Write("Username:");
            var username = Console.ReadLine();
            Console.Write("Password:");
            var password = Console.ReadLine();
            Console.Write("Comma separated build type ids (eg, \"bt64,bt12\"), or enter for all:");
            var buildTypeIds = (Console.ReadLine() ?? "")
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .ToArray();
            Console.Clear();

            var monitor = new Monitor();
            TurnOffLights(monitor);

            while (!Console.KeyAvailable)
            {
                var lastBuildStatus = RetrieveBuildStatus(
                    serverUrl,
                    username,
                    password,
                    buildTypeIds);
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
            string serverUrl, string username, string password,
            IEnumerable<string> buildTypeIds)
        {
            buildTypeIds = buildTypeIds.ToArray();

            dynamic query = new Query(serverUrl, username, password);

            var buildStatus = BuildStatus.Passed;

            try
            {
                foreach (var project in query.Projects)
                {
                    if (!project.BuildTypesExists)
                    {
                        continue;
                    }
                    foreach (var buildType in project.BuildTypes)
                    {
                        if (buildTypeIds.Any() &&
                            buildTypeIds.All(id => id != buildType.Id))
                        {
                            continue;
                        }
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

    }
}
