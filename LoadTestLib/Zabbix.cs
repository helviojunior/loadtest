using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Net.Sockets;

namespace LoadTestLib.ZabbixGet
{
    public class Zabbix : IDisposable
    {
        private String mHost;
        private Int32 mPort;

        public Zabbix(String host)
            : this(host, 10050) { }

        public Zabbix(String host, Int32 port)
        {
            this.mHost = host;
            this.mPort = port;
        }

        public String GetItem(String key)
        {
            return GetText(key);
        }

        public List<String> GetItemList(String key, String filter)
        {
            List<String> ret = new List<String>();

            return ret;
        }

        public void Dispose()
        {
            this.mHost = null;
        }

        private String GetText(String key)
        {

            String responseData = "";


            try
            {
#if DEBUG
                //Console.WriteLine("Zabbix>GetText Getting key " + key + " from " + mHost + ":" + mPort);
#endif

                TcpClient client = new TcpClient(mHost, mPort);

                using (MemoryStream sendBuffer = new MemoryStream())
                using (MemoryStream receivedBuffer = new MemoryStream())
                using (NetworkStream netStream = client.GetStream())
                {
                    Byte[] data = System.Text.Encoding.ASCII.GetBytes("ZBXD");

                    sendBuffer.Write(data, 0, data.Length);
                    data = new Byte[] { 0x01 };
                    sendBuffer.Write(data, 0, data.Length);

                    Byte[] data2 = System.Text.Encoding.ASCII.GetBytes(key);
                    Int64 size = data2.Length;

                    data = BitConverter.GetBytes(size);
                    sendBuffer.Write(data, 0, data.Length);

                    sendBuffer.Write(data2, 0, data2.Length);

                    data = new Byte[] { 0x0a };

                    sendBuffer.Write(data, 0, data.Length);

                    //Envia os dados
                    data = sendBuffer.ToArray();
                    netStream.Write(data, 0, data.Length);

                    data = new Byte[4096];

                    Int32 bytes = netStream.Read(data, 0, data.Length);
                    responseData = System.Text.Encoding.ASCII.GetString(data, 0, 5);

                    if (!responseData.Trim(new char[] { (char)0x01 }).Equals("ZBXD", StringComparison.InvariantCultureIgnoreCase))
                        return "";

                    Int64 len = -1;
                    Int32 offSet = 8;
                    if (bytes > 13)
                    {
                        len = BitConverter.ToInt64(data, 5);
                        offSet += 5;
                    }

                    if (len > 0)
                    {
                        receivedBuffer.Write(data, offSet, bytes - offSet);
                    }
                    else
                    {

                        bytes = netStream.Read(data, 0, data.Length);

                        len = BitConverter.ToInt64(data, 0);
                        receivedBuffer.Write(data, 8, bytes - 8);
                    }

                    while (receivedBuffer.Length < len)
                    {
                        bytes = netStream.Read(data, 0, data.Length);
                        receivedBuffer.Write(data, 0, bytes);
                    }


                    data = receivedBuffer.ToArray();
                    responseData = System.Text.Encoding.ASCII.GetString(data, 0, data.Length);
                }

                client.Close();
            }
            catch (ArgumentNullException e)
            {
#if DEBUG
                //Console.WriteLine("Zabbix>GetText ArgumentNullException: {0}", e);
#endif
                return "";
            }
            catch (SocketException e)
            {
#if DEBUG
                //Console.WriteLine("Zabbix>GetText SocketException: {0}", e);
#endif
                return "";
               
            }
#if DEBUG
            //Console.WriteLine("Zabbix>GetText response key " + key + " from " + mHost + ":" + mPort + " ==> " + responseData);
#endif
            return responseData;
        }
    }
}
