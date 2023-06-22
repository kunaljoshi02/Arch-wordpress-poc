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
using String = System.String;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Azure.Data.AppConfiguration;
using Azure.Messaging.ServiceBus;
using System.Dynamic;
using Axial;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.Azure.Batch;
using Microsoft.Azure.Batch.Auth;
using Microsoft.Azure.Batch.Common;
using Model;



namespace TaskActor
{
    public class TaskActor
    {
        private readonly ILogger _logger;
        private static ServiceBusReceivedMessage _messageIncoming;
        private static JsonNode? _messageIncomingUserProperties;
        private static String _workflowCorrelationId;

        public static String StorageActivityLoc;
        public static String StorageActivityContainer;
        public static String StorageWebViewLoc;
        public static String BatchAccountUri;
        public static String BatchAccountName;
        public static String BatchAccountKey;
        public static String BatchPoolId;
        public static String TaskPathInputFile;
        public static String TaskPathOutputDirectory;
        public static String ManifestParameter;

        public TaskActor(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<TaskActor>();
        }

        [Function("TaskActor")]
        public async Task ProcessServiceBusMessageTask([ServiceBusTrigger("c12a5sbus01top01", "c12a5sbus01top01Activity", Connection = "c12a5sbusConnection")] String messageRaw , FunctionContext functionContext)
        {
            _logger.LogInformation(($"TaskActor operation begin. "));
            _messageIncoming = ServiceBusModelFactory.ServiceBusReceivedMessage(BinaryData.FromString(messageRaw), correlationId: Guid.NewGuid().ToString());
            _messageIncomingUserProperties = JsonSerializer.Deserialize<JsonNode>(functionContext.BindingContext.BindingData["UserProperties"].ToString());
            _workflowCorrelationId = _messageIncomingUserProperties["workflowCorrelationId"].GetValue<string>();
            if (false == await ConfigPrep(_logger))
            {
                throw new Exception("TaskActor configuration failure.");
            }

            try
            {
                var blobActivityServiceClient = new BlobServiceClient(StorageActivityLoc);
                var containerActivityClient = blobActivityServiceClient.GetBlobContainerClient(StorageActivityContainer);
                var inputUri = containerActivityClient.GenerateSasUri(BlobContainerSasPermissions.All, DateTimeOffset.UtcNow.AddHours(4));

                var eventGridItem = System.Text.Json.JsonSerializer.Deserialize<EventGridItem>(_messageIncoming.Body);
                var fileName = eventGridItem.data.url.SubstringSafe(eventGridItem.subject.IndexOf("blobs/") + 8, eventGridItem.subject.Length);
                TaskPathInputFile = inputUri.ToString().Replace("?", $"{fileName}?");
                
                var webViewServiceClient = new BlobServiceClient(StorageWebViewLoc);
                var containerWebViewClient = webViewServiceClient.GetBlobContainerClient("$web");
                TaskPathOutputDirectory = containerWebViewClient.GenerateSasUri(BlobContainerSasPermissions.All, DateTimeOffset.UtcNow.AddHours(6)).ToString();

                dynamic manifestTemp = new ExpandoObject();
                manifestTemp.taskPathInput = TaskPathInputFile;
                manifestTemp.taskpathOutput = TaskPathOutputDirectory;
                manifestTemp.workflowCorrelationId = _workflowCorrelationId;
                manifestTemp.sbusConnection = Environment.GetEnvironmentVariable("c12a5sbusConnection") ?? "MISSING SERVICE BUS CONNECTION STRING";
                manifestTemp.sbusTopic = "c12a5sbus01top01";
                var taskManifest = JsonSerializer.Serialize(manifestTemp);
                ManifestParameter = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(taskManifest));
            }
            catch (Exception xxx)
            {
                _logger.LogError(xxx.ToString());
                throw;
            }
            
            // Removed Azure Batch launching until quota issues are fixed.

        }

        [Function("TaskStatusActor")]
        public async Task ProcessServiceBusMessageTaskStatus([ServiceBusTrigger("c12a5sbus01top01", "c12a5sbus01top01TaskStatus", Connection = "c12a5sbusConnection")] String messageRaw, FunctionContext functionContext)
        {
            _logger.LogInformation(($" {DateTime.Now.ToString("yyyyMMddHHmmss")}  TaskStatusActor operation begin. "));
            _messageIncoming = ServiceBusModelFactory.ServiceBusReceivedMessage(BinaryData.FromString(messageRaw), correlationId: Guid.NewGuid().ToString());
            _messageIncomingUserProperties = JsonSerializer.Deserialize<JsonNode>(functionContext.BindingContext.BindingData["UserProperties"].ToString());
            _workflowCorrelationId = _messageIncomingUserProperties["workflowCorrelationId"].GetValue<string>();

            try
            {
                var taskCompletionStatus = _messageIncomingUserProperties["taskCompletionStatus"].GetValue<double>();
                var reportMessage = 100 == taskCompletionStatus ? "Task complete." : "Task underway.";
                await Scribe.AppInfoWrite(_workflowCorrelationId, $"{reportMessage} Percentage complete: {taskCompletionStatus}");
            }
            catch { }

        }

        public static async Task<bool> ConfigPrep(ILogger loggerLocal)
        {
            var configsuccess = false;
            try
            {
                var appConfigReadClient = new ConfigurationClient(Environment.GetEnvironmentVariable("c12a5appc01ConnectionString"));
                StorageActivityLoc = ( await appConfigReadClient.GetConfigurationSettingAsync($"Activity:Storage:Connection") ).Value.Value.ToString();
                StorageActivityContainer = (await appConfigReadClient.GetConfigurationSettingAsync($"Activity:Storage:ContainerName") ).Value.Value.ToString();
                StorageWebViewLoc = (await appConfigReadClient.GetConfigurationSettingAsync($"WebView:Storage:Connection")).Value.Value.ToString();
                BatchAccountUri = (await appConfigReadClient.GetConfigurationSettingAsync($"Activity:Batch:AccountUri")).Value.Value.ToString();
                BatchAccountName = (await appConfigReadClient.GetConfigurationSettingAsync($"Activity:Batch:AccountName")).Value.Value.ToString();
                BatchAccountKey = (await appConfigReadClient.GetConfigurationSettingAsync($"Activity:Batch:AccountKey")).Value.Value.ToString();
                BatchPoolId = (await appConfigReadClient.GetConfigurationSettingAsync($"Activity:Batch:Pool:Standard:Id")).Value.Value.ToString();
                TaskPathInputFile = String.Empty;
                TaskPathOutputDirectory = String.Empty;
                ManifestParameter = String.Empty;
                configsuccess = true;
            }
            catch (Exception xxx)
            {
                loggerLocal.LogCritical( xxx.ToString() );
                throw;
            }
            return configsuccess;
        }

    }
}
