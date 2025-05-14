using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Services.Static
{
    public class CustomHttpClient
    {
        private string _url;
        private Dictionary<string, string> _queryParameter = new Dictionary<string, string>();
        private Dictionary<string, string> _headers = new Dictionary<string, string>();

        private CustomHttpClient() { }
        private CustomHttpClient(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                throw new NullReferenceException("url params cannot be null or empty");
            }
            _url = url.TrimEnd('/');
        }

        public static CustomHttpClient Create(string url)
        {
            return new CustomHttpClient(url.ToLower());
        }

        #region Query Pramams

        public CustomHttpClient AddQueryParams(params KeyValuePair<string, string>[] pairs)
        {
            if (pairs.Any(t => string.IsNullOrEmpty(t.Key) || string.IsNullOrEmpty(t.Value)))
            {
                throw new NullReferenceException("all query parameters name and value cannot be null or empty");
            }
            foreach (var pair in pairs)
            {
                if (!_queryParameter.Any(t => t.Key == pair.Key))
                {
                    _queryParameter.Add(pair.Key, pair.Value);
                }
                else
                {
                    _queryParameter[pair.Key] = pair.Value;
                }
            }
            return this;
        }

        public CustomHttpClient AddQueryParams(Dictionary<string, string> queries)
        {
            if (queries.Any(t => string.IsNullOrEmpty(t.Key) || string.IsNullOrEmpty(t.Value)))
            {
                throw new NullReferenceException("all query parameter name and value cannot be null or empty");
            }
            _queryParameter = queries;

            return this;
        }

        public CustomHttpClient AddQueryParam(string name, string value)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(value))
            {
                throw new NullReferenceException("query name and value cannot be null or empty");
            }
            if (!_queryParameter.Any(t => t.Key == name))
            {
                _queryParameter.Add(name, value);
            }
            else
            {
                _queryParameter[name] = value;
            }
            return this;
        }

        #endregion

        #region Headers

        public CustomHttpClient AddHeaders(params KeyValuePair<string, string>[] pairs)
        {
            if (pairs.Any(t => string.IsNullOrEmpty(t.Key) || string.IsNullOrEmpty(t.Value)))
            {
                throw new NullReferenceException("all headers name and value cannot be null or empty");
            }
            foreach (var pair in pairs)
            {
                if (!_headers.Any(t => t.Key == pair.Key))
                {
                    _headers.Add(pair.Key, pair.Value);
                }
                else
                {
                    _headers[pair.Key] = pair.Value;
                }
            }
            return this;
        }

        public CustomHttpClient AddHeaders(Dictionary<string, string> headers)
        {
            if (headers.Any(t => string.IsNullOrEmpty(t.Key) || string.IsNullOrEmpty(t.Value)))
            {
                throw new NullReferenceException("all headers name and value cannot be null or empty");
            }
            _headers = headers;

            return this;
        }

        public CustomHttpClient AddHeader(string key, string value)
        {
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value))
            {
                throw new NullReferenceException("header name and value cannot be null or empty");
            }
            if (!_headers.Any(t => t.Key == key))
            {
                _headers.Add(key, value);
            }
            else
            {
                _headers[key] = value;
            }
            return this;
        }

        #endregion

        public async Task<CustomHttpResponse<T>> PostFormUrlEncodedContentAsync<T>(Dictionary<string, string> body, string requestUrl = "")
        {
            using (var netclient = InitialNetClient())
            {
                return new CustomHttpResponse<T>(await netclient.PostAsync(requestUrl, new System.Net.Http.FormUrlEncodedContent(body)));
            }
        }
        public async Task<CustomHttpResponse<T>> PostRawJsonAsync<T>(object item, string requestUrl = "")
        {
            using (var netclient = InitialNetClient())
            {
                return new CustomHttpResponse<T>(await netclient.PostAsync(requestUrl,
                                                                                new System.Net.Http.StringContent(
                                                                                    Newtonsoft.Json.JsonConvert.SerializeObject(item),
                                                                                    Encoding.UTF8, "application/json")));
            }
        }
        public async Task<CustomHttpResponse<T>> PostRawStringAsync<T>(string item, string requestUrl = "")
        {
            using (var netclient = InitialNetClient())
            {
                return new CustomHttpResponse<T>(await netclient.PostAsync(requestUrl,  new System.Net.Http.StringContent(item,  Encoding.UTF8, "application/json")));
            }
        }
        public async Task<CustomHttpResponse<T>> PutFormUrlEncodedContentAsyncAsync<T>(Dictionary<string, string> body, string requestUrl = "")
        {
            using (var netclient = InitialNetClient())
            {
                return new CustomHttpResponse<T>(await netclient.PutAsync(requestUrl, new System.Net.Http.FormUrlEncodedContent(body)));
            }
        }
        public async Task<CustomHttpResponse<T>> PutRawJsonAsync<T>(object item, string requestUrl = "")
        {
            using (var netclient = InitialNetClient())
            {
                return new CustomHttpResponse<T>(await netclient.PutAsync(requestUrl,
                                                                                new System.Net.Http.StringContent(
                                                                                    Newtonsoft.Json.JsonConvert.SerializeObject(item),
                                                                                    Encoding.UTF8, "application/json")));
            }
        }
        public async Task<CustomHttpResponse<T>> DeleteAsync<T>(string requestUrl = "")
        {
            using (var netclient = InitialNetClient())
            {
                return new CustomHttpResponse<T>(await netclient.DeleteAsync(requestUrl));
            }
        }
        public async Task<CustomHttpResponse<T>> GetAsync<T>(string requestUrl = "")
        {
            using (var netclient = InitialNetClient())
            {
                return new CustomHttpResponse<T>(await netclient.GetAsync(requestUrl));
            }
        }
        private System.Net.Http.HttpClient InitialNetClient()
        {
            if (string.IsNullOrEmpty(_url))
            {
                throw new Exception("url can not be null, please parse url before make a request");
            }
            System.Net.Http.HttpClient netclient = null;
            System.Net.Http.HttpClientHandler handler =  new System.Net.Http.HttpClientHandler();;
            handler.AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate;
            handler.ServerCertificateCustomValidationCallback = Utils.AlwaysValidCertificateForRequest;
            
            if (ProgramArguments.Proxy != null)
            {
                handler.Proxy = ProgramArguments.Proxy;
                handler.PreAuthenticate = true;
                handler.UseDefaultCredentials = false;
            }
            netclient = new System.Net.Http.HttpClient(handler);
            
            netclient.Timeout = TimeSpan.FromSeconds(30);
            foreach (var header in _headers)
            {
                netclient.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
            }

            var queriesHelpers = new List<string>();
            foreach (var query in _queryParameter)
            {
                queriesHelpers.Add($"{System.Net.WebUtility.UrlEncode(query.Key)}={System.Net.WebUtility.UrlEncode(query.Value)}");
            }

            netclient.BaseAddress = queriesHelpers.Any()
                                        ? new Uri($"{_url}?{string.Join("&", queriesHelpers)}")
                                        : new Uri(_url);
            return netclient;
        }

    }
    public class CustomHttpResponse<T>
    {
        public CustomHttpResponse(System.Net.Http.HttpResponseMessage response)
        {
            StatusCode = response.StatusCode;
            Message = response.RequestMessage.ToString();
            try
            {
                Response = GetRequestResponse(response);
            }
            catch (Exception) { }
        }
        public System.Net.HttpStatusCode StatusCode { get; set; }
        public string Message { get; set; }
        public T Response { get; set; }
        private T GetRequestResponse(System.Net.Http.HttpResponseMessage response)
        {
            try
            {
                var task = response.Content.ReadAsStringAsync();
                task.Wait();
                var resultContent = task.Result;
                if (string.IsNullOrEmpty(resultContent))
                {
                    return default(T);
                }
                if (typeof(T) == typeof(string))
                {
                    return (T)Convert.ChangeType(resultContent, typeof(T));
                }
                return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(resultContent);
            }
            catch (Exception)
            {
                return default(T);
            }
        }
    }
}
