using System;
using System.Collections.Generic;
using System.Text;

namespace AirTableConsole.Options {
    /// <summary>
    /// Stores First Date & Last Date, both Non-inclusive. For the purposes of this app,
    /// there should be fewer than 100 months between the two dates (AirTable doesn't support
    /// retrieving more than 100 records at a time)
    /// </summary>
    public class DateOptions {
        public DateTime FirstDateNonInclusive { get; set; }
        public DateTime LastDateNonInclusive { get; set; }
    }
}
