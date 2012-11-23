using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        public event EventHandler BuildUnknown = delegate { };
        public event EventHandler CheckSuccessfull = delegate { };
        public event EventHandler CheckFailed = delegate { };
        public event EventHandler NoCompletedBuilds = delegate { };
        public event EventHandler ServerCheckException = delegate { };
        readonly object _lockObject = new object();

        public TeamCityBuildMonitor(ITeamCityApi api, string specificProject, bool failFast, List<string> buildLies, double checkIntervalInSeconds)
        {
            _api = api;
            _checkIntervalInSeconds = checkIntervalInSeconds;
            _failFast = failFast;
            _specificProject = specificProject;
            _buildLies = buildLies;
        }

        public void Start()
        {
            CheckBuilds();
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
            lock (_lockObject)
            {
                var buildStatus = BuildStatus.Passed;
                try
                {
                    var buildTypes = String.IsNullOrEmpty(_specificProject)
                                         ? _api.GetBuildTypes().ToList()
                                         : _api.GetBuildTypesByProjectName(_specificProject).ToList();
                    Parallel.ForEach(buildTypes.ToList(), buildType =>
                        {
                            {
                                BuildChecked(this, new EventArgs());
                                if (_buildLies.Contains(buildType.Name.ToLowerInvariant()))
                                {
                                    BuildSkipped(this, new EventArgs());
                                    return;
                                }
                                var details = _api.GetBuildTypeDetailsById(buildType.Id);

                                if (details.Paused)
                                {
                                    BuildPaused(this, new EventArgs());
                                    return;
                                }

                                var latestBuild = _api.GetLatestBuildByBuildType(buildType.Id);
                                if (latestBuild == null)
                                {
                                    NoCompletedBuilds(this, new EventArgs());
                                    return;
                                }


                                if ("success".Equals(latestBuild.Status, StringComparison.CurrentCultureIgnoreCase))
                                {
                                    BuildSuccess(this, new EventArgs());
                                    return;
                                }

                                if ("UNKNOWN".Equals(latestBuild.Status, StringComparison.CurrentCultureIgnoreCase))
                                {
                                    BuildUnknown(this, new EventArgs());
                                    return;
                                }

                                BuildFail(this, new EventArgs());
                                buildStatus = BuildStatus.Failed;
                                if (_failFast)
                                {
                                    CheckFailed(this, new EventArgs());
                                    return;
                                }
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
                        });
                    if (buildStatus == BuildStatus.Passed)
                    {
                        CheckSuccessfull(this, new EventArgs());
                    }
                    else
                    {
                        CheckFailed(this, new EventArgs());
                    }
                }
                catch (Exception)
                {
                    ServerCheckException(this, new EventArgs());
                }
            }
        }
    }
}