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


using System.Text.Json;
using System.Text.Json.Nodes;
using Axial;
using String = System.String;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Azure.Data.AppConfiguration;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Storage.DataMovement;
using Model;

namespace SecurityAnalysisActor
{
    public class SecurityAnalysisActor
    {
        private readonly ILogger _logger;
        private static ServiceBusReceivedMessage _messageIncoming;
        private static JsonNode? _messageIncomingUserProperties;
        private static String _workflowCorrelationId;

        public static String StorageUploadLoc;
        public static String StorageUploadContainer;
        public static String StorageAnalysisLoc;
        public static String StorageAnalysisContainer;
        public static String StorageActivityLoc;
        public static String StorageActivityContainer;

        public SecurityAnalysisActor(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<SecurityAnalysisActor>();
        }

        [Function("SecurityAnalysisActor")]
        public async Task ProcessServiceBusMessage([ServiceBusTrigger("c12a5sbus01top01", "c12a5sbus01top01SecurityAnalysis", Connection = "c12a5sbusConnection")]  String messageRaw, string runId , ILogger log , FunctionContext functionContext)
        {
            _logger.LogInformation(($"  SecurityAnalysisActor operation begin. "));
            _messageIncoming = ServiceBusModelFactory.ServiceBusReceivedMessage(BinaryData.FromString(messageRaw), correlationId: Guid.NewGuid().ToString());
            _messageIncomingUserProperties = JsonSerializer.Deserialize<JsonNode>(functionContext.BindingContext.BindingData["UserProperties"].ToString());
            _workflowCorrelationId = _messageIncomingUserProperties["workflowCorrelationId"].GetValue<string>();
            if (false == await ConfigPrep(_logger))
            {
                throw new Exception("TaskActor configuration failure.");
            }
            try
            {
                
                var eventGridItem = JsonSerializer.Deserialize<EventGridItem>(_messageIncoming.Body);
                var fileName = eventGridItem.data.url.SubstringSafe(eventGridItem.data.url.IndexOf(StorageUploadContainer) + StorageUploadContainer.Length+1, eventGridItem.data.url.Length);
               

                // Copy item from upload to security analysis space.

                var cloudStorageAccountUpload = CloudStorageAccount.Parse(StorageUploadLoc);
                var cloudClientUpload = cloudStorageAccountUpload.CreateCloudBlobClient();
                var containerUploadSource = cloudClientUpload.GetContainerReference(StorageUploadContainer);
                var blobUpload = containerUploadSource.GetBlockBlobReference(fileName);

                var cloudStorageAccountSecurity = CloudStorageAccount.Parse(StorageAnalysisLoc);
                var cloudClientSecurity = cloudStorageAccountSecurity.CreateCloudBlobClient();
                var containerSecurity = cloudClientSecurity.GetContainerReference(StorageAnalysisContainer);
                var blobSecurity = containerSecurity.GetBlockBlobReference(fileName);
                await blobSecurity.DeleteIfExistsAsync();
                var taskAnalysisTransfer = TransferManager.CopyAsync(blobUpload, blobSecurity, CopyMethod.ServiceSideAsyncCopy);
                while (!taskAnalysisTransfer.IsCompleted)
                {
                    Thread.Sleep(2000);
                    _logger.LogInformation(($"     Operation copy underway {blobSecurity.Name} waiting 2sec for completion. ") );
                }
                await taskAnalysisTransfer;
                await Scribe.AppInfoWrite(_workflowCorrelationId, $"Operation copy complete {blobSecurity.Name} ");


                // Feign successful call to security analysis API on blob in security analysis space.

                Thread.Sleep(2500);
                await Scribe.AppInfoWrite(_workflowCorrelationId, $"Operation scan complete {blobSecurity.Name} ");


                // Copy item from security analysis to activity space.

                var cloudStorageActivity = CloudStorageAccount.Parse(StorageActivityLoc);
                var cloudClientActivity = cloudStorageActivity.CreateCloudBlobClient();
                var containerActivity = cloudClientActivity.GetContainerReference(StorageActivityContainer);
                var blobActivity = containerActivity.GetBlockBlobReference(fileName);
                await blobActivity.DeleteIfExistsAsync();
                var taskActivityTransfer = TransferManager.CopyAsync(blobSecurity, blobActivity, CopyMethod.ServiceSideAsyncCopy);
                while (!taskActivityTransfer.IsCompleted)
                {
                    Thread.Sleep(1000);
                    _logger.LogInformation(($"  Operation Copy {blobActivity.Name}  ") );
                }
                await taskActivityTransfer;
                await Scribe.AppInfoWrite(_workflowCorrelationId,"File copied from security analysis space to task activity space.");
            }
            catch (Exception xxx)
            {
                // Wire content to Application Insights instance with xxx troubleshooting info, out of scope for this demonstration.
                // Throw to have the message unlocked in Service Bus for processing by another Function instance
                // Log Exception only to have message marked Complete by Service Bus so it will not be processed again.
                _logger.LogError( xxx.ToString() );
                throw;
            }


            var messageOutgoing = new ServiceBusMessage(_messageIncoming)
            {
                ApplicationProperties =
                {
                    ["workflowCorrelationId"] = _workflowCorrelationId , 
                    ["securityAnalysisState"] = "100",
                    ["activityOperationState"] = "0",
                    ["consumerReadyState"] = "0"
                }
            };
            var sbClient = new ServiceBusClient(Environment.GetEnvironmentVariable("c12a5sbusConnection"));
            var sbSender = sbClient.CreateSender("c12a5sbus01top01");
            await sbSender.SendMessageAsync(messageOutgoing);
            _logger.LogInformation(($" {DateTime.Now.ToString("yyyyMMddHHmmss")}  Operation end. ") );
        }




        public static async Task<bool> ConfigPrep(ILogger loggerLocal)
        {
            var configSuccess = false;
            try
            {
                var appConfigReadClient = new ConfigurationClient(Environment.GetEnvironmentVariable("c12a5appc01ConnectionString"));
                StorageUploadLoc = (await appConfigReadClient.GetConfigurationSettingAsync($"Upload:Storage:Connection")).Value.Value;
                StorageUploadContainer = (await appConfigReadClient.GetConfigurationSettingAsync($"Upload:Storage:ContainerName")).Value.Value;
                StorageAnalysisLoc = (await appConfigReadClient.GetConfigurationSettingAsync($"SecurityAnalysis:Storage:Connection")).Value.Value;
                StorageAnalysisContainer = (await appConfigReadClient.GetConfigurationSettingAsync($"SecurityAnalysis:Storage:ContainerName")).Value.Value;
                StorageActivityLoc = (await appConfigReadClient.GetConfigurationSettingAsync($"Activity:Storage:Connection")).Value.Value;
                StorageActivityContainer = (await appConfigReadClient.GetConfigurationSettingAsync($"Activity:Storage:ContainerName")).Value.Value;
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
