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


        private readonly ICollection<FileInfo> _unsortedFiles;

        private readonly IDictionary<MatchingAttributes, ICollection<FileInfo>> _byAttributes =  new Dictionary<MatchingAttributes, ICollection<FileInfo>>();

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

            int nbProcessedFiles = 0;
            int percentAdvance = _unsortedFiles.Count/100 + 1 ;

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


                nbProcessedFiles ++;
                Logger.Debug($"Parsed and ordered file '{file.Name}' as '{attribs}");
                if (nbProcessedFiles % percentAdvance == 0)
                {
                    Logger.Info($"Processed {nbProcessedFiles} files ({100*nbProcessedFiles/_unsortedFiles.Count} %)");
                }

            }
        }

        private static MatchingAttributes GetImageAttributes(FileInfo file)
        {
            IReadOnlyList<Directory> dirs = ImageMetadataReader.ReadMetadata(file.FullName);
            Directory subIfd = dirs.Single(d => d.Name.Equals("Exif SubIFD"));

            var attribs = new MatchingAttributes
            {
                FocalPlaneXResolution = subIfd.Tags.SingleOrDefault(t => t.Name.Equals("Focal Plane X Resolution"))?.Description,
                FocalPlaneYResolution = subIfd.Tags.SingleOrDefault(t => t.Name.Equals("Focal Plane Y Resolution"))?.Description,
                DateShutter = DateTimeManager.Parse(subIfd.Tags.SingleOrDefault(t => t.Name.Equals("Date/Time Original"))?.Description)
            };

            
            Logger.Debug($"Read attribs from {file.Name} : {attribs}");

            return attribs;
        }


        private static MatchingAttributes GetXmpAttributes(FileInfo file)
        {
            var xmp = file.OpenText().ReadToEnd();

            
            var regexX = new Regex("exif:FocalPlaneXResolution=.(.*?).$", RegexOptions.Multiline);
            var regexY = new Regex("exif:FocalPlaneYResolution=.(.*?).$", RegexOptions.Multiline);

            var attribs = new MatchingAttributes()
            {
                DateShutter = DateTimeManager.FindAndParse(xmp, @"exif:DateTimeOriginal="),
                FocalPlaneXResolution = regexX.Match(xmp).Groups[1].Value,
                FocalPlaneYResolution = regexY.Match(xmp).Groups[1].Value,
            };

            Logger.Debug($"File {file.Name} has attributes {attribs}");

            return attribs;
        }


        public void ProcessCollisions()
        {
            var coincidences = _byAttributes.Where(kvp => kvp.Value.Count >= 2);
            var oneOnOneMatch = coincidences.Where(kvp => IsOneOnOne(kvp.Value));

            Logger.Info($"Found {coincidences.Count()} coincidences, of which {oneOnOneMatch.Count()} 1-on-1 match");


        }

        private bool IsOneOnOne(ICollection<FileInfo> files)
        {
            if (files.Count != 2)
                return false;

            bool foundXmp = false;
            bool foundImage = false;

            foreach (var file in files)
            {
                switch (FileDiscriminator.Process(file))
                {
                    case FileType.Image:
                        foundImage = true;
                        break;

                    case FileType.Xmp:
                        foundXmp = true;
                        break;
                }
            }

            return (foundImage && foundXmp);
        }
    }
}