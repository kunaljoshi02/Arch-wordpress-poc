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

using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.Diagnostics;
using System.Text.Json;
using FFMpegCore;
using FFMpegCore.Enums;
using Azure.Messaging.ServiceBus;
using Azure.Storage;


namespace EncodingActor
{
    public class EncodingActor
    {
        public static String TaskPathInputFile;
        public static String TaskPathOutputDirectory;
        public static String WorkflowCorrelationId;
        public static String ServiceBusConnection;
        public static String ServiceBusTopic;
        public static Boolean LoggingFileShouldBeEnabled;
        public static TextWriterTraceListener? TraceListenerText = null;
        public static ConsoleTraceListener? TraceListenerConsole = null;



        static async Task Main(String[] argsStartup)
        {
            if (true == ConfigPrep(argsStartup) && true == LogPrep())
            {
                Environment.Exit(true == await AppPrimary() ? 0 : 1);
            }
        }
        
        

        public static async Task<bool> AppPrimary()
        {
            var workSuccess = false;
            Trace.TraceInformation("Run begin.");
            try
            {


                // Obtain input and output based on launch parameter payload.

                var inputFileName = TaskPathInputFile.Split('?')[0].Split('/').TakeLast(1).ToArray()[0];
                var directoryPath = "/" + String.Join('/', TaskPathInputFile.Split('?')[0].Split('/').Skip(4).Take(10).ToArray()).Replace(inputFileName, String.Empty);
                if (File.Exists(inputFileName))
                {
                    File.Delete(inputFileName);
                }

                // Download file from storage to local filesystem for operation.

                var blobInputClient = new BlobClient(new Uri(TaskPathInputFile));
                await using (var blobStream = await blobInputClient.OpenReadAsync())
                {
                    await using (var fileStream = new FileStream(inputFileName, FileMode.Create, FileAccess.Write))
                    {
                        await blobStream.CopyToAsync(fileStream);
                    }
                }


                // Convert the file locally.

                var outputName = $"{Path.GetFileNameWithoutExtension(inputFileName)}-web265{Path.GetExtension(inputFileName)}";
                var progressHandler = new Action<double>(counterProgress => { ProgressReportAsync(counterProgress,outputName); });
                var mediaInfo = await FFProbe.AnalyseAsync(inputFileName);
                               await FFMpegArguments
                    .FromFileInput(inputFileName)
                    .OutputToFile($"{outputName}", true, options => options
                        .WithVideoCodec(VideoCodec.LibX265)
                        .WithConstantRateFactor(21)
                        .WithAudioCodec(AudioCodec.Aac)
                        .WithVariableBitrate(4)
                        .WithVideoFilters(filterOptions => filterOptions.Scale(VideoSize.FullHd))
                        .WithFastStart()).NotifyOnProgress(progressHandler, mediaInfo.Duration)
                    .ProcessAsynchronously();
              

                // Push local converted content to a web-viewable Storage location.

                var containerOutputClient = new BlobContainerClient(new Uri(TaskPathOutputDirectory));
                var blobUploadOptionsVideo = new BlobUploadOptions()
                {
                    HttpHeaders = new BlobHttpHeaders() { ContentType = "video/mp4" },
                    TransferOptions = new StorageTransferOptions() { MaximumConcurrency = 4 }
                };
                var blobUpVideoClient = containerOutputClient.GetBlobClient($"{directoryPath}{outputName}");
                await blobUpVideoClient.UploadAsync(outputName, blobUploadOptionsVideo);
                await MessageSend(100, 100.0 , outputName);
                workSuccess = true;
            }
            catch (Exception xxx)
            {
                Trace.TraceError(xxx.ToString());
            }
            finally
            {
                Trace.TraceInformation("Run end.");
            }
            return workSuccess;
        }


        private static async Task ProgressReportAsync(double infoValue , String outputName)
        {
            Trace.TraceInformation($"    Conversion progress: {infoValue}");
            await MessageSend(infoValue, 0.0 , outputName );

        }

        private static async Task MessageSend(double statusInfo , double completionCode , String outputName )
        {
            var messageOutgoing = new ServiceBusMessage()
            {
                ApplicationProperties =
                {
                    ["workflowCorrelationId"] = WorkflowCorrelationId ,
                    ["taskCompletionStatus"] = statusInfo,
                    ["consumerReadyState"] = completionCode ,
                    ["outputName"] = outputName
                }
            };
            var sbClient = new ServiceBusClient(ServiceBusConnection);
            var sbSender = sbClient.CreateSender(ServiceBusTopic);
            await sbSender.SendMessageAsync(messageOutgoing);
        }



        #region Plumbing for Config and Logging
        public static bool ConfigPrep(String[] configItems)
        {
            var configPrepSuccess = false;
            try
            {
                var paramRaw = System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(configItems[0]));
                var configTemp = JsonSerializer.Deserialize<Dictionary<String, String>>(paramRaw);
                TaskPathInputFile = configTemp["taskPathInput"];
                TaskPathOutputDirectory = configTemp["taskpathOutput"];
                WorkflowCorrelationId = configTemp["workflowCorrelationId"];
                ServiceBusConnection = configTemp["sbusConnection"];
                ServiceBusTopic = configTemp["sbusTopic"];
                LoggingFileShouldBeEnabled = true;
                configPrepSuccess = true;
            }
            catch (Exception xxx)
            {
                Console.WriteLine(xxx);
                Environment.Exit(1);
            }
            return configPrepSuccess;
        }

        public static bool LogPrep()
        {
            var logPrepSuccess = false;
            var logFileLocalName = $"EncodingActor-{DateTime.UtcNow.ToString("yyyyMMddHHmmss")}.log";
            Trace.Listeners.Clear();
            try
            {
                if (true == LoggingFileShouldBeEnabled)
                {
                    TraceListenerText = new TextWriterTraceListener(logFileLocalName)
                    {
                        Filter = null,
                        IndentLevel = 0,
                        IndentSize = 0,
                        Name = $"Logger-{AppDomain.CurrentDomain.FriendlyName}-file",
                        TraceOutputOptions = TraceOptions.ThreadId | TraceOptions.DateTime,
                        Writer = null
                    };
                    Trace.Listeners.Add(TraceListenerText);
                }

                TraceListenerConsole = new ConsoleTraceListenerStamped()
                {
                    Filter = null,
                    Name = $"Logger-{AppDomain.CurrentDomain.FriendlyName}-console"
                };
                Trace.Listeners.Add(TraceListenerConsole);
                Trace.AutoFlush = true;
                logPrepSuccess = true;
            }
            catch (Exception xxx)
            {
                Console.WriteLine(xxx);
                Environment.Exit(1);
            }
            return logPrepSuccess;
        }

        public class ConsoleTraceListenerStamped : ConsoleTraceListener
        {
            public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message) => Console.WriteLine($"{DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")}: {message}");

            public override void Write(string message) => Console.Write(message);

            public override void WriteLine(string message) => Console.WriteLine(message);
        }

        #endregion  

    }



    
}
