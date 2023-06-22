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

using System.Diagnostics;
using System.Dynamic;
using System.Text;
using System.Text.Json;
using Microsoft.Azure.Cosmos;


namespace Axial
{
    public static class Scribe
    {
        private static CosmosClient _cosmosClient = new CosmosClient(Environment.GetEnvironmentVariable("c12a5cosm01Connection"));
        public static async Task<bool> AppInfoWrite(String workflowCorrelationId, String infoMessage)
        {
            var operationSuccess = false;
            try
            {
                var idNew = Guid.NewGuid().ToString();
                dynamic messageTemp = new ExpandoObject();
                messageTemp.id = idNew;
                messageTemp.messageStampUtc = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss");
                messageTemp.workflowCorrelationId = workflowCorrelationId;
                messageTemp.infoMessage = infoMessage;
                var messageOut = JsonSerializer.Serialize(messageTemp);
                var cosmosDatabase = _cosmosClient.GetDatabase("c12a5cosm01db01");
                var cosmosContainer = cosmosDatabase.GetContainer("items");
                using (var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(messageOut)))
                {
                    var responseMessage = await cosmosContainer.CreateItemStreamAsync(memoryStream, new PartitionKey(idNew));
                }
                operationSuccess = true;
            }
            catch (Exception xxx )
            {
                Debug.WriteLine(xxx);
                throw;
            }
            return operationSuccess;
        }
    }
}
