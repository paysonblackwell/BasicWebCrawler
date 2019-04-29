using HtmlAgilityPack;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
/*
    Payson Blackwell, 4/28/2019

    This project is part of a larger assignment for CSD 438, where the web crawler generates data via links in json files for an Azure Data Lake.
    I have taken out all the dependencies to show the web crawler working with just the console.


    The Crawler searches with FIFO (a Queue), however it outputs the children into finishedLinks as soon as it finds it.
    This helps the total number of host links to appear.

    The crawler searches through every single link it can find, but only outputs the unique destination to source pages it finds
    Sometimes it seems the crawler isn't finding new hosts, but it is just searching through a bunch of previously seen hosts


    TODO:
        LIMIT THE AMOUNT OF EACH HOST SITE CAN SEARCH ON THEIR ON SITE (SUCH AS FACEBOOK SENDING TO A FACEBOOK PAGE)
        HAVE A BETTER WAY TO SHOW THE OUTPUT
*/
namespace WebCrawler
{
    /// <summary>
    /// Crawls the web.
    /// </summary>
    public class MyCrawler : IWebCrawler
    {
        // Queue For links
        private Queue<PageData> linksToSearch;

        // Hash table to prevent redundancy when searching
        private Hashtable prevLinks;

        // Hash table for completed Host Links
        private Hashtable prevHosts;

        // Storing Page Data
        private List<PageData> finishedLinks;

        // Getting the page from the web
        private HtmlWeb linkLoader;

        // Current Page
        private HtmlDocument currentPage;

        // Current Page as PageData
        PageData currentPageData;


        // FOR TESTING
        //private List<string> tempFound;

        public MyCrawler()
        {
            linksToSearch = new Queue<PageData>();
            prevLinks = new Hashtable();
            prevHosts = new Hashtable();
            finishedLinks = new List<PageData>();
            linkLoader = new HtmlWeb();
            currentPage = new HtmlDocument();
            currentPageData = null;

            // FOR TESTING
            //tempFound = new List<string>();
        }


        /// <summary>
        /// Setting up the crawler with seed links
        /// </summary>
        public void loadSeedLinks(int amount)
        {
            string[] allSeeds = File.ReadAllLines("sites.txt");
            Random rand = new Random();
            string tempLink = "";

            for(int i = 0; i < amount; i++)
            {
                tempLink = allSeeds[rand.Next(0, allSeeds.Length - 1)];
                PageData pd = new PageData();
                pd.Source = tempLink;
                pd.Destination = tempLink;

                if (!prevLinks.Contains(tempLink))
                {
                    prevLinks.Add(tempLink, prevLinks.Count);
                    linksToSearch.Enqueue(pd);
                }              
            }
        }

        /// <summary>
        /// Returns the htmlDocument of a given Page's Destination
        /// </summary>
        public HtmlDocument loadPage(ref PageData page)
        {
            HtmlDocument tempDoc;
            try
            {
                tempDoc = linkLoader.Load(page.Destination);
                page.DateVisited = DateTime.UtcNow;
            }
            catch(Exception e)
            {
                //Console.WriteLine("Error opening file: "+e.ToString());
                return null;
            }

            return tempDoc;
        }

        /// <summary>
        /// Adds the href links of the currentPage to the linksToSearch Queue in a PageData format, Adds found link to finishedLinks
        /// </summary>
        public void addLinksFromDocument()
        {
            if (currentPage == null)
            {
                return;
            }

            HtmlNodeCollection nodes = currentPage.DocumentNode.SelectNodes("//a[@href]");
            if(nodes == null)
            {
                return;
            }

            foreach (HtmlNode node in nodes)
            {
                HtmlAttribute att = node.Attributes["href"];
                foreach (string link in att.Value.Split(' '))
                {
                    if (link.StartsWith("http")) //&& !listBox1.Items.Contains(link))
                    {
                        if (!prevLinks.Contains(link) && link.Length < 150)
                        {
                            //tempFound.Add(link); // For outputing for testing

                            // create new page for searching
                            PageData page = new PageData();
                            page.Source = currentPageData.Destination;
                            page.Destination = link;
                            page.DateVisited = DateTime.UtcNow;

                            prevLinks.Add(link, prevLinks.Count);
                            linksToSearch.Enqueue(page);

                            // Make a new page to extract host links
                            PageData page2 = new PageData();
                            page2.Source = page.Source;
                            page2.Destination = link;
                            page2.DateVisited = DateTime.UtcNow;

                            ExtractHost(ref page2);
                            string formattedHost = "Source: " + page2.Source + ", Dest: " + page2.Destination;

                            // Don't add Host if it has already been done
                            if (!prevHosts.Contains(formattedHost) && page2.Source != page2.Destination)
                            {
                                prevHosts.Add(formattedHost, prevHosts.Count);
                                // Finished with current Page, add it to the completed list
                                finishedLinks.Add(page2);
                            }

                        }

                    }
                }
            }
        }

        /// <summary>
        /// Crawls the next web page. 
        /// </summary>
        public void Crawl()
        {
            try
            {
                if (linksToSearch.Count <= 0)
                {
                    // Load with 5 random seed links if queue is empty
                    loadSeedLinks(5);
                }

                // Get the next page to search from
                currentPageData = linksToSearch.Dequeue();

                // Run as task to prevent from taking forever
                Task task = Task.Run(() =>
                {
                // Get current page
                currentPage = loadPage(ref currentPageData);
                });

                // At Most wait for 2 seconds for the page to load
                bool isFinished = task.Wait(TimeSpan.FromSeconds(2));

                if (isFinished == false)
                {
                    // Didn't load the page, skip to next page
                    return;
                }

                // Get Links from Current Page
                addLinksFromDocument();

                ExtractHost(ref currentPageData);
                string formattedHost = "Source: " + currentPageData.Source + ", Dest: " + currentPageData.Destination;

                // Don't add Host if it has already been done
                if (!prevHosts.Contains(formattedHost) && currentPageData.Source != currentPageData.Destination)
                {
                    prevHosts.Add(formattedHost, prevHosts.Count);
                    // Finished with current Page, add it to the completed list
                    finishedLinks.Add(currentPageData);
                }


                //---------------------------------------------------
                //--------------------FOR TESTING--------------------
                //---------------------------------------------------

                /*
                Console.WriteLine("---------------------Links Found on Page---------------------");
                foreach (var item in tempFound)
                {
                    Console.WriteLine(item.ToString());                  
                }
                //Console.WriteLine("Number of unique source to destination hosts: " + finishedLinks.Count);
                tempFound.Clear();
                */

                Console.WriteLine("---------------------Finished Links---------------------");
                foreach (var p in finishedLinks)
                {
                    Console.WriteLine("Source: " + p.Source + ", Dest: " + p.Destination);
                }
                Console.WriteLine("Number of unique source to destination hosts: " + finishedLinks.Count);
            }
            catch (Exception e)
            {
                //Console.WriteLine("Failed to Parse Link, moving to next Link: "+e.ToString());
                return;   
            }

        }

        /// <summary>
        /// Turns the Links in the PageData object to Host links
        /// </summary>
        public static void ExtractHost(ref PageData p)
        {
            if (p.Source.Contains(@"://"))
            {
                p.Source = p.Source.Split(new string[] { "://" }, 2, StringSplitOptions.None)[1];
            }
            p.Source = p.Source.Split('/')[0];

            if (p.Destination.Contains(@"://"))
            {
                p.Destination = p.Destination.Split(new string[] { "://" }, 2, StringSplitOptions.None)[1];
            }
            p.Destination = p.Destination.Split('/')[0];
        }

        /// <summary>
        /// Another way to turn the Links in the PageData object to Host links
        /// </summary>
        public static void ExtractHostBackup(ref PageData p)
        {
            // Change link to host URL
            Uri myUri = new Uri(p.Source);
            p.Source = myUri.Host;

            Uri myUri2 = new Uri(p.Destination);
            p.Destination = myUri2.Host;
        }
    }
}
