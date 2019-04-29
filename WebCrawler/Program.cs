using Microsoft.Rest;
using Microsoft.Rest.Azure.Authentication;
using System;
using System.Net;
using System.Threading;

namespace WebCrawler
{
    class Program
    {
        static void Main(string[] args)
        {
            IWebCrawler crawler = new MyCrawler();
            while (true)
            {
                crawler.Crawl();
            }
        }
    }
}
