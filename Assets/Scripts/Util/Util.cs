using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
public static class Util
{
    public static string ReadJsonParamFromStr(string key, string param)
    {
        string json = key;
        JObject data = (JObject)JsonConvert.DeserializeObject(json);
        return data[param].Value<string>();
    }
}