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


namespace Model
{

    public class EventGridItem
    {
        public string topic { get; set; }
        public string subject { get; set; }
        public string eventType { get; set; }
        public string id { get; set; }
        public EventGridMetaData data { get; set; }
        public string dataVersion { get; set; }
        public string metadataVersion { get; set; }
        public DateTime eventTime { get; set; }
    }
    public class EventGridMetaData
    {
        public string api { get; set; }
        public string clientRequestId { get; set; }
        public string requestId { get; set; }
        public string eTag { get; set; }
        public string contentType { get; set; }
        public int contentLength { get; set; }
        public string blobType { get; set; }
        public string url { get; set; }
        public string sequencer { get; set; }
        public EventGridBlobPayload storageDiagnostics { get; set; }
    }
    public class EventGridBlobPayload
    {
        public string batchId { get; set; }
    }


    public static class WorkExtensions
    {
        public static String SubstringSafe(this String inputContent, int indexStart, int inputLength)
        {
            return new String((inputContent ?? String.Empty).Skip(indexStart).Take(inputLength).ToArray());
        }
    }
}