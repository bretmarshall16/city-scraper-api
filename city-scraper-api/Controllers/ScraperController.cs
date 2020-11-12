
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Script.Serialization;

namespace city_scraper_api.Controllers
{
	[RoutePrefix("Scraper")]
	public class ScraperController :  BaseController
	{
		

		[HttpGet, Route("scrape/merge")]
		// GET api/values/5
		public IHttpActionResult ScraperAndMerge()
		{
			var start = DateTime.UtcNow;
			var scraper = new CHLScraper();
			var data = scraper.Scrape();
			var end = DateTime.UtcNow;

			db.ScraperSummaries.Add(new ScraperSummary()
			{
				StartTime = start,
				EndTime = end,
				PlayerCount = data.Players.Count
			});
			db.SaveChanges();

			var mergeStart = DateTime.UtcNow;
			var mergeCount = scraper.Merge(data);
			var mergeEnd = DateTime.UtcNow;

			int index = db.MergeSummaries.Select(o => o.Id).DefaultIfEmpty(0).Max() + 1;

			db.MergeSummaries.Add(new MergeSummary()
			{
				Id = index,
				StartTime = mergeStart,
				EndTime = mergeEnd,
				NewPlayerCount = mergeCount
			});

			var y = db.SaveChanges();

			return Ok(y);
		}




		[HttpGet, Route("addLeagues")]
		// GET api/values/5
		public IHttpActionResult AddLeagues()
		{
			var currentLeagueDict = db.Leagues.ToDictionary(o => o.Name, o => o);


			var leagueList = new List<League>
			{
				new League()
				{
					Name = "Cincinnati Hills League",
					Abbreviation = "CHL",
					Site = "http://www.chlsports.com/"
				},
				new League()
				{
					Name = "Southern Buckeye Athletic and Academic Conference",
					Abbreviation = "SBAAC",
					Site = "http://www.sbaac.com/"
				},
				new League()
				{
					Name = "Eastern Cincinnati Conference",
					Abbreviation = "ECC",
					Site = "http://eccsports.com/"
				},
				new League()
				{
					Name = "Greater Catholic League Coed",
					Abbreviation = "GCLC",
					Site = "http://gclc.gclsports.com/"
				},
				new League()
				{
					Name = "Girls Greater Catholic League",
					Abbreviation = "GGCL",
					Site = "http://ggcl.gclsports.com/"
				},
				new League()
				{
					Name = "Greater Miami Conference",
					Abbreviation = "GMC",
					Site = "http://www.gmcsports.com/"
				},
				new League()
				{
					Name = "Greater Western Ohio Conference",
					Abbreviation = "GWOC",
					Site = "http://www.gwocsports.com/"
				},
				new League()
				{
					Name = "Metro Buckeye Conference",
					Abbreviation = "MBC",
					Site = "http://www.metrobuckeyesports.com/"
				},
				new League()
				{
					Name = "Miami Valley Conference",
					Abbreviation = "MVC",
					Site = "http://miamivalleyconference.com/"
				},
				new League()
				{
					Name = "Miami Valley League",
					Abbreviation = "MVL",
					Site = "http://mvlathletics.com/"
				},
				new League()
				{
					Name = "South Western Ohio Conference",
					Abbreviation = "SWOC",
					Site = "http://www.swocsports.com/"
				},
				new League()
				{
					Name = "South Western Buckeye League",
					Abbreviation = "SWBL",
					Site = "http://www.swblsports.com/"
				},




			};

			foreach (var league in leagueList)
			{
				if (!currentLeagueDict.ContainsKey(league.Name))
				{
					db.Leagues.Add(league);
					currentLeagueDict.Add(league.Name, league);
				}
				else
				{
					var oldLeague = currentLeagueDict[league.Name];
					oldLeague.Abbreviation = league.Abbreviation;
					oldLeague.Site = league.Site;
				}
			}

			db.SaveChanges();

			return Ok(1);

		}
	}
}
