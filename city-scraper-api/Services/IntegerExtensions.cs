using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace city_scraper_api.Services
{
	public static class IntegerExtensions
	{
		public static int? ParseInt(this string value, int? defaultIntValue = null)
		{
			int parsedInt;
			if (int.TryParse(value, out parsedInt))
			{
				return parsedInt;
			}

			return defaultIntValue;
		}


		public static T GetValueAtOrNull<T>(this List<T> value, int index)
		{
			if (value.Count() <= index)
			{
				return default(T);
			}
			else
			{
				return value[index];
			}
		}

		public static T2 GetValueOrDefault<T1, T2>(this Dictionary<T1, T2> dict, T1 key)
		{
			if (dict.ContainsKey(key))
			{
				return dict[key];
			}
			else
			{
				return default(T2);
			}
		}
	}
}