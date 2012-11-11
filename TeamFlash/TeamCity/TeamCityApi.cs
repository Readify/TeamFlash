using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Web;
using RestSharp;

namespace TeamFlash
{
    public class TeamCityApi : ITeamCityApi
    {
        private readonly string _teamCityServer;
        private readonly RestClient _client;

        public TeamCityApi(string server)
        {
            _teamCityServer = server;
            _client = new RestClient(_teamCityServer + "/guestAuth/");
        }

        public List<BuildType> GetBuildTypes()
        {
            var request = new RestRequest("app/rest/buildTypes", Method.GET) { RequestFormat = DataFormat.Xml };
            request.AddHeader("Accept", "application/xml");

            var buildConfigs = _client.Execute<List<BuildType>>(request);
            return buildConfigs.Data;
        }

        public BuildTypeDetails GetBuildTypeDetailsById(string id)
        {
            var buildDetails = GetXmlBuildRequest("app/rest/buildTypes/id:{ID}", new Dictionary<string, string>(){{"ID", id}});
            var response = GetSafeResponse<BuildTypeDetails>(buildDetails);
            return response.Data;
        }

        private static RestRequest GetXmlBuildRequest(string endpoint, Dictionary<string,string> parameters = null)
        {
            var request = new RestRequest(endpoint, Method.GET);
            foreach (var parameter in parameters)
            {
                request.AddParameter(parameter.Key, parameter.Value, ParameterType.UrlSegment);
            }
            request.RequestFormat = DataFormat.Xml;
            request.AddHeader("Accept", "application/xml");
            return request;
        }

        public IEnumerable<Artifact> GetArtifactListByBuildType(string buildType)
        {
            var request = new RestRequest("repository/download/{ID}/lastSuccessful/teamcity-ivy.xml");
            request.AddParameter("ID", buildType, ParameterType.UrlSegment);
            request.RequestFormat = DataFormat.Xml;
            request.AddHeader("Accept", "application/xml");
            var response = GetSafeResponse<IvyModule>(request);
            return response.Data != null ? response.Data.Publications : new List<Artifact>();
        }

        public IEnumerable<ChangeDetail> GetChangeDetailsForLastBuildByBuildType(string buildType)
        {
            var latestBuild = GetLatestBuildByBuildType(buildType);
            return GetChangeDetailsByBuildId(latestBuild.Id);
        }

        public Build GetLatestSuccesfulBuildByBuildType(string buildType)
        {
            var builds = GetBuildsByBuildType(buildType);
            var latestBuild = builds.Where(b => b.Status == "SUCCESS").OrderByDescending(b => b.BuildTypeId).FirstOrDefault();
            return latestBuild;
        }

        public IEnumerable<ChangeDetail> GetChangeDetailsByBuildId(string buildId)
        {
            var changeList = GetChangeListByBuildId(buildId);
            var changeDetails = changeList.Changes.Select(c => GetChangeDetailsByChangeId(c.Id)).ToList();
            return changeDetails;
        }

        public IEnumerable<ChangeDetail> GetChangeDetailsForCurrentBuildByBuildType(string buildType)
        {
            var response = GetRunningBuildByBuildType(buildType);
            if (response == null)
                return new List<ChangeDetail>();

            var releaseNotes = GetChangeDetailsByBuildId(response.FirstOrDefault().Id);
            return releaseNotes;
        }

        public IEnumerable<Build> GetRunningBuildByBuildType(string buildType)
        {
            var request = GetXmlBuildRequest("app/rest/builds/?locator=buildType:{BT},running:true", new Dictionary<string, string>(){{"BT", buildType}});
            var response = GetSafeResponse<List<Build>>(request).Data;
            return response;
        }

        public IEnumerable<BuildType> GetBuildTypeByProjectAndName(string project, string buildName)
        {
            var request = GetXmlBuildRequest("app/rest/buildTypes");
            var response = GetSafeResponse<List<BuildType>>(request);
            return response.Data.Where(b => b.ProjectName.Equals(project, StringComparison.InvariantCultureIgnoreCase) && b.Name.Equals(buildName, StringComparison.InvariantCultureIgnoreCase));
        }

        private IEnumerable<ChangeDetail> GetChangeDetailsByBuildTypeAndBuildId(string buildType, string from, string to, Func<Build, string, bool> comparitor)
        {
            var builds = GetBuildsByBuildType(buildType);
            var changeDeltas = new List<ChangeDetail>();
            var captureChanges = false;
            foreach (var build in builds.OrderBy(b => b.Id))
            {
                if (comparitor(build, from))
                    captureChanges = true;

                if (captureChanges)
                    changeDeltas.AddRange(GetChangeDetailsByBuildId(build.Id));

                if (comparitor(build, to))
                    break;
            }
            return changeDeltas;
        }

        public IEnumerable<ChangeDetail> GetChangeDetailsByBuildTypeAndBuildId(string buildType, string from, string to)
        {
            return GetChangeDetailsByBuildTypeAndBuildId(buildType, from, to, (build, s) => build.Id.Equals(s, StringComparison.InvariantCultureIgnoreCase));
        }

        public IEnumerable<ChangeDetail> GetReleaseNotesByBuildTypeAndBuildNumber(string buildType, string from, string to)
        {
            return GetChangeDetailsByBuildTypeAndBuildId(buildType, from, to, (build, s) => build.Number.Equals(s, StringComparison.InvariantCultureIgnoreCase));
        }

        public BuildDetails GetBuildDetailsByBuildId(string id)
        {
            var request = GetXmlBuildRequest("app/rest/builds/id:{ID}", new Dictionary<string, string>(){{"ID", id}});
            var response = GetSafeResponse<BuildDetails>(request);
            return response.Data;
        }

        public ChangeList GetChangeListByBuildId(string id)
        {
            var request = GetXmlBuildRequest("app/rest/changes?build=id:{ID}", new Dictionary<string, string>(){{"ID", id}});
            var response = GetSafeResponse<ChangeList>(request);
            return response.Data;
        }

        public ChangeDetail GetChangeDetailsByChangeId(string id)
        {
            var request = GetXmlBuildRequest("app/rest/changes/id:{ID}", new Dictionary<string, string>(){{"ID", id}});
            var response = GetSafeResponse<ChangeDetail>(request);
            return response.Data;
        }

        public IEnumerable<Build> GetBuildsByBuildType(string buildType)
        {
            var request = GetXmlBuildRequest("app/rest/builds/?locator=buildType:{ID}", new Dictionary<string, string>(){{"ID", buildType}});
            var response = GetSafeResponse<List<Build>>(request);
            return response.Data;
        }

        public IEnumerable<Build> GetLatestBuildByBuildTypeAndStatus(string buildType, string status)
        {
            var request = GetXmlBuildRequest("app/rest/builds/?locator=buildType:{ID},status:{STATUS}", new Dictionary<string, string>(){{"ID", buildType},{"STATUS",status}});
            var response = GetSafeResponse<List<Build>>(request);
            return response.Data;
        }

        public Build GetLatestBuildByBuildType(string buildType)
        {
            var request = GetXmlBuildRequest("app/rest/builds/?locator=buildType:{ID},count:1", new Dictionary<string, string>(){{"ID", buildType}});
            var response = GetSafeResponse<List<Build>>(request);
            return response.Data != null ? response.Data.FirstOrDefault() : null;
        }

        private IRestResponse<T> GetSafeResponse<T>(RestRequest request) where T : new() 
        {
            IRestResponse<T> response = null;
            try
            {
                response = _client.Execute<T>(request);
            }
            catch (SocketException e) { }
            
            return response;
        }

        public IEnumerable<Project> GetProjects()
        {
            var request = GetXmlBuildRequest("app/rest/projects");
            var response = GetSafeResponse<List<Project>>(request);
            return response.Data;

        }

        public List<BuildType> GetBuildTypesByProjectName(string specificProject)
        {
            var request = GetXmlBuildRequest("app/rest/projects/{PROJECT}/buildTypes",new Dictionary<string, string>(){{"PROJECT", HttpUtility.UrlEncode(specificProject)}});
            var response = GetSafeResponse<List<BuildType>>(request);
            return response.Data;
        }
    }

    public class BuildType
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Href { get; set; }
        public string ProjectName { get; set; }
        public string ProjectId { get; set; }
        public Uri WebUrl { get; set; }

        public override string ToString()
        {
            return string.Format("Id:{0} Name:{1} HREF:{2} ProjectName:{3} ProjectID:{4} WebUrl:{5}", Id, Name, Href, ProjectName, ProjectId, WebUrl);
        }
    }

    public class BuildTypeDetails : BuildType
    {
        public string Description { get; set; }
        public bool Paused { get; set; }
        public Project Project { get; set; }
        public List<VcsRootEntry> VcsRootEntries { get; set; }
        public Settings Settings { get; set; }
        public List<string> Builds { get; set; }
        public List<Trigger> Triggers { get; set; }
        public List<Step> Steps { get; set; }
        public List<Feature> Features { get; set; }
    }

    public class Feature : GenericTeamCityPropertyGroup { }

    public class Step : GenericTeamCityPropertyGroup
    {
        public string Name { get; set; }
    }

    public class Trigger : GenericTeamCityPropertyGroup { }

    public class GenericTeamCityPropertyGroup
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public List<Property> Properties { get; set; }
    }

    public class Settings
    {
        public List<Property> Properties { get; set; }
    }

    public class Property
    {
        public string Name { get; set; }
        public string value { get; set; }
    }

    public class VcsRootEntry
    {
        public string Id { get; set; }
        public string CheckoutRules { get; set; }
        public List<VcsRoot> VcsRoot { get; set; }
    }

    public class Project : GenericTeamCityStubValue
    {
    }

    public class VcsRoot : GenericTeamCityStubValue
    {
    }

    public class GenericTeamCityStubValue : GenericTeamCityBase
    {
        public string Name { get; set; }
    }

    public class GenericTeamCityBase
    {
        public string Id { get; set; }
        public string Href { get; set; }
    }

    public class IvyModule
    {
        public List<Artifact> Publications { get; set; }
    }

    public class Artifact
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Ext { get; set; }
    }

    public class Build : GenericTeamCityStubValue
    {
        public string Number { get; set; }
        public string Status { get; set; }
        public string BuildTypeId { get; set; }
        public string WebUrl { get; set; }
    }

    public class BuildDetails : Build
    {
        public Boolean Personal { get; set; }
        public Boolean History { get; set; }
        public Boolean Pinned { get; set; }
        public string StatusText { get; set; }
        public BuildType BuildType { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime FinishDate { get; set; }
        public Agent Agent { get; set; }
        public List<Tag> Tags { get; set; }
        public List<Property> Properties { get; set; }
        public List<Revision> Revisions { get; set; }
        public Triggered Triggered { get; set; }
        public ChangeSummary ChangeSummary { get; set; }
    }

    public class ChangeSummary
    {
        public string Href { get; set; }
        public int Count { get; set; }
    }

    public class Triggered
    {
        public string Type { get; set; }
        public string Details { get; set; }
        public DateTime Date { get; set; }
        public User User { get; set; }
    }

    public class User : GenericTeamCityStubValue
    {
        public string UserName { get; set; }
    }

    public class Revision
    {
        public string Version { get; set; }
        public VcsRootInstance VcsRootInstance { get; set; }
    }

    public class VcsRootInstance : GenericTeamCityStubValue
    {
        public string VcsRootId { get; set; }
    }


    public class Tag
    {
        public string Name { get; set; }
    }

    public class Agent : GenericTeamCityStubValue
    {
    }

    public class ChangeList
    {
        public int Count { get; set; }
        public List<Change> Changes { get; set; }
    }

    public class Change : GenericTeamCityBase
    {
        public string Version { get; set; }
        public string Weblink { get; set; }
    }

    public class ChangeDetail : Change
    {
        public string Username { get; set; }
        public string Comment { get; set; }
        public List<FileDetails> Files { get; set; }
    }

    public class FileDetails
    {
        public string beforerevision { get; set; }
        public string afterrevision { get; set; }
        public string File { get; set; }
        public string relativefile { get; set; }
    }
}