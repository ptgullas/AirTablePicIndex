using AirTableConsole.Options;
using AirTablePicIndex;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace AirTableConsole {
    class Program {

        public static IConfigurationRoot Configuration;

        static async Task Main(string[] args) {
            SetUpConfiguration();
            AirtableCredentials airtableCredentials = GetAirtableCredentials();
            Console.WriteLine($"BaseId is {airtableCredentials.BaseId}. ApiKey is {airtableCredentials.ApiKey}");

            Retriever retriever = new Retriever(airtableCredentials.BaseId, airtableCredentials.ApiKey);

            string tableName = Configuration.GetSection("airtableOptions").GetValue<string>("tableName");

            DateOptions dateOptions = GetDateOptions();
            Console.WriteLine($"Date1 is {dateOptions.FirstDateNonInclusive}. Date2 is {dateOptions.LastDateNonInclusive}");

            await DownloadAllCovers(retriever, @"c:\temp\MagCovers", tableName);

            /* Add Covers to Issues within a Time Range */
            // await AddCoversToIssuesInTimeRange(retriever, dateOptions, tableName);

            // /* Get a single record */
            //string recordId = "recDDgkAWOdzhugCS";
            //string recordJson = await retriever.GetRecord(tableName, recordId);
            //Console.WriteLine(recordJson);

            // /* Get a number of records via a formula */
            //string formula = @"Issue='1970-01'";
            //var records = await retriever.GetRecordsFromFormula(tableName, formula);
            //foreach (string rec in records) {
            //    Console.WriteLine(rec);
            //}

            // /* add Cover to Record */
            // await AddCoverToRecord(retriever);


        } // end Main


        private static void SetUpConfiguration() {
            // this means that the app will look for appSettings.json in the folder above "bin"
            string projectRoot = AppContext.BaseDirectory.Substring(0, AppContext.BaseDirectory.LastIndexOf(@"\bin"));
            // Console.WriteLine($"BaseDirectory is {AppContext.BaseDirectory}");
            // Console.WriteLine($"projectRoot is {projectRoot}");
            var builder = new ConfigurationBuilder()
                .SetBasePath(projectRoot)
                .AddJsonFile("appSettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile("airtableCredentials.json", optional: true, reloadOnChange: true);
            // IConfigurationRoot configuration = builder.Build();
            Configuration = builder.Build();
        }
        private static AirtableCredentials GetAirtableCredentials() {
            AirtableCredentials airtableCredentials = new AirtableCredentials();
            IConfigurationSection atCredsSection = Configuration.GetSection("airtableCredentials");
            ConfigurationBinder.Bind(atCredsSection, airtableCredentials);
            return airtableCredentials;
        }

        private static DateOptions GetDateOptions() {
            DateOptions dateOptions = new DateOptions();
            IConfigurationSection dateOptionsSection = Configuration.GetSection("dateOptions");

            dateOptions.FirstDateNonInclusive = dateOptionsSection.GetValue<DateTime>("firstDateInclusive")
                                                                  .AddMonths(-1);
            dateOptions.LastDateNonInclusive = dateOptionsSection.GetValue<DateTime>("lastDateInclusive")
                                                                 .AddMonths(1);

            if (dateOptions.FirstDateNonInclusive >= dateOptions.LastDateNonInclusive) {
                throw new ArgumentOutOfRangeException("First Date should be before Last Date");
            }
            return dateOptions;
        }

        static async Task DownloadAllCovers(Retriever retriever, string targetFolder, string tableName) {
            DateTime year1 = new DateTime(1981, 1, 1);
            DateTime year2 = new DateTime(1981, 12, 1);
            DateTime finalDate = new DateTime(2018, 1, 1);

            DateOptions dateOptions = new DateOptions() {
                FirstDateNonInclusive = year1.AddMonths(-1),
                LastDateNonInclusive = year2.AddMonths(1)
            };

            var serviceProvider = new ServiceCollection().AddHttpClient().BuildServiceProvider();
            var httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient();

            while (dateOptions.FirstDateNonInclusive < finalDate) {
                
                List<(string, string)> idsAndUrls = await GetIdsAndCoverUrlsForIssuesInTimeRange(retriever, dateOptions, tableName);
                foreach ((_, string url) in idsAndUrls) {
                    await DownloadAndSaveAttachment(httpClient, targetFolder, url);
                }
                
                // download attachment
                // add 1 year to dateOptions dates
                dateOptions.FirstDateNonInclusive = dateOptions.FirstDateNonInclusive.AddYears(1);
                dateOptions.LastDateNonInclusive = dateOptions.LastDateNonInclusive.AddYears(1);
                Console.WriteLine($"{dateOptions.FirstDateNonInclusive}. {dateOptions.LastDateNonInclusive}");
            }
        }

        static async Task DownloadAndSaveAttachment(HttpClient httpClient, string targetFolder, string url) {
            if (!Directory.Exists(targetFolder)) {
                Directory.CreateDirectory(targetFolder);
            }
            Uri uri = new Uri(url);
            string fileName = Path.GetFileName(uri.LocalPath);

            string targetPath = Path.Combine(targetFolder, fileName);

            var imageBytes = await httpClient.GetByteArrayAsync(uri);
            await File.WriteAllBytesAsync(targetPath, imageBytes);
            Console.WriteLine($"Saved file {targetPath}");
        }

        static async Task<List<(string, string)>> GetIdsAndCoverUrlsForIssuesInTimeRange(Retriever retriever, DateOptions dateOptions, string tableName) {
            string formula = GenerateTimeRangeFormula(dateOptions);
            Console.WriteLine($"Retrieving Ids for Issues between {dateOptions.FirstDateNonInclusive} & {dateOptions.LastDateNonInclusive} (non-inclusive)");

            List<string> fields = new List<string>() {
                "Date", "Cover"
            };
            (bool success, string errorMessage, List<(string, string)> idsAndUrls) = await retriever.GetIdsAndAttachmentUrlsFromRecordsFilterByFormula(tableName, formula, fields);
            if (!success) {
                Console.WriteLine(errorMessage);
                return null;
            }
            else {
                return idsAndUrls;
            }
        }



        static async Task AddCoversToIssuesInTimeRange(Retriever retriever, DateOptions dateOptions, string tableName) {
            var ids = GetIdsForIssuesInTimeRange(retriever, dateOptions, tableName);
            if (ids != null) {
                string[] idArray = ids.Result.ToArray();
                int idArrayCounter = 0;

                MagazineIssueDate issueDate = new MagazineIssueDate(dateOptions.FirstDateNonInclusive);

                if (PromptForConfirmation(idArray.Count())) {
                    while (idArrayCounter < idArray.Count()) {
                        issueDate.IncrementMonth();
                        if (!issueDate.IsSkipMonth()) {
                            await AddCoverToRecord(retriever, idArray[idArrayCounter], issueDate);
                            idArrayCounter++;
                        }
                    }
                }
            }
        }

        static async Task<List<string>> GetIdsForIssuesInTimeRange(Retriever retriever, DateOptions dateOptions, string tableName) {
            string formula = GenerateTimeRangeFormula(dateOptions);
            Console.WriteLine($"Retrieving Ids for Issues between {dateOptions.FirstDateNonInclusive} & {dateOptions.LastDateNonInclusive} (non-inclusive)");

            List<string> fields = new List<string>() {
                "Date"
            };
            (bool success, string errorMessage, var ids) = await retriever.GetIdsFromRecordsFilterByFormula(tableName, formula, fields);
            if (!success) {
                Console.WriteLine(errorMessage);
                return null;
            }
            else {
                return ids;
            }
        }

        static bool PromptForConfirmation(int numberOfRecords) {
            Console.WriteLine($"Continue with adding attachments for {numberOfRecords} records?");
            string userInput = Console.ReadLine().ToUpper();
            if (userInput == "Y") {
                return true;
            }
            else {
                return false;
            }
        }

        private static string GenerateTimeRangeFormula(DateOptions dateOptions) {
            DateTime firstDate = dateOptions.FirstDateNonInclusive;
            DateTime lastDate = dateOptions.LastDateNonInclusive;

            string firstDateFormula = $"IS_AFTER(Date, '{firstDate:yyyy-MM-dd}')";
            string lastDateformula = $"IS_BEFORE(Date,'{lastDate:yyyy-MM-dd}')";
            string formula = $"AND({firstDateFormula}, {lastDateformula})";

            Console.WriteLine($"formula is {formula}");

            return formula;
        }

        static async Task AddCoverToRecord(Retriever retriever, string recordId, MagazineIssueDate issueDate) {

            string coverUrl = GenerateMagazineCoverUrl(issueDate);
            Console.WriteLine($"coverUrl is {coverUrl}");
            string tableName = "Issues";

            (bool success, string errorMessage) = await retriever.PostAttachmentToRecord(coverUrl, tableName, recordId);
            if (!success) {
                Console.WriteLine(errorMessage);
            }
            else {
                Console.WriteLine("Successfully posted!");
            }
        }

        static string GenerateMagazineCoverUrl(MagazineIssueDate issueDate) {
            MagazineOptions magazineOptions = GetMagazineOptions();

            string urlBase = magazineOptions.BaseUrl;
            string fileNameBase = magazineOptions.FilenameBase;
            string fileNameSuffix = magazineOptions.FilenameSuffix;

            return $"{urlBase}{fileNameBase}{issueDate.GetYYYYMM()}{fileNameSuffix}";
        }

        private static MagazineOptions GetMagazineOptions() {
            MagazineOptions magazineOptions = new MagazineOptions();
            IConfigurationSection magazineOptionsSection = Configuration.GetSection("magazineOptions");
            ConfigurationBinder.Bind(magazineOptionsSection, magazineOptions);
            return magazineOptions;
        }

    }
}
