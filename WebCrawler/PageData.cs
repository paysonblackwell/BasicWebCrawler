using System;
using System.Collections.Generic;

namespace WebCrawler
{
    /// <summary>
    /// Represents all of the data we want to store for visiting a particular web page.
    /// </summary>
    public class PageData
    {
        public string Source = String.Empty;
        public string Destination = String.Empty;
        public DateTimeOffset DateVisited;
    }
}
