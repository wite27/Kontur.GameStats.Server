using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Kontur.GameStats.Server.Data;
using System.Net;

namespace Kontur.GameStats.Server.API
{
    public class MatchController
    {
        #region PUT /servers/<endpoint>/matches/<timestamp>
        /// <summary>
        /// Puts match info played on <paramref name="endpoint"/> server at <paramref name="timestamp"/>
        /// </summary>
        /// <param name="endpoint">endpoint <see cref="string"/> from url</param>
        /// <param name="json">json-serialized <see cref="MatchJson"/></param>
        /// <param name="timestamp">timestamp <see cref="DateTime"/> from url</param>
        /// <returns></returns>
        public ApiResponse PutMatch(string endpoint, string json, DateTime timestamp)
        {
            return PutMatch(endpoint, JsonConvert.DeserializeObject<MatchJson>(json),
                     timestamp);
        }
        public ApiResponse PutMatch(string endpoint, MatchJson match, DateTime timestamp)
        {
            using (var context = new GameStatsDbDataContext())
            {
                var server = context.Servers.FirstOrDefault(s => s.endpoint == endpoint);
                if (server == null)
                    // there was not advertise-request with such endpoint
                    return new ApiResponse(endpoint + " server is not announced.", HttpStatusCode.BadRequest);

                // get players' ids to use it later
                List<int> playersIds = match.Scoreboard.Select(pl => addPlayer(pl.Name, context)).ToList();

                // insert new match

                // check game mode
                // find its id by name
                int gm_id;
                var gm = context.GameModes
                        .FirstOrDefault(gm_ => gm_.name == match.GameMode);
                if (gm == null)
                {
                    return new ApiResponse("Unknown game mode: " + match.GameMode, HttpStatusCode.BadRequest);
                } else
                {
                    gm_id = gm.id;
                    // does the sever support this game mode?
                    bool isBadGm = (from gmOnServer in context.GameModesOnServers
                                    where gmOnServer.gm_id == gm_id && gmOnServer.server_id == server.id
                                    select gm_id)
                                    .Count() == 0;
                    if (isBadGm)
                    {
                        return new ApiResponse("Game mode " + match.GameMode + " is not supported on server: " + endpoint,
                                               HttpStatusCode.BadRequest);
                    }
                }
                Matches newMatch = new Matches
                {
                    server_id = server.id,
                    timestamp = timestamp,
                    map_id = addMap(match.Map, context),
                    gm_id = gm_id,
                    frag_limit = match.FragLimit,
                    time_limit = match.TimeLimit,
                    time_elapsed = match.TimeElapsed,
                    players_count = playersIds.Count(),
                    winner_id = playersIds.First()
                };
                // save
                context.Matches.InsertOnSubmit(newMatch);
                context.SubmitChanges();

                // insert scoreboard
                int matchId = newMatch.id;
                int rank = 1;
                var newScoreboardRecords =
                        match.Scoreboard
                             .Zip(playersIds,
                                    (player, playerId) => new PlayersInMatches
                                    {
                                        match_id = matchId,
                                        player_id = playerId,
                                        player_rank = rank++,
                                        frags = player.Frags,
                                        kills = player.Kills,
                                        deaths = player.Deaths
                                    });
                // save
                context.PlayersInMatches.InsertAllOnSubmit(newScoreboardRecords);
                context.SubmitChanges();

                return new ApiResponse();
            }
        }
        private int addPlayer(string name, GameStatsDbDataContext context)
        {
            var player = context.Players.FirstOrDefault(pl => pl.name == name);
            if (player == null)
            {
                // add new player
                Players newPlayer = new Players { name = name };
                context.Players.InsertOnSubmit(newPlayer);
                context.SubmitChanges();
                return newPlayer.id;
            }
            else
            {
                return player.id;
            }
        }
        private int addMap(string name, GameStatsDbDataContext context)
        {
            var map = context.Maps.FirstOrDefault(m => m.name == name);
            if (map == null)
            {
                // add new map
                Maps newMap = new Maps { name = name };
                context.Maps.InsertOnSubmit(newMap);
                context.SubmitChanges();
                return newMap.id;
            }
            else
            {
                return map.id;
            }
        }

        #endregion

        #region GET /servers/<endpoint>/matches/<timestamp>

        /// <summary>
        /// Returns <see cref="ApiResponse"/> with string representation of <see cref="MatchJson"/> in body.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="timestamp"></param>
        /// <returns></returns>
        public ApiResponse GetMatch(string endpoint, DateTime timestamp)
        {
            using (var context = new GameStatsDbDataContext())
            {
                var matchRecord = (from server in context.Servers
                                   where server.endpoint == endpoint
                                   join match in context.Matches
                                   on server.id equals match.server_id
                                   where match.timestamp == timestamp
                                   select match)
                                   .FirstOrDefault();
                if (matchRecord == null)
                    return new ApiResponse(HttpStatusCode.NotFound);

                var scoreboardRecords = (from playerInMatch in context.PlayersInMatches
                                         where playerInMatch.match_id == matchRecord.id
                                         orderby playerInMatch.player_rank
                                         select playerInMatch);

                List<ScoreboardRecordJson> _scoreboard = 
                    scoreboardRecords
                    .Select( record => new ScoreboardRecordJson
                             {
                                 Deaths = record.deaths,
                                 Frags  = record.frags,
                                 Kills  = record.kills,
                                 Name   = (from player in context.Players
                                           where player.id == record.player_id
                                           select player.name)
                                          .First()
                             }).ToList();

                MatchJson _match = new MatchJson
                {
                    FragLimit   = matchRecord.frag_limit,
                    Scoreboard  = _scoreboard,
                    TimeElapsed = matchRecord.time_elapsed,
                    TimeLimit   = matchRecord.time_limit,
                    GameMode    = (from gm in context.GameModes
                                   where gm.id == matchRecord.gm_id
                                   select gm.name)
                                   .First(),
                    Map         = (from map in context.Maps
                                   where map.id == matchRecord.map_id
                                   select map.name)
                                   .First()
                };
                return new ApiResponse(JsonConvert.SerializeObject(_match, Formatting.Indented));
            }
        }

        #endregion

        #region GET /reports/recent-matches[/<count>]

        /// <summary>
        /// Returns <see cref="ApiResponse"/> with string representation of List&lt;<see cref="RecentMatchJson"/>&gt; in body.
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        public ApiResponse GetRecentMatches(int count)
        {
            using (var context = new GameStatsDbDataContext())
            {
                var recentMatches =
                    (from match in context.Matches
                     orderby match.timestamp descending
                     select new RecentMatchJson
                     {
                         Server = (from server in context.Servers
                                   where server.id == match.server_id
                                   select server.endpoint)
                                   .First(),

                         Timestamp = DateTime.SpecifyKind(match.timestamp, DateTimeKind.Utc),

                         Results = new MatchJson
                         {
                             FragLimit   = match.frag_limit,
                             TimeElapsed = match.time_elapsed,
                             TimeLimit   = match.time_limit,

                             GameMode = (from gm in context.GameModes
                                         where gm.id == match.gm_id
                                         select gm.name)
                                         .First(),

                             Map = (from map in context.Maps
                                    where map.id == match.map_id
                                    select map.name)
                                    .First(),

                             Scoreboard = (from scoreboardRecord in context.PlayersInMatches
                                           where scoreboardRecord.match_id == match.id
                                           orderby scoreboardRecord.player_rank ascending
                                           select new ScoreboardRecordJson
                                           {
                                               Deaths = scoreboardRecord.deaths,
                                               Frags = scoreboardRecord.frags,
                                               Kills = scoreboardRecord.kills,
                                               Name = (from player in context.Players
                                                       where player.id == scoreboardRecord.player_id
                                                       select player.name)
                                                       .First()
                                           })
                                           .ToList()
                         }
                     })
                    .Take(count);

                return new ApiResponse(
                    JsonConvert.SerializeObject(recentMatches.ToList(), Formatting.Indented));
            }
        }
        #endregion

        #region Api interfaces

        public class MatchJson
        {
            [JsonProperty("map")]
            public string Map { get; set; }

            [JsonProperty("gameMode")]
            public string GameMode { get; set; }

            [JsonProperty("fragLimit")]
            public int FragLimit { get; set; }

            [JsonProperty("timeLimit")]
            public int TimeLimit { get; set; }

            [JsonProperty("timeElapsed")]
            public double TimeElapsed { get; set; }

            [JsonProperty("scoreboard")]
            public List<ScoreboardRecordJson> Scoreboard { get; set; }
            public override int GetHashCode()
            {
                return Map.GetHashCode();
            }
            public override bool Equals(object obj)
            {
                if (obj == null) return false;
                MatchJson objAsMatchJson = obj as MatchJson;
                if (objAsMatchJson == null) return false;
                return (this.Map         == objAsMatchJson.Map &&
                        this.GameMode    == objAsMatchJson.GameMode &&
                        this.FragLimit   == objAsMatchJson.FragLimit &&
                        this.TimeLimit   == objAsMatchJson.TimeLimit &&
                        this.TimeElapsed == objAsMatchJson.TimeElapsed &&
                        this.Scoreboard.SequenceEqual(objAsMatchJson.Scoreboard));
            }
        }
        public class ScoreboardRecordJson
        {
            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("frags")]
            public int Frags { get; set; }

            [JsonProperty("kills")]
            public int Kills { get; set; }

            [JsonProperty("deaths")]
            public int Deaths { get; set; }
            public override int GetHashCode()
            {
                return Name.GetHashCode() ^ Kills;
            }
            public override bool Equals(object obj)
            {
                if (obj == null) return false;
                ScoreboardRecordJson objAsScoreboardRecordJson = obj as ScoreboardRecordJson;
                if (objAsScoreboardRecordJson == null) return false;
                return (this.Name   == objAsScoreboardRecordJson.Name &&
                        this.Frags  == objAsScoreboardRecordJson.Frags &&
                        this.Kills  == objAsScoreboardRecordJson.Kills &&
                        this.Deaths == objAsScoreboardRecordJson.Deaths);
            }
        }
        public class RecentMatchJson
        {
            [JsonProperty("server")]
            public string Server { get; set; }

            [JsonProperty("timestamp")]
            public DateTime Timestamp { get; set; }

            [JsonProperty("results")]
            public MatchJson Results { get; set; }

            public override int GetHashCode()
            {
                return Timestamp.GetHashCode();
            }
            public override bool Equals(object obj)
            {
                if (obj == null) return false;
                RecentMatchJson objAsRecentMatchJson = obj as RecentMatchJson;
                if (objAsRecentMatchJson == null) return false;
                return (this.Server      == objAsRecentMatchJson.Server &&
                        this.Timestamp   == objAsRecentMatchJson.Timestamp &&
                        this.Results.Equals(objAsRecentMatchJson.Results));
            }
        }

        #endregion
    }
}
