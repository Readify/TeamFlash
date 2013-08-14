using System.Collections.Generic;

namespace TeamFlash.TeamCity
{
    public interface ITeamCityApi
    {
        List<BuildType> GetBuildTypes();
        BuildTypeDetails GetBuildTypeDetailsById(string id);
        IEnumerable<Artifact> GetArtifactListByBuildType(string buildType);
        IEnumerable<ChangeDetail> GetChangeDetailsForLastBuildByBuildType(string buildType);
        Build GetLatestSuccesfulBuildByBuildType(string buildType);
        IEnumerable<ChangeDetail> GetChangeDetailsByBuildId(string buildId);
        IEnumerable<ChangeDetail> GetChangeDetailsForCurrentBuildByBuildType(string buildType);
        IEnumerable<Build> GetRunningBuildByBuildType(string buildType);
        IEnumerable<BuildType> GetBuildTypeByProjectAndName(string project, string buildName);
        IEnumerable<ChangeDetail> GetChangeDetailsByBuildTypeAndBuildId(string buildType, string from, string to);
        IEnumerable<ChangeDetail> GetReleaseNotesByBuildTypeAndBuildNumber(string buildType, string from, string to);
        BuildDetails GetBuildDetailsByBuildId(string id);
        ChangeList GetChangeListByBuildId(string id);
        ChangeDetail GetChangeDetailsByChangeId(string id);
        IEnumerable<Build> GetBuildsByBuildType(string buildType);
        IEnumerable<Build> GetLatestBuildByBuildTypeAndStatus(string buildType, string status);
        Build GetLatestBuildByBuildType(string buildType);
        IEnumerable<Project> GetProjects();
        List<BuildType> GetBuildTypesByProjectName(string specificProject);
    }
}