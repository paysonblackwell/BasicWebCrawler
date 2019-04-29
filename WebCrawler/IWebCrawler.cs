using System;

namespace WebCrawler
{
    /// <summary>
    /// Represents a web crawler program.
    /// </summary>
    public interface IWebCrawler
    {
        /// <summary>
        /// Crawls the next web page.
        /// </summary>
        void Crawl();
    }
}
