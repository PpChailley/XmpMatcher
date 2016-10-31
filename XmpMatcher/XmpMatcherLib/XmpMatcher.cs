using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using MetadataExtractor;
using NLog;
using Directory = MetadataExtractor.Directory;


namespace gbd.XmpMatcher.Lib
{
    public class XmpMatcher
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();


        private ICollection<FileInfo> _unsortedFiles;

        //private IDictionary<FileInfo, MatchingAttributes> _xmpFiles = new Dictionary<FileInfo, MatchingAttributes>();
        //private IDictionary<FileInfo, MatchingAttributes> _imgFiles = new Dictionary<FileInfo, MatchingAttributes>();

        private IDictionary<MatchingAttributes, ICollection<FileInfo>> _byAttributes =  new Dictionary<MatchingAttributes, ICollection<FileInfo>>();

        public XmpMatcher(FileInfo[] inputFiles)
        {
            Logger.Info($"Creating XMP Matcher with {inputFiles.Length} files");
            _unsortedFiles = inputFiles;
        }

        public XmpMatcher(IReadOnlyCollection<string> inputFiles)
        {
            Logger.Info($"Creating XMP Matcher with {inputFiles.Count} filenames");
            var files = new List<FileInfo>(inputFiles.Count);

            foreach (var inputFile in inputFiles)
            {
                var info = new FileInfo(inputFile);
                if (info.Exists == false)
                {
                    Logger.Warn($"Ignoring inexistant file :  {info.FullName}");
                }
                else
                {
                    files.Add(info);
                }

                _unsortedFiles = files;
            }
        }

        public void SortFiles()
        {
            Logger.Info($"Sorting {_unsortedFiles.Count} files into images and XMP");

            foreach (var file in _unsortedFiles)
            {
                MatchingAttributes attribs = null;

                switch (FileDiscriminator.Process(file))
                {
                    case FileType.Xmp:
                        attribs = GetXmpAttributes(file);
                        break;

                    case FileType.Image:
                        attribs = GetImageAttributes(file);
                        break;

                    case FileType.Unknown:
                    default:
                        Logger.Warn($"File '{file.Name}' is not recognized");
                        continue;
                }

                if (_byAttributes.ContainsKey(attribs))
                    _byAttributes[attribs].Add(file);
                else
                    _byAttributes[attribs] = new List<FileInfo>() { file };

            }
        }

        private static MatchingAttributes GetImageAttributes(FileInfo file)
        {
            IReadOnlyList<Directory> dirs = ImageMetadataReader.ReadMetadata(file.FullName);
            Directory subIfd = dirs.Single(d => d.Name.Equals("Exif SubIFD"));

            var dateTag = subIfd.Tags.Single(t => t.Name.Equals("Date/Time Original")).Description;
            var parsedExifOriginalTime = TryParseExifDateTime(dateTag, file);

            var attribs = new MatchingAttributes
            {
                FocalPlaneXResolution = subIfd.Tags.Single(t => t.Name.Equals("Focal Plane X Resolution")).Description,
                FocalPlaneYResolution = subIfd.Tags.Single(t => t.Name.Equals("Focal Plane Y Resolution")).Description,
                DateShutter = parsedExifOriginalTime
            };

            
            Logger.Debug($"Read attribs from {file.Name} : {attribs}");

            return attribs;
        }

        private static DateTime TryParseExifDateTime(string dateTag, FileInfo file)
        {
            DateTime parsedExifOriginalTime;
            try
            {
                parsedExifOriginalTime = DateTime.ParseExact(dateTag, "yyyy:MM:dd HH:mm:ss",
                    CultureInfo.InvariantCulture);
            }
            catch (FormatException fe)
            {
                Logger.Warn($"Problem parsing dateTime '{dateTag}' for file {file.Name} ... Trying fallbacks", fe);
                try
                {
                    var dateFormatReader = new Regex(@"(\d+):(\d+):(\d+)\s+(\d+):(\d+):(\d+)");
                    var regexGroups = dateFormatReader.Match(dateTag).Groups;
                    parsedExifOriginalTime = new DateTime(int.Parse(regexGroups[1].Value),
                        int.Parse(regexGroups[2].Value),
                        int.Parse(regexGroups[3].Value),
                        int.Parse(regexGroups[4].Value),
                        int.Parse(regexGroups[5].Value),
                        int.Parse(regexGroups[6].Value));
                }
                catch (Exception e)
                {
                    Logger.Error(e, $"DateTime decoding failed twice '{dateTag}' for file {file.Name} ");
                    throw;
                }
            }
            return parsedExifOriginalTime;
        }

        private static MatchingAttributes GetXmpAttributes(FileInfo file)
        {
            var xmp = file.OpenText().ReadToEnd();

            var regexDateTime = new Regex("exif:DateTimeOriginal=.(.*?).");
            var regexX = new Regex("exif:FocalPlaneXResolution=.(.*?).");
            var regexY = new Regex("exif:FocalPlaneYResolution=.(.*?).");

            var attribs = new MatchingAttributes()
            {
                DateShutter = DateTime.Parse(regexDateTime.Match(xmp).Groups[1].Value),
                FocalPlaneXResolution = regexX.Match(xmp).Groups[1].Value,
                FocalPlaneYResolution = regexY.Match(xmp).Groups[1].Value,
            };

            Logger.Debug($"File {file.Name} has attributes {attribs}");

            return attribs;
        }
    }
}