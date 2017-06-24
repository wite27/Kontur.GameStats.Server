using System.Net;
using Newtonsoft.Json.Linq;

namespace Kontur.GameStats.Server.API
{
    public class ApiResponse
    {
        private HttpStatusCode status;
        public HttpStatusCode Status
        {
            get { return status; }
            private set { status = value; }
        }

        private string body;
        public string Body
        {
            get { return body; }
            private set { body = value; }
        }
        public ApiResponse(HttpStatusCode status = HttpStatusCode.OK)
        {
            this.body   = "";
            this.status = status;
        }
        public ApiResponse(string body, HttpStatusCode status = HttpStatusCode.OK)
        {
            this.body   = body;
            this.status = status;
        }
        public ApiResponse(JContainer json, HttpStatusCode status = HttpStatusCode.OK)
        {
            this.body   = json.ToString(Newtonsoft.Json.Formatting.Indented);
            this.status = status;
        }
    }
}
