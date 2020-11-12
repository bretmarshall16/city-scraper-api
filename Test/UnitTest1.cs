using System;
using city_scraper_api.Controllers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test
{
	[TestClass]
	public class UnitTest1
	{
		[TestMethod]
		public void ScraperTest()
		{
			var scraper = new CHLScraper();
			scraper.Scrape();
			var db = 
		}
	}
}
