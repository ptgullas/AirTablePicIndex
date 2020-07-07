using AirtableApiClient;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace AirTablePicIndex {
    public class Retriever {
        public string BaseId { get; set; }
        public string ApiKey { get; set; }

        public Retriever(string baseId, string apiKey) {
            BaseId = baseId;
            ApiKey = apiKey;
        }

        public async Task<(bool, string)> PostAttachmentToRecord(string magazineCoverUrl, string table, string recordId) {
            AirtableAttachment attachment = new AirtableAttachment() {
                Url = magazineCoverUrl
            };
            List<AirtableAttachment> attachmentList = new List<AirtableAttachment> {
                attachment
            };

            using (AirtableBase airtableBase = new AirtableBase(ApiKey, BaseId)) {
                var fields = new Fields();
                fields.AddField("Cover", attachmentList);

                var task = airtableBase.UpdateRecord(table, fields, recordId);

                var response = await task;

                if (response.Success) {
                    return (true, null);
                }
                else if (response.AirtableApiError is AirtableApiException) {
                    return (false, response.AirtableApiError.GetBaseException().Message);
                }
                else {
                    return (false, "Unknown error");
                }
            }

        }

        public async Task<List<string>> GetRecordsFromFormula(string table, string formula) {
            string offset = null;
            string errorMessage = null;
            var records = new List<AirtableRecord>();
            AirtableRecord myRecord = new AirtableRecord();

            using (AirtableBase airtableBase = new AirtableBase(ApiKey, BaseId)) {
                Task<AirtableListRecordsResponse> task = airtableBase.ListRecords(
                    table, offset, null, formula);

                AirtableListRecordsResponse response = await task;

                if (response.Success) {
                    records.AddRange(response.Records.ToList());
                    offset = response.Offset;
                }
                else if (response.AirtableApiError is AirtableApiException) {
                    errorMessage = response.AirtableApiError.GetBaseException().Message;
                }
                else {
                    errorMessage = "Unknown error";
                }
            }
            List<string> jsonRecords = new List<string>();

            foreach (AirtableRecord record in records) {
                var jsonRec = JsonConvert.SerializeObject(record);
                jsonRecords.Add(jsonRec);
            }

            return jsonRecords;
        }

        public async Task<(bool, string, List<string>)> GetIdsFromRecordsFilterByFormula(string table, string formula, List<string> fields) {
            (bool success, string errorMessage, List<AirtableRecord> records) = await FetchRecordsFilterByFormula(table, formula, fields);

            List<string> recordIds = new List<string>();

            Console.WriteLine($"found {records.Count} records");

            foreach (AirtableRecord record in records) {
                Console.WriteLine($"{record.Id}: {record.Fields["Date"]}");
                recordIds.Add(record.Id);
            }

            return (success, errorMessage, recordIds);
        }

        public async Task<(bool, string, List<(string, string)>)> GetIdsAndAttachmentUrlsFromRecordsFilterByFormula(string table, string formula, List<string> fields) {
            (bool success, string errorMessage, List<AirtableRecord> records) = await FetchRecordsFilterByFormula(table, formula, fields);
            Console.WriteLine($"found {records.Count} records");

            List<(string, string)> idsAndUrls = new List<(string, string)>();

            foreach (AirtableRecord record in records) {
                JArray attachmentJson = (JArray) record.Fields["Cover"];

                AirtableAttachment attach = JsonConvert.DeserializeObject<AirtableAttachment>(attachmentJson[0].ToString());
                if (attach.Url != null) {
                    Console.WriteLine($"{record.Id}: {attach.Url}");
                    idsAndUrls.Add((record.Id, attach.Url));
                }
                else {
                    Console.WriteLine($"Attachment for {record.Fields["Date"]} is null!");
                }
            }

            return (success, errorMessage, idsAndUrls);
        }

        public async Task<(bool, string, List<AirtableRecord>)> FetchRecordsFilterByFormula(string table, string formula, List<string> fields) {
            string errorMessage = null;
            bool success = false;

            var records = new List<AirtableRecord>();
            string offset = null;
            using (AirtableBase airtableBase = new AirtableBase(ApiKey, BaseId)) {
                Task<AirtableListRecordsResponse> task = airtableBase.ListRecords(
                    table, offset, fields, formula);

                AirtableListRecordsResponse response = await task;

                if (response.Success) {
                    records.AddRange(response.Records.OrderBy(r => r.Fields["Date"]).ToList());
                    offset = response.Offset;
                }
                else if (response.AirtableApiError is AirtableApiException) {
                    errorMessage = response.AirtableApiError.GetBaseException().Message;
                }
                else {
                    errorMessage = "Unknown error";
                }
                success = response.Success;
            }
            return (success, errorMessage, records);
        }

        public async Task<string> GetRecord(string table, string recordId) {
            string errorMessage = null;
            var records = new List<AirtableRecord>();
            AirtableRecord myRecord = new AirtableRecord();

            using (AirtableBase airtableBase = new AirtableBase(ApiKey, BaseId)) {
                var response = await airtableBase.RetrieveRecord(table, recordId);
                if (response.Success) {
                    myRecord = response.Record;
                }
                else if (response.AirtableApiError is AirtableApiException) {
                    errorMessage = response.AirtableApiError.GetBaseException().Message;
                }
                else {
                    errorMessage = "Error!";
                }

            }
            var responseJson = JsonConvert.SerializeObject(myRecord);
            return responseJson;
        }


    }
}
