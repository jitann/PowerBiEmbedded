using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.PowerBI.Api;
using Microsoft.PowerBI.Api.Models;
using Microsoft.Rest;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PowerBISample.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UserController : ControllerBase
    {
        /// <summary>
        /// AppSettings
        /// </summary>
        private readonly AppSettings _appSettings;

        private readonly ILogger<UserController> _logger;

        public UserController(ILogger<UserController> logger,
            IOptions<AppSettings> appSettings)
        {
            _logger = logger;
            this._appSettings = appSettings.Value;
        }

        [HttpGet("power-bi-token")]
        public async Task<IActionResult> GetPowerBIToken()
        {
            var embedConfig = new EmbedConfig();
            string apiUrl = this._appSettings.PowerBISetting.PowerBIApi; // "https://api.powerbi.com/";
            Guid workspaceId = new Guid(this._appSettings.PowerBISetting.WorkspaceId); // new Guid("6c82a6ca-7786-43f5-bfa0-106a215eb55b");
            string reportId = this._appSettings.PowerBISetting.ReportId; // "ffe15693-939a-476f-b705-7c93c0033311";
            AuthenticationResult authenticationResult = null;
            Report report = null;

            // For app only authentication, we need the specific tenant id in the authority url
            // var tenantSpecificURL = "https://login.microsoftonline.com/60970fd0-5dce-4e4b-9327-402ea26b7ea2/";
            var tenantSpecificURL = $"https://login.microsoftonline.com/{this._appSettings.PowerBISetting.Tenant}";
            var authenticationContext = new AuthenticationContext(tenantSpecificURL);

            // Authentication using app credentials
            var credential = new ClientCredential(this._appSettings.PowerBISetting.ClientID, this._appSettings.PowerBISetting.ClientSecret);
            authenticationResult = await authenticationContext.AcquireTokenAsync(this._appSettings.PowerBISetting.PowerBITokenApi, credential);

            var m_tokenCredentials = new Microsoft.Rest.TokenCredentials(authenticationResult.AccessToken, "Bearer");
            try
            {
                // Create a Power BI Client object. It will be used to call Power BI APIs.
                using (var client = new PowerBIClient(new Uri(apiUrl), m_tokenCredentials))
                {
                    // Get a list of reports.
                    var reports = await client.Reports.GetReportsInGroupAsync(workspaceId);

                    // No reports retrieved for the given workspace.
                    if (reports.Value.Count() == 0)
                    {
                        embedConfig.ErrorMessage = "No reports were found in the workspace";
                        return this.Ok(embedConfig);
                    }

                    if (string.IsNullOrWhiteSpace(reportId))
                    {
                        // Get the first report in the workspace.
                        report = reports.Value.FirstOrDefault();
                    }
                    else
                    {
                        report = reports.Value.FirstOrDefault(r => r.Id.Equals(new Guid(reportId)));
                    }

                    if (report == null)
                    {
                        embedConfig.ErrorMessage = "No report with the given ID was found in the workspace. Make sure ReportId is valid.";
                        return this.Ok(embedConfig);
                    }

                    var datasets = await client.Datasets.GetDatasetInGroupAsync(workspaceId, report.DatasetId);
                    GenerateTokenRequest generateTokenRequestParameters;

                    generateTokenRequestParameters = new GenerateTokenRequest(accessLevel: "view");

                    EmbedToken tokenResponse = await client.Reports.GenerateTokenInGroupAsync(workspaceId, report.Id, generateTokenRequestParameters);
                    if (tokenResponse == null)
                    {
                        embedConfig.ErrorMessage = "Failed to generate embed token.";
                        return this.Ok(embedConfig);
                    }

                    // Generate Embed Configuration.
                    embedConfig.EmbedToken = tokenResponse;
                    embedConfig.EmbedUrl = report.EmbedUrl;
                    embedConfig.Id = report.Id.ToString();
                }
            }
            catch (HttpOperationException exc)
            {
                embedConfig.ErrorMessage = string.Format("Status: {0} ({1})\r\nResponse: {2}\r\nRequestId: {3}", exc.Response.StatusCode, (int)exc.Response.StatusCode, exc.Response.Content, exc.Response.Headers["RequestId"].FirstOrDefault());
                return this.Ok(embedConfig);
            }
            return this.Ok(new { EmbedConfig = embedConfig });
        }
    }
	
}