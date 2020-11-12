using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace city_scraper_api.Controllers
{
	[RoutePrefix("stats")]
	public class StatsController : BaseController
	{
		[HttpGet, Route("total")]
		public IHttpActionResult Get()
		{
			var totalStats = db.TotalStats.OrderByDescending(o => o.total).ToList();

			return Ok(totalStats);
		}


	}
}