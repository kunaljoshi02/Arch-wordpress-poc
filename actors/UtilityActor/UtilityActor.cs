/*

    MIT License
    Copyright (c) Microsoft Corporation. All rights reserved.
    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:
    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE,

    This is an example of working code, not how to code.

    Examples are for illustration only and are fictitious.  No real association is intended or inferred.

 */


using System.Net;
using Azure.Core;
using Azure.Data.AppConfiguration;
using Azure.Identity;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Azure.ResourceManager;
using Azure.ResourceManager.AppService;
using Azure.ResourceManager.AppService.Models;
using Azure.ResourceManager.MySql.FlexibleServers;
using Azure.ResourceManager.MySql.FlexibleServers.Models;
using Azure.ResourceManager.Resources;



namespace UtilityActor
{
    public class UtilityActor
    {
        public static String? DatabaseTarget;
        public static String? DatabaseLevel;
        public static String? AppServicePlanTarget;
        public static String? AppServicePlanLevel;
        public static Dictionary<String, String>? SettingsScaling;
        public static ArmClient ArmClientCurrent;
        public static SubscriptionCollection SubscriptionCollectionCurrent;

        private readonly ILogger _logger;

        public UtilityActor(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<UtilityActor>();
        }

        [Function("ScaleChange")]
        public async Task<HttpResponseData?> ScaleChange([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData httpRequestData, FunctionContext functionContext)
        {
            _logger.LogInformation("ScaleChange request received.");
            if (false == await ConfigPrep(_logger))
            {
                throw new Exception("ScaleChange configuration failure.");
            }
            HttpResponseData? httpResponseData = null;
            var actionMessage = String.Empty;
            try
            {
                if (false == ParamsPrep(functionContext))
                {
                    httpResponseData = httpRequestData.CreateResponse(HttpStatusCode.BadRequest);
                    httpResponseData.Headers.Add("Content-Type", "text/html; charset=utf-8");
                    await httpResponseData.WriteStringAsync("Required parameters are missing. See documentation.");
                }
                else
                {
                    var credentialCurrent = CredentialsGet();
                    ArmClientCurrent = new ArmClient(credentialCurrent);
                    SubscriptionCollectionCurrent = ArmClientCurrent.GetSubscriptions();
                    actionMessage += await WebTierScaleAction(_logger);
                    actionMessage += await DatabaseTierScaleAction(_logger);
                    httpResponseData = httpRequestData.CreateResponse(HttpStatusCode.Accepted);
                    httpResponseData.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                    await httpResponseData.WriteStringAsync(actionMessage);
                }
            }
            catch (Exception xxx)
            {
                _logger.LogError(xxx.ToString());
                httpResponseData = httpRequestData.CreateResponse(HttpStatusCode.ServiceUnavailable);
                httpResponseData.Headers.Add("Content-Type", "text/plain; charset=utf-8");
            }
            _logger.LogInformation("ScaleChange response generated.");
            return httpResponseData;
        }

        public static async Task<String> DatabaseTierScaleAction(ILogger _logger)
        {
            var returnMessage = String.Empty;
            if (String.IsNullOrWhiteSpace(DatabaseTarget))
            {
                returnMessage = "Database scale commands not sent to Azure fabric.";
            }
            else
            { 
                var subscriptionIdAppServicePlan = SettingsScaling!["Util:Scaling:Primary:Database:ResourcePath"].Split(new[] { "subscriptions/" }, StringSplitOptions.None)[1].Split('/').FirstOrDefault();
                var resourceGroupAppServicePlan = SettingsScaling["Util:Scaling:Primary:Database:ResourcePath"].Split(new[] { "resourceGroups/" }, StringSplitOptions.None)[1].Split('/').FirstOrDefault();
                var serverId = SettingsScaling["Util:Scaling:Primary:Database:ResourcePath"].Split(new[] { "flexibleServers/" }, StringSplitOptions.None)[1].Split('/').FirstOrDefault();
                _logger.LogInformation("ScaleChange operating on MySQL Flexible Server, begin.");
                SubscriptionResource subscriptionResource = SubscriptionCollectionCurrent.Get(subscriptionIdAppServicePlan);
                ResourceGroupResource resourceGroup = subscriptionResource.GetResourceGroup(resourceGroupAppServicePlan);
                var skuAttribs = SettingsScaling[$"Util:Scaling:Database:Level:{DatabaseLevel}"].Split('/');
                var newServerData = new MySqlFlexibleServerData(skuAttribs[0])
                {
                    Sku = new MySqlFlexibleServerSku(skuAttribs[1], new MySqlFlexibleServerSkuTier(skuAttribs[2]))
                };
                var mySqlDbCollection = resourceGroup.GetMySqlFlexibleServers();
                _logger.LogInformation("ScaleChange submitted change to Azure fabric for DB tier. This may take a long time, not waiting for completion.");
                var execResult = await mySqlDbCollection.CreateOrUpdateAsync(waitUntil: Azure.WaitUntil.Started, DatabaseTarget, newServerData);
                returnMessage = "Database tier scale commands sent and accepted by Azure fabric. ";
            }
            return returnMessage;
        }

        public static async Task<String> WebTierScaleAction(ILogger _logger)
        {
            var returnMessage = String.Empty;
            if (String.IsNullOrWhiteSpace(AppServicePlanTarget))
            {
                returnMessage = "Web tier scale commands not sent to Azure fabric.";
            }
            else
            {
                var subscriptionIdAppServicePlan = SettingsScaling!["Util:Scaling:Primary:Web:ResourcePath"].Split(new[] { "subscriptions/" }, StringSplitOptions.None)[1].Split('/').FirstOrDefault();
                var resourceGroupAppServicePlan = SettingsScaling["Util:Scaling:Primary:Web:ResourcePath"].Split(new[] { "resourceGroups/" }, StringSplitOptions.None)[1].Split('/').FirstOrDefault();
                _logger.LogInformation("ScaleChange operating on App Service Plan, begin.");
                SubscriptionResource subscriptionResource = SubscriptionCollectionCurrent.Get(subscriptionIdAppServicePlan);
                ResourceGroupResource resourceGroup = subscriptionResource.GetResourceGroup(resourceGroupAppServicePlan);
                var skuAttribs = SettingsScaling[$"Util:Scaling:Web:Level:{AppServicePlanLevel}"].Split('/');
                var newAppServicePlanData = new AppServicePlanData(skuAttribs[0])
                {
                    Kind = "app",
                    Sku = new AppServiceSkuDescription
                    {
                        Name = skuAttribs[1],
                        Tier = skuAttribs[2],
                        Size = skuAttribs[3],
                        Family = skuAttribs[4],
                        Capacity = Convert.ToInt32(skuAttribs[5])
                    }
                };
                var appServicePlanCollection = resourceGroup.GetAppServicePlans();
                _logger.LogInformation("ScaleChange submitted change to Azure fabric for web tier. This may take minutes, waiting for completion.");
                var execResult = await appServicePlanCollection.CreateOrUpdateAsync(waitUntil: Azure.WaitUntil.Completed, name: AppServicePlanTarget, data: newAppServicePlanData);
                _logger.LogInformation("ScaleChange change committed to Azure.");
                returnMessage = "Web tier scale commands sent and accepted by Azure fabric.";

            }
            return returnMessage;
        }
    

        public static TokenCredential CredentialsGet()
        {
            var tenantId = SettingsScaling["Util:Scaling:Auth:TenantId"];
            var clientId = SettingsScaling["Util:Scaling:Auth:AppId"];
            var clientSecret = SettingsScaling["Util:Scaling:Auth:Key"];
            var options = new TokenCredentialOptions
            {
                AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
            };
            // https://docs.microsoft.com/dotnet/api/azure.identity.clientsecretcredential
            var devClientSecretCredential = new ClientSecretCredential( tenantId, clientId, clientSecret, options);
            var chainedTokenCredential = new ChainedTokenCredential(devClientSecretCredential);
            return chainedTokenCredential;
        }

        public bool ParamsPrep(FunctionContext functionContext)
        {
            var actionSuccess = false;
            try
            {
                if (functionContext.BindingContext.BindingData.TryGetValue("appServicePlanTarget", out var appServicePlanTarget))
                {
                    AppServicePlanTarget = appServicePlanTarget.ToString();
                }
                _logger.LogInformation($"ScaleChange Parameters AppServicePlanTarget:\"{appServicePlanTarget}\"");
                if (functionContext.BindingContext.BindingData.TryGetValue("appServicePlanLevel", out var appServicePlanLevel))
                {
                    AppServicePlanLevel = appServicePlanLevel.ToString();
                }
                _logger.LogInformation($"ScaleChange Parameters AppServiceLevel:\"{appServicePlanLevel}\"");
                if (functionContext.BindingContext.BindingData.TryGetValue("databaseTarget", out var databaseTarget))
                {
                    DatabaseTarget = databaseTarget.ToString();
                }
                _logger.LogInformation($"ScaleChange Parameters DatabaseTarget:\"{DatabaseTarget}\"");
                if (functionContext.BindingContext.BindingData.TryGetValue("databaseLevel", out var databaseLevel))
                {
                    DatabaseLevel = databaseLevel.ToString();
                }
                _logger.LogInformation($"ScaleChange Parameters DatabaseLevel:\"{DatabaseLevel}\"");
                actionSuccess = ( !String.IsNullOrWhiteSpace(AppServicePlanTarget) && !String.IsNullOrWhiteSpace(AppServicePlanLevel) ) || (!String.IsNullOrWhiteSpace(DatabaseTarget) && !String.IsNullOrWhiteSpace(DatabaseLevel) ) ;
            }
            catch (Exception xxx)
            {
                _logger.LogError(xxx.ToString());
            }
            return actionSuccess;
        }

        public static async Task<bool> ConfigPrep(ILogger loggerLocal)
        {
            var configSuccess = false;
            var settingsSelector = new SettingSelector { KeyFilter = "Util:Scaling*" };
            try
            {
                var appConfigReadClient = new ConfigurationClient(Environment.GetEnvironmentVariable("c12a5appc01ConnectionString"));
                SettingsScaling = new Dictionary<String, String>();
                await foreach( var configItem in appConfigReadClient.GetConfigurationSettingsAsync( settingsSelector ) )
                {
                    SettingsScaling.Add( configItem.Key , configItem.Value);
                }
                configSuccess = true;
            }
            catch (Exception xxx)
            {
                loggerLocal.LogCritical(xxx.ToString());
                throw;
            }
            return configSuccess;
        }


    }



}
