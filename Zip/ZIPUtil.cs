using System;
using System.Collections.Generic;
using System.Text;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Core;
using System.IO;

namespace Zip
{
    public class ZIPUtil
    {
        static public void DecompressFile(FileInfo ZIPFile, DirectoryInfo tmpDir)
        {
            BinaryReader reader = new BinaryReader(ZIPFile.Open(FileMode.Open));
            Byte[] data = reader.ReadBytes((Int32)reader.BaseStream.Length);
            reader.BaseStream.Dispose();
            reader.Close();
            reader = null;

            DecompressData(data, tmpDir);

            data = null;
        }

        static public void DecompressData(Byte[] data, DirectoryInfo tmpDir)
        {

            if (!tmpDir.Exists)
                tmpDir.Create();

            MemoryStream stream = new MemoryStream(data);

            using (ZipInputStream inputStream = new ZipInputStream(stream))
            {
                ZipEntry theEntry;

                while ((theEntry = inputStream.GetNextEntry()) != null)
                {
                    if (theEntry.IsFile)
                    {
                        ExtractFile(inputStream, theEntry, tmpDir.FullName);
                    }
                    else if (theEntry.IsDirectory)
                    {
                        ExtractDirectory(inputStream, theEntry, tmpDir.FullName);
                    }
                }
            }
        }

        static public void CompressToFile(DirectoryInfo tmpDir, String filename)
        {
            Byte[] reportData = Compress(tmpDir);

            BinaryWriter writer = new BinaryWriter(File.Create(filename));
            writer.Write(reportData);
            writer.Close();

        }

        static public Byte[] Compress(DirectoryInfo tmpDir)
        {
            MemoryStream stream = new MemoryStream();
            ZipOutputStream strmZipOutputStream = new ZipOutputStream(stream);

            try
            {
                //Compression Level: 0-9
                //0: no(Compression)
                // 9: maximum compression
                strmZipOutputStream.SetLevel(9);

                Byte[] abyBuffer = new Byte[4096];

                foreach (FileInfo file in tmpDir.GetFiles("*", SearchOption.AllDirectories))
                {
                    FileStream strmFile = File.OpenRead(file.FullName);
                    try
                    {
                        String zipFileName = file.FullName.Replace(tmpDir.FullName, "").TrimStart("\\".ToCharArray()).Replace("\\", "/");
                        ZipEntry objZipEntry = new ZipEntry(zipFileName);

                        objZipEntry.DateTime = DateTime.Now;
                        objZipEntry.Size = strmFile.Length;

                        strmZipOutputStream.PutNextEntry(objZipEntry);
                        StreamUtils.Copy(strmFile, strmZipOutputStream, abyBuffer);
                    }
                    finally
                    {
                        strmFile.Close();
                    }
                }

                strmZipOutputStream.Finish();
            }
            finally
            {
                strmZipOutputStream.Close();
            }

            return stream.ToArray();
        }


        static private bool ExtractFile(ZipInputStream inputStream, ZipEntry theEntry, string targetDir)
        {
            // try and sort out the correct place to save this entry
            string entryFileName;

            if (Path.IsPathRooted(theEntry.Name))
            {
                string workName = Path.GetPathRoot(theEntry.Name);
                workName = theEntry.Name.Substring(workName.Length);
                entryFileName = Path.Combine(Path.GetDirectoryName(workName), Path.GetFileName(theEntry.Name));
            }
            else
            {
                entryFileName = theEntry.Name;
            }

            string targetName = Path.Combine(targetDir, entryFileName);

            string fullPath = Path.GetDirectoryName(Path.GetFullPath(targetName));
#if TEST
			Console.WriteLine("Decompress targetfile name " + entryFileName);
			Console.WriteLine("Decompress targetpath " + fullPath);
#endif

            // Could be an option or parameter to allow failure or try creation
            if (Directory.Exists(fullPath) == false)
            {
                try
                {
                    Directory.CreateDirectory(fullPath);
                }
                catch
                {
                    return false;
                }
            }


            if (entryFileName.Length > 0)
            {
#if TEST
				Console.WriteLine("Extracting...");
#endif
                using (FileStream streamWriter = File.Create(targetName))
                {
                    byte[] data = new byte[4096];
                    int size;

                    do
                    {
                        size = inputStream.Read(data, 0, data.Length);
                        streamWriter.Write(data, 0, size);
                    } while (size > 0);
                }

                File.SetLastWriteTime(targetName, theEntry.DateTime);
            }
            return true;
        }

        static private void ExtractDirectory(ZipInputStream inputStream, ZipEntry theEntry, string targetDir)
        {
            // For now do nothing.
        }
    }
}
