using System.Collections.Generic;
using System.Linq;
using Kontur.GameStats.Server.Data;
using System.Data;
using Newtonsoft.Json;

namespace Kontur.GameStats.Server.API
{
    public class ServerController
    {
        #region GET, PUT /servers/<endpoint>/info
        /// <summary>
        /// Puts server info and returns <see cref="ApiResponse"/> with empty body.
        /// </summary>
        /// <param name="endpoint">endpoint string from url</param>
        /// <param name="json">json-serialized <see cref="ServerInfoJson"/></param>
        /// <returns></returns>
        public ApiResponse PutInfo(string endpoint, string json)
        {
            var info = JsonConvert.DeserializeObject<ServerInfoJson>(json);
            return PutInfo(endpoint, info.Name, info.GameModes);
        }
        public ApiResponse PutInfo(string endpoint, ServerInfoJson serverInfoJson)
        {
            return PutInfo(endpoint, serverInfoJson.Name, serverInfoJson.GameModes);
        }
        public ApiResponse PutInfo(string endpoint, string name, List<string> gameModes)
        {
            using (var context = new GameStatsDbDataContext())
            {
                var allServers = context.Servers;
                var servers = allServers.Where(s => s.endpoint == endpoint);
                Servers newServer;
                if (servers.Count() > 0)
                {   // rewrite existing info
                    newServer = servers.First();
                    newServer.name = name;
                    // delete existing game modes links
                    var oldGameModesLinks = from gmLink in context.GameModesOnServers
                                            where gmLink.server_id == newServer.id
                                            select gmLink;
                    context.GameModesOnServers.DeleteAllOnSubmit(oldGameModesLinks);
                    context.SubmitChanges();
                }
                else
                {   // insert new server with same endpoint
                    newServer = new Servers()
                    {
                        endpoint = endpoint,
                        name = name
                    };
                    allServers.InsertOnSubmit(newServer);
                    context.SubmitChanges();
                }

                // link game modes and servers
                foreach (var newgm in gameModes)
                {   
                    var gm = addGameMode(newgm, context);
                    var link = addLinkGameModeOnServer(gm, newServer, context);
                }
                context.SubmitChanges();

                return new ApiResponse();
            }
        }
        /// <summary>
        /// Returns <see cref="ApiResponse"/> with string representation of <see cref="ServerInfoJson"/> in body.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        public ApiResponse GetInfo(string endpoint)
        {
            using (var context = new GameStatsDbDataContext())
            {
                var server = context.Servers.FirstOrDefault(s => s.endpoint == endpoint);
                if (server == null)
                    return new ApiResponse(System.Net.HttpStatusCode.NotFound);

                int serverId = server.id;
                var gameModes = (from gmOnServer in context.GameModesOnServers
                                 where gmOnServer.server_id == serverId
                                 join gameMode in context.GameModes
                                 on gmOnServer.gm_id equals gameMode.id
                                 select gameMode.name).ToList();
                ServerInfoJson serverInfo = new ServerInfoJson
                {
                    Name = server.name,
                    GameModes = gameModes.ToList()
                };
                return new ApiResponse(
                    body: JsonConvert.SerializeObject(serverInfo, Formatting.Indented));
            }
        }
        private GameModes addGameMode(string name, GameStatsDbDataContext context)
        {
            var gms = context.GameModes.Where(gm => gm.name == name);
            if (gms.Count() > 0)
            {
                return gms.First();
            }
            else
            {
                var newgm = new GameModes() { name = name };
                context.GameModes.InsertOnSubmit(newgm);
                context.SubmitChanges();
                return newgm;
            }
        }
        private GameModesOnServers addLinkGameModeOnServer(GameModes gm, Servers server, GameStatsDbDataContext context)
        {
            var links = context.GameModesOnServers
                               .Where(gmOnserv => gmOnserv.gm_id == gm.id &&
                                                  gmOnserv.server_id == server.id);
            if (links.Count() > 0)
            {   // link is created already
                return links.First();
            }
            else
            {
                // create link
                var link = new GameModesOnServers()
                {
                    gm_id = gm.id,
                    server_id = server.id
                };
                context.GameModesOnServers.InsertOnSubmit(link);
                context.SubmitChanges();
                return link;
            }
        }
        #endregion

        #region GET /servers/info
        /// <summary>
        /// Returns <see cref="ApiResponse"/> with string representation of List&lt;<see cref="ServersInfoRecordJson"/>&gt; in body.
        /// </summary>
        /// <returns></returns>
        public ApiResponse GetInfo()
        {
            using (var context = new GameStatsDbDataContext())
            {
                var servers = context.Servers;
                List<ServersInfoRecordJson> results = new List<ServersInfoRecordJson>();
                foreach (var server in servers)
                {
                    int serverId = server.id;
                    var gameModes = (from gmOnServer in context.GameModesOnServers
                                     where gmOnServer.server_id == serverId
                                     join gameMode in context.GameModes
                                     on gmOnServer.gm_id equals gameMode.id
                                     select gameMode.name).ToList();
                    ServerInfoJson serverInfo = new ServerInfoJson
                    {
                        Name      = server.name,
                        GameModes = gameModes
                    };

                    results.Add(new ServersInfoRecordJson
                    {
                        Endpoint = server.endpoint,
                        Info     = serverInfo
                    });
                }
                return new ApiResponse(
                    body: JsonConvert.SerializeObject(results));
            }
        }

        #endregion

        #region GET /servers/<endpoint>/stats

        /// <summary>
        /// Returns <see cref="ApiResponse"/> with string representation of <see cref="ServerStatsJson"/> in body.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        public ApiResponse GetStats(string endpoint)
        {
            using (var context = new GameStatsDbDataContext())
            {
                int server_id = (from server in context.Servers
                                 where server.endpoint == endpoint
                                 select server.id).FirstOrDefault();
                // server_id is identity (1,1), so if server_id is 0 <=> there is no server with such endpoint
                if (server_id == 0) 
                    return new ApiResponse(System.Net.HttpStatusCode.NotFound);

                var matchesOnCurrentServer =
                    context.Matches
                    .Where(match => match.server_id == server_id);

                ServerStatsJson result = new ServerStatsJson();
                result.TotalMatchesPlayed =
                    matchesOnCurrentServer.Count();
                if (result.TotalMatchesPlayed == 0)
                    // trivial stats
                    return new ApiResponse(
                        body: JsonConvert.SerializeObject(ServerStatsJson.Trivial()));

                var matchesPerDay =
                            (from match in matchesOnCurrentServer
                             group match by match.timestamp_day into matchGroup
                             //group by constant to use 2 aggregates
                             group matchGroup by 1 into matchGroup
                             select new
                             {
                                 maximum = matchGroup.Max
                                                      ( m => m.Count()),
                                 average = matchGroup.Average
                                                      ( m => m.Count())
                             })
                             .First();
                result.MaximumMatchesPerDay =
                    matchesPerDay.maximum;
                result.AverageMatchesPerDay =
                    matchesPerDay.average;

                result.MaximumPopulation = 
                    matchesOnCurrentServer
                    .Select(match => match.players_count)
                    .Max();
                result.AveragePopulation =
                    matchesOnCurrentServer
                    .Select(match => match.players_count)
                    .Average();

                result.Top5GameModes =
                    (from match in matchesOnCurrentServer
                     group match by match.gm_id into matchGroup
                     orderby matchGroup.Count() descending
                     join gm in context.GameModes
                     on matchGroup.Key equals gm.id
                     select gm.name)
                     .Take(5).ToList();
                result.Top5Maps =
                    (from match in matchesOnCurrentServer
                     group match by match.map_id into matchGroup
                     orderby matchGroup.Count() descending
                     join map in context.Maps
                     on matchGroup.Key equals map.id
                     select map.name)
                     .Take(5).ToList();
                return new ApiResponse(
                    body: JsonConvert.SerializeObject(result));
            }
        }
        #endregion

        #region GET /reports/popular-servers[/<count>]

        /// <summary>
        /// Returns <see cref="ApiResponse"/> with string representation of List&lt;<see cref="PopularServerJson"/>&gt; in body.
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        public ApiResponse GetPopularServers(int count)
        {
            using (var context = new GameStatsDbDataContext())
            {
                var popularServers =
                    (from serverDay in context.ServersByDay
                     group serverDay by serverDay.server_id into serverGroup
                     let avg = serverGroup.Average(sg => sg.matches_count)
                     orderby avg descending
                     join serverInfo in context.Servers
                     on serverGroup.Key equals serverInfo.id
                     select new PopularServerJson
                     {
                         Endpoint = serverInfo.endpoint,
                         Name = serverInfo.name,
                         avgMatches = avg
                     })
                     .Take(count)
                     .ToList();

                return new ApiResponse(
                    body: JsonConvert.SerializeObject(popularServers, Formatting.Indented));
            }
        }

        #endregion

        #region Api interfaces

        public class PopularServerJson
        {
            [JsonProperty("endpoint")]
            public string Endpoint { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("averageMatchesPerDay")]
            public double avgMatches { get; set; }
            public override int GetHashCode()
            {
                return Endpoint.GetHashCode();
            }
            public override bool Equals(object obj)
            {
                if (obj == null) return false;
                PopularServerJson objAsPopularServerJson = obj as PopularServerJson;
                if (objAsPopularServerJson == null) return false;
                return (this.Endpoint   == objAsPopularServerJson.Endpoint &&
                        this.avgMatches == objAsPopularServerJson.avgMatches &&
                        this.Name       == objAsPopularServerJson.Name);
            }
        }
        
        public class ServerInfoJson
        {
            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("gameModes")]
            public List<string> GameModes { get; set; }
            public override int GetHashCode()
            {
                return Name.GetHashCode();
            }
            public override bool Equals(object obj)
            {
                if (obj == null) return false;
                ServerInfoJson objAsServerInfoJson = obj as ServerInfoJson;
                if (objAsServerInfoJson == null)
                {
                    return false;
                }
                return this.Name            == objAsServerInfoJson.Name &&
                       this.GameModes.Count == objAsServerInfoJson.GameModes.Count &&
                       this.GameModes.OrderBy(s => s)
                           .SequenceEqual
                           (objAsServerInfoJson
                           .GameModes.OrderBy(s => s));
            }
        }
        public class ServersInfoRecordJson
        {
            [JsonProperty("endpoint")]
            public string Endpoint { get; set; }

            [JsonProperty("info")]
            public ServerInfoJson Info { get; set; }
            public override int GetHashCode()
            {
                return Endpoint.GetHashCode();
            }
            public override bool Equals(object obj)
            {
                if (obj == null) return false;
                ServersInfoRecordJson objAsServersInfoRecordJson = obj as ServersInfoRecordJson;
                if (objAsServersInfoRecordJson == null) return false;
                return (this.Endpoint == objAsServersInfoRecordJson.Endpoint &&
                        this.Info.Equals(objAsServersInfoRecordJson.Info));
            }
        }
        public class ServerStatsJson
        {
            [JsonProperty("totalMatchesPlayed")]
            public int TotalMatchesPlayed { get; set; }

            [JsonProperty("maximumMatchesPerDay")]
            public int MaximumMatchesPerDay { get; set; }
            
            [JsonProperty("averageMatchesPerDay")]
            public double AverageMatchesPerDay { get; set; }

            [JsonProperty("maximumPopulation")]
            public int MaximumPopulation { get; set; }

            [JsonProperty("averagePopulation")]
            public double AveragePopulation { get; set; }

            [JsonProperty("top5GameModes")]
            public List<string> Top5GameModes { get; set; }

            [JsonProperty("top5Maps")]
            public List<string> Top5Maps { get; set; }

            public static ServerStatsJson Trivial()
            {
                return new ServerStatsJson
                {
                    AverageMatchesPerDay = 0,
                    AveragePopulation = 0,
                    MaximumMatchesPerDay = 0,
                    MaximumPopulation = 0,
                    Top5GameModes = new List<string>(),
                    Top5Maps = new List<string>(),
                    TotalMatchesPlayed = 0
                };
            }
            public override int GetHashCode()
            {
                return TotalMatchesPlayed ^ MaximumPopulation;
            }
            public override bool Equals(object obj)
            {
                if (obj == null) return false;
                ServerStatsJson objAsServerStatsJson = obj as ServerStatsJson;
                if (objAsServerStatsJson == null) return false;
                return (this.AverageMatchesPerDay == objAsServerStatsJson.AverageMatchesPerDay &&
                        this.AveragePopulation    == objAsServerStatsJson.AveragePopulation &&
                        this.MaximumMatchesPerDay == objAsServerStatsJson.MaximumMatchesPerDay &&
                        this.MaximumPopulation    == objAsServerStatsJson.MaximumPopulation &&
                        this.TotalMatchesPlayed   == objAsServerStatsJson.TotalMatchesPlayed &&
                        this.Top5GameModes.SequenceEqual(objAsServerStatsJson.Top5GameModes) &&
                        this.Top5Maps.SequenceEqual(objAsServerStatsJson.Top5Maps));
            }
        }

        #endregion
    }
}