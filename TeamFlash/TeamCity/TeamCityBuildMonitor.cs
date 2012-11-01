using System;
using System.Collections.Generic;
using System.Timers;

namespace TeamFlash
{
    public class TeamCityBuildMonitor
    {
        private readonly ITeamCityApi _api;
        private Timer _timer;
        private readonly string _specificProject;
        private readonly bool _failFast;
        private readonly List<string> _buildLies;
        private readonly double _checkIntervalInSeconds;
        public event EventHandler ServerCheckStarted = delegate { };
        public event EventHandler ServerCheckFinished = delegate { };
        public event EventHandler BuildChecked = delegate { };
        public event EventHandler BuildFail = delegate { };
        public event EventHandler BuildSuccess = delegate { };
        public event EventHandler BuildPaused = delegate { };
        public event EventHandler BuildSkipped = delegate { };
        public event EventHandler NoCompletedBuilds = delegate { };
        public event EventHandler ServerCheckException = delegate { };

        public TeamCityBuildMonitor(ITeamCityApi api, string specificProject, bool failFast, List<string> buildLies, double checkIntervalInSeconds)
        {
            _api = api;
            _checkIntervalInSeconds = checkIntervalInSeconds;
            _timer.AutoReset = true;
            _failFast = failFast;
            _specificProject = specificProject;
            _buildLies = buildLies;
        }

        public void Start()
        {
            _timer = new Timer(_checkIntervalInSeconds) {AutoReset = true};
            _timer.Elapsed += (sender, args) => CheckBuilds();
            ServerCheckStarted(this, new EventArgs());
            _timer.Start();
        }

        public void Stop()
        {
            _timer.Stop();
            ServerCheckFinished(this, new EventArgs());
        }

        private void CheckBuilds()
        {
            var buildStatus = BuildStatus.Passed;
            try
            {
                var buildTypes = String.IsNullOrEmpty(_specificProject) ? _api.GetBuildTypes() : _api.GetBuildTypesByProjectName(_specificProject);
                foreach (var buildType in buildTypes)
                {
                    BuildChecked(this, new EventArgs());
                    if (_buildLies.Contains(buildType.Name.ToLowerInvariant()))
                    {
                        BuildSkipped(this, new EventArgs());
                        continue;
                    }
                    var details = _api.GetBuildTypeDetailsById(buildType.Id);

                    if (details.Paused)
                    {
                        BuildPaused(this, new EventArgs());
                        continue;
                    }
                        
                    var latestBuild = _api.GetLatestBuildByBuildType(buildType.Id);
                    if (latestBuild == null)
                    {
                        NoCompletedBuilds(this, new EventArgs());
                        continue;
                    }


                    if ("success".Equals(latestBuild.Status, StringComparison.CurrentCultureIgnoreCase))
                    { 
                        continue;
                    }

                    BuildFail(this, new EventArgs());
                    buildStatus = BuildStatus.Failed;
                    if (_failFast)
                        break;
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
                if (buildStatus == BuildStatus.Passed)
                    BuildSuccess(this, new EventArgs());
            }
            catch (Exception)
            {
                ServerCheckException(this, new EventArgs());
            }
        }
    }
}