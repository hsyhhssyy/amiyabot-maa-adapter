using AmiyaBotMaaAdapter.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace AmiyaBotMaaAdapter
{
    internal static class MaaAdapterHttpHelper
    {
        public static String GenerateSignature()
        {
            var _uuid = MaaAdapterConfig.CurrentConfig.Uuid;
            var _secret = MaaAdapterConfig.CurrentConfig.Secret;
            
            var error = GetResponseData(HttpHelper.PostAction(MaaAdapterConfig.CurrentConfig.Server + "/maa/token", JsonConvert.SerializeObject(new Dictionary<string, string>()
            {
                { "uuid", _uuid },
                { "secret", _secret  }
            })), out var signatureResponse);

            if (error != null)
            {
                MessageBox.Show("获取密钥出错，错误原因：" + error);
                return "";
            }

            MaaAdapterConfig.CurrentConfig.Signature = signatureResponse["code"].ToString();
            
            return MaaAdapterConfig.CurrentConfig.Signature;
        }

        public static String GetResponseData(this HttpHelper.DeserializedHttpResponse rawResponse, out Dictionary<String, Object> data)
        {
            data = new Dictionary<String, Object>();

            if (rawResponse.Success == false)
            {
                return rawResponse.RawData;
            }

            var dictResponse = JsonConvertHelper.FormatToDictionary(rawResponse.Data) as Dictionary<String, object>;

            if (dictResponse == null)
            {
                return "未知错误";
            }

            dictResponse = dictResponse["data"] as Dictionary<String, object>;

            if (dictResponse == null)
            {
                return "未知错误";
            }

            if ((bool)dictResponse["success"] == false)
            {
                if (dictResponse.ContainsKey("reason"))
                {
                    return dictResponse["reason"]?.ToString();
                }
                return "未知错误";
            }

            data = dictResponse;


            return null;
        }


    }
}
