using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Kontur.GameStats.Server.Data;
using Kontur.GameStats.Server.API;
using Newtonsoft.Json;
using System.Net;
using System.Collections.Generic;
using System.Linq;

namespace UnitTestProject
{
    [TestClass]
    public class ApiTest
    {
        static ServerController.ServerInfoJson staticServerInfo = new ServerController.ServerInfoJson
        {
            Name = "test_server",
            GameModes = new List<string> { "DM", "TDM" }
        };
        static string staticServerInfoJson = JsonConvert.SerializeObject(staticServerInfo);
        static string staticServerEndpoint = "test-server.com-8080";
        static DateTime staticTimestamp = Convert.ToDateTime("2017-01-22T15:11:12Z");

        [TestMethod]
        public void PutServerInfoTest()
        {
            // arrange
            //// using static fields

            // act
            ApiResponse responce = new ServerController().PutInfo("123.4.5.6-8080", staticServerInfoJson);

            // assert
            Assert.AreEqual(HttpStatusCode.OK, responce.Status);
        }

        [TestMethod]
        public void PutAndGetServerInfoTest()
        {
            // arrange
            //// using static fields
            PutServerInfoTest();

            // act
            ApiResponse getResponce = new ServerController().GetInfo("123.4.5.6-8080");
            ServerController.ServerInfoJson serverInfo_ =
                JsonConvert.DeserializeObject<ServerController.ServerInfoJson>(getResponce.Body);

            // assert
            Assert.AreEqual(HttpStatusCode.OK, getResponce.Status);
            Assert.AreEqual(staticServerInfo, serverInfo_);
        }

        #region Match
        static MatchController.MatchJson staticMatch = new MatchController.MatchJson
        {
            FragLimit = 20,
            GameMode = "DM",
            Map = "test_map",
            TimeElapsed = 15.15,
            TimeLimit = 20,
            Scoreboard = new List<MatchController.ScoreboardRecordJson>
                {
                    new MatchController.ScoreboardRecordJson
                    {
                        Name = "test_player_1",
                        Deaths = 1,
                        Frags = 20,
                        Kills = 20
                    },
                    new MatchController.ScoreboardRecordJson
                    {
                        Name = "test_player_2",
                        Deaths = 10,
                        Frags = 10,
                        Kills = 10
                    },
                    new MatchController.ScoreboardRecordJson
                    {
                        Name = "test_player_3",
                        Deaths = 20,
                        Frags = 0,
                        Kills = 0
                    }
                }
        };
        static string staticMatchJson = JsonConvert.SerializeObject(staticMatch);
        #endregion

        [TestMethod]
        public void PutMatchTest()
        {
            // arrange
            //// using static fields
            using (var context = new GameStatsDbDataContext())
            {
                context.ExecuteCommand("DELETE FROM Matches");
            }
            PutServerInfoTest();

            // act
            ApiResponse putMatchResponce = new MatchController().PutMatch("123.4.5.6-8080", staticMatchJson, staticTimestamp);
            
            // assert
            Assert.AreEqual(HttpStatusCode.OK, putMatchResponce.Status);
        }

        [TestMethod]
        public void PutAndGetMatchTest()
        {
            // arrange
            PutMatchTest();

            // act
            ApiResponse getMatchResponce = new MatchController().GetMatch("123.4.5.6-8080", staticTimestamp);
            MatchController.MatchJson match_ =
                JsonConvert.DeserializeObject<MatchController.MatchJson>(getMatchResponce.Body);

            // assert
            Assert.AreEqual(staticMatch, match_);
        }

        [TestMethod]
        public void GetServersInfo()
        {
            // arrange
            using (var context = new GameStatsDbDataContext())
            {
                context.ExecuteCommand("DELETE FROM Servers");
            }

            ServerController.ServerInfoJson server1 = new ServerController.ServerInfoJson
            {
                Name = "test_server_1",
                GameModes = new List<string> { "DM", "TDM" }
            };
            string server1json = JsonConvert.SerializeObject(server1);
            ServerController.ServerInfoJson server2 = new ServerController.ServerInfoJson
            {
                Name = "test_server_2",
                GameModes = new List<string> { "DM", "TDM", "CTF" }
            };
            string server2json = JsonConvert.SerializeObject(server2);
            ServerController.ServerInfoJson server3 = new ServerController.ServerInfoJson
            {
                Name = "test_server_3",
                GameModes = new List<string> { "CTF", "TDM" }
            };
            string server3json = JsonConvert.SerializeObject(server3);
            ServerController.ServersInfoRecordJson server1record = new ServerController.ServersInfoRecordJson
            {
                Endpoint = "server1.com-8080",
                Info = server1
            };
            ServerController.ServersInfoRecordJson server2record = new ServerController.ServersInfoRecordJson
            {
                Endpoint = "server2.com-8080",
                Info = server2
            };
            ServerController.ServersInfoRecordJson server3record = new ServerController.ServersInfoRecordJson
            {
                Endpoint = "server3.com-8080",
                Info = server3
            };
            // act
            ApiResponse putResponse1 = new ServerController().PutInfo("server1.com-8080", server1json);
            ApiResponse putResponse2 = new ServerController().PutInfo("server2.com-8080", server2json);
            ApiResponse putResponse3 = new ServerController().PutInfo("server3.com-8080", server3json);
            Assert.AreEqual(HttpStatusCode.OK, putResponse1.Status);
            Assert.AreEqual(HttpStatusCode.OK, putResponse2.Status);
            Assert.AreEqual(HttpStatusCode.OK, putResponse3.Status);

            ApiResponse getResponse = new ServerController().GetInfo();
            List<ServerController.ServersInfoRecordJson> servers =
                JsonConvert.DeserializeObject<List<ServerController.ServersInfoRecordJson>>(getResponse.Body);

            Assert.AreEqual(3, servers.Count);
            CollectionAssert.AreEqual(
                new List<ServerController.ServersInfoRecordJson> { server1record, server2record, server3record },
                servers.OrderBy(s => s.Endpoint).ToList());
            // assert
            Assert.AreEqual(HttpStatusCode.OK,  getResponse.Status);
        }

        [TestMethod]
        public void GetServerStats()
        {
            // arrange
            using (var context = new GameStatsDbDataContext())
            {
                context.ExecuteCommand("DELETE FROM Servers");
                context.ExecuteCommand("DELETE FROM Matches");
            }
            ServerController.ServerInfoJson server = new ServerController.ServerInfoJson
            {
                Name = "test_server",
                GameModes = new List<string> { "DM", "TDM" }
            };
            string serverJson = JsonConvert.SerializeObject(server);
            
            MatchController.MatchJson match1 = new MatchController.MatchJson
            {
                FragLimit = 20,
                GameMode = "DM",
                Map = "test_map",
                TimeElapsed = 10,
                TimeLimit = 20,
                Scoreboard = new List<MatchController.ScoreboardRecordJson>
                {
                    new MatchController.ScoreboardRecordJson
                    {
                        Deaths = 5,
                        Frags = 5,
                        Kills = 5,
                        Name = "test_player_1"
                    },
                    new MatchController.ScoreboardRecordJson
                    {
                        Deaths = 5,
                        Frags = 5,
                        Kills = 5,
                        Name = "test_player_2"
                    },
                    new MatchController.ScoreboardRecordJson
                    {
                        Deaths = 5,
                        Frags = 5,
                        Kills = 5,
                        Name = "test_player_3"
                    },
                    new MatchController.ScoreboardRecordJson
                    {
                        Deaths = 5,
                        Frags = 5,
                        Kills = 5,
                        Name = "test_player_4"
                    }
                }
            };
            string match1json = JsonConvert.SerializeObject(match1);
            MatchController.MatchJson match2 = new MatchController.MatchJson
            {
                FragLimit = 20,
                GameMode = "DM",
                Map = "test_map",
                TimeElapsed = 10,
                TimeLimit = 20,
                Scoreboard = new List<MatchController.ScoreboardRecordJson>
                {
                    new MatchController.ScoreboardRecordJson
                    {
                        Deaths = 5,
                        Frags = 5,
                        Kills = 5,
                        Name = "test_player_1"
                    },
                    new MatchController.ScoreboardRecordJson
                    {
                        Deaths = 5,
                        Frags = 5,
                        Kills = 5,
                        Name = "test_player_2"
                    }
                }
            };
            string match2json = JsonConvert.SerializeObject(match2);
            DateTime timestamp1 = Convert.ToDateTime("2017-01-22T15:11:12Z");
            DateTime timestamp2 = timestamp1.AddHours(1);

            // expected answer
            var expectedStats = new ServerController.ServerStatsJson
            {
                AverageMatchesPerDay = 2, // match1 and match2 played in 1 day
                AveragePopulation = (match1.Scoreboard.Count + match2.Scoreboard.Count) / 2.0,
                MaximumMatchesPerDay = 2,
                MaximumPopulation = Math.Max(match1.Scoreboard.Count, match2.Scoreboard.Count),
                Top5GameModes = new List<string> { "DM" },
                Top5Maps = new List<string> { "test_map" },
                TotalMatchesPlayed = 2
            };

            // act
            ApiResponse putServerResponse = new ServerController().PutInfo("test-server.com-1337", serverJson);
            ApiResponse putMatch1Response = new MatchController().PutMatch("test-server.com-1337", match1json, timestamp1);
            ApiResponse putMatch2Response = new MatchController().PutMatch("test-server.com-1337", match2json, timestamp2);
            ApiResponse getStatsResponse  = new ServerController().GetStats("test-server.com-1337");

            var actualStats = JsonConvert.DeserializeObject<ServerController.ServerStatsJson>(getStatsResponse.Body);
            
            // assert
            Assert.AreEqual(HttpStatusCode.OK, putServerResponse.Status);
            Assert.AreEqual(HttpStatusCode.OK, putMatch1Response.Status);
            Assert.AreEqual(HttpStatusCode.OK, putMatch2Response.Status);
            Assert.AreEqual(HttpStatusCode.OK,  getStatsResponse.Status);
            Assert.AreEqual(expectedStats, actualStats);
        }

        [TestMethod]
        public void GetPlayerStats()
        {
            // arrange
            using (var context = new GameStatsDbDataContext())
            {
                context.ExecuteCommand("DELETE FROM Servers");
                context.ExecuteCommand("DELETE FROM Matches");
                context.ExecuteCommand("DELETE FROM Players");
            }
            ServerController.ServerInfoJson server1 = new ServerController.ServerInfoJson
            {
                Name = "test_server_1",
                GameModes = new List<string> { "DM", "TDM" }
            };
            string server1json = JsonConvert.SerializeObject(server1);

            ServerController.ServerInfoJson server2 = new ServerController.ServerInfoJson
            {
                Name = "test_server_2",
                GameModes = new List<string> { "DM", "TDM" }
            };
            string server2json = JsonConvert.SerializeObject(server2);

            MatchController.MatchJson match1 = new MatchController.MatchJson
            {
                FragLimit = 20,
                GameMode = "DM",
                Map = "test_map",
                TimeElapsed = 10,
                TimeLimit = 20,
                Scoreboard = new List<MatchController.ScoreboardRecordJson>
                {
                    new MatchController.ScoreboardRecordJson
                    {
                        Deaths = 1,
                        Frags = 10,
                        Kills = 10,
                        Name = "test_player"
                    },
                    new MatchController.ScoreboardRecordJson
                    {
                        Deaths = 5,
                        Frags = 5,
                        Kills = 5,
                        Name = "dummy_player_1"
                    },
                    new MatchController.ScoreboardRecordJson
                    {
                        Deaths = 5,
                        Frags = 5,
                        Kills = 5,
                        Name = "dummy_player_2"
                    },
                    new MatchController.ScoreboardRecordJson
                    {
                        Deaths = 5,
                        Frags = 5,
                        Kills = 5,
                        Name = "dummy_player_3"
                    }
                }
            };
            string match1json = JsonConvert.SerializeObject(match1);

            MatchController.MatchJson match2 = new MatchController.MatchJson
            {
                FragLimit = 20,
                GameMode = "DM",
                Map = "test_map",
                TimeElapsed = 10,
                TimeLimit = 20,
                Scoreboard = new List<MatchController.ScoreboardRecordJson>
                {
                    new MatchController.ScoreboardRecordJson
                    {
                        Deaths = 1,
                        Frags = 10,
                        Kills = 10,
                        Name = "test_player"
                    },
                    new MatchController.ScoreboardRecordJson
                    {
                        Deaths = 5,
                        Frags = 5,
                        Kills = 5,
                        Name = "dummy_player_1"
                    },
                    new MatchController.ScoreboardRecordJson
                    {
                        Deaths = 5,
                        Frags = 5,
                        Kills = 5,
                        Name = "dummy_player_2"
                    },
                    new MatchController.ScoreboardRecordJson
                    {
                        Deaths = 5,
                        Frags = 5,
                        Kills = 5,
                        Name = "dummy_player_3"
                    }
                }
            };
            string match2json = JsonConvert.SerializeObject(match2);

            MatchController.MatchJson match3 = new MatchController.MatchJson
            {
                FragLimit = 20,
                GameMode = "TDM", // 1 TDM, 3 others DM
                Map = "test_map",
                TimeElapsed = 10,
                TimeLimit = 20,
                Scoreboard = new List<MatchController.ScoreboardRecordJson>
                {
                    new MatchController.ScoreboardRecordJson
                    {
                        Deaths = 1,
                        Frags = 5,
                        Kills = 15,
                        Name = "dummy_player_1"
                    },
                    new MatchController.ScoreboardRecordJson
                    {
                        Deaths = 2,
                        Frags = 5,
                        Kills = 5,
                        Name = "test_player"
                    },
                    new MatchController.ScoreboardRecordJson
                    {
                        Deaths = 5,
                        Frags = 5,
                        Kills = 5,
                        Name = "dummy_player_3"
                    }
                }
            };
            string match3json = JsonConvert.SerializeObject(match3);

            MatchController.MatchJson match4 = new MatchController.MatchJson
            {
                FragLimit = 20,
                GameMode = "DM",
                Map = "test_map",
                TimeElapsed = 10,
                TimeLimit = 20,
                Scoreboard = new List<MatchController.ScoreboardRecordJson>
                {
                    new MatchController.ScoreboardRecordJson
                    {
                        Deaths = 1,
                        Frags = 5,
                        Kills = 15,
                        Name = "dummy_player_1"
                    },
                    new MatchController.ScoreboardRecordJson
                    {
                        Deaths = 2,
                        Frags = 5,
                        Kills = 5,
                        Name = "test_player"
                    },
                    new MatchController.ScoreboardRecordJson
                    {
                        Deaths = 5,
                        Frags = 5,
                        Kills = 5,
                        Name = "dummy_player_3"
                    }
                }
            };
            string match4json = JsonConvert.SerializeObject(match4);
            
            DateTime day1timestamp = Convert.ToDateTime("2017-01-22T15:11:12Z");
            DateTime day2timestamp = day1timestamp.AddDays(1);
            var expectedStats = new PlayerController.PlayerStatsJson
            {
                AverageMatchesPerDay = (3 + 1) / 2,
                AverageScoreboardPercent = (100 + 100 + 50 + 50) / 4.0,
                FavoriteGameMode = "DM",
                FavoriteServer = "test-server-1.com-8080",
                KillToDeathRatio = (double)(10 + 10 + 5 + 5) / (1 + 1 + 2 + 2),
                LastMatchPlayed = day2timestamp,
                MaximumMatchesPerDay = 3,
                TotalMatchesPlayed = 4,
                TotalMatchesWon = 2,
                UniqueServers = 2
            };
            // act
            // prepare  servers
            ApiResponse putServer1Response = new ServerController().PutInfo("test-server-1.com-8080", server1json);
            ApiResponse putServer2Response = new ServerController().PutInfo("test-server-2.com-8080", server2json);
            Assert.AreEqual(HttpStatusCode.OK, putServer1Response.Status);
            Assert.AreEqual(HttpStatusCode.OK, putServer2Response.Status);
            // prepare matches
            ApiResponse putMatch1Response = new MatchController().PutMatch("test-server-1.com-8080", match1json, day1timestamp);
            ApiResponse putMatch2Response = new MatchController().PutMatch("test-server-1.com-8080", match2json, day2timestamp);
            ApiResponse putMatch3Response = new MatchController().PutMatch("test-server-1.com-8080", match3json, day1timestamp.AddHours(1));
            ApiResponse putMatch4Response = new MatchController().PutMatch("test-server-2.com-8080", match4json, day1timestamp.AddHours(2));
            Assert.AreEqual(HttpStatusCode.OK, putMatch1Response.Status);
            Assert.AreEqual(HttpStatusCode.OK, putMatch2Response.Status);
            Assert.AreEqual(HttpStatusCode.OK, putMatch3Response.Status);
            Assert.AreEqual(HttpStatusCode.OK, putMatch4Response.Status);
            // get players stats
            ApiResponse getStatsResponse = new PlayerController().GetStats("test_player");
            PlayerController.PlayerStatsJson actualStats =
                JsonConvert.DeserializeObject<PlayerController.PlayerStatsJson>(getStatsResponse.Body);

            // assert
            Assert.AreEqual(expectedStats, actualStats);
        }

        [TestMethod]
        public void GetRecentMatchesTest()
        {
            // arrange
            using (var context = new GameStatsDbDataContext())
            {
                context.ExecuteCommand("DELETE FROM Servers");
                context.ExecuteCommand("DELETE FROM Matches");
                context.ExecuteCommand("DELETE FROM Players");
                context.ExecuteCommand("DELETE FROM Maps");
                context.ExecuteCommand("DELETE FROM GameModes");
            }
            ServerController.ServerInfoJson server1 = new ServerController.ServerInfoJson
            {
                Name = "test_server_1",
                GameModes = new List<string> { "DM", "TDM" }
            };
            string server1json = JsonConvert.SerializeObject(server1);

            ServerController.ServerInfoJson server2 = new ServerController.ServerInfoJson
            {
                Name = "test_server_2",
                GameModes = new List<string> { "DM", "TDM" }
            };
            string server2json = JsonConvert.SerializeObject(server2);

            
            MatchController.MatchJson match1 = new MatchController.MatchJson
            {
                FragLimit = 20,
                GameMode = "DM",
                Map = "test_map_1",
                TimeElapsed = 10,
                TimeLimit = 20,
                Scoreboard = new List<MatchController.ScoreboardRecordJson>
                {
                    new MatchController.ScoreboardRecordJson
                    {
                        Deaths = 5,
                        Frags = 5,
                        Kills = 5,
                        Name = "dummy_player_1"
                    },
                    new MatchController.ScoreboardRecordJson
                    {
                        Deaths = 5,
                        Frags = 5,
                        Kills = 5,
                        Name = "dummy_player_2"
                    },
                    new MatchController.ScoreboardRecordJson
                    {
                        Deaths = 5,
                        Frags = 5,
                        Kills = 5,
                        Name = "dummy_player_3"
                    }
                }
            };
            string match1json = JsonConvert.SerializeObject(match1);

            MatchController.MatchJson match2 = new MatchController.MatchJson
            {
                FragLimit = 20,
                GameMode = "TDM",
                Map = "test_map_2",
                TimeElapsed = 10,
                TimeLimit = 20,
                Scoreboard = new List<MatchController.ScoreboardRecordJson>
                {
                    new MatchController.ScoreboardRecordJson
                    {
                        Deaths = 5,
                        Frags = 5,
                        Kills = 5,
                        Name = "dummy_player_4"
                    },
                    new MatchController.ScoreboardRecordJson
                    {
                        Deaths = 5,
                        Frags = 5,
                        Kills = 5,
                        Name = "dummy_player_5"
                    },
                    new MatchController.ScoreboardRecordJson
                    {
                        Deaths = 5,
                        Frags = 5,
                        Kills = 5,
                        Name = "dummy_player_6"
                    }
                }
            };
            string match2json = JsonConvert.SerializeObject(match2);

            // act
            // put servers
            ApiResponse putServer1response = new ServerController().PutInfo("test-1.com-8080", server1json);
            Assert.AreEqual(HttpStatusCode.OK, putServer1response.Status);
            ApiResponse putServer2response = new ServerController().PutInfo("test-2.com-8080", server2json);
            Assert.AreEqual(HttpStatusCode.OK, putServer1response.Status);

            // put matches
            const int totalMatches  = 20;
            const int requiredCount = 10;
            string[] endpoints  = new string[] { "test-1.com-8080", "test-2.com-8080" };
            MatchController.MatchJson[] matches = new MatchController.MatchJson[] { match1, match2 };
            string[] matchJsons = new string[] { match1json, match2json };
            DateTime timestamp = Convert.ToDateTime("2017-01-22T15:11:12Z");

            // these matches will be not shown in recent matches
            for (int i = 0; i < totalMatches - requiredCount; i++)
            {
                ApiResponse putMatchResponse = new MatchController().PutMatch(endpoints[i % 2], matchJsons[i % 2], timestamp);
                Assert.AreEqual(HttpStatusCode.OK, putMatchResponse.Status);

                timestamp = timestamp.AddHours(1);
            }

            // and these will be
            var expectedMatches = new List<MatchController.RecentMatchJson>();
            for (int i = 0; i < requiredCount; i++)
            {
                var recentMatch = new MatchController.RecentMatchJson
                {
                    Results = matches[i % 2],
                    Server = endpoints[i % 2],
                    Timestamp = timestamp
                };

                expectedMatches.Add(recentMatch);

                ApiResponse putMatchResponse = new MatchController().PutMatch(endpoints[i % 2], matchJsons[i % 2], timestamp);
                Assert.AreEqual(HttpStatusCode.OK, putMatchResponse.Status);

                timestamp = timestamp.AddHours(1);
            }
            // reverse expected matches to order by timestamp as in response
            expectedMatches.Reverse();

            ApiResponse getRecentMatchesResponse = new MatchController().GetRecentMatches(requiredCount);
            Assert.AreEqual(HttpStatusCode.OK, getRecentMatchesResponse.Status);

            var actualMatches =
                JsonConvert.DeserializeObject<List<MatchController.RecentMatchJson>>(getRecentMatchesResponse.Body);
            
            // assert
            CollectionAssert.AreEqual(expectedMatches, actualMatches);
        }

        [TestMethod]
        public void GetPopularServersTest()
        {
            // arrange
            using (var context = new GameStatsDbDataContext())
            {
                context.ExecuteCommand("DELETE FROM Servers");
                context.ExecuteCommand("DELETE FROM Matches");
                context.ExecuteCommand("DELETE FROM Players");
                context.ExecuteCommand("DELETE FROM Maps");
                context.ExecuteCommand("DELETE FROM GameModes");
                context.ExecuteCommand("DELETE FROM ServersByDay");
            }

            const int serversCount = 5;
            for (int i = 0; i < serversCount; i++)
            {
                var server = new ServerController.ServerInfoJson
                {
                    GameModes = new List<string> { "DM" },
                    Name = "test_server_" + i.ToString()                    
                };
                ApiResponse putServerResponse = new ServerController().PutInfo(
                    endpoint: String.Format("test-server-{0}.com-8080", i),
                    json: JsonConvert.SerializeObject(server));
                Assert.AreEqual(HttpStatusCode.OK, putServerResponse.Status);
            }

            var match = new MatchController.MatchJson
            {
                FragLimit = 20,
                GameMode = "DM",
                Map = "test_map",
                TimeElapsed = 10,
                TimeLimit = 20,
                Scoreboard = new List<MatchController.ScoreboardRecordJson>
                {
                    new MatchController.ScoreboardRecordJson
                    {
                        Deaths = 5,
                        Frags = 5,
                        Kills = 5,
                        Name = "dummy_player_1"
                    },
                    new MatchController.ScoreboardRecordJson
                    {
                        Deaths = 5,
                        Frags = 5,
                        Kills = 5,
                        Name = "dummy_player_2"
                    }
                }
            };
            string matchJson = JsonConvert.SerializeObject(match);

            const int daysCount = 5;
            int[,] matrix = new int[serversCount, daysCount];

            matrix = new int[,]
            {
                { 1, 1, 1, 1, 1},   // 5/5  = 1
                { 2, 2, 2, 2, 2},   // 10/5 = 2
                { 10, 10, 3, 2, 1}, // 25/5 = 5
                { 6, 3, 7, 2, 4},   // 20/5 = 4
                { 3, 3, 3, 3, 3},  //  15/5 = 3
            };
            int[] answerVector = { 2, 3, 4, 1, 0 };

            // put matches
            DateTime startTimestamp = Convert.ToDateTime("2017-01-22T15:11:12Z");
            for (int i = 0; i < serversCount; i++)
            {
                for (int j = 0; j < daysCount; j++)
                {
                    for (int c = 0; c < matrix[i, j]; c++)
                    {
                        ApiResponse putMatchResponse = new MatchController().PutMatch(
                            endpoint: String.Format("test-server-{0}.com-8080", i),
                            json: matchJson,
                            timestamp: startTimestamp.AddDays(j).AddMinutes(c)); // add minutes to do unique (server, timestamp)
                        Assert.AreEqual(HttpStatusCode.OK, putMatchResponse.Status);
                    }
                }
            }

            // act
            ApiResponse getPopularServersResponse = new ServerController().GetPopularServers(3);
            Assert.AreEqual(HttpStatusCode.OK, getPopularServersResponse.Status);

            List<ServerController.PopularServerJson> actualPopularServers =
                JsonConvert.DeserializeObject<List<ServerController.PopularServerJson>>(getPopularServersResponse.Body);
            var expectesPopularServers = new List<ServerController.PopularServerJson>
            {
                new ServerController.PopularServerJson
                {
                    avgMatches = 5,
                    Endpoint = "test-server-2.com-8080",
                    Name = "test_server_2"
                },
                new ServerController.PopularServerJson
                {
                    avgMatches = 4,
                    Endpoint = "test-server-3.com-8080",
                    Name = "test_server_3"
                },
                new ServerController.PopularServerJson
                {
                    avgMatches = 3,
                    Endpoint = "test-server-4.com-8080",
                    Name = "test_server_4"
                }
            };

            // assert
            CollectionAssert.AreEqual(expectesPopularServers, actualPopularServers);
        }

        [TestMethod]
        public void GetBestPlayersTest()
        {
            // arrange
            using (var context = new GameStatsDbDataContext())
            {
                context.ExecuteCommand("DELETE FROM Servers");
                context.ExecuteCommand("DELETE FROM Matches");
                context.ExecuteCommand("DELETE FROM Players");
                context.ExecuteCommand("DELETE FROM Maps");
                context.ExecuteCommand("DELETE FROM GameModes");
                context.ExecuteCommand("DELETE FROM ServersByDay");
                context.ExecuteCommand("DELETE FROM PlayersInMatches");
            }

            // put server
            ServerController.ServerInfoJson server = new ServerController.ServerInfoJson
            {
                Name = "test_server_1",
                GameModes = new List<string> { "DM", "TDM" }
            };
            string serverJson = JsonConvert.SerializeObject(server);
            ApiResponse putServerResponse = new ServerController().PutInfo("test-server.com-8080", serverJson);
            Assert.AreEqual(HttpStatusCode.OK, putServerResponse.Status);

            // put some matches
            var match = new MatchController.MatchJson
            {
                FragLimit = 100,
                GameMode = "DM",
                Map = "test_map",
                TimeElapsed = 10,
                TimeLimit = 20,
                Scoreboard = new List<MatchController.ScoreboardRecordJson>
                    {
                        new MatchController.ScoreboardRecordJson
                        {
                            Deaths = 0,
                            Frags = 100,
                            Kills = 100,
                            Name = "test_player_1"
                        },
                        new MatchController.ScoreboardRecordJson
                        {
                            Deaths = 1,
                            Frags = 10,
                            Kills = 10,
                            Name = "test_player_2"
                        },
                        new MatchController.ScoreboardRecordJson
                        {
                            Deaths = 10,
                            Frags = 1,
                            Kills = 1,
                            Name = "test_player_3"
                        },
                        new MatchController.ScoreboardRecordJson
                        {
                            Deaths = 1,
                            Frags = 0,
                            Kills = 0,
                            Name = "test_player_4"
                        }
                    }
            };
            string matchJson = JsonConvert.SerializeObject(match);
            DateTime timestamp = Convert.ToDateTime("2017-01-22T15:11:12Z");
            for (int i = 0; i < 10; i++)
            {
                ApiResponse putMatchResponse = new MatchController().PutMatch("test-server.com-8080", matchJson, timestamp.AddMinutes(i));
                Assert.AreEqual(HttpStatusCode.OK, putMatchResponse.Status);
            }

            // act
            // ignore test_player_1 with 0 deaths
            var expectedBestPlayers = new List<PlayerController.BestPlayerJson>
            {
                new PlayerController.BestPlayerJson
                {
                    Name = "test_player_2",
                    KDR = 10
                },
                new PlayerController.BestPlayerJson
                {
                    Name = "test_player_3",
                    KDR = 0.1
                },
                new PlayerController.BestPlayerJson
                {
                    Name = "test_player_4",
                    KDR = 0
                },
            };

            ApiResponse getBestPlayersResponse = new PlayerController().GetBestPlayers(5);
            Assert.AreEqual(HttpStatusCode.OK, getBestPlayersResponse.Status);

            List<PlayerController.BestPlayerJson> actualBestPlayers =
                JsonConvert.DeserializeObject<List<PlayerController.BestPlayerJson>>(getBestPlayersResponse.Body);

            // assert
            CollectionAssert.AreEqual(expectedBestPlayers, actualBestPlayers);
        }

        [TestMethod]
        public void GetNonExistingServer()
        {
            // arrange
            using (var context = new GameStatsDbDataContext())
            {
                context.ExecuteCommand("DELETE FROM Servers");
            }

            // act
            ApiResponse getServerInfoResponse  = new ServerController().GetInfo("non-existing-endpoint");
            ApiResponse getServerStatsResponse = new ServerController().GetStats("non-existing-endpoint");

            // assert
            Assert.AreEqual(HttpStatusCode.NotFound, getServerInfoResponse.Status);
            Assert.AreEqual(HttpStatusCode.NotFound, getServerStatsResponse.Status);
        }

        [TestMethod]
        public void GetNonExistingMatch()
        {
            // arrange
            using (var context = new GameStatsDbDataContext())
            {
                context.ExecuteCommand("DELETE FROM Servers");
                context.ExecuteCommand("DELETE FROM Matches");
            }

            ApiResponse putServerResponse = new ServerController().PutInfo(staticServerEndpoint, staticServerInfoJson);
            Assert.AreEqual(HttpStatusCode.OK, putServerResponse.Status);

            // act
            // get stats from server having no matches played on it
            ApiResponse getServerStats = new ServerController().GetStats(staticServerEndpoint);
            Assert.AreEqual(HttpStatusCode.OK, getServerStats.Status);

            ServerController.ServerStatsJson expectedStats = ServerController.ServerStatsJson.Trivial();
            ServerController.ServerStatsJson actualStats =
                JsonConvert.DeserializeObject<ServerController.ServerStatsJson>(getServerStats.Body);
            // assert
            Assert.AreEqual(expectedStats, actualStats);

            // act
            // get match from non-existing server
            ApiResponse getMatchInfo = new MatchController().GetMatch("non-existing-endpoint", staticTimestamp);
            // assert
            Assert.AreEqual(HttpStatusCode.NotFound, getMatchInfo.Status);

            // act
            // get non-existing match from existing server
            ApiResponse getMatchInfoFromExistingServer = 
                new MatchController().GetMatch(staticServerEndpoint, staticTimestamp);
            // assert
            Assert.AreEqual(HttpStatusCode.NotFound, getMatchInfoFromExistingServer.Status);


            // act
            // get recent matches
            ApiResponse getRecentMatches = new MatchController().GetRecentMatches(10);
            var expectedRecentMatches = new List<MatchController.RecentMatchJson>();
            var actualRecentMatches =
                JsonConvert.DeserializeObject<List<MatchController.RecentMatchJson>>(getRecentMatches.Body);
            Assert.AreEqual(HttpStatusCode.OK, getRecentMatches.Status);
            CollectionAssert.AreEqual(expectedRecentMatches, actualRecentMatches);
        }

        [TestMethod]
        public void GetNonExistingPlayerStats()
        {
            // arrange
            using (var context = new GameStatsDbDataContext())
            {
                context.ExecuteCommand("DELETE FROM Servers");
                context.ExecuteCommand("DELETE FROM Matches");
                context.ExecuteCommand("DELETE FROM Players");
            }

            ApiResponse putServerResponse = new ServerController().PutInfo(staticServerEndpoint, staticServerInfoJson);
            Assert.AreEqual(HttpStatusCode.OK, putServerResponse.Status);

            ApiResponse putMatchResponse = new MatchController().PutMatch(staticServerEndpoint, staticMatchJson, staticTimestamp);
            Assert.AreEqual(HttpStatusCode.OK, putMatchResponse.Status);

            // act
            ApiResponse getPlayerStats = new PlayerController().GetStats("non_existing_player");

            // assert
            Assert.AreEqual(HttpStatusCode.NotFound, getPlayerStats.Status);
        }
    }
}
