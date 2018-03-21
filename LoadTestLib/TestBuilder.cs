using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using Spring.Collections;
using HtmlAgilityPack;

namespace LoadTestLib
{
    public class TestBuilder
    {
        private List<UriInfo> uriList;
        private List<UriInfo> outUriList;
        private Uri baseUri;
        private HashedSet gets;

        public List<UriInfo> UriList { get { return uriList; } }
        public List<UriInfo> OutUriList { get { return outUriList; } }

        public TestBuilder()
        {
            this.uriList = new List<UriInfo>();
            this.outUriList = new List<UriInfo>();
            this.gets = new HashedSet();
        }

        public void Fetch(Profile profile)
        {
            this.baseUri = profile.BaseUri;
            uriList.Clear();

            foreach (UriInfo uri in profile.Uris)
                if (uri.Target.Host.ToLower() == profile.BaseUri.Host.ToLower())
                {
                    if (!uriList.Exists(u => (u.Equals(uri))))
                        uriList.Add(uri);
                }
                else
                {
                    if (!outUriList.Exists(u => (u.Equals(uri))))
                        outUriList.Add(uri);
                }

            foreach (UriInfo uri in profile.OutUris)
                if (uri.Target.Host.ToLower() == profile.BaseUri.Host.ToLower())
                {
                    if (!uriList.Exists(u => (u.Equals(uri))))
                        uriList.Add(uri);
                }
                else
                {
                    if (!outUriList.Exists(u => (u.Equals(uri))))
                        outUriList.Add(uri);
                }
        }

        public void Fetch(Uri baseUri)
        {
            Fetch(baseUri, null, 3, null);
        }

        public void Fetch(Uri baseUri, IPEndPoint proxy)
        {
            Fetch(baseUri, proxy, 3, null);
        }

        public void Fetch(Uri baseUri, Int32 levels)
        {
            Fetch(baseUri, null, levels, null);
        }

        public void Fetch(Uri baseUri, IPEndPoint proxy, Dictionary<String, String> headers)
        {
            Fetch(baseUri, proxy, 3, headers);
        }

        public void Fetch(Uri baseUri, Int32 levels, Dictionary<String, String> headers)
        {
            Fetch(baseUri, null, levels, headers);
        }

        public void Fetch(Uri baseUri, IPEndPoint proxy, Int32 levels, Dictionary<String,String> headers)
        {
            this.baseUri = baseUri;
            uriList.Clear();
            outUriList.Clear();

            //Captura as URLS do site a utilizar (css, javascript, img e etc...)
            ResultData baseRequest = Request.GetRequest(baseUri, proxy, headers);
            if (baseRequest.Error)
                throw new Exception("Erro ao criar o ambiente de teste: " + baseRequest.ErrorMessage);

            GetLinks(baseUri, proxy, 1, levels);

            this.gets.Clear();
        }

        public TestEnvironment Build(IPEndPoint proxy, ClientType type, Int16 sbuConcurrentConnections)
        {
            TestEnvironment env = new TestEnvironment();
            env.BaseUri = this.baseUri;
            env.SBUConcurrentConnections = sbuConcurrentConnections;
            env.Type = type;
            env.Proxy = proxy;
            env.Uris = this.uriList;
            env.OutUris = this.outUriList;

            return env;
        }

        private void GetLinks(Uri baseUri, IPEndPoint proxy, Int32 level, Int32 maxLevel)
        {

            //Se ja foi processado, ignora
            if (gets.Contains(baseUri.AbsoluteUri))
                return;

            Console.WriteLine(level + " ==> " + baseUri.AbsoluteUri);

            gets.Add(baseUri.AbsoluteUri);

            List<UriInfo> tags = new List<UriInfo>();

            tags.Add(new UriInfo(baseUri));

            //HtmlDocument doc = new HtmlDocument();
            //doc.LoadHtml(html);

            HtmlWeb web = new HtmlWeb();

            HtmlDocument doc = null;
            
            if (proxy == null)
                doc = web.Load(baseUri.AbsoluteUri);
            else
                doc = web.Load(baseUri.AbsoluteUri, "GET", new WebProxy("http://" + proxy.Address + ":" + proxy.Port), null);

            HtmlNodeCollection scripts = doc.DocumentNode.SelectNodes("//script[@src]");
            if (scripts != null)
                foreach (HtmlNode link in scripts)
                {
                    HtmlAttribute att = link.Attributes["src"];
                    UriInfo tmp = StringToLink(baseUri, att.Value);
                    if (tmp != null)
                        tags.Add(tmp);

                }

            HtmlNodeCollection links = doc.DocumentNode.SelectNodes("//link[@href]");
            if (links != null)
                foreach (HtmlNode link in links)
                {
                    HtmlAttribute att = link.Attributes["href"];
                    HtmlAttribute rel = link.Attributes["rel"];

                    if ((rel != null) && (!String.IsNullOrEmpty(rel.Value)))
                    {
                        switch (rel.Value.ToLower())
                        {
                            case "stylesheet":
                            case "icon":
                            case "next":
                            case "canonical":
                                UriInfo tmp = StringToLink(baseUri, att.Value);
                                if (tmp != null)
                                    tags.Add(tmp);

                                break;
                        }
                    }

                }


            HtmlNodeCollection imgs = doc.DocumentNode.SelectNodes("//img[@src]");
            if (imgs != null)
                foreach (HtmlNode link in imgs)
                {
                    HtmlAttribute att = link.Attributes["src"];
                    UriInfo tmp = StringToLink(baseUri, att.Value);
                    if (tmp != null)
                        tags.Add(tmp);

                }

            if (level < maxLevel)
            {
                HtmlNodeCollection aLinks = doc.DocumentNode.SelectNodes("//a[@href]");
                if (aLinks != null)
                    foreach (HtmlNode link in aLinks)
                    {
                        HtmlAttribute att = link.Attributes["href"];
                        UriInfo tmp = StringToLink(baseUri, att.Value);
                        if ((tmp != null) && (tmp.Target.Host.ToLower() == baseUri.Host.ToLower()) && (tmp.Target.AbsoluteUri != baseUri.AbsoluteUri))
                            GetLinks(tmp.Target, proxy, level + 1, maxLevel);
                    }
            }

            foreach (UriInfo l in tags)
            {
                if ((l != null) && (l.Target.Host.ToLower() == baseUri.Host.ToLower()))
                {
                    if (!uriList.Exists(u => (u.Equals(l))))
                        uriList.Add(l);
                }
                else
                {
                    if (!outUriList.Exists(u => (u.Equals(l))))
                        outUriList.Add(l);
                }

            }

        }

        private static UriInfo StringToLink(Uri referer, String s)
        {
            UriInfo ret = null;

            try
            {
                if (s.IndexOf("//") == 0)
                    s = referer.Scheme + ":" + s;

                Uri tmp = new Uri(s);
                ret = new UriInfo(tmp, referer);
            }
            catch
            {
                try
                {
                    Uri tmp = new Uri(referer.Scheme + "://" + referer.Host + (s.IndexOf("/") == 0 ? s : "/" + s));
                    ret = new UriInfo(tmp, referer);
                }
                catch { }
            }

            return ret;
        }
    }
}
