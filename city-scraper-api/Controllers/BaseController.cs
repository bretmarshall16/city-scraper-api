using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace city_scraper_api.Controllers
{
    public class BaseController : ApiController
    {
		public CityScraperDb db = new CityScraperDb();
    }
}
