using System.Net;

namespace Hypernex.Networking.Server.SandboxedClasses
{
    public class HTTP
    {
        private string MediaTypeToString(HttpMediaType mediaType)
        {
            switch (mediaType)
            {
                case HttpMediaType.ApplicationJSON:
                    return "application/json";
                case HttpMediaType.ApplicationXML:
                    return "application/xml";
                case HttpMediaType.ApplicationURLEncoded:
                    return "application/x-www-form-urlencoded";
                case HttpMediaType.TextPlain:
                    return "text/plain";
                case HttpMediaType.TextXML:
                    return "text/xml";
            }
            return MediaTypeToString(HttpMediaType.TextPlain);
        }

        public string Get(string url)
        {
            using WebClient webClient = new WebClient();
            return webClient.DownloadString(url);
        }

        public string Post(string url, string data, HttpMediaType mediaType)
        {
            using WebClient webClient = new WebClient();
            webClient.Headers[HttpRequestHeader.ContentType] = MediaTypeToString(mediaType);
            return webClient.UploadString(url, data);
        }
    }

    public enum HttpMediaType
    {
        ApplicationJSON = 1,
        ApplicationXML = 2,
        ApplicationURLEncoded = 3,
        TextPlain = 4,
        TextXML = 5
    }
}