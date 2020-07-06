using AirTableConsole.Options;
using AirTablePicIndex;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
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
            Console.WriteLine($"tableName is {tableName}");

            DateOptions dateOptions = GetDateOptions();
            Console.WriteLine($"Date1 is {dateOptions.FirstDateNonInclusive}. Date2 is {dateOptions.LastDateNonInclusive}");

            await AddCoversToIssuesInTimeRange(retriever, dateOptions, tableName);

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


        static async Task AddCoversToIssuesInTimeRange(Retriever retriever, DateOptions dateOptions, string tableName) {
            var ids = GetIdsForIssuesInTimeRange(retriever, dateOptions, tableName);
            if (ids != null) {
                string[] idArray = ids.Result.ToArray();
                int idArrayCounter = 0;

                MagazineIssueDate issueDate = new MagazineIssueDate(dateOptions.FirstDateNonInclusive);

                while (idArrayCounter < idArray.Count()) {
                    issueDate.IncrementMonth();
                    await AddCoverToRecord(retriever, idArray[idArrayCounter], issueDate);
                    idArrayCounter++;
                }
            }
        }

        static async Task<List<string>> GetIdsForIssuesInTimeRange(Retriever retriever, DateOptions dateOptions, string tableName) {
            string formula = GenerateTimeRangeFormula(dateOptions);
            Console.WriteLine($"Retrieving Ids for Issues between {dateOptions.FirstDateNonInclusive} & {dateOptions.LastDateNonInclusive} (non-inclusive)");
            (bool success, string errorMessage, var ids) = await retriever.GetIdsFromRecordsFilterByFormula(tableName, formula);
            if (!success) {
                Console.WriteLine(errorMessage);
                return null;
            }
            else {
                return ids;
            }
        }

        private static string GenerateTimeRangeFormula(DateOptions dateOptions) {
            DateTime firstDate = dateOptions.FirstDateNonInclusive;
            DateTime lastDate = dateOptions.LastDateNonInclusive;

            string lastDateformula = $"IS_BEFORE(Date,'{lastDate:yyyy-MM-dd}')";
            string firstDateFormula = $"IS_AFTER(Date, '{firstDate:yyyy-MM-dd}')";
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
