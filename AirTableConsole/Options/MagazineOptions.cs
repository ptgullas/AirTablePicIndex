using System;
using System.Collections.Generic;
using System.Text;

namespace AirTableConsole.Options {
    public class MagazineOptions {
        public string BaseUrl { get; set; }
        public string FilenameBase { get; set; }
        public string FilenameSuffix { get; set; }

        public MagazineOptions() {
            BaseUrl = "http://www.newyorker.com/data/images/p/";
            FilenameBase = "newyorkercover_";
            FilenameSuffix = ".jpg";
        }

    }
}
