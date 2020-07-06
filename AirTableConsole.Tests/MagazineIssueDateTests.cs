using System;
using Xunit;

namespace AirTableConsole.Tests {
    public class MagazineIssueDateTests {
        [Fact]
        public void IncrementMonth_WithinYear_Passes() {
            DateTime initialDate = new DateTime(1990, 8, 15);
            DateTime expectedDate = new DateTime(1990, 9, 1);

            MagazineIssueDate magazineIssueDate = new MagazineIssueDate(initialDate);

            magazineIssueDate.IncrementMonth();
            var result = magazineIssueDate.Date;

            Assert.Equal(expectedDate, result);
        }

        [Fact]
        public void IncrementMonth_NewYear_Passes() {
            DateTime initialDate = new DateTime(1990, 12, 1);
            DateTime expectedDate = new DateTime(1991, 1, 1);

            MagazineIssueDate magazineIssueDate = new MagazineIssueDate(initialDate);

            magazineIssueDate.IncrementMonth();
            var result = magazineIssueDate.Date;

            Assert.Equal(expectedDate, result);
        }

        [Fact]
        public void GetYYYYMM_Passes() {
            DateTime initialDate = new DateTime(1990, 12, 1);
            string expected = "199012";

            MagazineIssueDate magazineIssueDate = new MagazineIssueDate(initialDate);

            var result = magazineIssueDate.GetYYYYMM();

            Assert.Equal(expected, result);
        }

        [Fact]
        public void GetIssueFormat_Passes() {
            DateTime initialDate = new DateTime(1975, 8, 1);
            string expected = "1975-08";

            MagazineIssueDate magazineIssueDate = new MagazineIssueDate(initialDate);

            var result = magazineIssueDate.GetIssueFormat();

            Assert.Equal(expected, result);
        }

    }
}
