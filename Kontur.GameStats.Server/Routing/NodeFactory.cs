using System;
using System.Collections.Generic;
using Kontur.GameStats.Server.API;

namespace Kontur.GameStats.Server.Routing
{
    public class NodeFactory
    {
        /// <summary>
        /// Creates node by its <paramref name="token"/>. If <paramref name="token"/> starts with '?', it would be parameter.
        /// </summary>
        /// <param name="token">Fixed part of url or parameter, starts with '?'.</param>
        /// <param name="children">List of children of this node.</param>
        /// <param name="method">Method, assigned to this node if it's last in the route, or one of its children are unnecessary parameter.</param>
        /// <returns></returns>
        public static RoutingTreeNode Create(string token, List<RoutingTreeNode> children = null,
                                             Func<Parameters, ApiResponse> method = null)
        {
            if (token.StartsWith("?"))
            {
                return CreateParameter(token, children, method);
            } else
            {
                return CreateNonParameter(token, children, method);
            }
        }
        public static RoutingTreeNode CreateNonParameter
            (string token, List<RoutingTreeNode> children = null,
                           Func<Parameters, ApiResponse> method = null)
        {
            // do not allow set constraints to non-parameter nodes
            return new RoutingTreeNode(token, children, null, method);
        }
        public static RoutingTreeNode CreateParameter(string paramName, List<RoutingTreeNode> children = null, Func<Parameters, ApiResponse> method = null)
        {
            switch (paramName)
            {
                case "?endpoint": { return endpointParam(children, method); }
                case "?name": { return nameParam(children, method); }
                case "?timestamp": { return timestampParam(children, method); }
                case "?count": { return countParam(children, method); }
                default: throw new ArgumentException("There is no instance for " + paramName + " parameter");
            }
        }

        #region Parameters' creating methods
        private static RoutingTreeNode endpointParam(List<RoutingTreeNode> children, Func<Parameters, ApiResponse> method = null)
        {
            return new RoutingTreeNode(
                    "?endpoint",
                    children,
                    endpoint => endpoint,
                    method);
        }
        private static RoutingTreeNode nameParam(List<RoutingTreeNode> children, Func<Parameters, ApiResponse> method = null)
        {
            return
                new RoutingTreeNode(
                       "?name",
                       children,
                       name => name,
                       method);
        }
        private static RoutingTreeNode timestampParam(List<RoutingTreeNode> children, Func<Parameters, ApiResponse> method = null)
        {
            return
                new RoutingTreeNode(
                       "?timestamp",
                       children,
                       timestamp =>
                       {
                           DateTime res;
                           if (DateTime.TryParse(timestamp, out res) == true)
                           {
                               return res.ToUniversalTime();
                           }
                           else
                           {
                               return null;
                           }
                       },
                       method);
        }
        private static RoutingTreeNode countParam(List<RoutingTreeNode> children, Func<Parameters, ApiResponse> method = null)
        {
            return
                new RoutingTreeNode(
                    "?count",
                    children,
                    count =>
                    {
                        if (count == String.Empty)
                        {
                            // default value
                            return 5;
                        }
                        int res;
                        if (int.TryParse(count, out res))
                        {
                            if (res <= 0)
                            {
                                return 0;
                            }
                            else if (res >= 50)
                            {
                                return 50;
                            }
                            else
                            {
                                return res;
                            }
                        }
                        else
                        {
                            return null;
                        }
                    },
                    method);
        }
        #endregion
    }
}
