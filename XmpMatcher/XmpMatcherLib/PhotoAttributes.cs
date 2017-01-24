using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using gbd.Tools.Maths;
using gbd.Tools.Time;
using JetBrains.Annotations;
using MetadataExtractor;
using MetadataExtractor.Formats.Xmp;
using NLog;
using XmpCore.Impl;
using Directory = MetadataExtractor.Directory;

namespace gbd.XmpMatcher.Lib
{
    public class PhotoAttributes: IEquatable<PhotoAttributes>
    {

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private static readonly Regex RegexX = new Regex("exif:FocalPlaneXResolution=.(.*?).$", RegexOptions.Multiline);
        private static readonly Regex RegexY = new Regex("exif:FocalPlaneYResolution=.(.*?).$", RegexOptions.Multiline);
        private static readonly Regex RegexFNumber = new Regex("exif:FNumber=.(.*?).$", RegexOptions.Multiline);
        private static readonly Regex RegexFocal = new Regex("exif:FocalLength=.(.*?).$", RegexOptions.Multiline);
        private static readonly Regex RegexTime = new Regex("exif:ExposureTime=.(.*?).$", RegexOptions.Multiline);



        public readonly DateTime? DateShutter;
        public readonly DateTime? DateCreatedPhotoshop;
        public readonly double? FocalPlaneXResolution;
        public readonly double? FocalPlaneYResolution;
        public readonly double? FNumber;
        public readonly double? ExposureTime;
        public readonly double? FocalLength;

        public PhotoAttributes(FileInfo file)
        {
            var type = FileDiscriminator.Process(file);
            switch (type)
            {
                case FileType.Raw:
                    var dirs = ImageMetadataReader.ReadMetadata(file.FullName);
                    var subIfd = dirs.SingleOrDefault(d => d.Name.Equals("Exif SubIFD"));

                    var xmpMeta = dirs.OfType<XmpDirectory>();
                    

                    if (subIfd == null)
                        return;

                    var tagFNumber = subIfd.Tags.Single(t => t.Name.Equals("F-Number"));
                    var tagFocalLen = subIfd.Tags.Single(t => t.Name.Equals("Focal Length"));
                    var tagX = subIfd.Tags.SingleOrDefault(t => t.Name.Equals("Focal Plane X Resolution"));
                    var tagY = subIfd.Tags.SingleOrDefault(t => t.Name.Equals("Focal Plane Y Resolution"));
                    var tagDateOrig = subIfd.Tags.SingleOrDefault(t => t.Name.Equals("Date/Time Original"));
                    var tagTime = subIfd.Tags.SingleOrDefault(t => t.Name.Equals("Exposure Time"));


                    DateShutter = DateTimeManager.Parse(tagDateOrig?.Description);
                    DateCreatedPhotoshop = WpfMetadataReader.GetPhotoshopCreateDate(file);
                    FNumber = double.Parse(tagFNumber.Description.Replace("f/", ""));
                    ExposureTime = Fraction.Parse(tagTime.Description.Replace("sec", ""));
                    FocalPlaneXResolution = Fraction.Parse(tagX?.Description.Replace("inches", ""));
                    FocalPlaneYResolution = Fraction.Parse(tagY?.Description.Replace("inches", ""));
                    FocalLength = double.Parse(tagFocalLen.Description.Replace("mm", ""));
                    

                    break;

                case FileType.Xmp:
                    var xmp = file.OpenText().ReadToEnd();
                    
                    var matchFNumber = RegexFNumber.Match(xmp);
                    var matchX = RegexX.Match(xmp);
                    var matchY = RegexY.Match(xmp);
                    var matchFocal = RegexFocal.Match(xmp);
                    var matchTime = RegexTime.Match(xmp);

                    DateShutter = DateTimeManager.FindAndParse(xmp, @"exif:DateTimeOriginal=");
                    FNumber = Fraction.Parse(matchFNumber.Groups[1].Value);
                    ExposureTime = Fraction.Parse(matchTime.Groups[1].Value);
                    FocalLength = Fraction.Parse(matchFocal.Groups[1].Value);
                    FocalPlaneXResolution = Fraction.Parse(matchX.Groups[1].Value);
                    FocalPlaneYResolution = Fraction.Parse(matchY.Groups[1].Value);
                    DateCreatedPhotoshop = DateTimeManager.FindAndParse(xmp, @"photoshop:DateCreated=");

                    break;
                    
                case FileType.Jpg:
                case FileType.Unknown:
                default:
                    Logger.Warn($"Cannot process {type} file: {file.Name}");
                    throw new NotImplementedException();
            }

            Logger.Debug($"File {file.Name} has attributes {this}");

        }

        


        public PhotoAttributes(PhotoAttributes a)
        {
            DateShutter = a.DateShutter;
            FocalPlaneXResolution = a.FocalPlaneXResolution;
            FocalPlaneYResolution = a.FocalPlaneYResolution;
            FNumber = a.FNumber;
            ExposureTime = a.ExposureTime;
            FocalLength = a.FocalLength;
        }

        public override string ToString()
        {
            return $"({DateShutter}, f/{FocalLength:0.000}, {1000*ExposureTime:0.000}ms, [{DateShutter.Value.Millisecond}])";
        }

        public override int GetHashCode()
        {
            var flooredDate = (DateShutter == null) ? 
                    (DateTime?) null: 
                    new DateTime(
                        DateShutter.Value.Year,
                        DateShutter.Value.Month,
                        DateShutter.Value.Day,
                        DateShutter.Value.Hour,
                        DateShutter.Value.Minute,
                        DateShutter.Value.Second  );

            int a = (flooredDate?.GetHashCode() ?? 0);
            int b = (FNumber?.GetHashCode() ?? 0);
            int c =  (ExposureTime?.GetHashCode() ?? 0);

            return a + b + c;
        }



        public bool Equals(PhotoAttributes other)
        {
            if (other == null)
                return false;

            bool match = DateTimeExtensions.Equals(DateShutter, other.DateShutter, DateTimeExtensions.Field.SecondAndAbove)
                         && FocalLength.Equals(other.FocalLength)
                         && ExposureTime.Equals(other.ExposureTime)
                         && FNumber.EqualsWithError(other.FNumber, 0.1);

            return match;
        }


        public override bool Equals(object obj)
        {
            if (obj is PhotoAttributes == false)
                return false;

            return Equals((PhotoAttributes) obj);
        }

        public bool CloseEnoughTo(PhotoAttributes attribs)
        {
            if (DateShutter == null && attribs.DateShutter == null)
                return true;

            if ((DateShutter == null) != (attribs.DateShutter == null))
                return false;

            if (DateShutter.Value.Subtract(attribs.DateShutter.Value).TotalMilliseconds < 1600)
                return true;

            return false;
        }
    }
}