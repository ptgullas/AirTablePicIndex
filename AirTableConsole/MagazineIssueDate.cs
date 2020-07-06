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
            return Date.ToString("yyyyMM");
        }

        public string GetIssueFormat() {
            return Date.ToString("yyyy-MM");
        }

    }
}
