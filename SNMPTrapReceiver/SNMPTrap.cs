using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;

namespace SNMPTrapReceiver
{
    public class SNMPTrapVarBind
    {
        public String oID;
        public String value;

        public SNMPTrapVarBind(String oID, String value)
        {
            this.oID = oID;
            this.value = value;
        }

        public String ToString()
        {
            return this.oID + " ==> " + this.value;
        }
    }

    public enum DataType
    {
        trap = 0x04
    }

    public enum GenericTrap
    {
        coldStart = 0,
        warmStart = 1,
        linkDown = 2,
        linkUp = 3,
        authenticationFailure = 4,
        egpNeighborLoss = 5,
        enterpriseSpecific = 6
    }


    public class SNMPTrap
    {
        public DateTime receivedDate;
        public IPEndPoint receivedEP;
        public DataType datatype;
        public String community;
        public String enterprise;
        public IPAddress agentAddr;
        public GenericTrap genericTrap;
        public Int32 specificTrap;
        public List<SNMPTrapVarBind> varBindList = new List<SNMPTrapVarBind>();

        public SNMPTrap(DateTime receivedDate, IPEndPoint receivedEP, Byte[] packet)
        {
            this.receivedDate = receivedDate;
            this.receivedEP = new IPEndPoint(receivedEP.Address, receivedEP.Port);
            ParsePacket(receivedDate, receivedEP, packet);
        }

        public String ToString()
        {
            String ret = "";

            ret += "Date: " + receivedDate.ToString(Thread.CurrentThread.CurrentCulture) + Environment.NewLine;
            ret += "From: " + receivedEP.Address.ToString() + Environment.NewLine;
            ret += "Community: " + community + Environment.NewLine;
            ret += "Enterprise: " + enterprise + Environment.NewLine;
            ret += "Agent addr: " + agentAddr.ToString() + Environment.NewLine;

            Int32 i = 1;
            foreach (SNMPTrapVarBind b in varBindList)
            {
                ret += "VarBind[" + i + "] OID: " + b.oID + Environment.NewLine;
                ret += "VarBind[" + i + "] Value: " + b.value + Environment.NewLine;
                i++;
            }

            return ret;
        }

        private void ParsePacket(DateTime receivedDate, IPEndPoint receivedEP, Byte[] packet)
        {
            int commlength, miblength;
            
            if (packet[0] == 0xff)
            {
                Console.Error.WriteLine("Invalid Packet received from {0}", receivedEP.ToString());
                return;
            }

            //Só é valido para versão 1 do SNMP
            Int32 zeroPos = Encoding.UTF8.GetString(packet).IndexOf("\0");

            if (zeroPos > 7)
            {
                Console.Error.WriteLine("Invalid Packet rceeived from {0}", receivedEP.ToString());
                return;
            }

            using (MemoryStream stm = new MemoryStream(packet, zeroPos + 1, packet.Length - zeroPos - 1))
            using (BinaryReader pData = new BinaryReader(stm))
            {
                datatype = (DataType)pData.ReadByte();
                commlength = Convert.ToInt16(pData.ReadByte());
                community = Encoding.UTF8.GetString(pData.ReadBytes(commlength));
                pData.ReadByte(); //i have no idea
                miblength = Convert.ToInt16(pData.ReadByte());

                switch (datatype)
                {
                    case DataType.trap://Trap PDU Item 4.1.6.  The Trap-PDU RFC 1157
                        //https://docs.microsoft.com/pt-br/windows/desktop/SecCertEnroll/about-object-identifier
                        pData.ReadByte(); //i have no idea
                        Int32 oIDLen = Convert.ToInt16(pData.ReadByte());
                        enterprise = OidByteArrayToString(pData.ReadBytes(oIDLen));

                        pData.ReadByte(); //i have no idea
                        Int32 agentAddrLen = Convert.ToInt16(pData.ReadByte());

                        agentAddr = new IPAddress(pData.ReadBytes(agentAddrLen));

                        pData.ReadByte(); //i have no idea
                        pData.ReadByte(); //i have no idea

                        genericTrap = (GenericTrap)pData.ReadByte();

                        pData.ReadByte(); //i have no idea
                        pData.ReadByte(); //i have no idea

                        specificTrap = Convert.ToInt32(pData.ReadByte());

                        //VarBindList

                        pData.ReadBytes(5);

                        while (pData.BaseStream.Position < pData.BaseStream.Length - 2)
                        {
                            pData.ReadByte(); //i have no idea
                            Int32 varBindLen = Convert.ToInt16(pData.ReadByte());
                            pData.ReadByte(); //i have no idea

                            oIDLen = Convert.ToInt16(pData.ReadByte());
                            String varBindOID = OidByteArrayToString(pData.ReadBytes(oIDLen));

                            pData.ReadByte(); //i have no idea

                            Int32 valueLen = Convert.ToInt16(pData.ReadByte());
                            Byte[] bValue = pData.ReadBytes(valueLen);
                            String value = Encoding.UTF8.GetString(bValue);

                            varBindList.Add(new SNMPTrapVarBind(varBindOID, value));
                        }

                        break;

                    default:
#if DEBUG
                        Console.Error.WriteLine("DataType não reconhecido: {0}", datatype);
#endif
                        break;
                }

            }

        }

        private string OidByteArrayToString(byte[] oid)
        {
            StringBuilder retVal = new StringBuilder();

            for (int i = 0; i < oid.Length; i++)
            {
                if (i == 0)
                {
                    int b = oid[0] % 40;
                    int a = (oid[0] - b) / 40;
                    retVal.AppendFormat("{0}.{1}", a, b);
                }
                else
                {
                    if (oid[i] < 128)
                        retVal.AppendFormat(".{0}", oid[i]);
                    else
                    {
                        retVal.AppendFormat(".{0}",
                           ((oid[i] - 128) * 128) + oid[i + 1]);
                        i++;
                    }
                }
            }

            return retVal.ToString();
        }
    }
}
