using System;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Collections;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Media.Imaging; //PresentationCore

namespace ImageTools
{
    ///<summary>This Class retrives Image Properties using Image.GetPropertyItem() method 
    /// and gives access to some of them trough its public properties. Or to all of them
    /// trough its public property PropertyItems.
    ///</summary>
    public class ImageInfo : IDisposable
    {
        public void Dispose()
        {
            if (_image != null) _image.Dispose();
            _image = null;
        }

        ///<summary>Wenn using this constructor the Image property must be set before accessing properties.</summary>
        public ImageInfo()
        {
        }

        ///<summary>Creates Info Class to read properties of an Image given from a file.</summary>
        /// <param name="imageFileName">A string specifiing image file name on a file system.</param>
        public ImageInfo(string imageFileName)
        {

            using (Stream fileStream = File.Open(imageFileName, FileMode.Open))
            {
                BitmapDecoder decoder = BitmapDecoder.Create(fileStream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.None);
                _metadata = decoder.Frames[0].Metadata as BitmapMetadata;
            }

            _image = System.Drawing.Image.FromFile(imageFileName);
        }

        ///<summary>Creates Info Class to read properties of a given Image object.</summary>
        /// <param name="anImage">An Image object to analise.</param>
        public ImageInfo(System.Drawing.Image anImage)
        {
            _image = anImage;

            using (MemoryStream stm = new MemoryStream())
            {

                _image.Save(stm, System.Drawing.Imaging.ImageFormat.Jpeg);

                BitmapDecoder decoder = BitmapDecoder.Create(stm, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.None);
                _metadata = decoder.Frames[0].Metadata as BitmapMetadata;
            }

        }

        ///<summary>Creates Info Class to read properties of a given Image object.</summary>
        /// <param name="anImage">An Image object to analise.</param>
        public ImageInfo(System.Drawing.Bitmap anImage)
        {
            _image = (System.Drawing.Image)anImage;
        }

        System.Windows.Media.Imaging.BitmapMetadata _metadata;
        public System.Windows.Media.Imaging.BitmapMetadata Metadata
        {
            get { return _metadata; }
        }

        System.Drawing.Image _image;
        ///<summary>Sets or returns the current Image object.</summary>
        public System.Drawing.Image Image
        {
            set { _image = value; }
            get { return _image; }
        }

        public Boolean HasExif
        {

            get
            {
                try
                {
                    String tst = (string)PropertyTag.getValue(_image.GetPropertyItem((int)PropertyTagId.EquipMake));
                    return true;
                }
                catch {
                    return false;
                }
            }

        }

        ///<summary>
        /// Type is PropertyTagTypeShort or PropertyTagTypeLong
        ///Information specific to compressed data. When a compressed file is recorded, the valid width of the meaningful image must be recorded in this tag, whether or not there is padding data or a restart marker. This tag should not exist in an uncompressed file.
        /// </summary>
        public uint PixXDim
        {
            get
            {
                object tmpValue = PropertyTag.getValue(_image.GetPropertyItem((int)PropertyTagId.ExifPixXDim));
                if (tmpValue.GetType().ToString().Equals("System.UInt16")) return (uint)(ushort)tmpValue;
                return (uint)tmpValue;
            }
        }
        ///<summary>
        /// Type is PropertyTagTypeShort or PropertyTagTypeLong
        /// Information specific to compressed data. When a compressed file is recorded, the valid height of the meaningful image must be recorded in this tag whether or not there is padding data or a restart marker. This tag should not exist in an uncompressed file. Because data padding is unnecessary in the vertical direction, the number of lines recorded in this valid image height tag will be the same as that recorded in the SOF.
        /// </summary>
        public uint PixYDim
        {
            get
            {
                object tmpValue = PropertyTag.getValue(_image.GetPropertyItem((int)PropertyTagId.ExifPixYDim));
                if (tmpValue.GetType().ToString().Equals("System.UInt16")) return (uint)(ushort)tmpValue;
                return (uint)tmpValue;
            }
        }

        ///<summary>
        ///Number of pixels per unit in the image width (x) direction. The unit is specified by PropertyTagResolutionUnit
        ///</summary>
        public Fraction XResolution
        {
            get
            {
                return (Fraction)PropertyTag.getValue(_image.GetPropertyItem((int)PropertyTagId.XResolution));
            }
        }

        ///<summary>
        ///Number of pixels per unit in the image height (y) direction. The unit is specified by PropertyTagResolutionUnit.
        ///</summary>
        public Fraction YResolution
        {
            get
            {
                return (Fraction)PropertyTag.getValue(_image.GetPropertyItem((int)PropertyTagId.YResolution));
            }
        }

        ///<summary>
        ///Unit of measure for the horizontal resolution and the vertical resolution.
        ///2 - inch 3 - centimeter
        ///</summary>
        public ResolutionUnit ResolutionUnit
        {
            get
            {
                return (ResolutionUnit)PropertyTag.getValue(_image.GetPropertyItem((int)PropertyTagId.ResolutionUnit));
            }
        }

        ///<summary>
        ///Brightness value. The unit is the APEX value. Ordinarily it is given in the range of -99.99 to 99.99.
        ///</summary>
        public Fraction Brightness
        {
            get
            {
                return (Fraction)PropertyTag.getValue(_image.GetPropertyItem((int)PropertyTagId.ExifBrightness));
            }
        }

        ///<summary>
        /// The manufacturer of the equipment used to record the image.
        ///</summary>
        public string EquipMake
        {
            get
            {
                return (string)PropertyTag.getValue(_image.GetPropertyItem((int)PropertyTagId.EquipMake));
            }
            set
            {
                SetPropertyItemString(_image, PropertyTagId.EquipMake, value);
            }
        }

        ///<summary>
        /// The model name or model number of the equipment used to record the image.
        /// </summary>
        public string EquipModel
        {
            get
            {
                return (string)PropertyTag.getValue(_image.GetPropertyItem((int)PropertyTagId.EquipModel));
            }
            set
            {
                SetPropertyItemString(_image, PropertyTagId.EquipModel, value);
            }
        }

        ///<summary>
        ///Copyright information.
        ///</summary>
        public string Copyright
        {
            get
            {
                return Encoding.Unicode.GetString((Byte[])PropertyTag.getValue(_image.GetPropertyItem((int)PropertyTagId.Copyright)));
            }
            set
            {
                SetPropertyItemString(_image, PropertyTagId.Copyright, value);
            }
        }

        ///<summary>
        ///Author information.
        ///</summary>
        public string Author
        {
            get
            {
                return Encoding.Unicode.GetString((Byte[])PropertyTag.getValue(_image.GetPropertyItem((int)PropertyTagId.Author)));
            }
            set
            {
                SetPropertyItemString(_image, PropertyTagId.Author, value);
            }
        }

        ///<summary>
        ///Comment information.
        ///</summary>
        public string Comment
        {
            get
            {
                return Encoding.Unicode.GetString((Byte[])PropertyTag.getValue(_image.GetPropertyItem((int)PropertyTagId.Comment)));
            }
            set
            {
                SetPropertyItemString(_image, PropertyTagId.Comment, value);
            }
        }

        ///<summary>
        ///Comment information.
        ///</summary>
        public string Keywords
        {
            get
            {
                return Encoding.Unicode.GetString((Byte[])PropertyTag.getValue(_image.GetPropertyItem((int)PropertyTagId.Keywords)));
            }
            set
            {
                SetPropertyItemString(_image, PropertyTagId.Keywords, value);
            }
        }

        ///<summary>
        ///Subject information.
        ///</summary>
        public string Subject
        {
            get
            {
                return Encoding.Unicode.GetString((Byte[])PropertyTag.getValue(_image.GetPropertyItem((int)PropertyTagId.Subject)));
            }
            set
            {
                SetPropertyItemString(_image, PropertyTagId.Subject, value);
            }
        }

        ///<summary>
        ///Title information.
        ///</summary>
        public string Title
        {
            get
            {
                return Encoding.Unicode.GetString((Byte[])PropertyTag.getValue(_image.GetPropertyItem((int)PropertyTagId.Title)));
            }
            set
            {
                SetPropertyItemString(_image, PropertyTagId.Title, value);
            }
        }

        ///<summary>
        ///Date and time the image was created.
        ///</summary>		
        public string DateTime
        {
            get
            {
                return (string)PropertyTag.getValue(_image.GetPropertyItem((int)PropertyTagId.DateTime));
            }
        }

        //The format is YYYY:MM:DD HH:MM:SS with time shown in 24-hour format and the date and time separated by one blank character (0x2000). The character string length is 20 bytes including the NULL terminator. When the field is empty, it is treated as unknown.
        private static DateTime ExifDTToDateTime(string exifDT)
        {
            exifDT = exifDT.Replace(' ', ':');
            string[] ymdhms = exifDT.Split(':');
            int years = int.Parse(ymdhms[0]);
            int months = int.Parse(ymdhms[1]);
            int days = int.Parse(ymdhms[2]);
            int hours = int.Parse(ymdhms[3]);
            int minutes = int.Parse(ymdhms[4]);
            int seconds = int.Parse(ymdhms[5]);
            return new DateTime(years, months, days, hours, minutes, seconds);
        }

        ///<summary>
        ///Date and time when the original image data was generated. For a DSC, the date and time when the picture was taken. 
        ///</summary>
        public DateTime DTOrig
        {
            get
            {
                string tmpStr = (string)PropertyTag.getValue(_image.GetPropertyItem((int)PropertyTagId.ExifDTOrig));
                return ExifDTToDateTime(tmpStr);
            }
        }

        ///<summary>
        ///Date and time when the image was stored as digital data. If, for example, an image was captured by DSC and at the same time the file was recorded, then DateTimeOriginal and DateTimeDigitized will have the same contents.
        ///</summary>
        public DateTime DTDigitized
        {
            get
            {
                string tmpStr = (string)PropertyTag.getValue(_image.GetPropertyItem((int)PropertyTagId.ExifDTDigitized));
                return ExifDTToDateTime(tmpStr);
            }
        }


        ///<summary>
        ///ISO speed and ISO latitude of the camera or input device as specified in ISO 12232.
        ///</summary>		
        public ushort ISOSpeed
        {
            get
            {
                return (ushort)PropertyTag.getValue(_image.GetPropertyItem((int)PropertyTagId.ExifISOSpeed));
            }
        }

        ///<summary>
        ///Image orientation viewed in terms of rows and columns.
        ///</summary>				
        public Orientation Orientation
        {
            get
            {
                return (Orientation)PropertyTag.getValue(_image.GetPropertyItem((int)PropertyTagId.Orientation));
            }
        }

        ///<summary>
        ///Actual focal length, in millimeters, of the lens. Conversion is not made to the focal length of a 35 millimeter film camera.
        ///</summary>						
        public Fraction FocalLength
        {
            get
            {
                return (Fraction)PropertyTag.getValue(_image.GetPropertyItem((int)PropertyTagId.ExifFocalLength));
            }
        }

        ///<summary>
        ///F number.
        ///</summary>						
        public Fraction FNumber
        {
            get
            {
                return (Fraction)PropertyTag.getValue(_image.GetPropertyItem((int)PropertyTagId.ExifFNumber));
            }
        }

        //
        ///<summary>
        ///Class of the exposure when the picture is taken.
        ///</summary>						
        public Fraction ExposureTime
        {
            get
            {
                return (Fraction)PropertyTag.getValue(_image.GetPropertyItem((int)PropertyTagId.ExifExposureTime));
            }
        }

        ///<summary>
        ///Class of the program used by the camera to set exposure when the picture is taken.
        ///</summary>						
        public ExposureProg ExposureProg
        {
            get
            {
                return (ExposureProg)PropertyTag.getValue(_image.GetPropertyItem((int)PropertyTagId.ExifExposureProg));
            }
        }

        ///<summary>
        ///Metering mode.
        ///</summary>						
        public MeteringMode MeteringMode
        {
            get
            {
                return (MeteringMode)PropertyTag.getValue(_image.GetPropertyItem((int)PropertyTagId.ExifMeteringMode));
            }
        }


        ///<summary>
        ///Metering mode.
        ///</summary>						
        public Int32 Rating
        {
            get
            {
                Object ret = _metadata.GetQuery("/xmp/xmp:Rating");
                Int32 rating = 0;
                if (ret != null)
                    rating = Int32.Parse(ret.ToString());
                else
                    rating = 0;

                if (rating > 5)
                {
                    rating = (5 * rating) / 100;
                }
                return rating;
            }
        }

        private Hashtable _propertyItems;
        ///<summary>
        /// Returns a Hashtable of all available Properties of a gieven Image. Keys of this Hashtable are
        /// Display names of the Property Tags and values are transformed (typed) data.
        ///</summary>
        /// <example>
        /// <code>
        /// if (openFileDialog.ShowDialog()==DialogResult.OK)
        ///	{
        ///		Info inf=new Info(Image.FromFile(openFileDialog.FileName));
        ///		listView.Items.Clear();
        ///		foreach (string propertyname in inf.PropertyItems.Keys)
        ///		{
        ///			ListViewItem item1 = new ListViewItem(propertyname,0);
        ///		    item1.SubItems.Add((inf.PropertyItems[propertyname]).ToString());
        ///			listView.Items.Add(item1);
        ///		}
        ///	}
        /// </code>
        ///</example>
        public Hashtable PropertyItems
        {
            get
            {

                foreach (int id in _image.PropertyIdList)
                    Console.WriteLine("case PropertyTagId." + ((PropertyTagId)id).ToString() + ":\r\nPropertyItem pi = new PropertyItem();\r\npi.Type = " + ((PropertyItem)_image.GetPropertyItem(id)).Type + ";\r\nbreak;");

                if (_propertyItems == null)
                {
                    _propertyItems = new Hashtable();
                    foreach (int id in _image.PropertyIdList)
                        _propertyItems[((PropertyTagId)id).ToString()] = PropertyTag.getValue(_image.GetPropertyItem(id));

                }
                return _propertyItems;
            }
        }

        public System.Drawing.Image CopyMetadataTo(System.Drawing.Image image)
        {

            System.Drawing.Image ret = (System.Drawing.Image)image.Clone();

            BitmapMetadata myBitmapMetadata = null;
            JpegBitmapEncoder encoder3 = new JpegBitmapEncoder();
            

            using (MemoryStream stmSrc = new MemoryStream())
            {

                _image.Save(stmSrc, System.Drawing.Imaging.ImageFormat.Jpeg);
                stmSrc.Position = 0;

                BitmapDecoder decoderSrc = BitmapDecoder.Create(stmSrc, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.None);

                //
                // Check to see that the image's metadata is not null
                //    
                /*/if (decoderSrc.Frames[0].Metadata == null)
                {
                    myBitmapMetadata = new BitmapMetadata("jpg");
                }
                else
                {
                    myBitmapMetadata = decoderSrc.Frames[0].Metadata as BitmapMetadata;
                }*/
                myBitmapMetadata = new BitmapMetadata("jpg");

                
                myBitmapMetadata.ApplicationName = "Photoe.com.br Digital Image Transfer";
                myBitmapMetadata.Author = new ReadOnlyCollection<string>(
                    new List<string>() { "Helvio Junior" });
                myBitmapMetadata.CameraManufacturer = _metadata.CameraManufacturer;
                myBitmapMetadata.CameraModel = _metadata.CameraModel;
                myBitmapMetadata.Comment = _metadata.Comment;
                myBitmapMetadata.Copyright = "photoe.com.br - Helvio Junior";
                myBitmapMetadata.DateTaken = _metadata.DateTaken;
                //myBitmapMetadata.Keywords = new ReadOnlyCollection<string>(new List<string>() { "Lori", "Kane" });
                myBitmapMetadata.Keywords = _metadata.Keywords;
                myBitmapMetadata.Rating = _metadata.Rating;
                myBitmapMetadata.Subject = _metadata.Subject;
                myBitmapMetadata.Title = _metadata.Title;


                // Create a new frame that is identical to the one  
                // from the original image, except for the new metadata. 
                
                encoder3.Frames.Add(
                    BitmapFrame.Create(
                    decoderSrc.Frames[0],
                    decoderSrc.Frames[0].Thumbnail,
                    myBitmapMetadata,
                    decoderSrc.Frames[0].ColorContexts));

                FileInfo tmpFile = new FileInfo(Path.GetTempFileName());

                using (Stream tempStream = File.Open(tmpFile.FullName, FileMode.Create, FileAccess.ReadWrite))
                {
                    encoder3.Save(tempStream);
                }

                ret = System.Drawing.Image.FromFile(tmpFile.FullName);

                try
                {
                    tmpFile.Delete();
                }
                catch { }

                /*
                using (MemoryStream stm2 = new MemoryStream())
                {

                    encoder3.Save(stm2);
                    stm2.Position = 0;

                    ret = System.Drawing.Image.FromStream(stm2);
                }*/

            }

            return ret;
        }

        public void CopyExifTo(System.Drawing.Image image)
        {

            List<PropertyTagId> tags = new List<PropertyTagId>();
            tags.Add(PropertyTagId.Title);
            tags.Add(PropertyTagId.Subject);
            tags.Add(PropertyTagId.Keywords);
            tags.Add(PropertyTagId.Comment);
            tags.Add(PropertyTagId.Author);
            tags.Add(PropertyTagId.Copyright);
            tags.Add(PropertyTagId.EquipModel);
            tags.Add(PropertyTagId.EquipMake);

            foreach (PropertyTagId id in tags)
            {
                try
                {
                    CopyPropertyItem(_image, image, id);

                }
                catch { }
            }

        }

        private void CopyPropertyItem(System.Drawing.Image source, System.Drawing.Image destination, PropertyTagId id)
        {
            PropertyItem propItem = source.GetPropertyItem((int)id);
            destination.SetPropertyItem(propItem);

            /*
            switch (propItem.Type)
            {
                case 1: //Specifies that Value is an array of bytes.
                    destination.SetPropertyItem(propItem);
                    break;

                case 2: //Specifies that Value is a null-terminated ASCII string. If you set the type data member to ASCII type, you should set the Len property to the length of the string including the null terminator. For example, the string "Hello" would have a length of 6.
                    throw new NotImplementedException();
                    break;

                case 3: //Specifies that Value is an array of unsigned short (16-bit) integers.
                    throw new NotImplementedException();
                    break;

                case 4: //Specifies that Value is an array of unsigned long (32-bit) integers.
                    throw new NotImplementedException();
                    break;

                case 5: //Specifies that Value data member is an array of pairs of unsigned long integers. Each pair represents a fraction; the first integer is the numerator and the second integer is the denominator.
                    throw new NotImplementedException();
                    break;

                case 6: //Specifies that Value is an array of bytes that can hold values of any data type.
                    throw new NotImplementedException();
                    break;

                case 7: //Specifies that Value is an array of signed long (32-bit) integers.
                    throw new NotImplementedException();
                    break;

                case 10: //Specifies that Value is an array of pairs of signed long integers. Each pair represents a fraction; the first integer is the numerator and the second integer is the denominator.
                    throw new NotImplementedException();
                    break;

            }*/
            
        }

        private void SetPropertyItemString(System.Drawing.Image image, PropertyTagId id, String value)
        {
            byte[] buffer = Encoding.Unicode.GetBytes(value);
            PropertyItem propItem = image.GetPropertyItem((int)id);
            propItem.Len = buffer.Length;
            propItem.Value = buffer;
            image.SetPropertyItem(propItem);
        }
    }
}
