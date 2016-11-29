using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using gbd.Tools.Maths;
using MetadataExtractor;
using NLog;
using Directory = MetadataExtractor.Directory;


namespace gbd.XmpMatcher.Lib
{
    public class XmpMatcher
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();


        private static readonly Regex RegexX = new Regex("exif:FocalPlaneXResolution=.(.*?).$", RegexOptions.Multiline);
        private static readonly Regex RegexY = new Regex("exif:FocalPlaneYResolution=.(.*?).$", RegexOptions.Multiline);
        private static readonly Regex RegexFNumber = new Regex("exif:FNumber=.(.*?).$", RegexOptions.Multiline);
        private static readonly Regex RegexFocal = new Regex("exif:FocalLength=.(.*?).$", RegexOptions.Multiline);
        private static readonly Regex RegexTime = new Regex("exif:ExposureTime=.(.*?).$", RegexOptions.Multiline);


        private readonly ICollection<FileInfo> _unsortedFiles;
        private readonly IDictionary<PhotoAttributes, ICollection<FileInfo>> _byAttributes =  new Dictionary<PhotoAttributes, ICollection<FileInfo>>();

        public XmpMatcher(FileInfo[] inputFiles)
        {
            Logger.Info($"Creating XMP Matcher with {inputFiles.Length} files");
            _unsortedFiles = inputFiles;
            _byAttributes = new Dictionary<PhotoAttributes, ICollection<FileInfo>>(inputFiles.Length);
        }

        public XmpMatcher(IReadOnlyCollection<string> inputFiles)
        {
            Logger.Info($"Creating XMP Matcher with {inputFiles.Count} filenames");
            var files = new List<FileInfo>(inputFiles.Count);
            _byAttributes = new Dictionary<PhotoAttributes, ICollection<FileInfo>>(inputFiles.Count);

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

        public void DiscriminateFileTypes()
        {
            Logger.Info($"Sorting {_unsortedFiles.Count} files into images and XMP");

            int nbProcessedFiles = 0;
            int percentAdvance = _unsortedFiles.Count/100 + 1 ;

            foreach (var file in _unsortedFiles)
            {
                PhotoAttributes attribs = null;

                try
                {
                    switch (FileDiscriminator.Process(file))
                    {
                        case FileType.Xmp:
                            attribs = GetXmpAttributes(file);
                            break;

                        case FileType.Raw:
                            attribs = GetRawAttributes(file);
                            break;

                        case FileType.Jpg:
                            Logger.Debug($"Ignoring JPG file {file.Name}");
                            continue;

                        case FileType.Unknown:
                        default:
                            Logger.Warn($"File '{file.Name}' is not recognized");
                            continue;
                    }

                    if (attribs == null)
                        continue;

                    if (_byAttributes.ContainsKey(attribs))
                        _byAttributes[attribs].Add(file);
                    else
                        _byAttributes[attribs] = new List<FileInfo>() {file};
                    
                }
                catch (FormatException fe)
                {
                    Logger.Warn(fe, $"Some data was not recognized in {file.Name} ({file.DirectoryName}). Skipping");
                }
                catch (InvalidOperationException ioe)
                {
                    Logger.Warn(ioe, $"Invalid Op: Probably EXIF not found for {file.Name}.. Skipping");
                }



                nbProcessedFiles ++;
                Logger.Debug($"Parsed and ordered file '{file.Name}' as '{attribs}");
                if (nbProcessedFiles % percentAdvance == 0)
                {
                    Logger.Info($"Processed {nbProcessedFiles} files ({100*nbProcessedFiles/_unsortedFiles.Count} %)");
                }

            }
        }

        private static PhotoAttributes GetRawAttributes(FileInfo file)
        {
            IReadOnlyList<Directory> dirs = ImageMetadataReader.ReadMetadata(file.FullName);
            Directory subIfd = dirs.SingleOrDefault(d => d.Name.Equals("Exif SubIFD"));

            if (subIfd == null)
                return null;

            var tagFNumber = subIfd.Tags.Single(t => t.Name.Equals("F-Number"));
            var tagFocalLen = subIfd.Tags.Single(t => t.Name.Equals("Focal Length"));
            var tagX = subIfd.Tags.SingleOrDefault(t => t.Name.Equals("Focal Plane X Resolution"));
            var tagY = subIfd.Tags.SingleOrDefault(t => t.Name.Equals("Focal Plane Y Resolution"));
            var tagDateOrig = subIfd.Tags.SingleOrDefault(t => t.Name.Equals("Date/Time Original"));
            var tagTime = subIfd.Tags.SingleOrDefault(t => t.Name.Equals("Exposure Time"));


            var attribs = new PhotoAttributes(
                DateTimeManager.Parse(tagDateOrig?.Description),
                double.Parse(tagFNumber.Description.Replace("f/", "")),
                Fraction.Parse(tagTime.Description.Replace("sec", "")),
                double.Parse(tagFocalLen.Description.Replace("mm", "")),
                Fraction.Parse(tagX?.Description.Replace("inches", "")),
                Fraction.Parse(tagY?.Description.Replace("inches", "")));

 
            Logger.Debug($"Read attribs from {file.Name} : {attribs}");

            return attribs;
        }


        private static PhotoAttributes GetXmpAttributes(FileInfo file)
        {
            var xmp = file.OpenText().ReadToEnd();

            PhotoAttributes attribs;
            try
            {
                var matchFNumber = RegexFNumber.Match(xmp);
                var matchX = RegexX.Match(xmp);
                var matchY = RegexY.Match(xmp);
                var matchFocal = RegexFocal.Match(xmp);
                var matchTime = RegexTime.Match(xmp);

                attribs = new PhotoAttributes(
                    DateTimeManager.FindAndParse(xmp, @"exif:DateTimeOriginal="),
                    Fraction.Parse(matchFNumber.Groups[1].Value),
                    Fraction.Parse(matchTime.Groups[1].Value),
                    Fraction.Parse(matchFocal.Groups[1].Value),
                    Fraction.Parse(matchX.Groups[1].Value),
                    Fraction.Parse(matchY.Groups[1].Value));
                

            }
            catch (Exception)
            {
                throw;
            }


            Logger.Debug($"File {file.Name} has attributes {attribs}");

            return attribs;
        }


  

        public CollisionsManager MakeCollisionsManager()
        {

            return new CollisionsManager(_byAttributes);
        }
    }

    
}
