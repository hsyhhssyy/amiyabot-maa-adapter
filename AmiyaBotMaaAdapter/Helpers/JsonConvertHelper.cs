using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

/*
 * JsonConvertHelper
 * 版本 v1.2
 * 最后修改 2022-08-31
 * 
 * 版本更新：
 * v1.2 修改为.Net6语法，未来的项目全部向.Net6进行迁移。
 */

namespace AmiyaBotMaaAdapter.Helpers
{
    public static class JsonConvertHelper
    {
        [UsedImplicitly]
        public static JObject FromDictionary(Dictionary<String, object> dict)
        {
            JObject json = new JObject();
            foreach (var oPair in dict)
            {
                if (oPair.Value is string strValue)
                {
                    json[oPair.Key] = strValue;
                }
                else
                if (oPair.Value is int iValue)
                {
                    json[oPair.Key] = iValue;
                }
                else
                if (oPair.Value is DateTime dtValue)
                {
                    json[oPair.Key] = dtValue;
                }
                else
                if (oPair.Value is bool bValue)
                {
                    json[oPair.Key] = bValue;
                }
                else
                {
                    json[oPair.Key] = JObject.FromObject(oPair.Value);
                }

            }

            return json;
        }

        [UsedImplicitly]
        public static bool TryDeserializeObject<T>(String json, out T value)
        {
            return TryDeserializeObject(json, null, out value);
        }

        [UsedImplicitly]
        public static bool TryDeserializeObject<T>(String json, JsonConverter[] converters, out T value)
        {
            if (String.IsNullOrWhiteSpace(json))
            {
                value = default(T);
                return false;
            }

            try
            {
                if (converters != null)
                {
                    value = JsonConvert.DeserializeObject<T>(json, converters);
                }
                else
                {
                    value = JsonConvert.DeserializeObject<T>(json);
                }
                return true;
            }
            catch (Exception exp)
            {
                Logger.Current.Report($"序列化时发生错误:{exp.Message},原文:{json}");
                value = default(T);
                return false;
            }
        }

        [UsedImplicitly]
        public static bool TryDeserializeObject(String json, Type t, out object value)
        {
            if (String.IsNullOrWhiteSpace(json))
            {
                value = null;
                return false;
            }

            try
            {
                value = JsonConvert.DeserializeObject(json, t);
                return true;
            }
            catch (Exception exp)
            {
                Logger.Current.Report($"序列化时发生错误:{exp.Message},原文:{json}");
                value = null;
                return false;
            }
        }

        [UsedImplicitly]
        public static bool TryDeserializeObject(String json, out Object value)
        {
            if (String.IsNullOrWhiteSpace(json))
            {
                value = null;
                return false;
            }

            try
            {
                value = JsonConvert.DeserializeObject(json);
                return true;
            }
            catch (Exception exp)
            {
                Logger.Current.Report($"序列化时发生错误:{exp.Message},原文:{json}");
                value = null;
                return false;
            }
        }

        [UsedImplicitly]
        public static T LoadFromTextFile<T>(String txtPath)
        {
            FileInfo file = new FileInfo(txtPath);

            if (!file.Exists)
            {
                return default(T);
            }

            var sr = new StreamReader(new FileStream(file.FullName, FileMode.Open));
            var cfgJson = sr.ReadToEnd();
            sr.Close();

            if (JsonConvertHelper.TryDeserializeObject<T>(cfgJson, out var helper))
            {
                return helper;
            }

            return default(T);
        }

        [UsedImplicitly]
        public static object FormatToDictionary(object? data)
        {
            return RecursiveConvert(data);
        }

        private static object RecursiveConvert(object data)
        {
            if (data is Dictionary<String, object> dict)
            {
                foreach (var pair in dict.ToList())
                {
                    dict[pair.Key] = RecursiveConvert(pair.Value);
                }

                return dict;
            }

            if (data is JArray array)
            {
                var value = new List<object>();
                foreach (var arrayValue in array)
                {
                    value.Add(RecursiveConvert(arrayValue));
                }

                return value;
            }

            if (data is JObject jObject)
            {
                var value = new Dictionary<String, object>();
                foreach (var arrayValue in jObject)
                {
                    value.Add(arrayValue.Key, RecursiveConvert(arrayValue.Value));
                }

                return value;
            }

            if (data is JValue jValue)
            {
                switch (jValue.Type)
                {
                    case JTokenType.Boolean:
                        return jValue.Value as bool?;
                    case JTokenType.String:
                        return jValue.Value as String;
                    default:
                        return data;
                }
            }

            return data;

        }
    }
}
