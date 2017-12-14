using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Data;
using Zip;
//using SafeTrend.Data;

namespace LoadTestLib
{
    public class ReportBuilder: IDisposable
    {
        private TestEnvironment enviroment;
        private DirectoryInfo tempDir;
        private DirectoryInfo reportDir;

        public ReportBuilder(TestEnvironment enviroment)
        {
            this.enviroment = enviroment;

            System.Reflection.Assembly asm = System.Reflection.Assembly.GetAssembly(typeof(ReportBuilder));
            this.reportDir = new DirectoryInfo(Path.Combine(Path.GetDirectoryName(asm.Location), "reports\\" + this.enviroment.TestName + "-" + this.enviroment.Type.ToString() + "-" + this.enviroment.VirtualUsers.ToString()));
            if (this.reportDir.Exists)
                this.reportDir.Create();

        }

        public void Build()
        {
            LoadTestDatabase db = new LoadTestDatabase(this.enviroment.ConnectionString);

            //SQLDB db = new SQLDB(this.enviroment.SQLConfig.Server, this.enviroment.SQLConfig.Database, this.enviroment.SQLConfig.Username, this.enviroment.SQLConfig.Password);
            //db.openDB();
            
            //tempDir = new DirectoryInfo(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
            tempDir = reportDir;
            if (tempDir.Exists)
                tempDir.Create();

            ZIPUtil.DecompressData(FileResources.ReportZIPData(), tempDir);

            Int64 VU = 0;
	        Int64 connections = 0;
	        Int64 throughput = 0;
	        Int64 bytesReceived = 0;
	        Int64 requests  = 0;
	        Int64 rps = 0;


            StringBuilder js = new StringBuilder();

            js.AppendLine("jQuery(document).ready(function ($) {");

            String html = "";

            List<String> labels = new List<String>();
            List<String> values = new List<String>();
            List<String> values2 = new List<String>();

            DateTime date = enviroment.dStart;
            Int64 value = 0;

            DataTable dtTmp = db.selectGeneral(enviroment.TestName, enviroment.dStart, enviroment.dEnd);
            if (dtTmp.Rows.Count > 0)
            {
                VU = (Int64)dtTmp.Rows[0]["vu"];
                connections = (Int64)dtTmp.Rows[0]["connections"];
                throughput = (Int64)dtTmp.Rows[0]["throughput"];
                bytesReceived = (Int64)dtTmp.Rows[0]["bytesReceived"];
                requests = (Int64)dtTmp.Rows[0]["requests"];
                rps = (Int64)dtTmp.Rows[0]["rps"];
            }

            //Graficos de monitoramento Zabbix
            StringBuilder extra = new StringBuilder();


            DataTable dtZabbix = db.selectMonitoredZabbix(enviroment.TestName, enviroment.dStart, enviroment.dEnd);

            if (dtZabbix != null)
                foreach (DataRow dr in dtZabbix.Rows)
                {
                    String key = Guid.NewGuid().ToString().Trim(" {}".ToCharArray()).Replace("-","");
                    String name = dr["host"].ToString();

                    try
                    {
                        foreach (SafeTrend.Data.ZabbixConfig cfg in enviroment.ZabbixMonitors)
                            if (cfg.Host == name)
                                name = cfg.Name;
                    }
                    catch { }

                    extra.AppendLine("<div class=\"full-line topspace\">");
                    extra.AppendLine("<h2>" + name + " - CPU (%)</h2>");
                    extra.AppendLine("</div>");
                    extra.AppendLine("<div class=\"data1 data2 full-line\">");
                    extra.AppendLine("<div class=\"chartcontent\"><canvas id=\"" + key + "_cpu\" width=\"921\" height=\"150\"></canvas></div>");
                    extra.AppendLine("<div class=\"clearfix\"></div>");
                    extra.AppendLine("</div>");
                    extra.AppendLine("");

                    extra.AppendLine("<div class=\"full-line topspace\">");
                    extra.AppendLine("<h2>" + name + " - Memoria</h2>");
                    extra.AppendLine("</div>");
                    extra.AppendLine("<div class=\"data1 data2 full-line\">");
                    extra.AppendLine("<div class=\"chartcontent\"><canvas id=\"" + key + "_memory\" width=\"921\" height=\"150\"></canvas></div>");
                    extra.AppendLine("<div class=\"clearfix\"></div>");
                    extra.AppendLine("</div>");
                    extra.AppendLine("");


                    labels.Clear();
                    values.Clear();

                    labels.Add("Início");
                    values.Add("0");
                    values2.Add("0");

                    date = enviroment.dStart;
                    value = 0;

                    dtTmp = db.selectZabbixCPU(enviroment.TestName, dr["host"].ToString(), enviroment.dStart, enviroment.dEnd);
                    foreach (DataRow drH in dtTmp.Rows)
                    {
                        date = DateTime.Parse(drH["dateg"].ToString());
                        value = (Int64)drH["cpu"];

                        labels.Add(date.ToString("HH:mm"));
                        values.Add(value.ToString());
                    }

                    date.AddMinutes(1);

                    labels.Add("Final");
                    values.Add("0");
                     
                    js.AppendLine("var data1 = {");
                    js.AppendLine("    labels: ['" + String.Join("','", labels) + "'],");
                    js.AppendLine("    datasets: [");
		            
                    js.AppendLine("        {");
		            js.AppendLine("            fillColor: \"rgba(151,187,205,0.5)\",");
		            js.AppendLine("            strokeColor: \"rgba(151,187,205,1)\",");
		            js.AppendLine("            pointColor: \"rgba(151,187,205,1)\",");
		            js.AppendLine("            pointStrokeColor: \"#fff\",");
                    js.AppendLine("            data: [" + String.Join(",", values) + "]");
		            js.AppendLine("        }");

	                js.AppendLine("    ]");
                    js.AppendLine("}");

                    js.AppendLine("var theCanvas = $('#" + key + "_cpu').get(0);");
                    js.AppendLine("var ctx = theCanvas.getContext(\"2d\");");
                    js.AppendLine("var myNewChart = new Chart(ctx).Line(data1, null);");


                    //Memoria
                    
                    labels.Clear();
                    values.Clear();
                    values2.Clear();

                    labels.Add("Início");
                    values.Add("0");
                    values2.Add("0");

                    date = enviroment.dStart;
                    value = 0;

                    dtTmp = db.selectZabbixMemory(enviroment.TestName, dr["host"].ToString(), enviroment.dStart, enviroment.dEnd);
                    foreach (DataRow drH in dtTmp.Rows)
                    {
                        date = DateTime.Parse(drH["dateg"].ToString());

                        labels.Add(date.ToString("HH:mm"));
                        values.Add(((Int64)drH["total_memory"]).ToString());
                        values2.Add(((Int64)drH["memory"]).ToString());
                    }

                    date.AddMinutes(1);

                    labels.Add("Final");
                    values.Add("0");
                    values2.Add("0");

                    js.AppendLine("var data1 = {");
                    js.AppendLine("    labels: ['" + String.Join("','", labels) + "'],");
                    js.AppendLine("    datasets: [");

                    js.AppendLine("        {");
                    js.AppendLine("            label: \"Total\",");
                    js.AppendLine("            fillColor: \"rgba(220,220,220,0.2)\",");
                    js.AppendLine("            strokeColor: \"rgba(220,220,220,1)\",");
                    js.AppendLine("            pointColor: \"rgba(220,220,220,1)\",");
                    js.AppendLine("            pointStrokeColor: \"#fff\",");
                    js.AppendLine("            pointHighlightFill: \"#fff\",");
                    js.AppendLine("            pointHighlightStroke: \"rgba(220,220,220,1)\",");
                    js.AppendLine("            data: [" + String.Join(",", values) + "]");
                    js.AppendLine("        },");

                    js.AppendLine("        {");
                    js.AppendLine("            label: \"Usada\",");
                    js.AppendLine("            fillColor: \"rgba(151,187,205,0.5)\",");
                    js.AppendLine("            strokeColor: \"rgba(151,187,205,1)\",");
                    js.AppendLine("            pointColor: \"rgba(151,187,205,1)\",");
                    js.AppendLine("            pointStrokeColor: \"#fff\",");
                    js.AppendLine("            data: [" + String.Join(",", values2) + "]");
                    js.AppendLine("        }");

                    js.AppendLine("    ]");
                    js.AppendLine("}");

                    js.AppendLine("var theCanvas = $('#" + key + "_memory').get(0);");
                    js.AppendLine("var ctx = theCanvas.getContext(\"2d\");");
                    js.AppendLine("var myNewChart = new Chart(ctx).Line(data1, null);");
                    
                }

            File.WriteAllBytes(Path.Combine(tempDir.FullName, "index.html"), Encoding.UTF8.GetBytes(FileResources.GetIndexText(this.enviroment.TestName, this.enviroment, VU, connections, throughput, bytesReceived, requests, rps, extra.ToString())));

            DirectoryInfo jsDir = new DirectoryInfo(Path.Combine(tempDir.FullName, "js"));
            try
            {
                jsDir.Create();
            }
            catch { }

            //Mensagens
            //

            dtTmp = db.selectMessages(enviroment.TestName, enviroment.dStart, enviroment.dEnd);
            if ((dtTmp == null) || (dtTmp.Rows.Count == 0))
            {
                js.AppendLine("$('#message thead th').html('Nenhuma mensagem gerada pelo sistema');");
            }
            else
            {
                foreach (DataRow dr in dtTmp.Rows)
                    html += "<div class=\"title\">" + dr["title"] + "</div><div class=\"message\">" + dr["text"].ToString().Replace("\r\n", "<br />") + "</div>";

                js.AppendLine("$('#message thead th').html('" + html + "');");
            }
            
            //Tabela de otimização
            dtTmp = db.selectOptimization(enviroment.TestName, enviroment.dStart, enviroment.dEnd);

            if (dtTmp.Rows.Count > 0)
            {
                html = "";
                Int64 tOriginalLength = 0;
                Int64 tOptimizedLength = 0;

                foreach (DataRow dr in dtTmp.Rows)
                {
                    Int64 originalLength = (Int64)dr["originalLength"];
                    Int64 optimizedLength = (Int64)dr["optimizedLength"];

                    tOriginalLength += originalLength;
                    tOptimizedLength += optimizedLength;

                    double percent = 100F - (((double)optimizedLength / (double)originalLength) * 100F);

                    html += "<tr><td>" + dr["uri"].ToString() + "</td><td>" + FileResources.formatData(originalLength, ChartDataType.Bytes) + "</td><td>" + FileResources.formatData(optimizedLength, ChartDataType.Bytes) + "</td><td>" + FileResources.formatData(percent, ChartDataType.Percent) + "</td></tr>";
                }

                if (html != "")
                {
                    double percent2 = 100F - (((double)tOptimizedLength / (double)tOriginalLength) * 100F);
                    html += "<tr><td>Total de otimização a se realizar</td><td>" + FileResources.formatData(tOriginalLength, ChartDataType.Bytes) + "</td><td>" + FileResources.formatData(tOptimizedLength, ChartDataType.Bytes) + "</td><td>" + FileResources.formatData(percent2, ChartDataType.Percent) + "</td></tr>";

                    js.AppendLine("$('#optimization-table tbody').html('" + html + "');");
                }
                else
                    js.AppendLine("$('#optimization-table').remove();");
            }
            else
            {
                js.AppendLine("$('#optimization-table').remove();");
            }


            //Tabela de Não Otimização (Ou seja os objetos que ja estão otimizados)
            dtTmp = db.selectNonOptimization(enviroment.TestName, enviroment.dStart, enviroment.dEnd);

            if (dtTmp.Rows.Count > 0)
            {
                html = "";
                Int64 tOriginalLength = 0;
                Int64 tNonOptimizedLength = 0;

                foreach (DataRow dr in dtTmp.Rows)
                {
                    Int64 originalLength = (Int64)dr["originalLength"];
                    Int64 nonOptimizedLength = (Int64)dr["nonOptimizedLength"];

                    tOriginalLength += originalLength;
                    tNonOptimizedLength += nonOptimizedLength;

                    double percent = 100F - (((double)originalLength / (double)nonOptimizedLength) * 100F);

                    html += "<tr><td>" + dr["uri"].ToString() + "</td><td>" + FileResources.formatData(originalLength, ChartDataType.Bytes) + "</td><td>" + FileResources.formatData(nonOptimizedLength, ChartDataType.Bytes) + "</td><td>" + FileResources.formatData(percent, ChartDataType.Percent) + "</td></tr>";
                }

                if (html != "")
                {
                    double percent2 = 100F - (((double)tOriginalLength / (double)tNonOptimizedLength) * 100F);
                    html += "<tr><td>Total de otimização existente</td><td>" + FileResources.formatData(tOriginalLength, ChartDataType.Bytes) + "</td><td>" + FileResources.formatData(tNonOptimizedLength, ChartDataType.Bytes) + "</td><td>" + FileResources.formatData(percent2, ChartDataType.Percent) + "</td></tr>";

                    js.AppendLine("$('#nonoptimization-table tbody').html('" + html + "');");
                }
                else
                    js.AppendLine("$('#nonoptimization-table').remove();");
            }
            else
            {
                js.AppendLine("$('#nonoptimization-table').remove();");
            }


            //Tabela de otimização GZIP/Deflate
            dtTmp = db.selecGzipOptimization(enviroment.TestName, enviroment.dStart, enviroment.dEnd);

            if (dtTmp.Rows.Count > 0)
            {
                html = "";
                Int64 tGzipLength = 0;
                Int64 tContentLength = 0;

                foreach (DataRow dr in dtTmp.Rows)
                {
                    Int64 gzipLength = (Int64)dr["gzipLength"];
                    Int64 contentLength = (Int64)dr["contentLength"];

                    tGzipLength += gzipLength;
                    tContentLength += contentLength;

                    double percent = 100F - (((double)gzipLength / (double)contentLength) * 100F);

                    html += "<tr><td>" + dr["uri"].ToString() + "</td><td>" + FileResources.formatData(contentLength, ChartDataType.Bytes) + "</td><td>" + FileResources.formatData(gzipLength, ChartDataType.Bytes) + "</td><td>" + FileResources.formatData(percent, ChartDataType.Percent) + "</td></tr>";
                }

                if (html != "")
                {
                    double percent2 = 100F - (((double)tGzipLength / (double)tContentLength) * 100F);
                    html += "<tr><td>Total de compactação utilizada</td><td>" + FileResources.formatData(tContentLength, ChartDataType.Bytes) + "</td><td>" + FileResources.formatData(tGzipLength, ChartDataType.Bytes) + "</td><td>" + FileResources.formatData(percent2, ChartDataType.Percent) + "</td></tr>";

                    js.AppendLine("$('#gzip-table tbody').html('" + html + "');");
                }
                else
                    js.AppendLine("$('#gzip-table').remove();");
            }
            else
            {
                js.AppendLine("$('#gzip-table').remove();");
            }

            //

            //Chart 1 (Usuários virtuais (VU) ativos)
            labels.Clear();
            values.Clear();

            dtTmp = db.selectVUAndConnections(enviroment.TestName, enviroment.dStart, enviroment.dEnd);
            
            labels.Add("Início");
            values.Add("0");

            foreach (DataRow dr in dtTmp.Rows)
            {
                date = DateTime.Parse(dr["dateg"].ToString());
                value = (Int64)dr["virtualusers"];

                labels.Add(date.ToString("HH:mm"));
                values.Add(value.ToString());
            }

            date.AddMinutes(1);

            labels.Add("Final");
            values.Add("0");

            js.AppendLine("var data1 = {");
            js.AppendLine("    labels: ['" + String.Join("','", labels) + "'],");
            js.AppendLine("    datasets: [");
		    js.AppendLine("        {");
		    js.AppendLine("            fillColor: \"rgba(151,187,205,0.5)\",");
		    js.AppendLine("            strokeColor: \"rgba(151,187,205,1)\",");
		    js.AppendLine("            pointColor: \"rgba(151,187,205,1)\",");
		    js.AppendLine("            pointStrokeColor: \"#fff\",");
            js.AppendLine("            data: [" + String.Join(",", values) + "]");
		    js.AppendLine("        }");
	        js.AppendLine("    ]");
            js.AppendLine("}");

            js.AppendLine("var theCanvas = $('#c1').get(0);");
            js.AppendLine("var ctx = theCanvas.getContext(\"2d\");");
            js.AppendLine("var myNewChart = new Chart(ctx).Line(data1, null);");

            //Chart 2 (Conexões TCP ativas)
            labels.Clear();
            values.Clear();

            labels.Add("Início");
            values.Add("0");

            date = enviroment.dStart;
            value = 0;
            foreach (DataRow dr in dtTmp.Rows)
            {
                date = DateTime.Parse( dr["dateg"].ToString());
                value = (Int64)dr["connections"];

                labels.Add(date.ToString("HH:mm"));
                values.Add(value.ToString());
            }

            date.AddMinutes(1);

            labels.Add("Final");
            values.Add("0");

            js.AppendLine("var data2 = {");
            js.AppendLine("    labels: ['" + String.Join("','", labels) + "'],");
            js.AppendLine("    datasets: [");
		    js.AppendLine("        {");
		    js.AppendLine("            fillColor: \"rgba(151,187,205,0.5)\",");
		    js.AppendLine("            strokeColor: \"rgba(151,187,205,1)\",");
		    js.AppendLine("            pointColor: \"rgba(151,187,205,1)\",");
		    js.AppendLine("            pointStrokeColor: \"#fff\",");
            js.AppendLine("            data: [" + String.Join(",", values) + "]");
		    js.AppendLine("        }");
	        js.AppendLine("    ]");
            js.AppendLine("}");

            js.AppendLine("var theCanvas = $('#c2').get(0);");
            js.AppendLine("var ctx = theCanvas.getContext(\"2d\");");
            js.AppendLine("var myNewChart = new Chart(ctx).Line(data2, null);");

            //Chart 3 (Requisições)

            labels.Clear();
            values.Clear();

            dtTmp = db.selectRequests(enviroment.TestName, enviroment.dStart, enviroment.dEnd);
            
            labels.Add("Início");
            values.Add("0");

            date = enviroment.dStart;
            value = 0;
            foreach (DataRow dr in dtTmp.Rows)
            {
                date = DateTime.Parse(dr["dateg"].ToString());
                value = (Int64)dr["rps"];

                labels.Add(date.ToString("HH:mm"));
                values.Add(value.ToString());
            }

            date.AddMinutes(1);

            labels.Add("Final");
            values.Add("0");

            js.AppendLine("var data3 = {");
            js.AppendLine("    labels: ['" + String.Join("','", labels) + "'],");
            js.AppendLine("    datasets: [");
		    js.AppendLine("        {");
		    js.AppendLine("            fillColor: \"rgba(151,187,205,0.5)\",");
		    js.AppendLine("            strokeColor: \"rgba(151,187,205,1)\",");
		    js.AppendLine("            pointColor: \"rgba(151,187,205,1)\",");
		    js.AppendLine("            pointStrokeColor: \"#fff\",");
            js.AppendLine("            data: [" + String.Join(",", values) + "]");
		    js.AppendLine("        }");
	        js.AppendLine("    ]");
            js.AppendLine("}");


            js.AppendLine("var theCanvas = $('#c3').get(0);");
            js.AppendLine("var ctx = theCanvas.getContext(\"2d\");");
            js.AppendLine("var myNewChart = new Chart(ctx).Line(data3, null);");

            //Chart 4 (Errors)
            
            labels.Clear();
            values.Clear();

            dtTmp = db.selectErrors(enviroment.TestName, enviroment.dStart, enviroment.dEnd);
            
            labels.Add("Início");
            values.Add("0");

            date = enviroment.dStart;
            value = 0;
            foreach (DataRow dr in dtTmp.Rows)
            {
                date = DateTime.Parse(dr["dateg"].ToString());
                value = (Int64)dr["errors"];

                labels.Add(date.ToString("HH:mm"));
                values.Add(value.ToString());
            }

            date.AddMinutes(1);

            labels.Add("Final");
            values.Add("0");

            js.AppendLine("var data4 = {");
            js.AppendLine("    labels: ['" + String.Join("','", labels) + "'],");
            js.AppendLine("    datasets: [");
		    js.AppendLine("        {");
		    js.AppendLine("            fillColor: \"rgba(151,187,205,0.5)\",");
		    js.AppendLine("            strokeColor: \"rgba(151,187,205,1)\",");
		    js.AppendLine("            pointColor: \"rgba(151,187,205,1)\",");
		    js.AppendLine("            pointStrokeColor: \"#fff\",");
            js.AppendLine("            data: [" + String.Join(",", values) + "]");
		    js.AppendLine("        }");
	        js.AppendLine("    ]");
            js.AppendLine("}");

            js.AppendLine("var theCanvas = $('#c4').get(0);");
            js.AppendLine("var ctx = theCanvas.getContext(\"2d\");");
            js.AppendLine("var myNewChart = new Chart(ctx).Line(data4, null);");

            //Chart 5 (Bandwidth (Mbps))
            labels.Clear();
            values.Clear();

            dtTmp = db.selectThroughput(enviroment.TestName, enviroment.dStart, enviroment.dEnd);

            labels.Add("Início");
            values.Add("0");

            date = enviroment.dStart;
            value = 0;
            foreach (DataRow dr in dtTmp.Rows)
            {
                date = DateTime.Parse(dr["dateg"].ToString());
                value = (Int64)dr["mbps"];

                labels.Add(date.ToString("HH:mm"));
                values.Add(value.ToString());
            }

            date.AddMinutes(1);

            labels.Add("Final");
            values.Add("0");

            js.AppendLine("var data5 = {");
            js.AppendLine("    labels: ['" + String.Join("','", labels) + "'],");
            js.AppendLine("    datasets: [");
            js.AppendLine("        {");
            js.AppendLine("            fillColor: \"rgba(151,187,205,0.5)\",");
            js.AppendLine("            strokeColor: \"rgba(151,187,205,1)\",");
            js.AppendLine("            pointColor: \"rgba(151,187,205,1)\",");
            js.AppendLine("            pointStrokeColor: \"#fff\",");
            js.AppendLine("            data: [" + String.Join(",", values) + "]");
            js.AppendLine("        }");
            js.AppendLine("    ]");
            js.AppendLine("}");


            js.AppendLine("var theCanvas = $('#c5').get(0);");
            js.AppendLine("var ctx = theCanvas.getContext(\"2d\");");
            js.AppendLine("var myNewChart = new Chart(ctx).Line(data5, null);");

            //Chart 6 (Tempo de resposta geral)
            labels.Clear();
            values.Clear();

            dtTmp = db.selectResponseTime(enviroment.TestName, enviroment.dStart, enviroment.dEnd, null);

            labels.Add("Início");
            values.Add("0");

            date = enviroment.dStart;
            value = 0;
            foreach (DataRow dr in dtTmp.Rows)
            {
                date = DateTime.Parse(dr["dateg"].ToString());
                value = (Int64)dr["time"];

                labels.Add(date.ToString("HH:mm"));
                values.Add(value.ToString());
            }

            date.AddMinutes(1);

            labels.Add("Final");
            values.Add("0");

            js.AppendLine("var data6 = {");
            js.AppendLine("    labels: ['" + String.Join("','", labels) + "'],");
            js.AppendLine("    datasets: [");
            js.AppendLine("        {");
            js.AppendLine("            fillColor: \"rgba(151,187,205,0.5)\",");
            js.AppendLine("            strokeColor: \"rgba(151,187,205,1)\",");
            js.AppendLine("            pointColor: \"rgba(151,187,205,1)\",");
            js.AppendLine("            pointStrokeColor: \"#fff\",");
            js.AppendLine("            data: [" + String.Join(",", values) + "]");
            js.AppendLine("        }");
            js.AppendLine("    ]");
            js.AppendLine("}");


            js.AppendLine("var theCanvas = $('#rp1').get(0);");
            js.AppendLine("var ctx = theCanvas.getContext(\"2d\");");
            js.AppendLine("var myNewChart = new Chart(ctx).Line(data6, null);");

            //Chart 7 (Tempo de resposta geral)
            labels.Clear();
            values.Clear();

            dtTmp = db.selectResponseTime(enviroment.TestName, enviroment.dStart, enviroment.dEnd, enviroment.BaseUri);

            labels.Add("Início");
            values.Add("0");

            date = enviroment.dStart;
            value = 0;
            foreach (DataRow dr in dtTmp.Rows)
            {
                date = DateTime.Parse(dr["dateg"].ToString());
                value = (Int64)dr["time"];

                labels.Add(date.ToString("HH:mm"));
                values.Add(value.ToString());
            }

            date.AddMinutes(1);

            labels.Add("Final");
            values.Add("0");

            js.AppendLine("var data7 = {");
            js.AppendLine("    labels: ['" + String.Join("','", labels) + "'],");
            js.AppendLine("    datasets: [");
            js.AppendLine("        {");
            js.AppendLine("            fillColor: \"rgba(151,187,205,0.5)\",");
            js.AppendLine("            strokeColor: \"rgba(151,187,205,1)\",");
            js.AppendLine("            pointColor: \"rgba(151,187,205,1)\",");
            js.AppendLine("            pointStrokeColor: \"#fff\",");
            js.AppendLine("            data: [" + String.Join(",", values) + "]");
            js.AppendLine("        }");
            js.AppendLine("    ]");
            js.AppendLine("}");


            js.AppendLine("var theCanvas = $('#rp2').get(0);");
            js.AppendLine("var ctx = theCanvas.getContext(\"2d\");");
            js.AppendLine("var myNewChart = new Chart(ctx).Line(data7, null);");

            //Chart 8 (Tempo de carga por conteúdo)
            labels.Clear();
            values.Clear();

            dtTmp = db.selectContentTypeDistribution(enviroment.TestName, enviroment.dStart, enviroment.dEnd);

            date = enviroment.dStart;
            value = 0;
            foreach (DataRow dr in dtTmp.Rows)
            {
                value = (Int64)dr["qty"];
                Int64 total = (Int64)dr["total"];

                labels.Add(dr["contentType"].ToString() + ": " + value + " (" + ((Int64)(((double)value / (double)total) * 100)) + "%)");
                values.Add(value.ToString());
            }

            js.AppendLine("var radar = new RGraph.Radar('ctd',[" + String.Join(",", values) + "])");
            js.AppendLine("        .Set('strokestyle', 'black')");
            js.AppendLine("        .Set('colors.alpha', 0.3)");
            js.AppendLine("        .Set('colors', ['rgba(151,187,205,0.5)'])");
            js.AppendLine("        .Set('labels', ['" + String.Join("','", labels) + "'])");
            js.AppendLine("        .Set('labels.axes', '')");
            js.AppendLine("        .Set('gutter.top', 35)");
            js.AppendLine("        .Set('accumulative', true)");
            js.AppendLine("        .Set('axes.color', 'rgba(0,0,0,0)')");
            js.AppendLine("        .Set('background.circles.poly', true)");
            js.AppendLine("        .Set('background.circles.spacing', 25)");
            js.AppendLine("        .Draw();");

            //Chart 9 (Tempo de carga por conteúdo)
            labels.Clear();
            values.Clear();

            dtTmp = db.selectContentTypeTimeDistribution(enviroment.TestName, enviroment.dStart, enviroment.dEnd);

            date = enviroment.dStart;
            value = 0;
            foreach (DataRow dr in dtTmp.Rows)
            {
                value = (Int64)dr["time"];
                Int64 total = (Int64)dr["total"];

                labels.Add(dr["contentType"].ToString() + ": " + value + "ms (" + ((Int64)(((double)value / (double)total) * 100)) + "%)");
                values.Add(value.ToString());
            }

            js.AppendLine("var radar = new RGraph.Radar('ctt',[" + String.Join(",", values) + "])");
            js.AppendLine("        .Set('strokestyle', 'black')");
            js.AppendLine("        .Set('colors.alpha', 0.3)");
            js.AppendLine("        .Set('colors', ['rgba(151,187,205,0.5)'])");
            js.AppendLine("        .Set('labels', ['" + String.Join("','", labels) + "'])");
            js.AppendLine("        .Set('labels.axes', '')");
            js.AppendLine("        .Set('gutter.top', 35)");
            js.AppendLine("        .Set('accumulative', true)");
            js.AppendLine("        .Set('axes.color', 'rgba(0,0,0,0)')");
            js.AppendLine("        .Set('background.circles.poly', true)");
            js.AppendLine("        .Set('background.circles.spacing', 25)");
            js.AppendLine("        .Draw();");

            //content-trafic
            labels.Clear();
            values.Clear();

            dtTmp = db.selectContentTypeBytes(enviroment.TestName, enviroment.dStart, enviroment.dEnd);

            date = enviroment.dStart;
            value = 0;
            foreach (DataRow dr in dtTmp.Rows)
            {
                value = (Int64)dr["bytesReceived"];
                Int64 total = (Int64)dr["total"];

                labels.Add(dr["contentType"].ToString() + ": " + FileResources.formatData(value, ChartDataType.Bytes) + " (" + ((Int64)(((double)value / (double)total) * 100)) + "%)");
                values.Add(value.ToString());
            }

            js.AppendLine("var radar = new RGraph.Radar('content-bytes',[" + String.Join(",", values) + "])");
            js.AppendLine("        .Set('strokestyle', 'black')");
            js.AppendLine("        .Set('colors.alpha', 0.3)");
            js.AppendLine("        .Set('colors', ['rgba(151,187,205,0.5)'])");
            js.AppendLine("        .Set('labels', ['" + String.Join("','", labels) + "'])");
            js.AppendLine("        .Set('labels.axes', '')");
            js.AppendLine("        .Set('gutter.top', 35)");
            js.AppendLine("        .Set('accumulative', true)");
            js.AppendLine("        .Set('axes.color', 'rgba(0,0,0,0)')");
            js.AppendLine("        .Set('background.circles.poly', true)");
            js.AppendLine("        .Set('background.circles.spacing', 25)");
            js.AppendLine("        .Draw();");


            //Table 1
            dtTmp = db.selectTopHit(enviroment.TestName, enviroment.dStart, enviroment.dEnd);

            html = "";
            foreach (DataRow dr in dtTmp.Rows)
            {
                html += "<tr><td>" + dr["uri"].ToString() + "</td><td>" + dr["success"].ToString() + "</td><td>" + dr["error"].ToString() + "</td></tr>";
            }
            js.AppendLine("$('#dist-uri tbody').html('" + html + "');");


            //Table 2
            dtTmp = db.selectTopTime(enviroment.TestName, enviroment.dStart, enviroment.dEnd);

            html = "";
            foreach (DataRow dr in dtTmp.Rows)
            {
                html += "<tr><td>" + dr["uri"].ToString() + "</td><td>" + dr["contentType"].ToString() + "</td><td>" + dr["time"].ToString() + "</td></tr>";
            }
            js.AppendLine("$('#time-uri tbody').html('" + html + "');");


            //Table 3
            dtTmp = db.selectTopBytes(enviroment.TestName, enviroment.dStart, enviroment.dEnd);

            html = "";
            foreach (DataRow dr in dtTmp.Rows)
            {
                html += "<tr><td>" + dr["uri"].ToString() + "</td><td>" + dr["qty"].ToString() + "</td><td>" + FileResources.formatData((Int64)(dr["bytesReceived"]), ChartDataType.Bytes) + "</td></tr>";
            }
            js.AppendLine("$('#bytes-uri tbody').html('" + html + "');");


            js.AppendLine("});");

            File.WriteAllBytes(Path.Combine(jsDir.FullName, "loadtest.report.js"), Encoding.UTF8.GetBytes(js.ToString()));

            db.Dispose();



            //Espera para liberar o arquivo
            //System.Threading.Thread.Sleep(5000);

            //Gera o final
            /*
            try
            {
                ZIPUtil.CompressToFile(tempDir, Path.Combine(reportDir.FullName, DateTime.Now.ToString("yyyyMMddHHmm") + ".zip"));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Falha ao gerar o ZIP " + ex.Message);
                
                foreach (FileInfo f in tempDir.GetFiles())
                {
                    try
                    {
                        f.CopyTo(Path.Combine(reportDir.FullName, f.Name), true);
                    }
                    catch (Exception ex1)
                    {
                        Console.WriteLine("Falha ao copiar o arquivo " + f.Name + ex1.Message);
                    }
                }

                foreach (DirectoryInfo d in tempDir.GetDirectories())
                    CopyTo(tempDir, d, reportDir);
            }*/


        }

        public void Dispose()
        {

        }


        private void CopyTo(DirectoryInfo baseDir, DirectoryInfo from, DirectoryInfo to)
        {
            DirectoryInfo newTo = new DirectoryInfo(to.FullName + "\\" + from.FullName.Replace(baseDir.FullName, "").Trim("\\ ".ToCharArray()));
            if (!newTo.Exists)
                newTo.Create();

            foreach (FileInfo f in from.GetFiles())
                try
                {
                    f.CopyTo(Path.Combine(newTo.FullName, f.Name), true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Falha ao copiar o arquivo " + f.Name + ex.Message);
                }


            foreach (DirectoryInfo d in from.GetDirectories())
                CopyTo(baseDir, d, to);
        }


    }
}
