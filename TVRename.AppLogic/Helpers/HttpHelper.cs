using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json.Linq;

namespace TVRename.AppLogic.Helpers
{
    public static class HttpHelper
    {
        // TODO: Put this back
        // private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public static string HttpRequest(string method, string url, string json, string contentType, string authToken = "", string lang = "")
        {
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
            httpWebRequest.ContentType = contentType;
            httpWebRequest.Method = method;
            if (authToken != "")
            {
                httpWebRequest.Headers.Add("Authorization", "Bearer " + authToken);
            }
            if (lang != "")
            {
                httpWebRequest.Headers.Add("Accept-Language", lang);
            }

            // TODO: Put this back
            // logger.Trace("Obtaining {0}", url);

            if (method == "POST")
            {
                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    streamWriter.Write(json);
                    streamWriter.Flush();
                }
            }

            string result;
            var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream() ?? throw new InvalidOperationException()))
            {
                result = streamReader.ReadToEnd();
            }
            // TODO: Put this back
            // logger.Trace("Returned {0}", result);
            return result;
        }

        public static JObject JsonHttpPostRequest(string url, JObject request)
        {
            var response = HttpRequest("POST", url, request.ToString(), "application/json");

            return JObject.Parse(response);
        }

        public static JObject JsonHttpGetRequest(string url, Dictionary<string, string> parameters, string authToken, string lang = "")
        {
            var response = HttpRequest("GET", url + GetHttpParameters(parameters), null, "application/json", authToken, lang);

            return JObject.Parse(response);

        }

        public static string GetHttpParameters(Dictionary<string, string> parameters)
        {
            if (parameters == null) return "";

            var sb = new StringBuilder();
            sb.Append("?");

            foreach (var item in parameters)
            {
                sb.Append($"{item.Key}={item.Value}&");
            }
            string finalUrl = sb.ToString();
            return finalUrl.Remove(finalUrl.LastIndexOf("&", StringComparison.Ordinal));
        }

    }
}
