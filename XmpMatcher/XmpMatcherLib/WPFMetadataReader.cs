using System;
using System.Collections.Generic;
using System.IO;

namespace gbd.XmpMatcher.Lib
{
    public class WpfMetadataReader
    {
        public static DateTime? GetPhotoshopCreateDate(FileInfo file)
        {
            var rawMetadataItems = new List<RawMetadataItem>();

            BitmapFrame s = null;

            using (Stream fileStream = File.Open(file, FileMode.Open))
            {
                System.Windows.Media.Imaging.BitmapDecoder decoder = BitmapDecoder.Create(fileStream,
                    BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.None);
                CaptureMetadata(decoder.Frames[0].Metadata, string.Empty);
            }



            throw new NotImplementedException();
        }
    }
}