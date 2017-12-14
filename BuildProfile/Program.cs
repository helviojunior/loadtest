using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fiddler;
using System.Net;
using LoadTestLib;

namespace BuildProfile
{
    class Program
    {
        static List<UriInfo> urls = new List<UriInfo>();
        static List<UriInfo> outUrls = new List<UriInfo>();
        static Uri baseURI = null;
        static String host = null;
        static void Main(string[] args)
        {
            Console.CancelKeyPress += new ConsoleCancelEventHandler(Console_CancelKeyPress);

            while (host == null)
            {
                String tmp = "";
                Console.Write("Digite o domínio filtro: ");
                tmp = Console.ReadLine();

                try
                {
                    WebClient client = new WebClient();
                    client.DownloadData(new Uri("http://" + tmp));

                    host = tmp;
                }
                catch(Exception ex) {
                    Console.WriteLine("Falha na checagem do domínio: " + ex.Message);
                }
            }

            
            Fiddler.CONFIG.IgnoreServerCertErrors = true;
            Fiddler.FiddlerApplication.BeforeResponse += new SessionStateHandler(FiddlerApplication_BeforeResponse);

            // Because we've chosen to decrypt HTTPS traffic, makecert.exe must
            // be present in the Application folder.
            Fiddler.FiddlerApplication.Startup(8080, true, true, true);

            Console.WriteLine("Proxy iniciado na porta " + Fiddler.FiddlerApplication.oProxy.ListenPort);
            Console.WriteLine("Pressione CTRL+C para finalizar o programa.");

            // Wait Forever for the user to hit CTRL+C.  
            // BUG BUG: Doesn't properly handle shutdown of Windows, etc.
            Object forever = new Object();
            lock (forever)
            {
                System.Threading.Monitor.Wait(forever);
            }
        }


        static void FiddlerApplication_BeforeResponse(Session oSession)
        {
            try
            {
                Uri tmp = new Uri(oSession.fullUrl);
                Uri referer = getReferer(oSession);

                if (baseURI == null)
                    baseURI = tmp;

                if (tmp.Host.ToLower() == host)
                    urls.Add(new UriInfo(tmp, referer));
                else
                    outUrls.Add(new UriInfo(tmp, referer));

                Console.WriteLine(oSession.fullUrl);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + ex.StackTrace);
            }
        }

        /// <summary>
        /// When the user hits CTRL+C, this event fires.  We use this to shut down and unregister our FiddlerCore.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            Console.WriteLine("Shutting down...");
            Fiddler.FiddlerApplication.Shutdown();
            System.Threading.Thread.Sleep(750);

            LoadTestLib.Profile prof = new LoadTestLib.Profile();
            prof.BaseUri = baseURI;

            foreach (UriInfo s in urls)
            {
                try
                {
                    prof.Uris.Add(s);
                }
                catch { }
            }

            foreach (UriInfo s in outUrls)
            {
                try
                {
                    prof.OutUris.Add(s);
                }
                catch { }
            }

            if (prof.Uris.Count > 0)
                prof.SaveToFile(host + "-" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".prof");

        }


        static Uri getReferer(Session oSession)
        {
            Uri ret = new Uri(oSession.fullUrl);
            ClientChatter request = oSession.oRequest;
            if (request.headers.Exists("Referer"))
            {
                ret = new Uri(request.headers["Referer"]);
            }
            else if (request.headers.Exists("referer"))
            {
                ret = new Uri(request.headers["referer"]);
            }
            return ret;
        }

    }
}
