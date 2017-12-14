using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Web.Script.Serialization;
using System.Xml.Serialization;
using System.Xml;
using System.Xml.Linq;
using System.IO;
using System.Net;

namespace SafeTrend.Json
{
    public static class JSON
    {
        public delegate void DebugMessage(String data, String debug);

        public static T Deserialize<T>(String jsonText)
        {
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(T));
            using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(jsonText)))
                return (T)ser.ReadObject(ms);
        }

        public static T Deserialize2<T>(String jsonText)
        {
            JavaScriptSerializer ser = new JavaScriptSerializer();
            return ser.Deserialize<T>(jsonText);
        }

        public static T DeserializeFromBase64<T>(String jsonText)
        {
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(T));
            using (MemoryStream ms = new MemoryStream(Convert.FromBase64String(jsonText)))
                return (T)ser.ReadObject(ms);
        }

        public static T DeserializeFromXML<T>(String xmlText, Boolean ignoreRootNode)
        {

            XElement xml = XElement.Parse(xmlText);

            String json = XmlToJSON(xmlText, ignoreRootNode);

            return Deserialize<T>(json);        
        }

        public static String Dump(String jsonData, Boolean replacePassword)
        {
            JavaScriptSerializer _ser = new JavaScriptSerializer();
            try
            {
                Dictionary<String, Object> jData = _ser.Deserialize<Dictionary<String, Object>>(jsonData);
                return Dump("", jData);
            }
            catch
            {
                //Testa parse com multi-call
                List<Dictionary<String, Object>> requests = new List<Dictionary<String, Object>>();
                try
                {
                    List<Dictionary<String, Object>> jData = _ser.Deserialize<List<Dictionary<String, Object>>>(jsonData);
                    return Dump("", jData);
                }
                catch (Exception ex)
                {
                    throw new Exception("Invalid Json Data");
                    return null;
                }
            }

        }

        private static String Dump(String prefix, Object obj)
        {
            StringBuilder retDump = new StringBuilder();
            try
            {
                if (obj is List<Dictionary<String, Object>>)
                {
                    List<Dictionary<String, Object>> ht = (List<Dictionary<String, Object>>)obj;
                    retDump.AppendLine(prefix + "array(" + ht.Count + ") {");

                    for (Int32 i = 0; i < ht.Count; i++)
                        retDump.Append(prefix + "\t[" + i + "] => " + Dump(prefix + "\t", ht[i]));

                    retDump.AppendLine(prefix + "}");

                }
                else if (obj is Dictionary<String, Object>)
                {
                    Dictionary<String, Object> ht = (Dictionary<String, Object>)obj;
                    retDump.AppendLine(prefix + "array(" + ht.Count + ") {");

                    foreach (String key in ht.Keys)
                        if (key.ToLower() == "password")
                            retDump.AppendLine(prefix + "\t[\"" + key + "\"] => replaced for security");
                        else
                            retDump.Append(prefix + "\t[\"" + key + "\"] => " + Dump(prefix + "\t", ht[key]));

                    retDump.AppendLine(prefix + "}");
                }
                else if (obj is ArrayList)
                {
                    List<Object> ht = new List<Object>();
                    ht.AddRange(((ArrayList)obj).ToArray());

                    retDump.AppendLine(prefix + "array(" + ht.Count + ") {");

                    for (Int32 i = 0; i < ht.Count; i++)
                        retDump.Append(prefix + "\t[" + i + "] => " + Dump(prefix + "\t", ht[i]));

                    retDump.AppendLine(prefix + "}");
                }
                else if (obj is Int16)
                {
                    retDump.AppendLine("short(" + (Int32)obj + ") ");
                }
                else if (obj is Int32)
                {
                    retDump.AppendLine("integer(" + (Int32)obj + ") ");
                }
                else if (obj is Int64)
                {
                    retDump.AppendLine("long(" + (Int64)obj + ") ");
                }
                else if (obj is Boolean)
                {
                    retDump.AppendLine("boolean(" + (Boolean)obj + ") ");
                }
                else
                {
                    String s = ((String)obj);
                    retDump.AppendLine("string(" + (s != null ? s.Length : 0) + ") " + (s != null ? s : "null"));
                }
            }
            catch (Exception ex)
            {
                retDump.AppendLine("error: " + ex.Message);
            }

            return retDump.ToString();
        }

        public static String Serialize<T>(T obj)
        {
            String ret = "";

            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(T));
            
            using (MemoryStream ms = new MemoryStream())
            {
                ser.WriteObject(ms, obj);
                ms.Flush();
                ret = Encoding.UTF8.GetString(ms.ToArray());
            }

            return ret;
        }

        public static String Serialize2(Object obj)
        {
            JavaScriptSerializer _ser = new JavaScriptSerializer();
            return _ser.Serialize(obj);
        }


        public static String SerializeToBase64(Object obj)
        {
            JavaScriptSerializer _ser = new JavaScriptSerializer();
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(_ser.Serialize(obj)));
        }


        public static T JsonWebRequest<T>(Uri uri, String postData, String ContentType, Dictionary<String, String> headers)
        {
            return JsonWebRequest<T>(uri, postData, ContentType, headers, null, null, null);
        }

        public static T JsonWebRequest<T>(Uri uri, String postData, String ContentType, Dictionary<String, String> headers, String method)
        {
            return JsonWebRequest<T>(uri, postData, ContentType, headers, method, null, null);
        }

        public static T JsonWebRequest<T>(Uri uri, String postData, String ContentType, Dictionary<String, String> headers, String method, CookieContainer cookie)
        {
            return JsonWebRequest<T>(uri, postData, ContentType, headers, method, cookie, null);
        }

        public static T JsonWebRequest<T>(Uri uri, String postData, String ContentType, Dictionary<String, String> headers, String method, CookieContainer cookie, DebugMessage debugCallback)
        {

            String jData = TextWebRequest(uri, postData, ContentType, headers, method, cookie, debugCallback);

            if (jData == "")
                return (T)((Object)null);
            else
                return Deserialize<T>(jData);
        }


        public static String TextWebRequest(Uri uri, String postData, String ContentType, Dictionary<String, String> headers, String method, CookieContainer cookie, DebugMessage debugCallback)
        {

            if (debugCallback != null) debugCallback("Request URI: ", uri.AbsoluteUri);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.UserAgent = "Mozilla/5.0 (compatible; SafeID/1.0; +http://www.safeid.com.br)";

            if (cookie != null)
                request.CookieContainer = cookie;

            if (headers != null)
                foreach (String k in headers.Keys)
                    switch (k.ToLower())
                    {

                        default:
                            request.Headers.Add(k, headers[k]);
                            break;
                    }

            //request.ServicePoint.Expect100Continue = false;
            //ServicePointManager.MaxServicePointIdleTime = 2000;

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

            String jData = "";
            try
            {
                // Get the response.
                if (debugCallback != null) debugCallback("GetResponse", "");
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    Encoding enc = Encoding.UTF8;
                    try
                    {
                        enc = Encoding.GetEncoding(response.ContentEncoding);
                    }
                    catch { }

                    Stream dataStream = response.GetResponseStream();
                    using (StreamReader reader = new StreamReader(dataStream, enc))
                        jData = reader.ReadToEnd();
                }

            }
            catch (Exception ex)
            {
                if (debugCallback != null) debugCallback("GetResponse Error", ex.Message);
                try
                {
                    if (ex is WebException)
                        using (WebResponse response = ((WebException)ex).Response)
                        {
                            HttpWebResponse httpResponse = (HttpWebResponse)response;
                            using (Stream data = response.GetResponseStream())
                            using (var reader = new StreamReader(data))
                            {
                                jData = reader.ReadToEnd();
                            }
                        }
                }
                catch { }
            }

            if (debugCallback != null) debugCallback("Return Text", jData);
            if (jData == "")
                return "";
            else
                return jData;
        }


        public static String GetRequest(String request, String host, String data)
        {
            
            JSONRequest r = new JSONRequest();
            r.request = request;
            r.host = host;
            r.data = data;

            return Serialize<JSONRequest>(r);

        }

        public static JSONRequest GetRequest(Stream stream)
        {
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(JSONRequest));
            JSONRequest req = (JSONRequest)ser.ReadObject(stream);
            return req;
        }


        public static JSONResponse GetResponse(String response)
        {
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(JSONResponse));
            using(MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(response)))
                return (JSONResponse)ser.ReadObject(ms);
        }

        public static JSONResponse GetResponse(Stream stream)
        {
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(JSONResponse));
            JSONResponse req = (JSONResponse)ser.ReadObject(stream);
            return req;
        }

        public static String GetResponse(Boolean sucess, String error, String data)
        {
            String ret = "";

            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(JSONResponse));
            JSONResponse r = new JSONResponse();
            r.response = (sucess ? "success" : "failed");
            r.error = error;
            r.data = data;

            using (MemoryStream ms = new MemoryStream())
            {
                ser.WriteObject(ms, r);
                ms.Flush();
                ret = Encoding.UTF8.GetString(ms.ToArray());
            }

            return ret;
        }

        private static string XmlToJSON(string xml, Boolean ignoreRootNode = false)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            if (ignoreRootNode)
                doc.LoadXml(doc.DocumentElement.InnerXml);

            return XmlToJSON(doc);
        }

        private static string XmlToJSON(XmlDocument xmlDoc)
        {
            StringBuilder sbJSON = new StringBuilder();
            sbJSON.Append("{ ");
            XmlToJSONnode(sbJSON, xmlDoc.DocumentElement, true);
            sbJSON.Append("}");

            return sbJSON.ToString();
        }

        //  XmlToJSONnode:  Output an XmlElement, possibly as part of a higher array
        private static void XmlToJSONnode(StringBuilder sbJSON, XmlElement node, bool showNodeName)
        {
            if (showNodeName)
                sbJSON.Append("\"" + SafeJSON(node.Name) + "\": ");
            sbJSON.Append("{");
            // Build a sorted list of key-value pairs
            //  where   key is case-sensitive nodeName
            //          value is an ArrayList of string or XmlElement
            //  so that we know whether the nodeName is an array or not.
            SortedList<string, object> childNodeNames = new SortedList<string, object>();

            //  Add in all node attributes
            if (node.Attributes != null)
                foreach (XmlAttribute attr in node.Attributes)
                    StoreChildNode(childNodeNames, attr.Name, attr.InnerText);

            //  Add in all nodes
            foreach (XmlNode cnode in node.ChildNodes)
            {
                if (cnode is XmlText)
                    StoreChildNode(childNodeNames, "value", cnode.InnerText);
                else if (cnode is XmlElement)
                    StoreChildNode(childNodeNames, cnode.Name, cnode);
            }

            // Now output all stored info
            foreach (string childname in childNodeNames.Keys)
            {
                List<object> alChild = (List<object>)childNodeNames[childname];
                if (alChild.Count == 1)
                    OutputNode(childname, alChild[0], sbJSON, true);
                else
                {
                    sbJSON.Append(" \"" + SafeJSON(childname) + "\": [ ");
                    foreach (object Child in alChild)
                        OutputNode(childname, Child, sbJSON, false);
                    sbJSON.Remove(sbJSON.Length - 2, 2);
                    sbJSON.Append(" ], ");
                }
            }
            sbJSON.Remove(sbJSON.Length - 2, 2);
            sbJSON.Append(" }");
        }

        //  StoreChildNode: Store data associated with each nodeName
        //                  so that we know whether the nodeName is an array or not.
        private static void StoreChildNode(SortedList<string, object> childNodeNames, string nodeName, object nodeValue)
        {
            // Pre-process contraction of XmlElement-s
            if (nodeValue is XmlElement)
            {
                // Convert  <aa></aa> into "aa":null
                //          <aa>xx</aa> into "aa":"xx"
                XmlNode cnode = (XmlNode)nodeValue;
                if (cnode.Attributes.Count == 0)
                {
                    XmlNodeList children = cnode.ChildNodes;
                    if (children.Count == 0)
                        nodeValue = null;
                    else if (children.Count == 1 && (children[0] is XmlText))
                        nodeValue = ((XmlText)(children[0])).InnerText;
                }
            }
            // Add nodeValue to ArrayList associated with each nodeName
            // If nodeName doesn't exist then add it
            List<object> ValuesAL;

            if (childNodeNames.ContainsKey(nodeName))
            {
                ValuesAL = (List<object>)childNodeNames[nodeName];
            }
            else
            {
                ValuesAL = new List<object>();
                childNodeNames[nodeName] = ValuesAL;
            }
            ValuesAL.Add(nodeValue);
        }

        private static void OutputNode(string childname, object alChild, StringBuilder sbJSON, bool showNodeName)
        {
            if (alChild == null)
            {
                if (showNodeName)
                    sbJSON.Append("\"" + SafeJSON(childname) + "\": ");
                sbJSON.Append("null");
            }
            else if (alChild is string)
            {
                if (showNodeName)
                    sbJSON.Append("\"" + SafeJSON(childname) + "\": ");
                string sChild = (string)alChild;
                sChild = sChild.Trim();
                sbJSON.Append("\"" + SafeJSON(sChild) + "\"");
            }
            else
                XmlToJSONnode(sbJSON, (XmlElement)alChild, showNodeName);
            sbJSON.Append(", ");
        }

        // Make a string safe for JSON
        private static string SafeJSON(string sIn)
        {
            StringBuilder sbOut = new StringBuilder(sIn.Length);
            foreach (char ch in sIn)
            {
                if (Char.IsControl(ch) || ch == '\'')
                {
                    int ich = (int)ch;
                    sbOut.Append(@"\u" + ich.ToString("x4"));
                    continue;
                }
                else if (ch == '\"' || ch == '\\' || ch == '/')
                {
                    sbOut.Append('\\');
                }
                sbOut.Append(ch);
            }
            return sbOut.ToString();
        }
    }

    [DataContract]
    public class JSONRequest
    {
        [DataMember]
        public string request;

        [DataMember]
        public string host;

        [DataMember]
        public string data;

        [DataMember]
        public string enterpriseid;
    }

    [DataContract]
    public class JSONResponse
    {
        [DataMember]
        public string response;

        [DataMember]
        public string error;

        [DataMember]
        public string data;
    }
}
