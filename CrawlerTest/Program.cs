using CrawlerLib;

var crawler = new Crawler("nevelingreply.de");
var links = await crawler.FindUrls(2);

File.WriteAllLines("C:/Users/User/Desktop/pages.txt", links);

Console.WriteLine("Completed!");
