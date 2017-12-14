using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace LoadTestLib
{
    [Serializable()]
    public class Profile
    {

        public Uri BaseUri { get; set; }
        public List<UriInfo> Uris { get; set; }
        public List<UriInfo> OutUris { get; set; }

        public Profile()
        {
            this.Uris = new List<UriInfo>();
            this.OutUris = new List<UriInfo>();
        }

        public void LoadProfile(String fileName)
        {
            Byte[] fData = File.ReadAllBytes(fileName);
            LoadProfile(new MemoryStream(fData));
        }

        public void LoadProfile(MemoryStream rawData)
        {
            IFormatter formato = new BinaryFormatter();

            Profile item = (Profile)formato.Deserialize(rawData);
            rawData.Dispose();

            rawData.Close();
            rawData = null;

            this.BaseUri = item.BaseUri;
            this.Uris = item.Uris;
            this.OutUris = item.OutUris;

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


    }
}
