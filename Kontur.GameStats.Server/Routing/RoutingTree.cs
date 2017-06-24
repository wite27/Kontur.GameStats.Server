using System;
using System.Collections.Generic;
using System.Linq;
using Kontur.GameStats.Server.API;

namespace Kontur.GameStats.Server.Routing
{
    public class RoutingTree
    {
        private RoutingTreeNode head;
        public RoutingTreeNode Head
        {
            get { return head; }
            private set { head = value; }
        }
                
        public RoutingTree()
        {
            this.head = NodeFactory.Create(String.Empty);            
        }
        public void AddRule(string url, Func<Parameters, ApiResponse> method)
        {
            AddRule(url.Split(new string[] { "/" }, StringSplitOptions.RemoveEmptyEntries), method);
        }
        public void AddRule(IEnumerable<string> tokens, Func<Parameters, ApiResponse> method)
        {
            addRule(tokens, head, method);
        }
        private void addRule(IEnumerable<string> tokens, RoutingTreeNode currentNode, Func<Parameters, ApiResponse> method)
        {
            if (tokens.Count() == 0)
            {
                if (currentNode.Method != null)
                {
                    // there is action already
                    throw new ArgumentException("Rule with this route was added before. Last token: " + currentNode.Token);
                } else
                {
                    // add method
                    currentNode.Method = method;
                }
                return;
            } else
            {
                string currentToken = tokens.First();
                // go deeper
                RoutingTreeNode nextNode =
                    currentNode.Children.Where(ch => ch.Token == currentToken).FirstOrDefault();
                if (nextNode == null)
                {
                    // create next node
                    nextNode = NodeFactory.Create(currentToken);
                    // add it to tree
                    currentNode.Children.Add(nextNode);
                }

                // go to next node
                addRule(tokens.Skip(1), nextNode, method);                
            }
        }
        public ApiResponse Route(string url, string requestBody)
        {
            return Route(url.Split(new string[] { "/" }, StringSplitOptions.RemoveEmptyEntries), requestBody);
        }
        public ApiResponse Route(IEnumerable<string> tokens, string requestBody)
        {
            Parameters parameters = new Parameters("requestBody", requestBody);
            return route(tokens, head, parameters);
        }
        private ApiResponse route(IEnumerable<string> tokens, RoutingTreeNode currentNode, Parameters parameters)
        {
            if (tokens.Count() == 0)
            {
                // it's last token in route, execute method

                if (currentNode.IsAction)
                {
                    // if node contains api call
                    return currentNode.Method(parameters);
                } else
                {
                    // if node doesn't contain api call
                    // check its children to be optional parameters
                    // for that, try to check it by passing empty string as token
                    RoutingTreeNode nextNode;
                    object param;
                    if (tryCheckParameter(String.Empty, currentNode.Children, out nextNode, out param, requireMethod: true))
                    {
                        // add param to route parameters
                        parameters.Add(nextNode.Token, param);
                        // finally, execute method
                        return nextNode.Method(parameters);
                    } else
                    {
                        // error, bad url
                        return new ApiResponse(System.Net.HttpStatusCode.BadRequest);
                    }
                }
            } else
            {
                // go deeper
                var currentToken = tokens.First();
                // firstly, check non-param tokens
                var nextNode = currentNode.Children
                                           .Where(ch => !ch.IsParam &&
                                                  ch.Token == currentToken)
                                           .FirstOrDefault();
                if (nextNode == null)
                {
                    // if required token was not found,
                    // try to validate it as parameter
                    object param;
                    if (tryCheckParameter(currentToken, currentNode.Children, out nextNode, out param))
                    {
                        // add param to route parameters
                        parameters.Add(nextNode.Token, param);
                        // next node is initialized by out-keyword
                    } else
                    {
                        // error, bad url
                        return new ApiResponse("Error parsing url at: " + currentToken + "." , System.Net.HttpStatusCode.BadRequest);
                    }
                }
                // process remaining tokens from next node
                return route(tokens.Skip(1), nextNode, parameters);
            }
        }
        private bool tryCheckParameter(string token, List<RoutingTreeNode> children, out RoutingTreeNode nextNode, out object parameter, bool requireMethod = false)
        {
            parameter = null;
            nextNode  = null;
            foreach (var ch in children)
            {
                // check all child parameters
                // if method is not reuiqred, (!requireMethod || ch.IsAction) == (!false || ..) == true
                if (ch.IsParam && (!requireMethod || ch.IsAction))
                {
                    parameter = ch.CheckConstraint(token);
                    // if parameter is valid (not null), return
                    if (parameter != null)
                    {
                        // set next node to node with valid parameter
                        nextNode = ch;
                        return true;
                    }
                }
            }
            // there are no parameters for this token
            return false;
        }
    }   
}
