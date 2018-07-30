using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Net;
using System.Diagnostics;
//using DBManager;
using System.Text.RegularExpressions;
using ImageTools;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Configuration;
using SafeTrend.Data;
using SafeTrend.Data.Update;
using System.IO.Compression;
using LoadTestLib.ZabbixGet;
using System.Reflection;
using System.Web.Script.Serialization;

namespace LoadTestLib
{
    [Serializable()]
    public class TestEnvironment: IDisposable, ICloneable
    {
        private enum CheckType
        {
            Css = 1,
            JS = 2,
            Image = 3
        }

        public Uri BaseUri { get; set; }
        public List<UriInfo> Uris { get; set; }
        public List<UriInfo> OutUris { get; set; }
        public ClientType Type { get; set; }
        public Int16 SBUConcurrentConnections { get; set; }
        public IPEndPoint Proxy { get; set; }
        public Int16 VirtualUsers { get; set; }
        public Int32 SleepTime { get; set; }
        
        //public SQLConfig SQLConfig { get; set; }
        public DbConnectionString ConnectionString { get; set; }
        public String TestName { get; set; }
        public Dictionary<String,String> HTTPHeaders { get; set; }

        public List<ZabbixConfig> ZabbixMonitors { get; set; }

        public DateTime dStart = DateTime.Now;
        public DateTime dEnd = DateTime.Now;

        public TestEnvironment()
        {
            this.Uris = new List<UriInfo>();
            this.OutUris = new List<UriInfo>();
            this.Type = ClientType.VU;
            this.SBUConcurrentConnections = 5;
            this.SleepTime = 0;
            this.Proxy = null;
            this.VirtualUsers = 10;
            this.TestName = DateTime.Now.ToString("yyyyMMddHHmmssffff");
            this.HTTPHeaders = new Dictionary<string, string>();
            this.ZabbixMonitors = new List<ZabbixConfig>();
            //this.SQLConfig = new SQLConfig();
            //this.SQLConfig.Database = this.TestName;
        }

        public void BuildReports()
        {
            using(ReportBuilder rpt = new ReportBuilder(this))
                rpt.Build();
        }

        public void DropDatabase()
        {
            LoadTestDatabase db = new LoadTestDatabase(this.ConnectionString);
            try
            {
                db.DropDatabase("");
            }
            catch (Exception ex)
            {
                throw new Exception("Falha ao excluir a base de dados: " + ex.Message);
            }

        }

        public void Start()
        {
            if (Uris.Count == 0)
                throw new Exception("Listagem de URLs está vazia");
            
            LoadTestDatabase db = null;

            try
            {

                if (this.ConnectionString == null)
                    throw new Exception("ConnectionStrings is null");

                db = new LoadTestDatabase(this.ConnectionString);

                new AutomaticUpdater().Run(db, UpdateScriptRepository.GetScriptsBySqlProviderName(this.ConnectionString));

            }
            catch (Exception ex)
            {
                throw new Exception("Erro on load/update database", ex);
            }

            try
            {
                this.TestName = DateTime.Now.ToString("yyyyMMddHHmmssffff");
                db = new LoadTestDatabase(this.ConnectionString);
                //db.CreateDatabase(this.TestName);    
            }
            catch (Exception ex)
            {
                throw new Exception("Falha ao conectar a base de dados: " + ex.Message);
            }

            dStart = DateTime.Now;
            
            //Verifica a limitação de utilização free
            if (this.VirtualUsers > 5000)
            {
                db.insertMessages(this.TestName, "0. Licenciamento safetrend.com.br", "A versão free deste aplicativo permite no máximo 5000 VU/SBU");
                this.VirtualUsers = 5000;
            }


            JavaScriptSerializer ser = new JavaScriptSerializer();
            
            if (this.ZabbixMonitors != null)
            {
                foreach (ZabbixConfig zbxHost in this.ZabbixMonitors)
                {
                    //Realiza teste de conex'ao em cada um dos zabbix listados
                    try
                    {
                        using (Zabbix zbx = new Zabbix(zbxHost.Host, zbxHost.Port))
                        {
                            StringBuilder hostInfo = new StringBuilder();

                            String hostname = zbx.GetItem("system.hostname");

                            hostInfo.AppendLine("<strong>Config name:</strong>&nbsp;" + zbxHost.Name);
                            hostInfo.AppendLine("<strong>Config ip:</strong>&nbsp;" + zbxHost.Host + ":" + zbxHost.Port);
                            hostInfo.AppendLine("<strong>Hostname:</strong>&nbsp;" + hostname);

                            String memory = zbx.GetItem("vm.memory.size[total]");
                            try
                            {
                                Double m = Double.Parse(memory);
                                ;

                                hostInfo.AppendLine("<strong>Memória total:</strong>&nbsp;" + FileResources.formatData(m, ChartDataType.Bytes));
                            }
                            catch { }

                            String cpus = zbx.GetItem("system.cpu.discovery");
                            try
                            {

                                Dictionary<String, Object[]> values = ser.Deserialize<Dictionary<String, Object[]>>(cpus);
                                //List<Dictionary<String, String>> values = ser.Deserialize<List<Dictionary<String, String>>>(cpus);
                                hostInfo.AppendLine("<strong>Quantidade de vCPU:</strong>&nbsp;" + values["data"].Length);

                            }
                            catch { }


                            String disk = zbx.GetItem("vfs.fs.discovery");
                            try
                            {

                                Dictionary<String, Object[]> values = ser.Deserialize<Dictionary<String, Object[]>>(disk);
                                //List<Dictionary<String, String>> values = ser.Deserialize<List<Dictionary<String, String>>>(cpus);
                                Int32 cnt = 1;
                                foreach (Object o in values["data"])
                                {
                                    String name = "";
                                    String type = "";

                                    if (o is Dictionary<String, Object>)
                                    {
                                        Dictionary<String, Object> tO = (Dictionary<String, Object>)o;

                                        name = tO["{#FSNAME}"].ToString();
                                        type = tO["{#FSTYPE}"].ToString();

                                        if (!String.IsNullOrEmpty(name))
                                        {
                                            switch (type.ToLower())
                                            {
                                                case "rootfs":
                                                case "sysfs":
                                                case "proc":
                                                case "devtmpfs":
                                                case "devpts":
                                                case "tmpfs":
                                                case "fusectl":
                                                case "debugfs":
                                                case "securityfs":
                                                case "pstore":
                                                case "cgroup":
                                                case "rpc_pipefs":
                                                case "unknown":
                                                case "usbfs":
                                                case "binfmt_misc":
                                                case "autofs":
                                                    break;

                                                default:
                                                    hostInfo.AppendLine("<strong>Disco " + cnt + ":</strong>&nbsp;" + name + " --> " + type);
                                                    cnt++;
                                                    break;
                                            }
                                        }
                                    }
                                }


                            }
                            catch { }

                            String netIfs = zbx.GetItem("net.if.discovery");
                            try
                            {
                                List<String> exclusionList = new List<string>();
                                exclusionList.Add("Bluetooth");
                                exclusionList.Add("TAP-Windows");
                                exclusionList.Add("WFP");
                                exclusionList.Add("QoS");
                                exclusionList.Add("Diebold");
                                exclusionList.Add("Microsoft Kernel Debug");
                                exclusionList.Add("WAN Miniport");
                                exclusionList.Add("Loopback");
                                exclusionList.Add("Wi-Fi Direct Virtual");
                                exclusionList.Add("Filter Driver");
                                exclusionList.Add("Pseudo-Interface");

                                Dictionary<String, Object[]> values = ser.Deserialize<Dictionary<String, Object[]>>(netIfs);
                                Int32 cnt = 1;
                                foreach (Object o in values["data"])
                                {
                                    String ifName = "";

                                    if (o is Dictionary<String, Object>)
                                    {
                                        Dictionary<String, Object> tO = (Dictionary<String, Object>)o;

                                        ifName = tO["{#IFNAME}"].ToString();

                                        if (!String.IsNullOrEmpty(ifName))
                                        {
                                            Boolean insert = true;

                                            foreach (String e in exclusionList)
                                                if (ifName.IndexOf(e, StringComparison.CurrentCultureIgnoreCase) >= 0)
                                                    insert = false;

                                            if (insert)
                                            {
                                                hostInfo.AppendLine("<strong>Interface de rede " + cnt + ":</strong>&nbsp;" + ifName);
                                                cnt++;
                                            }

                                        }
                                    }
                                }
                            }
                            catch { }

                            db.insertMessages(this.TestName, "1. Zabbix Monitor", hostInfo.ToString());
                        }

                    }
                    catch
                    {
                        db.insertMessages(this.TestName, "0. Zabbix Monitor", "Erro ao resgatar informação do Host Zabbix " + zbxHost.Name + " (" + zbxHost.Host + ":" + zbxHost.Port + ")");
                    }
                }
            }


            //Antes de iniciar o stress test realiza a análise de conteúdo
            this.ContentAnalizer();

            if (this.SleepTime < 0)
                this.SleepTime = 0;

            Int16 factor = 300;

            if (this.Type == ClientType.VU)
                factor = (Int16)(factor * 2);

            if (this.VirtualUsers < 0)
                this.VirtualUsers = 1;

            //Inicia servi;co de monitoramento
            using (TestEnvironment tmp = (TestEnvironment)this.Clone())
            {
                StartApplication("ZabbixGet.exe", tmp);
            }


            using (TestEnvironment tmp = (TestEnvironment)this.Clone())
            {
                Int16 r = this.VirtualUsers;
                if (r > factor)
                    r = factor;
                tmp.VirtualUsers = r;
                FileInfo tFile = new FileInfo(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));

                tmp.SaveToFile(tFile.FullName);


                Console.WriteLine("Para iniciar mais conexões utilize o comando abaixo, cada cliente iniciará {0} usuários virtuais", tmp.VirtualUsers);
                Console.WriteLine("\"{0}\\{1}\" \"{2}\"", Environment.CurrentDirectory, "client.exe", tFile.FullName);
                Console.WriteLine("");

            }


            Int16 restConn = this.VirtualUsers;
            while (restConn > factor)
            {
                using (TestEnvironment tmp = (TestEnvironment)this.Clone())
                {
                    tmp.VirtualUsers = factor;
                    StartApplication("client.exe", tmp);
                    restConn -= factor;
                }
            }

            if (restConn > 0)
            {
                using (TestEnvironment tmp = (TestEnvironment)this.Clone())
                {
                    tmp.VirtualUsers = restConn;
                    StartApplication("client.exe", tmp);
                }
            }

        }

        public void ContentAnalizer()
        {
            try
            {

                LoadTestDatabase db = new LoadTestDatabase(this.ConnectionString);

                //Grava os IPs de dns
                try
                {
                    IPAddress[] dns = Dns.GetHostAddresses(this.BaseUri.Host);
                    List<String> ips = new List<string>();
                    foreach (IPAddress ip in dns)
                        ips.Add(ip.ToString());
                    db.insertMessages(this.TestName, "1. Resolução de nome", "Endereços IP associados ao host " + this.BaseUri.Host + ": " + String.Join(", ", ips));
                }
                catch { }

                try
                {
                    if (this.Proxy != null)
                        db.insertMessages(this.TestName, "1. Proxy", "Proxy Server: " + Proxy.Address.ToString() + ":" + Proxy.Port);
                }
                catch { }

                //Minifica arquivos arquivos css (.css) e javascript (.js)
                foreach (UriInfo u in this.Uris)
                    AnalizeURI(db, u);

                foreach (UriInfo u in this.OutUris)
                    AnalizeURI(db, u);
                
                db.Dispose();
            }
            catch { }
        }

        private void AnalizeURI(LoadTestDatabase db, UriInfo u)
        {

            switch (GetExtension(u.Target))
            {
                case ".txt":
                    //Sai sem analizar
                    return; 
                    break;
            }

            Console.WriteLine("ContentAnalizer> " + u.Target);
            Console.Write("\tBaixando: ");
            ResultData request = Request.GetRequest(u.Target, this.Proxy, false, HTTPHeaders);

            if (request.Error)
            {
                Console.WriteLine("Err " + request.ErrorMessage);
                db.insertMessages(this.TestName, "3. Erro na chamada de URL", "URL: <a href=\"" + u.Target.AbsoluteUri + "\" target=\"_blank\">" + u.Target.AbsoluteUri + "</a>" + Environment.NewLine + (u.Referer != null ? "Referer: <a href=\"" + u.Referer.AbsoluteUri + "\" target=\"_blank\">" + u.Referer.AbsoluteUri + "</a>" + Environment.NewLine : "") + Environment.NewLine + "Tempo de resposta: " + FileResources.formatData(request.Time.TotalMilliseconds, ChartDataType.Integer) + "ms " + Environment.NewLine + "Código de retorno HTTP: " + request.Code + Environment.NewLine + Environment.NewLine + (!String.IsNullOrEmpty(request.ErrorMessage) ? "Texto do erro: " + request.ErrorMessage : ""));
            }
            else
            {
                Console.WriteLine("OK " + request.DataLength);

                Console.Write("\tVerificando: ");

                //Tempo de resposta
                if (request.Time.TotalMilliseconds > 1500F)
                    db.insertMessages(this.TestName, "2. Alto tempo de resposta", "URL: <a href=\"" + u.Target.AbsoluteUri + "\" target=\"_blank\">" + u.Target.AbsoluteUri + "</a>" + Environment.NewLine + (u.Referer != null ? "Referer: <a href=\"" + u.Referer.AbsoluteUri + "\" target=\"_blank\">" + u.Referer.AbsoluteUri + "</a>" + Environment.NewLine : "") + Environment.NewLine + "Tempo de resposta: " + FileResources.formatData(request.Time.TotalMilliseconds, ChartDataType.Integer) + " ms");

                String extension = GetExtension(u.Target);

                if (!MIMECheck.CheckMime(request.ContentType.ToLower(), extension))
                {
                    db.insertMessages(this.TestName, "4. ContentType inválido", "URL: <a href=\"" + u.Target.AbsoluteUri + "\" target=\"_blank\">" + u.Target.AbsoluteUri + "</a>" + Environment.NewLine + (u.Referer != null ? "Referer: <a href=\"" + u.Referer.AbsoluteUri + "\" target=\"_blank\">" + u.Referer.AbsoluteUri + "</a>" + Environment.NewLine : "") + Environment.NewLine + "Extensão: " + extension + Environment.NewLine + "MIME Type: " + request.ContentType);

                }

                if ((request.OriginalDataLength > 0) && (request.OriginalDataLength < request.DataLength))
                    db.insertGzipOptimization(this.TestName, request.RequestUri, request.OriginalDataLength, request.DataLength);
                else if ((request.OriginalDataLength > 0) && (request.OriginalDataLength == request.DataLength))
                {
                    //Realiza a compactação Gzip para estimar a otimização
                    Byte[] tmp = GzipCompress(request.Data);
                    
                    double percent = 100F - (((double)tmp.Length / (double)request.DataLength) * 100F);

                    if (percent > 10)
                        db.insertMessages(this.TestName, "7. Otimização de rede (compactação gzip/deflate)", "URL: <a href=\"" + u.Target.AbsoluteUri + "\" target=\"_blank\">" + u.Target.AbsoluteUri + "</a>" + Environment.NewLine + (u.Referer != null ? "Referer: <a href=\"" + u.Referer.AbsoluteUri + "\" target=\"_blank\">" + u.Referer.AbsoluteUri + "</a>" + Environment.NewLine : "") + Environment.NewLine + "Redução estimada de: " + FileResources.formatData(percent, ChartDataType.Percent) + Environment.NewLine + "Tamanho original: " + FileResources.formatData(request.DataLength, ChartDataType.Bytes) + Environment.NewLine + "Tamanho reduzido: " + FileResources.formatData(tmp.Length, ChartDataType.Bytes));
                }

               //Consições específicas por conteúdo
                switch (request.ContentType.ToLower())
                {
                    case "application/x-pointplus":
                    case "text/css":
                        Check(db, CheckType.Css, u, request);
                        break;

                    case "application/x-javascript":
                    case "application/javascript":
                    case "application/ecmascript":
                    case "text/javascript":
                    case "text/ecmascript":
                        Check(db, CheckType.JS, u, request);
                        break;

                    case "image/png":
                        Check(db, CheckType.Image, u, request);
                        break;

                    default:
                        //Verifica por extenção
                        switch (extension)
                        {
                            case ".css":
                                Check(db, CheckType.Css, u, request);
                                break;

                            case ".js":
                                Check(db, CheckType.JS, u, request);
                                break;

                            case ".png":
                            case ".jpg":
                            case ".gif":
                                Check(db, CheckType.Image, u, request);
                                break;
                        }
                        break;

                }

                Console.WriteLine("OK");
            }
        }

        private String GetExtension(Uri uri)
        {
            String ext = "";
            FileInfo tmpFile = null;
            try
            {
                String filename = uri.PathAndQuery.Replace('/', Path.DirectorySeparatorChar);
                tmpFile = new FileInfo(filename);
            }
            catch { }

            if (tmpFile != null)
                ext = tmpFile.Extension.ToLower();

            return ext;
        }

        private void Check(LoadTestDatabase db, CheckType type, UriInfo info, ResultData request)
        {
            switch (type){
                case CheckType.Css:
                case CheckType.JS:
                    String value = request.ContentEncoding2.GetString(request.Data);
                    String min = minify(value);
                    String expanded = expand(value);

                    double percentMin = 100F - (((double)min.Length / (double)value.Length) * 100F);
                    double percentExp = 100F - (((double)value.Length / (double)expanded.Length) * 100F);

                    if (percentMin > 10)
                    {
                        db.insertMessages(this.TestName, "5. Otimização de arquivos (minify *.js, *.css)", "URL: <a href=\"" + info.Target.AbsoluteUri + "\" target=\"_blank\">" + info.Target.AbsoluteUri + "</a>" + Environment.NewLine + (info.Referer != null ? "Referer: <a href=\"" + info.Referer.AbsoluteUri + "\" target=\"_blank\">" + info.Referer.AbsoluteUri + "</a>" + Environment.NewLine : "") + Environment.NewLine + "Redução estimada de: " + FileResources.formatData(percentMin, ChartDataType.Percent) + Environment.NewLine + "Tamanho original: " + FileResources.formatData(value.Length, ChartDataType.Bytes) + Environment.NewLine + "Tamanho reduzido: " + FileResources.formatData(min.Length, ChartDataType.Bytes));
                        db.insertOptimization(this.TestName, request.RequestUri, value.Length, min.Length);
                    }

                    if (percentExp > 3)
                        db.insertNonOptimization(this.TestName, request.RequestUri, value.Length, expanded.Length);

                    break;

                case CheckType.Image:

                    String text = "";
                    String imageInfo = "";

                    imageInfo = "Tamanho: " + FileResources.formatData(request.DataLength, ChartDataType.Bytes) + Environment.NewLine;


                    if (request.DataLength > (100 * 1024)) //100 Kb
                        text += "* Tamanho da imagem acima de 100Kb" + Environment.NewLine;

                    

                    using (MemoryStream stm = new MemoryStream(request.Data))
                    using (Image bmp = Image.FromStream(stm))
                    {
                        Boolean resolution = false;
                        PixelFormat pxFormat = PixelFormat.Format24bppRgb;
                        float saveRes = bmp.HorizontalResolution;

                        try
                        {
                            imageInfo += "Resolução: " + bmp.Width + " x " + bmp.Height + "px" + Environment.NewLine;
                        }
                        catch { }

                        try
                        {
                            if ((((Int32)bmp.HorizontalResolution) > 72) || (((Int32)bmp.VerticalResolution) > 72))
                                text += "* Qualidade (DPI) da imagem acima de 72" + Environment.NewLine;

                            imageInfo += "DPI: " + bmp.HorizontalResolution + Environment.NewLine;
                            resolution = true;
                            saveRes = 72;
                        }
                        catch { }

                        try
                        {
                            switch (bmp.PixelFormat)
                            {
                                case System.Drawing.Imaging.PixelFormat.Format32bppArgb:
                                case System.Drawing.Imaging.PixelFormat.Format32bppPArgb:
                                case System.Drawing.Imaging.PixelFormat.Format32bppRgb:
                                case System.Drawing.Imaging.PixelFormat.Format48bppRgb:
                                case System.Drawing.Imaging.PixelFormat.Format64bppArgb:
                                case System.Drawing.Imaging.PixelFormat.Format64bppPArgb:
                                    text += "* Qualidade (Bit depth) da imagem acima de 24 bits" + Environment.NewLine;
                                    resolution = true;
                                    pxFormat = PixelFormat.Format24bppRgb;
                                    break;
                            }

                            switch (bmp.PixelFormat)
                            {
                                case System.Drawing.Imaging.PixelFormat.Format8bppIndexed:
                                    imageInfo += "Bit depth: 16 bits" + Environment.NewLine;
                                    pxFormat = bmp.PixelFormat;
                                    break;

                                case System.Drawing.Imaging.PixelFormat.Format16bppArgb1555:
                                case System.Drawing.Imaging.PixelFormat.Format16bppGrayScale:
                                case System.Drawing.Imaging.PixelFormat.Format16bppRgb555:
                                case System.Drawing.Imaging.PixelFormat.Format16bppRgb565:
                                    imageInfo += "Bit depth: 16 bits" + Environment.NewLine;
                                    pxFormat = bmp.PixelFormat;
                                    break;


                                case System.Drawing.Imaging.PixelFormat.Format24bppRgb:
                                    imageInfo += "Bit depth: 24 bits" + Environment.NewLine;
                                    pxFormat = PixelFormat.Format24bppRgb;
                                    break;

                                case System.Drawing.Imaging.PixelFormat.Format32bppArgb:
                                case System.Drawing.Imaging.PixelFormat.Format32bppPArgb:
                                case System.Drawing.Imaging.PixelFormat.Format32bppRgb:
                                    imageInfo += "Bit depth: 32 bits" + Environment.NewLine;
                                    pxFormat = PixelFormat.Format24bppRgb;
                                    break;

                                case System.Drawing.Imaging.PixelFormat.Format48bppRgb:
                                    imageInfo += "Bit depth: 48 bits" + Environment.NewLine;
                                    pxFormat = PixelFormat.Format24bppRgb;
                                    break;

                                case System.Drawing.Imaging.PixelFormat.Format64bppArgb:
                                case System.Drawing.Imaging.PixelFormat.Format64bppPArgb:
                                    imageInfo += "Bit depth: 64 bits" + Environment.NewLine;
                                    pxFormat = PixelFormat.Format24bppRgb;
                                    break;

                                default:
                                    imageInfo += "Bit depth: " + bmp.PixelFormat.ToString() + Environment.NewLine;
                                    break;
                            }
                        }
                        catch { }

                        try
                        {
                            using (ImageInfo imgInfo = new ImageInfo(bmp))
                            {
                                if (imgInfo.HasExif)
                                {
                                    text += "* Imagem com informações EXIF. A remoção desta informação reduz o tamanho da imagem" + Environment.NewLine;
                                    resolution = true;
                                }

                            }
                        }
                        catch { }

                        //Realiza a Otimização sugerida e calcula o tamanho estimado a conseguir
                        if (resolution)
                        {

                            try
                            {
                                ImageFormat saveFormat = ImageFormat.Jpeg;
                                if (bmp.RawFormat.Equals(System.Drawing.Imaging.ImageFormat.Jpeg)) saveFormat = System.Drawing.Imaging.ImageFormat.Jpeg;
                                if (bmp.RawFormat.Equals(System.Drawing.Imaging.ImageFormat.Bmp)) saveFormat = System.Drawing.Imaging.ImageFormat.Bmp;
                                if (bmp.RawFormat.Equals(System.Drawing.Imaging.ImageFormat.Png)) saveFormat = System.Drawing.Imaging.ImageFormat.Png;
                                if (bmp.RawFormat.Equals(System.Drawing.Imaging.ImageFormat.Emf)) saveFormat = System.Drawing.Imaging.ImageFormat.Emf;
                                if (bmp.RawFormat.Equals(System.Drawing.Imaging.ImageFormat.Exif)) saveFormat = System.Drawing.Imaging.ImageFormat.Exif;
                                if (bmp.RawFormat.Equals(System.Drawing.Imaging.ImageFormat.Gif)) saveFormat = System.Drawing.Imaging.ImageFormat.Gif;
                                if (bmp.RawFormat.Equals(System.Drawing.Imaging.ImageFormat.Icon)) saveFormat = System.Drawing.Imaging.ImageFormat.Icon;
                                if (bmp.RawFormat.Equals(System.Drawing.Imaging.ImageFormat.MemoryBmp)) saveFormat = System.Drawing.Imaging.ImageFormat.MemoryBmp;
                                if (bmp.RawFormat.Equals(System.Drawing.Imaging.ImageFormat.Tiff)) saveFormat = System.Drawing.Imaging.ImageFormat.Tiff;
                                if (bmp.RawFormat.Equals(System.Drawing.Imaging.ImageFormat.Wmf)) saveFormat = System.Drawing.Imaging.ImageFormat.Wmf;


                                Bitmap newImage = new Bitmap(bmp.Width, bmp.Height, pxFormat);
                                newImage.SetResolution(saveRes, saveRes);
                                Graphics g = Graphics.FromImage(newImage);
                                g.SmoothingMode = SmoothingMode.AntiAlias;
                                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                                g.DrawImage(bmp, 0, 0);
                                g.Dispose();

                                MemoryStream newStm = new MemoryStream();
                                newImage.Save(newStm, saveFormat);

                                if (newStm.Length < request.DataLength)
                                {
                                    db.insertOptimization(this.TestName, request.RequestUri, request.DataLength, newStm.Length);

                                    double percent2 = 100F - (((double)newStm.Length / (double)request.DataLength) * 100F);

                                    text += Environment.NewLine;
                                    text += "Tamanho estimado após redução: " + FileResources.formatData(newStm.Length, ChartDataType.Bytes) + Environment.NewLine;
                                    text += "Redução estimada de: " + FileResources.formatData(percent2, ChartDataType.Percent) + Environment.NewLine;
                                }
                                else
                                {
                                    //Não há redução, a imagem está otimizada
                                    text = "";
                                }

                                newImage.Dispose();
                                newImage = null;
                                newStm.Dispose();
                                newStm = null;
                            }
                            catch (Exception ex)
                            {
#if DEBUG
                                text += "Erro ao calcular a redução: " + ex.Message;
#endif
                            }
                        }

                    }

                    if (text != "")
                        db.insertMessages(this.TestName, "6. Otimização de imagem", "Img URL: <a href=\"" + info.Target.AbsoluteUri + "\" target=\"_blank\">" + info.Target.AbsoluteUri + "</a>" + Environment.NewLine + (info.Referer != null ? "Referer: <a href=\"" + info.Referer.AbsoluteUri + "\" target=\"_blank\">" + info.Referer.AbsoluteUri + "</a>" + Environment.NewLine : "") + Environment.NewLine + imageInfo + Environment.NewLine + text);

                    break;
            }
        }

        public void LoadConfig(String fileName)
        {
            Byte[] fData = File.ReadAllBytes(fileName);
            LoadConfig(new MemoryStream(fData));
        }

        public void LoadConfig(MemoryStream rawData)
        {
            IFormatter formato = new BinaryFormatter();

            TestEnvironment item = (TestEnvironment)formato.Deserialize(rawData);
            rawData.Dispose();

            rawData.Close();
            rawData = null;

            this.BaseUri = item.BaseUri;
            this.Uris = item.Uris;
            this.OutUris = item.OutUris;
            this.Type = item.Type;
            this.SBUConcurrentConnections = item.SBUConcurrentConnections;
            this.Proxy = item.Proxy;
            this.VirtualUsers = item.VirtualUsers;
            this.ConnectionString = item.ConnectionString;
            this.TestName = item.TestName;
            this.dEnd = item.dEnd;
            this.dStart = item.dStart;
            this.ZabbixMonitors = item.ZabbixMonitors;
            this.SleepTime = item.SleepTime;
        }

        private static Byte[] GzipCompress(Byte[] data)
        {
            MemoryStream ms = new MemoryStream();
            using (GZipStream zip = new GZipStream(ms, CompressionMode.Compress, true))
            {
                zip.Write(data, 0, data.Length);
            }

            ms.Position = 0;
            MemoryStream outStream = new MemoryStream();

            byte[] compressed = new byte[ms.Length];
            ms.Read(compressed, 0, compressed.Length);

            byte[] gzBuffer = new byte[compressed.Length + 4];
            System.Buffer.BlockCopy(compressed, 0, gzBuffer, 4, compressed.Length);
            System.Buffer.BlockCopy(BitConverter.GetBytes(data.Length), 0, gzBuffer, 0, 4);
            return gzBuffer;
        }

        public Object Clone()
        {
            TestEnvironment tmp = new TestEnvironment();
            tmp.BaseUri = this.BaseUri;
            tmp.Uris = this.Uris;
            tmp.Type = this.Type;
            tmp.SBUConcurrentConnections = this.SBUConcurrentConnections;
            tmp.Proxy = this.Proxy;
            tmp.VirtualUsers = this.VirtualUsers;
            tmp.ConnectionString = this.ConnectionString;
            tmp.TestName = this.TestName;
            tmp.dStart = this.dStart;
            tmp.dEnd = this.dEnd;
            tmp.ZabbixMonitors = this.ZabbixMonitors;
            tmp.SleepTime = this.SleepTime;

            return tmp;
        }


        public void Stop()
        {

            Console.WriteLine("Finalizando, aguardando 10 segundos...");
            this.dEnd = DateTime.Now;
            System.Threading.Thread.Sleep(10000);

#if DEBUG
            this.SaveToFile("temp.env");
#endif

            KillAll("client");
            KillAll("ZabbixGet");
        }


        public void Dispose()
        {
            this.Uris = null;
            this.Proxy = null;

        }



        public void SaveToFile(String filename)
        {
            FileInfo file = new FileInfo(filename);
            if (!file.Directory.Exists)
                file.Directory.Create();
            file = null;

            IFormatter formato = new BinaryFormatter();
            Byte[] returnBytes = new Byte[0];
            MemoryStream stream = new MemoryStream();
            formato.Serialize(stream, this);

            BinaryWriter writer = new BinaryWriter(File.Open(filename, FileMode.Create));
            writer.Write(stream.ToArray());
            writer.Flush();
            writer.BaseStream.Dispose();
            writer.Close();
            writer = null;

            stream.Dispose();
            stream.Close();
            stream = null;

        }


        private void KillAll(String ProcessName)
        {
            Process CurrentProcess = Process.GetCurrentProcess();
            Process[] allProcs = Process.GetProcesses();
            foreach (Process thisProc in allProcs)
            {
                if ((CurrentProcess.Id != thisProc.Id) && (thisProc.ProcessName.ToLower() == ProcessName.ToLower()))
                {
                    //thisProc.CloseMainWindow();
                    Debug.WriteLine("Matando processo '" + ProcessName + "' " + thisProc.Id);
                    thisProc.Kill();
                }
            }
        }

        private void StartApplication(String filename, TestEnvironment startEnv)
        {

            ProcessStartInfo st = new ProcessStartInfo();
            st.WorkingDirectory = Environment.CurrentDirectory;
            st.FileName = filename;
            st.WindowStyle = ProcessWindowStyle.Minimized;

            Process proc = new Process();
            proc.StartInfo = st;


            FileInfo tmp = new FileInfo(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));

            startEnv.SaveToFile(tmp.FullName);

            st.Arguments = "\"" + tmp.FullName + "\"";

#if DEBUG
            startEnv.SaveToFile("temp.env");
#endif

            proc.Start();

        }

        public static String minify(String text)
        {
            String iText = text;

            while (iText.IndexOf("  ") != -1)
                iText = iText.Replace("  ", " ");
            
            //iText = Regex.Replace(iText, @"(@(?:""[^""]*"")+|""(?:[^""\n\\]+|\\.)*""|'(?:[^'\n\\]+|\\.)*')|//.*|/\*(?s:.*?)\*/", "$1", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            iText = iText.Replace("\r\n", "");
            iText = iText.Replace("; ", ";");
            iText = iText.Replace(": ", ":");
            iText = iText.Replace(" {", "{");
            iText = iText.Replace("{ ", "{");
            iText = iText.Replace(", ", ",");
            iText = iText.Replace("} ", "}");
            iText = iText.Replace(";}", "}");

            return iText.Trim();
        }


        public static String expand(String text)
        {
            String iText = minify(text);

            iText = iText.Replace("}", "}\r\n");
            iText = iText.Replace(";", "; ");
            iText = iText.Replace(":", ": ");
            iText = iText.Replace("{", "\r\n{ ");
            iText = iText.Replace(",", ", ");
            iText = iText.Replace("}", "} ");

            return iText.Trim();
        }

    }
}
