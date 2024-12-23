

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using CallWebAPI;
using Newtonsoft.Json;

public class Program
{
    static async Task Main(string[] args)
    {
        try
        {
            IList<Configuration> DBlistFiles = GetACOList.GetDBConfigurations();
            
            // Set the Azure DevOps organization URL
            string organizationUrl = "https://PasteyourURL.com/";

            // Set the project name
            string projectName = "Your ProjectPlaced on Azure";

            // Set the personal access token
            string personalAccessToken = "dntfijdouxllqdnjfpmfvha6vzmziq4thzc3wa";

            foreach (var File in DBlistFiles)
            {
                GetACOList.AddLog("Count Files Verfication started for the File on Azure Devops " + File.Name);
                // Set the path of the release pipeline
                string pipelinePath = @"\Azure DevOps Feature File Scrips\" + File.Environment;                

                // Set the target stage name
                string targetStageName =File.Name;
                if (targetStageName.ToUpper().EndsWith("CEG"))
                {
                    pipelinePath = pipelinePath + "\\CGE";
                    
                }
                else if (targetStageName.ToUpper().EndsWith("DCCEG"))
                {
                    pipelinePath = pipelinePath + "\\DCCEG";
                }
                else if (targetStageName.ToUpper().EndsWith("EA"))
                {
                    pipelinePath = pipelinePath + "\\efg\\EA";
                }

                else
                {
                    pipelinePath = pipelinePath + "\\COTT";
                }
                GetACOList.AddLog("Selected Pipeline path " + pipelinePath);
                // Get the latest release ID for the specific stage
                string releaseId = await GetLatestReleaseId(organizationUrl, projectName, pipelinePath, targetStageName, personalAccessToken);

                if (!string.IsNullOrEmpty(releaseId))
                {
                    // Perform the deployment using the obtained release ID
                    bool deploymentResult = await PerformDeployment(organizationUrl, projectName, releaseId, personalAccessToken);

                    if (deploymentResult)
                    {
                        Console.WriteLine("Deployment of " + File.Name +" is successful");
                    }
                    else
                    {
                        Console.WriteLine("Deployment failed");
                    }
                }
                else
                {
                    Console.WriteLine("Release not found.");
                }
            }
        }
        catch (Exception ex)
        {

        }
    }
    static string pipelineId;
    static async Task<string> GetLatestReleaseId(string organizationUrl, string projectName, string pipelinePath, string targetStageName, string personalAccessToken)
    {
        string releaseId = string.Empty;

        using (HttpClient client = new HttpClient())
        {
            // Set the base address and default request headers
            client.BaseAddress = new Uri(organizationUrl);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            //client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", personalAccessToken);
            string authHeaderValue = Convert.ToBase64String(Encoding.ASCII.GetBytes($":{personalAccessToken}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authHeaderValue);

            // Get the pipeline ID using the pipeline path
            pipelineId = await GetPipelineId(client, projectName, pipelinePath);

            if (!string.IsNullOrEmpty(pipelineId))
            {
                // Get the latest release for the specific stage
                dynamic releases = await GetReleases(client, projectName, pipelineId);
                //dynamic releases = await GetReleases(client, projectName, pipelinePath);                

                //if (releases != null && releases.value.Count > 0)
                if (releases != null)
                {
                    //dynamic targetRelease = releases.value[0];
                    //dynamic environments = targetRelease.environments;
                    dynamic environments = releases.environments;
                    //releaseId = environments.id;
                    foreach (dynamic environment in environments)
                    {
                        if (environment.name.ToString().ToUpper() == targetStageName.ToUpper())
                        {
                            //releaseId = targetRelease.id;
                            releaseId = environment.id;
                            GetACOList.AddLog("Selected Release ID " + releaseId);
                            break;
                        }
                    }
                }
            }
        }

        return releaseId;
    }

    static async Task<string> GetPipelineId(HttpClient client, string projectName, string pipelinePath)
    {
        string pipelineId = string.Empty;

        // Get the pipeline definition using the pipeline path
        //HttpResponseMessage response = await client.GetAsync($"{projectName}/_apis/release/releases?api-version=6.0");
        HttpResponseMessage response = await client.GetAsync($"{projectName}/_apis/release/releases?path={pipelinePath}&$top=1&api-version=6.0");

        if (response.IsSuccessStatusCode)
        {
            string jsonContent = await response.Content.ReadAsStringAsync();
            dynamic result = JsonConvert.DeserializeObject(jsonContent);

            if (result != null && result.value.Count > 0)
            {
                pipelineId = result.value[0].id;
                GetACOList.AddLog("Selected Pipeline ID " + pipelineId);
                //foreach (dynamic pipeline in result.value)
                //{
                //    var a = pipeline.releaseDefinition;
                //    if (pipeline != null)
                //    { 
                    
                //    }
                //    //if (pipeline.path == pipelinePath)
                //    if (pipeline.releaseDefinition.path == pipelinePath.ToUpper())
                //    {
                //        pipelineId = pipeline.id;
                //        break;
                //    }
                //}
            }
        }

        return pipelineId;
    }

    static async Task<dynamic> GetReleases(HttpClient client, string projectName, string pipelineId)
    {
        dynamic releases = null;

        //HttpResponseMessage response = await client.GetAsync($"{projectName}/_apis/release/releases?releasedefinitionId={pipelineId}&$top=1&$expand=environments&api-version=6.0-preview.6");
        HttpResponseMessage response = await client.GetAsync($"{projectName}/_apis/release/releases/{pipelineId}?api-version=6.0-preview.6");        

        if (response.IsSuccessStatusCode)
        {
            string jsonContent = await response.Content.ReadAsStringAsync();
            releases = JsonConvert.DeserializeObject(jsonContent);
        }

        return releases;
    }

    static async Task<bool> PerformDeployment(string organizationUrl, string projectName, string releaseId, string personalAccessToken)
    {
        bool deploymentResult = false;

        using (HttpClient client = new HttpClient())
        {
            // Set the base address and default request headers
            client.BaseAddress = new Uri(organizationUrl);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            //client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", personalAccessToken);
            string authHeaderValue = Convert.ToBase64String(Encoding.ASCII.GetBytes($":{personalAccessToken}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authHeaderValue);

            // Set the deployment status to 'inProgress'
            var body = new { status = "inProgress" };
            var jsonBody = JsonConvert.SerializeObject(body);
            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            // Perform the deployment by sending a PATCH request
            HttpResponseMessage response = await client.PatchAsync($"{projectName}/_apis/release/releases/{pipelineId}/environments/{releaseId}?api-version=6.1-preview.1", content);

            if (response.IsSuccessStatusCode)
            {
                deploymentResult = true;
            }
        }

        return deploymentResult;
    }
}
