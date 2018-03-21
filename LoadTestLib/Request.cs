using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Diagnostics;
using System.IO.Compression;

namespace LoadTestLib
{
    public class ResultData: IDisposable
    {
        public Int32 Code { get; set; }
        public Byte[] Data { get; set; }
        public Int64 DataLength { get; set; }
        public Int64 OriginalDataLength { get; set; }
        public TimeSpan Time { get; set; }
        public String ContentType { get; set; }
        public String ContentEncoding { get; set; }
        public Uri RequestUri { get; set; }

        public Encoding ContentEncoding2
        {
            get
            {
                Encoding enc = Encoding.ASCII;
                try
                {
                    enc = Encoding.GetEncoding(this.ContentEncoding);
                }
                catch { }

                return enc;
            }
        }

        public Boolean Error { get; set; }
        public String ErrorMessage { get; set; }

        public ResultData()
        {
            this.Error = false;
            this.Data = new byte[0];
            this.Time = new TimeSpan(0);
            this.Code = 0;
            this.DataLength = 0;
            this.ErrorMessage = null;
            this.ContentType = null;
            this.ContentEncoding = null;
            this.RequestUri = null;
        }

        public void Dispose()
        {
            this.Error = false;
            this.Data = new byte[0];
            this.Time = new TimeSpan(0);
            this.Code = 0;
            this.ErrorMessage = null;
            this.ContentType = null;
            this.ContentEncoding = null;
            this.RequestUri = null;
        }
    }

    public static class Request
    {
        public delegate void DebugMessage(String data, String debug);

        public static ResultData GetRequest(Uri uri)
        {
            return GetRequest(uri, null, null, null, true, null, null, null, null);
        }


        public static ResultData GetRequest(Uri uri, IPEndPoint proxy)
        {
            return GetRequest(uri, proxy, null, null, true, null, null, null, null);
        }

        public static ResultData GetRequest(Uri uri, IPEndPoint proxy, Dictionary<String, String> headers)
        {
            return GetRequest(uri, proxy, null, null, true, headers, null, null, null);
        }

        public static ResultData GetRequest(Uri uri, IPEndPoint proxy, Boolean allowRedirect)
        {
            return GetRequest(uri, proxy, null, null, allowRedirect, null, null, null, null);
        }

        public static ResultData GetRequest(Uri uri, IPEndPoint proxy, Boolean allowRedirect, Dictionary<String, String> headers)
        {
            return GetRequest(uri, proxy, null, null, allowRedirect, headers, null, null, null);
        }

        public static ResultData GetRequest(Uri uri, IPEndPoint proxy, String postData, String ContentType, Boolean allowRedirect, Dictionary<String, String> headers, String method, CookieContainer cookie, DebugMessage debugCallback)
        {
            ResultData res = new ResultData();
            res.RequestUri = uri;
            
            try
            {
                Stopwatch timer = new Stopwatch();

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
                request.UserAgent = "Mozilla/5.0 (compatible; LoadTest/1.0; +http://www.safetrend.com.br)";
                request.AllowAutoRedirect = allowRedirect;
                request.Headers.Add("Accept-Encoding", "gzip,deflate");
                //request.ProtocolVersion = new Version("1.0");

                if (cookie != null)
                    request.CookieContainer = cookie;

                if (headers != null)
                    foreach (String k in headers.Keys)
                        switch (k.ToLower())
                        {
                            case "accept-encoding":
                            case "connection":
                            case "host":
                            case "referer":
                            case "content-Length":
                            case "content-type":
                                //Nada
                                break;

                            case "user-agent":
                                request.UserAgent = headers[k];
                                break;

                            case "cookie":
                                if (request.CookieContainer == null)
                                    request.CookieContainer = new CookieContainer();
                                
                                request.CookieContainer.SetCookies(uri, headers[k]);
                                break;

                            default:
                                request.Headers.Remove(k);
                                request.Headers.Add(k, headers[k]);
                                break;
                        }


                if (proxy != null)
                    request.Proxy = new WebProxy("http://" + proxy.Address + ":" + proxy.Port);


                if (!String.IsNullOrWhiteSpace(method))
                {
                    switch (method.ToUpper())
                    {
                        case "GET":
                        case "POST":
                        case "PUT":
                        case "DELETE":
                            request.Method = method.ToUpper();
                            break;

                        default:
                            request.Method = "GET";
                            break;
                    }
                }
                else
                {
                    request.Method = "GET";
                }

                try
                {
                    if (debugCallback != null) debugCallback("POST Data", postData);
                    if (!String.IsNullOrWhiteSpace(postData))
                    {
                        request.ContentType = ContentType.Split(";".ToCharArray(), 2)[0].Trim() + "; charset=UTF-8";

                        // Create POST data and convert it to a byte array.
                        byte[] byteArray = Encoding.UTF8.GetBytes(postData);
                        request.ContentLength = byteArray.Length;
                        using (Stream dataStream = request.GetRequestStream())
                        {
                            dataStream.Write(byteArray, 0, byteArray.Length);
                        }
                    }

                    //request.Headers.Add("Content-Type", "application/json; charset=UTF-8");
                }
                catch (Exception ex)
                {
                    if (debugCallback != null) debugCallback("POST Data Error", ex.Message);
                }

                try
                {
                    // Get the response.
                    if (debugCallback != null) debugCallback("GetResponse", "");
                    timer.Start();


                    using (WebResponse r1 = request.GetResponse())
                    using (HttpWebResponse response = (HttpWebResponse)r1)
                    {
                        timer.Stop();
                        res.Code = (Int32)response.StatusCode;

                        String[] ct = response.ContentType.Split(";".ToCharArray(), 2);

                        res.ContentType = ct[0].Trim();

                        res.ContentEncoding = response.ContentEncoding;
                        if (String.IsNullOrEmpty(res.ContentEncoding) && ct.Length == 2)
                            res.ContentEncoding = ct[1].Replace("charset=", "").Trim();

                        //text/html; charset=UTF-8

                        using (Stream dataStream = response.GetResponseStream())
                            res.Data = ReadBuffer(dataStream);

                        if ((res.Data != null) && (res.Data.Length > 0))
                        {
                            res.OriginalDataLength = res.Data.Length;

                            switch (response.ContentEncoding.ToUpperInvariant())
                            {
                                case "GZIP":
                                    using (MemoryStream streamIn = new MemoryStream(res.Data))
                                    using (Stream dataStream = new GZipStream(streamIn, CompressionMode.Decompress))
                                        res.Data = ReadBuffer(dataStream);
                                    break;

                                case "DEFLATE":
                                    using (MemoryStream streamIn = new MemoryStream(res.Data))
                                    using (Stream dataStream = new DeflateStream(streamIn, CompressionMode.Decompress))
                                        res.Data = ReadBuffer(dataStream);
                                    break;
                            }
                        }


                        /*
                        using(StreamReader readStream = new StreamReader(dataStream, Encoding.UTF8))
                        {
                            //Quando o response.ContentLength retorna -1 pois o site não informou o tamanho
                            //Faz a leitura completa
                            String tmp = readStream.ReadToEnd();
                            res.Data = Encoding.UTF8.GetBytes(tmp);
                        }*/
                    }


                }
                catch (Exception ex)
                {
                    res.Error = true;
                    timer.Stop();

                    res.ErrorMessage = ex.Message;

                    if (debugCallback != null) debugCallback("GetResponse Error", ex.Message);
                    try
                    {
                        if (ex is WebException)
                        {
                            WebException webEx = (WebException)ex;

                            if (webEx.Response != null)
                                using (HttpWebResponse response = (HttpWebResponse)webEx.Response)
                                {
                                    res.Code = (Int32)response.StatusCode;

                                    HttpWebResponse httpResponse = (HttpWebResponse)response;
                                    
                                    /*using (Stream data = response.GetResponseStream())
                                    {
                                        res.Data = new Byte[response.ContentLength];
                                        data.Read(res.Data, 0, res.Data.Length);
                                    }*/

                                    using (Stream dataStream = response.GetResponseStream())
                                        res.Data = ReadBuffer(dataStream);

                                }


                        }
                    }
                    catch { }
                }
                
                res.Time = timer.Elapsed;

            }
            catch (Exception ex)
            {
                res.Error = true;
                res.ErrorMessage = ex.Message;
            }

            if ((res.Data != null) && (res.Data.Length > 0))
                res.DataLength = res.Data.Length;

            if (res.OriginalDataLength == 0)
                res.OriginalDataLength = res.DataLength;

            return res;
        }


        public static Byte[] ReadBuffer(Stream stm)
        {
            Byte[] ret = new Byte[0];
            Byte[] buffer = new Byte[4096];

            using (MemoryStream mBuffer = new MemoryStream())
            {
                int bytesRead;
                while ((bytesRead = stm.Read(buffer, 0, buffer.Length)) > 0)
                {
                    mBuffer.Write(buffer, 0, bytesRead);
                }

                ret = mBuffer.ToArray();
                Array.Clear(buffer, 0, buffer.Length);
                buffer = null;
            }

            return ret;
        }
        
    }
}
