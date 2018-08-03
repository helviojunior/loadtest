using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.ComponentModel;

namespace LoadTest
{
    public class NTP
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct SystemTime
        {
            public ushort Year;
            public ushort Month;
            public ushort DayOfWeek;
            public ushort Day;
            public ushort Hour;
            public ushort Minute;
            public ushort Second;
            public ushort Millisecond;
        };

        [DllImport("kernel32.dll", EntryPoint = "SetSystemTime", SetLastError = true)]
        public extern static bool _SetSystemTime(ref SystemTime sysTime);

        public static Boolean SetSystemTime(DateTime date)
        {

            SystemTime st = new SystemTime();
            st.Year = (ushort)date.Year; // must be short
            st.Month = (ushort)date.Month;
            st.Day = (ushort)date.Day;
            st.Hour = (ushort)date.Hour;
            st.Minute = (ushort)date.Minute;
            st.Second = (ushort)date.Second;
            st.Millisecond = (ushort)date.Millisecond;

            if (_SetSystemTime(ref st)) // invoke this method.
            {
                return true;
            }
            else
            {

                throw new Win32Exception(Marshal.GetLastWin32Error());
            }


        }

        public static Boolean SetSystemTimeFromNTP()
        {
            DateTime ntpdate = GetNetworkUTCTime();
            TimeSpan ts = ntpdate - DateTime.Now;
            if (Math.Abs(ts.TotalSeconds) > 60)
                return SetSystemTime(ntpdate);

            return false;
        }

        public static DateTime GetNetworkUTCTime()
        {
            //default Windows time server
            String[] ntpServerLst = new String[] { "a.st1.ntp.br", "b.st1.ntp.br", "c.st1.ntp.br", "d.st1.ntp.br", "a.ntp.br", "b.ntp.br", "c.ntp.br", "gps.ntp.br", "time.windows.com" };

            // NTP message size - 16 bytes of the digest (RFC 2030)
            var ntpData = new byte[48];

            //Setting the Leap Indicator, Version Number and Mode values
            ntpData[0] = 0x1B; //LI = 0 (no warning), VN = 3 (IPv4 only), Mode = 3 (Client Mode)

            Boolean find = false;

            foreach (String ntpServer in ntpServerLst)
            {
                try
                {

                    IPAddress[] addresses = Dns.GetHostEntry(ntpServer).AddressList;

                    //The UDP port number assigned to NTP is 123
                    var ipEndPoint = new IPEndPoint(addresses[0], 123);
                    //NTP uses UDP
                    var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

                    socket.Connect(ipEndPoint);

                    //Stops code hang if NTP is blocked
                    socket.ReceiveTimeout = 3000;

                    socket.Send(ntpData);
                    socket.Receive(ntpData);
                    socket.Close();

                    find = true;
                    break;
                }
                catch { }
            }

            DateTime networkDateTime = DateTime.UtcNow;
            if (find)
            {
                //Offset to get to the "Transmit Timestamp" field (time at which the reply 
                //departed the server for the client, in 64-bit timestamp format."
                const byte serverReplyTime = 40;

                //Get the seconds part
                ulong intPart = BitConverter.ToUInt32(ntpData, serverReplyTime);

                //Get the seconds fraction
                ulong fractPart = BitConverter.ToUInt32(ntpData, serverReplyTime + 4);

                //Convert From big-endian to little-endian
                intPart = SwapEndianness(intPart);
                fractPart = SwapEndianness(fractPart);

                var milliseconds = (intPart * 1000) + ((fractPart * 1000) / 0x100000000L);

                //**UTC** time
                networkDateTime = (new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc)).AddMilliseconds((long)milliseconds);

#if DEBUG
                //Console.WriteLine("NTP - UTC Date " + networkDateTime.ToString("yyyy-MM-dd HH:mm:ss"), "");
#endif
            }
            else
            {
#if DEBUG
                Console.WriteLine("NTP - Erro on get UTC date from NTP server, using system UTC date " + networkDateTime.ToString("yyyy-MM-dd HH:mm:ss"), "");
#endif
            }

            return networkDateTime;
        }

        // stackoverflow.com/a/3294698/162671
        private static uint SwapEndianness(ulong x)
        {
            return (uint)(((x & 0x000000ff) << 24) +
                           ((x & 0x0000ff00) << 8) +
                           ((x & 0x00ff0000) >> 8) +
                           ((x & 0xff000000) >> 24));
        }
    }
}
