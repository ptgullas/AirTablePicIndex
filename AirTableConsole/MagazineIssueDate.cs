using System;
using System.Collections.Generic;
using System.Text;

namespace AirTableConsole {
    public class MagazineIssueDate {
        public DateTime Date { get; set; }
        public MagazineIssueDate(DateTime date) {
            // always set to first day of month
            Date = new DateTime(date.Year, date.Month, 1);
        }

        public void IncrementMonth() {
            Date = Date.AddMonths(1);
        }

        public string GetYYYYMM() {
            string coverImageDateFormat = Date.ToString("yyyyMM");
            if (IsBimonthlyIssue()) {
                int followingMonthNumber = Date.Month + 1;
                return $"{coverImageDateFormat}-{followingMonthNumber:D2}";
            }
            return coverImageDateFormat;
        }

        private bool IsBimonthlyIssue() {
            bool isBimonthly = false;
            if ((Date.Year >= 2009) && (Date.Month == 7) && (Date.Year != 2018)
                || (Date.Year == 2017 && Date.Month == 1)
                || (Date.Year == 2018) && (Date.Month == 1 || Date.Month == 5)
                || ((Date.Year >= 2019) && (Date.Month % 2 != 0))
                ) {
                isBimonthly = true;
            }
            return isBimonthly;
        }

        public bool IsSkipMonth() {
            bool isSkipMonth = false;
            if ((Date.Year >= 2009) && (Date.Month == 8) && (Date.Year != 2018)
                || (Date.Year == 2017 && Date.Month == 2)
                || (Date.Year == 2018) && (Date.Month == 2 || Date.Month == 6)
                || ((Date.Year >= 2019) && (Date.Month % 2 == 0))
                ) {
                isSkipMonth = true;
            }
            return isSkipMonth;
        }

        public string GetIssueFormat() {
            return Date.ToString("yyyy-MM");
        }

    }
}
