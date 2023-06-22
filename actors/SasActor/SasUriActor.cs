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
using Axial;
using Azure;
using Azure.Data.AppConfiguration;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace SasOpeerator
{
    public class SasUriActor
    {
        private readonly ILogger _logger;
        public static String FileName = String.Empty;
        List<String> FilesRaw;
        List<String> FilesReady;
        public static String UploadStorageAcctConn;
        public static String UploadStorageContainer;
        public static String WebViewStorateConnection;
        public static String WebViewUxStaticWebsitePrimaryEndpoint;
        public SasUriActor(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<SasUriActor>();
        }


        [Function("UploadFileSasGet")]
        public async Task<HttpResponseData?> UploadFileSasGet([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData httpRequestData, FunctionContext functionContext)
        {
            _logger.LogInformation("UploadFileSasGet request received.");
            if (false == await ConfigPrep(_logger))
            {
                throw new Exception("UploadFileSasGet configuration failure.");
            }
            HttpResponseData? httpResponseData = null;
            var sasWorkDeliveryId = Guid.NewGuid().ToString();
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
                    var blobServiceClient = new BlobServiceClient(UploadStorageAcctConn);
                    var containerClient = blobServiceClient.GetBlobContainerClient(UploadStorageContainer);
                    var fileNameTemp = $"{FileName}.bin";
                    var blobClient = containerClient.GetBlobClient(fileNameTemp);
                    var blobSasBuilder = new BlobSasBuilder()
                    {
                        BlobContainerName = UploadStorageContainer,
                        BlobName  = fileNameTemp,
                        Resource  = "b", 
                        StartsOn  = DateTime.UtcNow.AddMinutes(-62),  
                        ExpiresOn = DateTime.UtcNow.AddMinutes(122),
                        Protocol  = SasProtocol.Https
                    };
                    blobSasBuilder.SetPermissions(BlobSasPermissions.Write);
                    var sasUri = $"{blobClient.GenerateSasUri(blobSasBuilder).ToString()}&sasWorkDeliveryId={sasWorkDeliveryId}";
                    _logger.LogInformation($"UploadFileSasGet URI prepared for upload {sasUri}");
                    httpResponseData = httpRequestData.CreateResponse(HttpStatusCode.Accepted);
                    httpResponseData.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                    await httpResponseData.WriteStringAsync(sasUri);
                }
            }
            catch (Exception xxx)
            {
                _logger.LogError(xxx.ToString());
            }
            await Scribe.AppInfoWrite(sasWorkDeliveryId, "UploadFileSasGet response generated.");
            return httpResponseData;
        }


        [Function("FilesAvailableEnum")]
        public async Task<HttpResponseData?> FilesAvailableEnum([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData httpRequestData, FunctionContext functionContext)
        {
            _logger.LogInformation("FilesAvailableEnum request received.");
            if (false == await ConfigPrep(_logger))
            {
                throw new Exception("FilesAvailableEnum configuration failure.");
            }
            HttpResponseData? httpResponseData = null;
            FilesReady = new List<String>();
            FilesRaw = new List<String>();
            try
            {
                var blobServiceClient = new BlobServiceClient(WebViewStorateConnection);
                var containerClient = blobServiceClient.GetBlobContainerClient("$web");
                var resultSegment = containerClient.GetBlobsAsync().AsPages(default, 12);
                await foreach (Page<BlobItem> blobPage in resultSegment)
                {
                    FilesRaw.AddRange(blobPage.Values.Select(blobItem => blobItem.Name));
                }
                foreach (var blobOne in FilesRaw)
                {
                    var blobClient = containerClient.GetBlobClient(blobOne);
                    var blobSasBuilder = new BlobSasBuilder()
                    {
                        BlobName = blobOne,
                        Resource = "b",
                        StartsOn = DateTime.UtcNow.AddMinutes(-62),
                        ExpiresOn = DateTime.UtcNow.AddMinutes(122),
                        Protocol = SasProtocol.Https,
                    };
                    blobSasBuilder.SetPermissions(BlobSasPermissions.Read);
                    FilesReady.Add($"{blobClient.GenerateSasUri(blobSasBuilder).ToString()}{Environment.NewLine}");
                    FilesReady.Add($"{WebViewUxStaticWebsitePrimaryEndpoint}{blobOne}");
                }
                
                _logger.LogInformation($"FilesAvailableEnum URIs prepared.");
                httpResponseData = httpRequestData.CreateResponse(HttpStatusCode.Accepted);
                httpResponseData.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                await httpResponseData.WriteStringAsync(string.Join(",", FilesReady));
                _logger.LogInformation($"FilesAvailableEnum Response prepared.");
            }
            catch (Exception xxx)
            {
                _logger.LogError(xxx.ToString());
                httpResponseData = httpRequestData.CreateResponse(HttpStatusCode.ServiceUnavailable);
                httpResponseData.Headers.Add("Content-Type", "text/plain; charset=utf-8");
            }
            return httpResponseData;
        }





        public bool ParamsPrep( FunctionContext functionContext )
        {
            var actionSuccess = false;
            try
            {
                if (functionContext.BindingContext.BindingData.TryGetValue("fileName", out var fileNameInput))
                {
                    FileName = fileNameInput.ToString();
                }
                _logger.LogInformation($"UploadFileSasGet Parameters FileName:\"{FileName}\"");
                actionSuccess = !String.IsNullOrWhiteSpace(FileName);
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
            try
            {
                var appConfigReadClient = new ConfigurationClient(Environment.GetEnvironmentVariable("c12a5appc01ConnectionString"));
                UploadStorageAcctConn = (await appConfigReadClient.GetConfigurationSettingAsync($"Upload:Storage:Connection")).Value.Value.ToString();
                UploadStorageContainer = (await appConfigReadClient.GetConfigurationSettingAsync($"Upload:Storage:ContainerName")).Value.Value.ToString();
                WebViewStorateConnection = (await appConfigReadClient.GetConfigurationSettingAsync($"WebView:Storage:Connection")).Value.Value.ToString();
                WebViewUxStaticWebsitePrimaryEndpoint = (await appConfigReadClient.GetConfigurationSettingAsync($"WebView:Ux:StaticWebsitePrimaryEndpoint")).Value.Value.ToString();
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
