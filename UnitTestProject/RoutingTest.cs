using Microsoft.VisualStudio.TestTools.UnitTesting;
using Kontur.GameStats.Server.Routing;
using Kontur.GameStats.Server.API;
using System;
using System.Net;

namespace UnitTestProject
{
    [TestClass]
    public class RoutingTest
    {
        [TestMethod]
        public void RoutingTestMethod()
        {
            // arrange
            RoutingTree routingTree = new RoutingTree();
            string endpoint = "123.4.5.6:7070";
            string timestampString = "2017-01-22T15:11:12Z";
            DateTime timestamp = DateTime.Parse(timestampString).ToUniversalTime();
            string name = "player1";
            string requestBody = "someBody";

            int[,] countMatrix = new int[,]
            {
                { -1, 0 },
                { 100, 50 },
                { 27, 27 }
            };
            routingTree.AddRule(
                "PUT/servers/?endpoint/info",
                param => 
                {
                    Assert.AreEqual(endpoint, param["?endpoint"]);
                    Assert.AreEqual(requestBody, param["requestBody"]);
                    return new ApiResponse();
                });

            routingTree.AddRule(
                "PUT/servers/?endpoint/matches/?timestamp",
                param =>
                {
                    Assert.AreEqual(endpoint, param["?endpoint"]);
                    Assert.AreEqual(timestamp, param["?timestamp"]);
                    Assert.AreEqual(requestBody, param["requestBody"]);
                    return new ApiResponse();
                });

            routingTree.AddRule(
                "GET/servers/info",
                param =>
                {
                    Assert.AreEqual(requestBody, param["requestBody"]);
                    return new ApiResponse();
                });

            routingTree.AddRule(
                "GET/servers/?endpoint/info",
                param =>
                {
                    Assert.AreEqual(endpoint, param["?endpoint"]);
                    Assert.AreEqual(requestBody, param["requestBody"]);
                    return new ApiResponse();
                });

            routingTree.AddRule(
                "GET/servers/?endpoint/stats",
                param =>
                {
                    Assert.AreEqual(endpoint, param["?endpoint"]);
                    Assert.AreEqual(requestBody, param["requestBody"]);
                    return new ApiResponse();
                });

            routingTree.AddRule(
                "GET/servers/?endpoint/matches/?timestamp",
                param =>
                {
                    Assert.AreEqual(endpoint, param["?endpoint"]);
                    Assert.AreEqual(timestamp, param["?timestamp"]);
                    Assert.AreEqual(requestBody, param["requestBody"]);
                    return new ApiResponse();
                });

            routingTree.AddRule(
                "GET/players/?name/stats",
                param =>
                {
                    Assert.AreEqual(name, param["?name"]);
                    Assert.AreEqual(requestBody, param["requestBody"]);
                    return new ApiResponse();
                });

            routingTree.AddRule(
                "GET/reports/recent-matches/?count",
                param =>
                {
                    Assert.AreEqual(countMatrix[0, 1], param["?count"]);
                    Assert.AreEqual(requestBody, param["requestBody"]);
                    return new ApiResponse();
                });

            routingTree.AddRule(
                "GET/reports/best-players/?count",
                param =>
                {
                    Assert.AreEqual(countMatrix[1, 1], param["?count"]);
                    Assert.AreEqual(requestBody, param["requestBody"]);
                    return new ApiResponse();
                });

            routingTree.AddRule(
                "GET/reports/popular-servers/?count",
                param =>
                {
                    Assert.AreEqual(countMatrix[2, 1], param["?count"]);
                    Assert.AreEqual(requestBody, param["requestBody"]);
                    return new ApiResponse();
                });

            // act
            string[] urls = new string[]
            {
                "PUT/servers/" + endpoint + "/info",
                "PUT/servers/" + endpoint + "/matches/" + timestampString,
                "GET/servers/info",
                "GET/servers/" + endpoint + "/info",
                "GET/servers/" + endpoint + "/stats",
                "GET/servers/" + endpoint + "/matches/" + timestampString,
                "GET/players/" + name + "/stats",
                "GET/reports/recent-matches/" + countMatrix[0, 0],
                "GET/reports/best-players/" + countMatrix[1, 0],
                "GET/reports/popular-servers/" + countMatrix[2, 0]
            };

            // assert
            foreach (var url in urls)
            {
                Assert.AreEqual(HttpStatusCode.OK, routingTree.Route(url, requestBody).Status);
            }
        }
    }
}
