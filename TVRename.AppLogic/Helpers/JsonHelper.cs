using Newtonsoft.Json.Linq;

namespace TVRename.AppLogic.Helpers
{
    public static class JsonHelper
    {
        public static string Flatten(JToken jToken, string delimiter = ",")
        {
            if (jToken == null)
            {
                return string.Empty;
            }

            if (jToken.Type != JTokenType.Array)
            {
                return string.Empty;
            }

            JArray ja2 = (JArray)jToken;
            string[] values = ja2.ToObject<string[]>();
            return string.Join(delimiter, values);
        }
    }
}
