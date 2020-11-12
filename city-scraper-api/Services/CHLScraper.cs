
using city_scraper_api.Services;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.WebPages;

namespace city_scraper_api.Controllers
{

	public class CHLScraper
	{
		CityScraperDb db = new CityScraperDb();
		public CHLScraper()
		{ }

		public MergeObject Scrape()
		{
			var mergeObject = new MergeObject();


			var leagues = db.Leagues.ToList().ToList();

			Parallel.ForEach(leagues, league =>
			{


				HtmlWeb web = new HtmlWeb();

				////SCRAPING FOR SCHOOLS AND SCORES
				var url = $"{league.Site}confStandings.aspx?satc=100";
				var doc = web.Load(url);


				var firstTable = doc.DocumentNode.SelectSingleNode("//table");

				var nodes = doc.DocumentNode.SelectNodes(".//tr")?.ToList();
				nodes = nodes?.Where(o => (o.HasClass("odd") || o.HasClass("even"))
				&& o.Id.ToLower().Contains("standings")
				).ToList();

				nodes = nodes != null ? nodes : new List<HtmlNode>();

				foreach (var node in nodes)
				{
					var row = node.SelectSingleNode("./td");

					var link = Regex.Replace(row.SelectSingleNode("./a")?.GetAttributeValue("href", ""), "amp;", string.Empty);
					var name = RemoveExtra(row?.InnerText?.ToLower().Trim());
					var teamUrl = $"{league.Site}{link}";
					var tempDoc = web.Load(teamUrl);

					var gameRows = tempDoc.DocumentNode.SelectNodes("//tr")?.Skip(1);

					var monthYearString = "";
					if (gameRows != null)
					{
						foreach (var gameRow in gameRows)
						{
							if (gameRow.Id == "")
							{
								monthYearString = gameRow.SelectSingleNode("./th").InnerText.Trim();
							}
							else
							{
								var gameData = gameRow.SelectNodes("./td");

								var date = RemoveExtra(gameData[0].InnerText.Trim());
								var otherTeam = gameData[1].InnerText.Trim();
								var score = RemoveExtra(gameData[2].InnerText).Trim();
								var newGame = CreateGameFromData(monthYearString, date, otherTeam, score, name);

								mergeObject.Games.Add(newGame);


							}
						}
					}

				}







				//SCRAPING FOR PLAYERS AND STATS
				web = new HtmlWeb();
				url = $"{league.Site}statsPage.aspx?satc=99&v=a&stat=off";
				doc = web.Load(url);
				nodes = doc.DocumentNode.SelectNodes("//tr").Skip(2).ToList();

				nodes = nodes != null ? nodes : new List<HtmlNode>();

				Parallel.ForEach(nodes, playerNode =>
			   {
				   var temp = new PlayerDto();
				   temp.LeagueId = league.Id;
				   var rows = playerNode.SelectNodes("./td").ToList();

				   var link = rows[1].SelectSingleNode("./a").GetAttributeValue("href", "");
				   var nameSchoolSplit = rows[1]?.InnerText?.ToLower().Trim().Split(',');


				   temp.Name = nameSchoolSplit[0]?.Trim();
				   temp.School = nameSchoolSplit[1]?.Trim().Replace("-", " ");
				   temp.Games = new List<PlayerGameDto>();

				   var playerUrl = $"{league.Site}{link}";
				   var playerDoc = web.Load(playerUrl);
				   var gameNodes = playerDoc.DocumentNode.SelectNodes(".//tr")?.Where(o => o.Id.ToLower().Contains("games")).ToList();

				   gameNodes = gameNodes != null ? gameNodes : new List<HtmlNode>();
				   foreach (var row in gameNodes)
				   {
					   var tableDatas = row.SelectNodes("./td").ToList();
					   var year = "2019";
					   var date = tableDatas.GetValueAtOrNull(0)?.InnerText.Trim();
					   var Opponent = tableDatas.GetValueAtOrNull(1)?.InnerText.Trim().ToLower();
					   var Goals = tableDatas.GetValueAtOrNull(3)?.InnerText.Trim();
					   var Assists = tableDatas.GetValueAtOrNull(4)?.InnerText.Trim();
					   var newPlayerGameDto = CreatePlayerGameFromDate(year, date, Opponent, Goals, Assists);

						temp.Games.Add(newPlayerGameDto);
				   }

				   mergeObject.Players.Add(temp);
			   });

			});

			return mergeObject;
		}



		public int Merge(MergeObject data)
		{
			var schoolIndex = db.Schools.Select(o => o.Id).DefaultIfEmpty(0).Max() + 1;
			int teamIndex = db.Teams.Select(o => o.Id).DefaultIfEmpty(0).Max() + 1;
			int gameIndex = db.Games.Select(o => o.Id).DefaultIfEmpty(0).Max() + 1;
			int playerIndex = db.Players.Select(o => o.Id).DefaultIfEmpty(0).Max() + 1;
			int playerGameIndex = db.PlayerGames.Select(o => o.Id).DefaultIfEmpty(0).Max() + 1;

			var schoolDict = db.Schools.ToDictionary(o => o.Name, o => o);
			var teamDict = db.Teams.ToDictionary(o => new Tuple<int, int>(o.SchoolId, o.SportId), o => o.Id);
			var gameDict = db.Games.ToDictionary(o => new Tuple<int, int, DateTime>(o.HomeTeamId, o.AwayTeamId, o.Date), o => o);

			var playersDict = db.Players.ToDictionary(o => new Tuple<string, int>(o.Name, o.TeamId), o => o);
			var playerGamesDict = db.PlayerGames.ToDictionary(o => new Tuple<int, int>(o.PlayerId, o.GameId), o => o);


			var playerCount = 0;

			foreach (var game in data.Games)
			{
				if (!schoolDict.ContainsKey(game.HomeTeam))
				{

					var newSchool = db.Schools.Add(new School()
					{
						Id = schoolIndex + 1,
						Name = game.HomeTeam,

					});

					var newteam = db.Teams.Add(new Team()
					{
						Id = teamIndex + 1,
						SchoolId = newSchool.Id,
						SportId = 1
					});

					schoolIndex++;
					teamIndex++;
					schoolDict.Add(newSchool.Name, newSchool);
					teamDict.Add(new Tuple<int, int>(newteam.SchoolId, newteam.SportId), newteam.Id);
				}

				if (!schoolDict.ContainsKey(game.AwayTeam))
				{

					var newSchool = db.Schools.Add(new School()
					{
						Id = schoolIndex + 1,
						Name = game.AwayTeam,

					});

					var newteam = db.Teams.Add(new Team()
					{
						Id = teamIndex + 1,
						SchoolId = newSchool.Id,
						SportId = 1
					});

					schoolIndex++;
					teamIndex++;

					schoolDict.Add(newSchool.Name, newSchool);
					teamDict.Add(new Tuple<int, int>(newteam.SchoolId, newteam.SportId), newteam.Id);
				}

				var homeTeamSchool = schoolDict[game.HomeTeam];
				var homeTeamId = teamDict[new Tuple<int, int>(homeTeamSchool.Id, 1)];
				var awayTeamSchool = schoolDict[game.AwayTeam];
				var awayTeamId = teamDict[new Tuple<int, int>(awayTeamSchool.Id, 1)];
				var tupleToTest = new Tuple<int, int, DateTime>(
					homeTeamId,
					awayTeamId,
					game.Date
					);


				if (!gameDict.ContainsKey(tupleToTest))
				{
					var gameToAdd = new Game()
					{
						Id = gameIndex + 1,
						HomeTeamId = homeTeamId,
						AwayTeamId = awayTeamId,
						Date = game.Date,
						HomeTeamScore = game.HomeScore,
						AwayTeamScore = game.AwayScore
					};
					gameIndex++;
					gameToAdd = db.Games.Add(gameToAdd);
					gameDict.Add(tupleToTest, gameToAdd);
				}

			}


			foreach (var player in data.Players)
			{
				Player playerToEdit = null;
				var school = schoolDict.GetValueOrDefault(player.School);
				if (school.LeagueId == null)
				{
					school.LeagueId = player.LeagueId;
				}

				var teamId = teamDict.GetValueOrDefault(new Tuple<int, int>(school.Id, 1));
				var tupleToTest = new Tuple<string, int>(player.Name, teamId);
				if (!playersDict.ContainsKey(tupleToTest))
				{
					playerIndex++;
					var playerToAdd = new Player()
					{
						Id = playerIndex,
						Name = player.Name,
						TeamId = teamId
					};

					db.Players.Add(playerToAdd);
					playersDict.Add(tupleToTest, playerToAdd);

					playerToEdit = playerToAdd;
				}
				else
				{
					playerToEdit = playersDict[tupleToTest];
				}

				foreach (var playerGame in player.Games)
				{
					Game game = null;
					if (playerGame.Opponent.Contains("@"))
					{
						var awayTeamId = playerToEdit.TeamId;
						var homeTeamSchool = schoolDict[playerGame.Opponent.Replace("@", string.Empty)];
						var homeTeamId = teamDict[new Tuple<int, int>(homeTeamSchool.Id, 1)];
						var newtupleToTest = new Tuple<int, int, DateTime>(
						homeTeamId,
						awayTeamId,
						playerGame.Date
						);

						game = gameDict.GetValueOrDefault(newtupleToTest);
					}

					else
					{
						var homeTeamId = playerToEdit.TeamId;
						var awayTeamSchool = schoolDict[playerGame.Opponent.Replace("@", string.Empty)];
						var awayTeamId = teamDict[new Tuple<int, int>(awayTeamSchool.Id, 1)];
						var newtupleToTest = new Tuple<int, int, DateTime>(
						homeTeamId,
						awayTeamId,
						playerGame.Date
						);

						game = gameDict.GetValueOrDefault(newtupleToTest);
					}

					var tupleToTest1 = new Tuple<int, int>(playerToEdit.Id, game.Id);
					if (!playerGamesDict.ContainsKey(tupleToTest1))
					{
						playerGameIndex++;
						var playerGameToAdd = new PlayerGame()
						{
							Id = playerGameIndex,
							PlayerId = playerToEdit.Id,
							GameId = game.Id,
							Goals = playerGame.Goals,
							Assists = playerGame.Assists
						};

						db.PlayerGames.Add(playerGameToAdd);

						playerGamesDict.Add(tupleToTest1, playerGameToAdd);
					}
				}



			}

			db.SaveChanges();

			return playerCount;
		}

		private PlayerGameDto CreatePlayerGameFromDate(string year, string date, string opponent, string goals, string assists)
		{
			var newPlayerGame = new PlayerGameDto();

			var dateString = $"{date} {year}";
			newPlayerGame.Date = Convert.ToDateTime(dateString);

			newPlayerGame.Opponent = opponent.Replace("-", " ");
			newPlayerGame.Goals = goals.ParseInt();
			newPlayerGame.Assists = assists.ParseInt();


			return newPlayerGame;
		}
		private GameDto CreateGameFromData(string month, string date, string otherTeam, string score, string myTeam)
		{
			var newGame = new GameDto();
			myTeam = myTeam.Replace("-", " ");
			var otherTeamName = "";
			if (otherTeam.Substring(0, 3) == "at ")
			{
				var regex = new Regex("at");
				otherTeamName = regex.Replace(otherTeam.ToLower().Replace("-", " "), string.Empty, 1);
			}
			else
			{
				otherTeamName = otherTeam.ToLower().Replace("-", " ");
			}
			otherTeamName = RemoveExtra(otherTeamName.Replace("postseason", string.Empty));
			Char[] separators = { '-', ' ' };

			var tempScore = RemoveExtra(score).Split(separators, 4, StringSplitOptions.RemoveEmptyEntries).ToList();
			var result = tempScore[0]?.ToLower().Trim();
			var score1 = tempScore.GetValueAtOrNull(1)?.ParseInt();
			var score2 = tempScore.GetValueAtOrNull(2)?.ParseInt();

			if (otherTeam.Substring(0, 3) == "at ")
			{
				newGame.HomeTeam = otherTeamName;
				newGame.AwayTeam = myTeam;


				switch (result)
				{
					case "w":
						newGame.HomeScore = score2;
						newGame.AwayScore = score1;
						break;
					case "l":
						newGame.HomeScore = score1;
						newGame.AwayScore = score2;
						break;
					default:
						newGame.HomeScore = score1;
						newGame.AwayScore = score2;
						break;
				}
			}
			else
			{
				newGame.HomeTeam = myTeam;
				newGame.AwayTeam = otherTeamName;

				switch (result)
				{
					case "w":
						newGame.HomeScore = score1;
						newGame.AwayScore = score2;
						break;
					case "l":
						newGame.HomeScore = score2;
						newGame.AwayScore = score1;
						break;
					default:
						newGame.HomeScore = score1;
						newGame.AwayScore = score2;
						break;
				}

			}

			var dateString = $"{date} {month}";
			newGame.Date = Convert.ToDateTime(dateString);

			return newGame;
		}

		public string RemoveExtra(string x)
		{
			x = Regex.Replace(x, "&nbsp", " ");
			x = Regex.Replace(x, "/\n/", " ");
			x = Regex.Replace(x, "/\t/", " ");
			x = x.Replace(";", "");
			return x.Trim();
		}
	}

	public class MergeObject
	{
		public MergeObject()
		{
			Games = new List<GameDto>();
			Players = new List<PlayerDto>();
		}
		public List<GameDto> Games { get; set; }
		public List<PlayerDto> Players { get; set; }

	}




	public class PlayerDto
	{
		public string Name { get; set; }
		public string School { get; set; }
		public int LeagueId { get; set; }
		public List<PlayerGameDto> Games { get; set; }

	}

	public class PlayerGameDto
	{
		public DateTime Date { get; set; }
		public string Opponent { get; set; }
		public int? Goals { get; set; }
		public int? Assists { get; set; }
	}

	public class GameDto
	{
		public string HomeTeam { get; set; }
		public string AwayTeam { get; set; }
		public int? HomeScore { get; set; }
		public int? AwayScore { get; set; }
		public DateTime Date { get; set; }
	}




}