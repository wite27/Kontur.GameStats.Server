using System;
using System.Linq;
using Newtonsoft.Json;
using Kontur.GameStats.Server.Data;
using System.Net;

namespace Kontur.GameStats.Server.API
{
    public class PlayerController
    {
        #region GET /players/<name>/stats
        /// <summary>
        /// Returns <see cref="ApiResponse"/> with string representation of <see cref="PlayerStatsJson"/> in body.
        /// </summary>
        /// <param name="name"></param>
        /// <returns><see cref="ApiResponse"/> with string representation of <see cref="PlayerStatsJson"/> in body.</returns>
        public ApiResponse GetStats(string name)
        {
            using (var context = new GameStatsDbDataContext())
            {
                var currPlayer = context.Players.Where(pl => pl.name == name).FirstOrDefault();
                if (currPlayer == null)
                    return new ApiResponse(HttpStatusCode.NotFound);

                int currPlayerId = currPlayer.id;

                PlayerStatsJson playerStats = new PlayerStatsJson();

                /*totalMatchesPlayed*/
                playerStats.TotalMatchesPlayed =
                    context.PlayersInMatches
                           .Where(m => m.player_id == currPlayerId)
                           .Count();

                /*totalMatchesWon*/
                playerStats.TotalMatchesWon =
                    context.Matches
                           .Where(m => m.winner_id == currPlayerId)
                           .Count();

                // join player's row in match with info about match
                var playerMatches = 
                    (from playerInMatch in context.PlayersInMatches
                     where playerInMatch.player_id == currPlayerId
                     join match in context.Matches
                     on playerInMatch.match_id equals match.id
                     select new
                     {
                         matchInfo  = match,
                         playerInfo = playerInMatch
                     });

                // group by server_id, then count it
                // use it in follow queries
                var orderedUniqueServers =
                   (from p in playerMatches
                    group p.matchInfo.server_id by p.matchInfo.server_id into serverGroup
                    orderby serverGroup.Count() descending
                    select serverGroup);

                /*favoriteServer*/
                // take top1 server endpoint
                int top1serverId = orderedUniqueServers.First().Key;
                playerStats.FavoriteServer =
                    context.Servers
                           .Where(s => s.id == top1serverId) // top1 server
                           .First()
                           .endpoint;

                /*uniqueServers*/
                // count all unqiue servers
                playerStats.UniqueServers =
                    orderedUniqueServers.Count();

                /*favoriteGM*/
                int favGMid =
                    (from p in playerMatches
                     group p.matchInfo by p.matchInfo.gm_id into gmGroup
                     orderby gmGroup.Count() descending
                     select gmGroup)
                     .First().Key;
                playerStats.FavoriteGameMode =
                    context.GameModes
                           .Where(gm => gm.id == favGMid)
                           .First().name;

                /*averageScoreboardPercent*/

                playerStats.AverageScoreboardPercent = 
                    (from p in playerMatches
                    let players_count = p.matchInfo.players_count
                    select (players_count == 1 ?
                            100 :
                            (players_count - p.playerInfo.player_rank)
                            * 100.0
                            / (players_count - 1) 
                            )
                    )
                    .Average();

                /*matchesPerDay*/
                var matchesPerDays =
                    (from p in playerMatches
                     group p.matchInfo by p.matchInfo.timestamp_day into matchGroup
                     //group by constant to use 2 aggregates
                     group matchGroup by 1 into matchGroup
                     select new
                     {
                         maximum = matchGroup.Max
                                              (m => m.Count()),
                         average = matchGroup.Average
                                              ( m => m.Count())
                     }).First();
                /*maximumMatchesPerDay*/
                playerStats.MaximumMatchesPerDay =
                    matchesPerDays.maximum;
                /*averageMatchesPerDay*/
                playerStats.AverageMatchesPerDay =
                    matchesPerDays.average;

                /*lastMatchPlayed*/
                playerStats.LastMatchPlayed =
                    (from p in playerMatches
                     orderby p.matchInfo.timestamp descending
                     select p.matchInfo.timestamp)
                     .First();
                // add "Z" to date
                playerStats.LastMatchPlayed = DateTime.SpecifyKind(playerStats.LastMatchPlayed, DateTimeKind.Utc);

                /*killToDeathRatio*/
                playerStats.KillToDeathRatio =
                    (from p in playerMatches
                     //group by constant to use 2 aggregates
                     group p.playerInfo by 1 into gr 
                     select (double)gr.Sum(x => x.kills) /
                                    gr.Sum(x => x.deaths))
                     .First();

                return new ApiResponse(
                    body: JsonConvert.SerializeObject(playerStats));
            }
        }
        
        #endregion

        #region GET /reports/best-players[/<count>]
        /// <summary>
        /// Returns <see cref="ApiResponse"/> with string representation of List&lt;<see cref="BestPlayerJson"/>&gt; in body.
        /// </summary>
        /// <param name="count"></param>
        /// <returns><see cref="ApiResponse"/> with string representation of List&lt;<see cref="BestPlayerJson"/>&gt; in body.</returns>
        public ApiResponse GetBestPlayers(int count)
        {
            using (var context = new GameStatsDbDataContext())
            {
                var bestPlayers =
                    (from player in context.Players
                     where player.matches_played >= 10 &&
                           player.total_deaths != 0
                     let kdr = (double)player.total_kills / player.total_deaths
                     orderby kdr descending
                     select new BestPlayerJson
                     {
                         Name = player.name,
                         KDR = kdr
                     })
                     .Take(count)
                     .ToList();

                return new ApiResponse(
                    body: JsonConvert.SerializeObject(bestPlayers, Formatting.Indented));
            }
        }
        #endregion

        #region Api interfaces

        public class PlayerStatsJson
        {
            [JsonProperty("totalMatchesPlayed")]
            public int TotalMatchesPlayed { get; set; }

            [JsonProperty("totalMatchesWon")]
            public int TotalMatchesWon { get; set; }

            [JsonProperty("favoriteServer")]
            public string FavoriteServer { get; set; }

            [JsonProperty("uniqueServers")]
            public int UniqueServers { get; set; }

            [JsonProperty("favoriteGameMode")]
            public string FavoriteGameMode { get; set; }

            [JsonProperty("averageScoreboardPercent")]
            public double AverageScoreboardPercent { get; set; }

            [JsonProperty("maximumMatchesPerDay")]
            public int MaximumMatchesPerDay { get; set; }

            [JsonProperty("averageMatchesPerDay")]
            public double AverageMatchesPerDay { get; set; }

            [JsonProperty("lastMatchPlayed")]
            public DateTime LastMatchPlayed { get; set; }

            [JsonProperty("killToDeathRatio")]
            public double KillToDeathRatio { get; set; }

            public override int GetHashCode()
            {
                return TotalMatchesPlayed ^ TotalMatchesWon;
            }

            public override bool Equals(object obj)
            {
                if (obj == null) return false;
                PlayerStatsJson objAsPlayerStatsJson = obj as PlayerStatsJson;
                if (objAsPlayerStatsJson == null) return false;
                return (this.AverageMatchesPerDay == objAsPlayerStatsJson.AverageMatchesPerDay &&
                        this.AverageScoreboardPercent == objAsPlayerStatsJson.AverageScoreboardPercent &&
                        this.FavoriteGameMode == objAsPlayerStatsJson.FavoriteGameMode &&
                        this.FavoriteServer == objAsPlayerStatsJson.FavoriteServer &&
                        this.KillToDeathRatio == objAsPlayerStatsJson.KillToDeathRatio &&
                        this.LastMatchPlayed == objAsPlayerStatsJson.LastMatchPlayed &&
                        this.MaximumMatchesPerDay == objAsPlayerStatsJson.MaximumMatchesPerDay &&
                        this.TotalMatchesPlayed == objAsPlayerStatsJson.TotalMatchesPlayed &&
                        this.TotalMatchesWon == objAsPlayerStatsJson.TotalMatchesWon &&
                        this.UniqueServers == objAsPlayerStatsJson.UniqueServers);
            }
        }
        public class BestPlayerJson
        {
            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("killToDeathRatio")]
            public double KDR { get; set; }

            public override int GetHashCode()
            {
                return Name.GetHashCode();
            }
            public override bool Equals(object obj)
            {
                if (obj == null) return false;
                BestPlayerJson objAsBestPlayerJson = obj as BestPlayerJson;
                if (objAsBestPlayerJson == null) return false;
                return (this.Name == objAsBestPlayerJson.Name &&
                        this.KDR == objAsBestPlayerJson.KDR);
            }
        }

        #endregion
    }
}
