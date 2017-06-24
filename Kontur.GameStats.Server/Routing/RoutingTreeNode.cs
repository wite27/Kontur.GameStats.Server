using System;
using System.Collections.Generic;
using Kontur.GameStats.Server.API;

namespace Kontur.GameStats.Server.Routing
{
    public class RoutingTreeNode
    {
        private string token;
        public string Token
        {
            get { return token; }
            set { token = value; }
        }

        private bool isParam;
        public bool IsParam
        {
            get { return isParam; }
            private set { isParam = value; }
        }
        
        public bool IsAction
        {
            get { return method != null; }
        }

        private List<RoutingTreeNode> children;
        public List<RoutingTreeNode> Children
        {
            get { return children; }
            set { children = value; }
        }

        private Func<string, object> checkConstraint;
        public Func<string, object> CheckConstraint
        {
            get { return checkConstraint; }
            private set { checkConstraint = value; }
        }

        private Func<Parameters, ApiResponse> method;
        public Func<Parameters, ApiResponse> Method
        {
            get { return method; }
            set { method = value; }
        }

        /// <summary>
        /// Creates routing tree node.
        /// </summary>
        public RoutingTreeNode(string token,
                               List<RoutingTreeNode> children = null,
                               Func<string, object> checkConstraint = null,
                               Func<Parameters, ApiResponse> method = null)
        {
            this.token = token;
            this.checkConstraint = checkConstraint;
            this.method = method;
            this.isParam  = !(checkConstraint == null);
            this.children = children ?? new List<RoutingTreeNode>();
        }
    }
}
