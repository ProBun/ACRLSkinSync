using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AcrlSync.Model
{
    public class UploadFiles
    {
        public string Name { get; set; }
        public string Fullname { get; set; }
        public string Ext { get; set; }
        public bool AllowedExt { get; set; }
        public bool IsDDS { get; set; }
        public bool IsCompressed { get; set; }
        public bool ValidSize { get; set; }
        public bool Valid { get; set; } //used for strikethrough
        public bool Missing { get; set; } //used for red
        public bool Transferable { get; set; } //used for showing upload box
        public bool Transfer { get; set; } 
        public string Error { get; set; }

        private static readonly string[] _validExtensions = { ".jpg", ".png", ".dds", ".ini", ".jpeg", ".json" };

        public UploadFiles(string path)
        {
            Fullname = path;
            Name = Path.GetFileName(path);
            Ext = Path.GetExtension(path);
            AllowedExt = _validExtensions.Contains(this.Ext.ToLower());
            Missing = false;
            Valid = true;
            Transfer = true;
            Error = "";

            if (AllowedExt == false)
            {
                Valid = false;
                Transfer = false;
                Error = "Invalid ext";
            }

            IsDDS = String.Compare(this.Ext, ".dds", true) == 0;
            if (IsDDS)
            {
                long length = new System.IO.FileInfo(path).Length;
                IsCompressed = length < 1e7;
                // Console.WriteLine("length " + length.ToString());

                if (IsCompressed == false)
                {
                    Valid = false;
                    Transfer = false;
                    Error = "Too large";
                }

                var dds = new DdsParser(path);
                ValidSize = dds.Width < 2049 && dds.Height < 2049;

                if (ValidSize == false)
                {
                    Valid = false;
                    Transfer = false;
                    Error = "Exceeds 2048px";
                }
            }
            Transferable = Valid;
        }


        public UploadFiles(string fullName, bool missing)
        {
            Name = fullName;
            Missing = true;
            Valid = true;
            Transfer = false;
            Transferable = false;
            Error = "File is missing!";
        }
    }

    /// <summary>
    /// read dds header after size in px
    /// Stolen code from https://gist.github.com/soeminnminn/e9c4c99867743a717f5b
    /// </summary>
    class DdsParser
    {
        public uint Width { get; set; }
        public uint Height { get; set; }
        public DdsParser(string image)
        {
            Stream ddsImage = File.Open(image, FileMode.Open);
            if (ddsImage == null) return;
            if (!ddsImage.CanRead) return;

            using (BinaryReader reader = new BinaryReader(ddsImage))
            {
                DDSStruct header = new DDSStruct();

                if (this.ReadHeader(reader, ref header))
                {
                    //Console.WriteLine(String.Format("DDS: {0} is {1}x{2}",image,header.width,header.height));
                    Width = header.width;
                    Height = header.height;
                }
                else
                {
                    //failed to read dds
                    string errorMessage = "Could not connect to FTP: " + ConnectionSettings.Options.HostName;
                    System.Windows.MessageBox.Show(errorMessage, "Connection Failure", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }

        private bool ReadHeader(BinaryReader reader, ref DDSStruct header)
        {
            try
            {
                byte[] signature = reader.ReadBytes(4);
                if (!(signature[0] == 'D' && signature[1] == 'D' && signature[2] == 'S' && signature[3] == ' '))
                    return false;

                header.size = reader.ReadUInt32();
                if (header.size != 124)
                    return false;

                //convert the data
                header.flags = reader.ReadUInt32();
                header.height = reader.ReadUInt32();
                header.width = reader.ReadUInt32();
                header.sizeorpitch = reader.ReadUInt32();
                header.depth = reader.ReadUInt32();
                header.mipmapcount = reader.ReadUInt32();
                header.alphabitdepth = reader.ReadUInt32();

                header.reserved = new uint[10];
                for (int i = 0; i < 10; i++)
                {
                    header.reserved[i] = reader.ReadUInt32();
                }

                //pixelfromat
                header.pixelformat.size = reader.ReadUInt32();
                header.pixelformat.flags = reader.ReadUInt32();
                header.pixelformat.fourcc = reader.ReadUInt32();
                header.pixelformat.rgbbitcount = reader.ReadUInt32();
                header.pixelformat.rbitmask = reader.ReadUInt32();
                header.pixelformat.gbitmask = reader.ReadUInt32();
                header.pixelformat.bbitmask = reader.ReadUInt32();
                header.pixelformat.alphabitmask = reader.ReadUInt32();

                //caps
                header.ddscaps.caps1 = reader.ReadUInt32();
                header.ddscaps.caps2 = reader.ReadUInt32();
                header.ddscaps.caps3 = reader.ReadUInt32();
                header.ddscaps.caps4 = reader.ReadUInt32();
                header.texturestage = reader.ReadUInt32();

                return true;
            }
            catch(Exception e)
            {
                string errorMessage = "Could not read DDS information: " + e.InnerException;
                System.Windows.MessageBox.Show(errorMessage, "DDS Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return false;
            }
        }

        #region DDSStruct
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct DDSStruct
        {
            public uint size;		// equals size of struct (which is part of the data file!)
            public uint flags;
            public uint height;
            public uint width;
            public uint sizeorpitch;
            public uint depth;
            public uint mipmapcount;
            public uint alphabitdepth;
            //[MarshalAs(UnmanagedType.U4, SizeConst = 11)]
            public uint[] reserved;//[11];

            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
            public struct Pixelformatstruct
            {
                public uint size;	// equals size of struct (which is part of the data file!)
                public uint flags;
                public uint fourcc;
                public uint rgbbitcount;
                public uint rbitmask;
                public uint gbitmask;
                public uint bbitmask;
                public uint alphabitmask;
            }
            public Pixelformatstruct pixelformat;

            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
            public struct DdsCapsStruct
            {
                public uint caps1;
                public uint caps2;
                public uint caps3;
                public uint caps4;
            }
            public DdsCapsStruct ddscaps;
            public uint texturestage;

            //#ifndef __i386__
            //void to_little_endian()
            //{
            //	size_t size = sizeof(DDSStruct);
            //	assert(size % 4 == 0);
            //	size /= 4;
            //	for (size_t i=0; i<size; i++)
            //	{
            //		((int32_t*) this)[i] = little_endian(((int32_t*) this)[i]);
            //	}
            //}
            //#endif
        }
        #endregion

        #region DDSStruct Flags
        private const int DDSD_CAPS = 0x00000001;
        private const int DDSD_HEIGHT = 0x00000002;
        private const int DDSD_WIDTH = 0x00000004;
        private const int DDSD_PITCH = 0x00000008;
        private const int DDSD_PIXELFORMAT = 0x00001000;
        private const int DDSD_MIPMAPCOUNT = 0x00020000;
        private const int DDSD_LINEARSIZE = 0x00080000;
        private const int DDSD_DEPTH = 0x00800000;
        #endregion

        #region PixelFormat
        /// <summary>
        /// Various pixel formats/compressors used by the DDS image.
        /// </summary>
        private enum PixelFormat
        {
            /// <summary>
            /// 32-bit image, with 8-bit red, green, blue and alpha.
            /// </summary>
            RGBA,
            /// <summary>
            /// 24-bit image with 8-bit red, green, blue.
            /// </summary>
            RGB,
            /// <summary>
            /// 16-bit DXT-1 compression, 1-bit alpha.
            /// </summary>
            DXT1,
            /// <summary>
            /// DXT-2 Compression
            /// </summary>
            DXT2,
            /// <summary>
            /// DXT-3 Compression
            /// </summary>
            DXT3,
            /// <summary>
            /// DXT-4 Compression
            /// </summary>
            DXT4,
            /// <summary>
            /// DXT-5 Compression
            /// </summary>
            DXT5,
            /// <summary>
            /// 3DC Compression
            /// </summary>
            THREEDC,
            /// <summary>
            /// ATI1n Compression
            /// </summary>
            ATI1N,
            LUMINANCE,
            LUMINANCE_ALPHA,
            RXGB,
            A16B16G16R16,
            R16F,
            G16R16F,
            A16B16G16R16F,
            R32F,
            G32R32F,
            A32B32G32R32F,
            /// <summary>
            /// Unknown pixel format.
            /// </summary>
            UNKNOWN
        }
        #endregion

    }
}
