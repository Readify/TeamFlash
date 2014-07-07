using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml.Serialization;

namespace TeamFlash
{
    class Program
    {
        static void Main()
        {
            Logger.VerboseEnabled = PromptForVerboseMode();

            var teamFlashConfig = LoadConfig();

            teamFlashConfig.ServerUrl = ReadConfig("TeamCity URL", teamFlashConfig.ServerUrl);
            teamFlashConfig.Username = ReadConfig("Username", teamFlashConfig.Username);
            var password = ReadConfig("Password", "");
            teamFlashConfig.BuildTypeIds = ReadConfig("Comma separated build type ids (eg, \"bt64,bt12\"), or * for all", teamFlashConfig.BuildTypeIds);

            SaveConfig(teamFlashConfig);

            Console.Clear();

            var buildTypeIds = teamFlashConfig.BuildTypeIds == "*"
                ? new string[0]
                : teamFlashConfig.BuildTypeIds.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToArray();

#if __MonoCS__
            var buildLight = new Linux.BuildLight();
#else
            var buildLight = new BuildLight();
#endif

            buildLight.Off();

            while (!Console.KeyAvailable)
            {
                var lastBuildStatus = RetrieveBuildStatus(
                    teamFlashConfig.ServerUrl,
                    teamFlashConfig.Username,
                    password,
                    buildTypeIds);
                switch (lastBuildStatus)
                {
                    case BuildStatus.Unavailable:
                        buildLight.Off();
                        Logger.WriteLine("Build status not available");
                        break;
                    case BuildStatus.Passed:
                        buildLight.Success();
                        Logger.WriteLine("Passed");
                        break;
                    case BuildStatus.Investigating:
                        buildLight.Warning();
                        Logger.WriteLine("Investigating");
                        break;
                    case BuildStatus.Failed:
                        buildLight.Fail();
                        Logger.WriteLine("Failed");
                        break;
                }

                Wait();
            }

            buildLight.Off();
        }

        static bool PromptForVerboseMode()
        {
            Console.Write("Would you like to start in VERBOSE mode? (y/n)");
            var selection = Console.ReadKey(true /*intercept*/).KeyChar;
            Console.WriteLine();
            var verbose = char.ToLower(selection) == 'y';
            if(verbose) Console.WriteLine("VERBOSE mode is ON. Re-run TeamFlash in order to revert this.");
            Console.WriteLine();
            return verbose;
        }

        static TeamFlashConfig LoadConfig()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var configFilePath = Path.Combine(appDataPath, @"TeamFlash\config.json");
            try
            {
                if (!File.Exists(configFilePath))return new TeamFlashConfig();

                Logger.WriteLine("Reading config values from: {0}", configFilePath);

                var serializer = new XmlSerializer(typeof(TeamFlashConfig));
                using (var stream = File.OpenRead(configFilePath))
                {
                    return (TeamFlashConfig)serializer.Deserialize(stream);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLine("The following exception occurred loading the config file \"{0}\":{2}Message: {1}{2}Feel free to quit and fix it or re-enter your server details...", configFilePath, ex.Message, Environment.NewLine);
            }
            return new TeamFlashConfig();
        }

        static void SaveConfig(TeamFlashConfig config)
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var teamFlashPath = Path.Combine(appDataPath, @"TeamFlash\");
            if (!Directory.Exists(teamFlashPath))
                Directory.CreateDirectory(teamFlashPath);

            var configFilePath = Path.Combine(teamFlashPath, @"config.json");

            var serializer = new XmlSerializer(typeof(TeamFlashConfig));
            using (var stream = File.OpenWrite(configFilePath))
            {
                serializer.Serialize(stream, config);
            }
        }

        static string ReadConfig(string name, string previousValue)
        {
            string input = null;
            while (string.IsNullOrEmpty(input))
            {
                Logger.WriteLine("{0}?", name);
                if (!string.IsNullOrEmpty(previousValue))
                {
                    Logger.WriteLine("(press enter for previous value: {0})", previousValue);
                }
                input = Console.ReadLine();
                if (!string.IsNullOrEmpty(previousValue) &&
                    string.IsNullOrEmpty(input))
                {
                    input = previousValue;
                }
                Console.WriteLine();
            }
            return input;
        }

        static void Wait()
        {
            Logger.Verbose("Waiting for 30 seconds.");
            var delayCount = 0;
            while (delayCount < 30 &&
                !Console.KeyAvailable)
            {
                delayCount++;
                Thread.Sleep(1000);
            }
        }

        static BuildStatus RetrieveBuildStatus(
            string serverUrl, string username, string password,
            IEnumerable<string> buildTypeIds)
        {
            Logger.Verbose("Checking build status.");

            buildTypeIds = buildTypeIds.ToArray();

            dynamic query = new Query(serverUrl, username, password);

            var buildStatus = BuildStatus.Passed;

            try
            {
                var couldFindProjects = false;
                foreach (var project in query.Projects)
                {
                    couldFindProjects = true;
                    Logger.Verbose("Checking Project '{0}'.", project.Name);
                    if (!project.BuildTypesExists)
                    {
                        Logger.Verbose("Bypassing Project '{0}' because it has no 'BuiltTypes' property defined.", project.Name);
                        continue;
                    }

                    foreach (var buildType in project.BuildTypes)
                    {
                        Logger.Verbose("Checking Built Type '{0}\\{1}'.", project.Name, buildType.Name);
                        if (buildTypeIds.Any() &&
                            buildTypeIds.All(id => id != buildType.Id))
                        {
                            Logger.Verbose("Bypassing Built Type '{0}\\{1}' because it does NOT match configured built-type list to monitor.", project.Name, buildType.Name);
                            continue;
                        }

                        if (buildType.PausedExists && "true".Equals(buildType.Paused, StringComparison.CurrentCultureIgnoreCase))
                        {
                            Logger.Verbose("Bypassing Built Type '{0}\\{1}' because it has 'Paused' property set to 'true'.", project.Name, buildType.Name);
                            continue;
                        }

                        var builds = buildType.Builds;
                        var latestBuild = builds.First;
                        if (latestBuild == null)
                        {
                            Logger.Verbose("Bypassing Built Type '{0}\\{1}' because no built history is available to it yet.", project.Name, buildType.Name);
                            continue;
                        }

                        if ("success".Equals(latestBuild.Status, StringComparison.CurrentCultureIgnoreCase))
                        {
                            dynamic runningBuild = new Query(serverUrl, username, password) { RestBasePath = string.Format("/httpAuth/app/rest/buildTypes/id:{0}/builds/running:any", buildType.Id) };

                            runningBuild.Load();
                            if ("success".Equals(runningBuild.Status, StringComparison.CurrentCultureIgnoreCase))
                            {
                                Logger.Verbose("Bypassing Built Type '{0}\\{1}' because status of last build and all running builds are 'success'.", project.Name, buildType.Name);
                                continue;
                            }
                        }

                        if (latestBuild.PropertiesExists)
                        {
                            var isUnstableBuild = false;
                            foreach (var property in latestBuild.Properties)
                            {
                                if (
                                    "system.BuildState".Equals(property.Name, StringComparison.CurrentCultureIgnoreCase) &&
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
                                Logger.Verbose("Bypassing Built Type '{0}\\{1}' because it is marked as 'unstable'.", project.Name, buildType.Name);
                                continue;
                            }
                        }

                        Logger.Verbose("Now checking investigation status of Built Type '{0}\\{1}'.", project.Name, buildType.Name);
                        var buildId = buildType.Id;
                        dynamic investigationQuery = new Query(serverUrl, username, password);
                        investigationQuery.RestBasePath = @"/httpAuth/app/rest/buildTypes/id:" + buildId + @"/";
                        buildStatus = BuildStatus.Failed;

                        foreach (var investigation in investigationQuery.Investigations)
                        {
                            var investigationState = investigation.State;
                            if ("taken".Equals(investigationState, StringComparison.CurrentCultureIgnoreCase) ||
                                "fixed".Equals(investigationState, StringComparison.CurrentCultureIgnoreCase))
                            {
                                Logger.Verbose("Investigation status of Built Type '{0}\\{1}' detected as either 'taken' or 'fixed'.", project.Name, buildType.Name);
                                buildStatus = BuildStatus.Investigating;
                            }
                        }

                        if (buildStatus == BuildStatus.Failed)
                        {
                            Logger.Verbose("Concluding status of Built Type '{0}\\{1}' as FAIL.", project.Name, buildType.Name);
                            return BuildStatus.Failed;
                        }
                    }

                }

                if (!couldFindProjects)
                {
                    Logger.Verbose("No Projects found! Please ensure if TeamCity URL is valid and also TeamCity setup and credentials are correct.");
                    return BuildStatus.Unavailable;
                }
            }
            catch (Exception exception)
            {
                Logger.Error(exception);
                return BuildStatus.Unavailable;
            }

            return buildStatus;
        }
    }
}
