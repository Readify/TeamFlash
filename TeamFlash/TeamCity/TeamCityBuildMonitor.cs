using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace TeamFlash.TeamCity
{
    public class TeamCityBuildMonitor
    {
        private readonly ITeamCityApi _api;
        private Timer _timer;
        private readonly string _specificProject;
        private readonly bool _failFast;
        private readonly List<string> _buildLies;
        private readonly Int64 _checkIntervalInMilliSeconds;
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

        public TeamCityBuildMonitor(ITeamCityApi api, string specificProject, bool failFast, List<string> buildLies, Int64 checkIntervalInMilliSeconds)
        {
            _api = api;
            _checkIntervalInMilliSeconds = checkIntervalInMilliSeconds;
            _failFast = failFast;
            _specificProject = specificProject;
            _buildLies = buildLies;
        }

        public void Start()
        {
            _timer = new Timer(CheckBuild, null, 0, _checkIntervalInMilliSeconds);
            ServerCheckStarted(this, new EventArgs());
        }

        public void Stop()
        {
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
            ServerCheckFinished(this, new EventArgs());
        }

        private void CheckBuild(object state)
        {
            lock (_lockObject)
            {
                var buildStatus = BuildStatus.Passed;
                try
                {
                    var buildTypes = String.IsNullOrEmpty(_specificProject)
                                         ? _api.GetBuildTypes().ToList()
                                         : _api.GetBuildTypesByProjectName(_specificProject).ToList();
                    Parallel.ForEach(buildTypes.ToList(), (buildType, loopState) =>
                        {
                            {
                                if (!loopState.IsStopped)
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
                                        loopState.Stop();
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