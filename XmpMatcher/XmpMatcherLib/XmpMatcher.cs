using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using ExifLibrary;
using NLog;



namespace gbd.XmpMatcher.Lib
{
    public class XmpMatcher
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();


        private ICollection<FileInfo> _unsortedFiles;

        private IDictionary<FileInfo, MatchingAttributes> _xmpFiles = new Dictionary<FileInfo, MatchingAttributes>();
        private IDictionary<FileInfo, MatchingAttributes> _imgFiles = new Dictionary<FileInfo, MatchingAttributes>();

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
                switch (FileDiscriminator.Process(file))
                {
                    case FileType.Xmp:
                        _xmpFiles[file] = GetXmpAttributes(file);
                        break;

                    case FileType.Image:
                        _imgFiles[file] = GetImageAttributes(file);
                        break;

                    case FileType.Unknown:
                    default:
                        Logger.Warn($"File '{file.Name}' is not recognized");
                        break;
                }
            }
        }

        private static MatchingAttributes GetImageAttributes(FileInfo file)
        {
            var exif = ExifFile.Read(file.FullName);

            var attribs = new MatchingAttributes
            {
                DateShutter = DateTime.Parse(exif.Properties[ExifTag.DateTimeOriginal].Value.ToString()),
                FocalPlaneXResolution = exif.Properties[ExifTag.FocalPlaneXResolution].Value.ToString(),
                FocalPlaneYResolution = exif.Properties[ExifTag.FocalPlaneYResolution].Value.ToString(),

            };


            Logger.Debug($"Read attribs from {file.Name} : {attribs}");

            return attribs;
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