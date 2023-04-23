using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Newtonsoft.Json.Linq;

/*
 * HttpHelper
 * 版本 v1.2
 * 最后修改 2022-09-02
 *
 * v1.0 初始版本
 * v1.1 不再提供返回任意内容的接口，所有接口统一改为返回JToken
 *      为了方便大部分Api的调用，默认ContentType改为Json，AdditionalHeader移动为紧跟Data的参数。
 * v1.2 加入Put
 */

namespace AmiyaBotMaaAdapter.Helpers
{
    public static class HttpHelper
    {
        public class DeserializedHttpResponse
        {
            public bool Success;
            public bool IsHttpError;
            public HttpStatusCode StatusCode;
            public JToken Data;
            public String RawData;
        }

        #region 基础方法


        [UsedImplicitly]
        private static DeserializedHttpResponse WebAction(String method,String url, String postData,
            Dictionary<String, String> additionalHeader = null,
            String contentType = "application/json",
            String cookieHost = null, CookieCollection cookies = null,
            Encoding encoding = null, int timeout = System.Threading.Timeout.Infinite,
            List<X509Certificate2> certs = null)
        {
            encoding ??= Encoding.UTF8;

            if (timeout == 0)
            {
                timeout = 1500;
            }
            
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.KeepAlive = false;
            request.Method = method;

            if (contentType != null)
            {
                request.ContentType = contentType;
            }


            request.Timeout = timeout;
            if (certs != null)
            {
                foreach (var x509Certificate in certs)
                {
                    request.ClientCertificates.Add(x509Certificate);

                }
            }

            if (cookieHost != null && cookies != null)
            {
                request.CookieContainer = new CookieContainer();
                request.CookieContainer.Add(cookies);
            }

            if (additionalHeader != null)
            {
                foreach (var pair in additionalHeader)
                {
                    request.Headers.Add(pair.Key, pair.Value);
                }
            }

            try
            {
                if (postData != null)
                {
                    byte[] data = encoding.GetBytes(postData);
                    request.ContentLength = data.Length;
                    Stream formStream = request.GetRequestStream();
                    formStream.Write(data, 0, data.Length);
                    formStream.Close();
                }

                var response = (HttpWebResponse)request.GetResponse();
                var rStream = response.GetResponseStream();

                if (cookieHost != null && cookies != null)
                {
                    SetCookies(cookieHost, cookies, response);
                }

                StreamReader reader = new StreamReader(rStream!, encoding);
                string content = reader.ReadToEnd();

                if (!JsonConvertHelper.TryDeserializeObject(content, out JToken value))
                {
                    value = null;
                }

                return new DeserializedHttpResponse()
                {
                    Success = true,
                    StatusCode = response.StatusCode,
                    Data = value,
                    RawData = content
                };
            }
            catch (WebException exp)
            {
                if (exp.Response is HttpWebResponse hResponse)
                {
                    var rStream = hResponse.GetResponseStream();

                    StreamReader reader = new StreamReader(rStream, Encoding.UTF8);
                    string content = reader.ReadToEnd();
                    Logger.Current.Report($"[POST][HTTP{hResponse.StatusCode}][Url({url})]{content}");

                    if (!JsonConvertHelper.TryDeserializeObject(content, out JToken value))
                    {
                        value = null;
                    }

                    return new DeserializedHttpResponse()
                    {
                        Success = false,
                        IsHttpError = true,
                        StatusCode = hResponse.StatusCode,
                        Data = value,
                        RawData = content
                    };
                }

                if (exp.Status == WebExceptionStatus.Timeout)
                {
                    Logger.Current.Report($"[POST][HTTPTimeout][Url({url})]");
                    return new DeserializedHttpResponse()
                    {
                        Success = false,
                        IsHttpError = true,
                        StatusCode = HttpStatusCode.RequestTimeout,
                        Data = null,
                        RawData = null
                    };
                }

                Logger.Current.Report($"[POST][HTTPUnknownError][Url({url})]{Logger.Current.FormatException(exp, "")}");
                return new DeserializedHttpResponse()
                {
                    Success = false,
                    IsHttpError = false,
                    Data = null,
                    RawData = null
                };
            }
        }

        #endregion

        #region 扩展方法

        [UsedImplicitly]
        public static DeserializedHttpResponse PostAction(String url, String postData,
            Dictionary<String, String> additionalHeader = null,
            String contentType = "application/json",
            String cookieHost = null, CookieCollection cookies = null,
            Encoding encoding = null, int timeout = System.Threading.Timeout.Infinite,
            List<X509Certificate2> certs = null)
        {
            return WebAction("POST", url, postData, additionalHeader, contentType, cookieHost, cookies, encoding,
                timeout, certs);
        }


        [UsedImplicitly]
        public static DeserializedHttpResponse GetAction(String url, Dictionary<String, String> additionalHeader = null, String cookieHost = null, CookieCollection cookies = null,
            Encoding encoding = null, int timeout = System.Threading.Timeout.Infinite,
            List<X509Certificate2> certs = null)
        {
            return WebAction("GET", url, null, additionalHeader, null, cookieHost, cookies, encoding,
                timeout, certs);
        }

        [UsedImplicitly]
        public static DeserializedHttpResponse PutAction(String url, String postData,
            Dictionary<String, String> additionalHeader = null,
            String contentType = "application/json",
            String cookieHost = null, CookieCollection cookies = null,
            Encoding encoding = null, int timeout = System.Threading.Timeout.Infinite,
            List<X509Certificate2> certs = null)
        {
            return WebAction("PUT", url, postData, additionalHeader, contentType, cookieHost, cookies, encoding,
                timeout, certs);
        }

        #endregion


        [UsedImplicitly]
        public static DeserializedHttpResponse ExecuteWithRetry(int maxRetry, Func<DeserializedHttpResponse> func)
        {
            if (maxRetry <= 1)
            {
                maxRetry = 1;
            }

            if (maxRetry > 20)
            {
                maxRetry = 20;
            }

            DeserializedHttpResponse response = null;

            for (int retry = 0; retry < maxRetry; retry++)
            {
                response = func();
                if (response?.Success == true)
                {
                    break;
                }
            }

            if (response == null)
            {
                return new DeserializedHttpResponse()
                {
                    Success = false,
                    IsHttpError = false,
                    Data = null,
                    RawData = null
                };
            }

            return response;
        }

        #region Async版本

        [UsedImplicitly]
        public static async Task<DeserializedHttpResponse> PostActionAsync(String url, String postData,
            String contentType = "application/x-www-form-urlencoded",
            String cookieHost = null, CookieCollection cookies = null,
            Dictionary<String, String> additionalHeader = null,
            Encoding encoding = null, int timeout = System.Threading.Timeout.Infinite,
            List<X509Certificate2> certs = null)
        {
            var responseStr = await Task.Run(() => PostAction(url, postData, additionalHeader, contentType,
                cookieHost, cookies, encoding, timeout, certs));

            return responseStr;
        }

        [UsedImplicitly]
        public static async Task<DeserializedHttpResponse> GetActionAsync(String url, String cookieHost = null,
            CookieCollection cookies = null, Dictionary<String, String> additionalHeader = null,
            Encoding encoding = null, int timeout = System.Threading.Timeout.Infinite,
            List<X509Certificate2> certs = null)
        {
            var responseStr = await Task.Run(() =>
                GetAction(url, additionalHeader, cookieHost, cookies, encoding, timeout, certs));

            return responseStr;
        }

        #endregion

        private static void SetCookies(String host, CookieCollection cookies, HttpWebResponse response)
        {
            var newCookies = response.Headers.GetValues("Set-Cookie");
            if (newCookies != null)
            {
                foreach (var newCookie in newCookies)
                {
                    var reg = new Regex(@"(\S*?)=([^;]*?);.*ath=([^;]*?);?$");
                    var mc = reg.Match(newCookie);
                    if (mc.Success)
                    {
                        cookies.Add(new Cookie(mc.Groups[1].Value, mc.Groups[2].Value, mc.Groups[3].Value,
                            host));
                    }
                }
            }
        }
    }
}
