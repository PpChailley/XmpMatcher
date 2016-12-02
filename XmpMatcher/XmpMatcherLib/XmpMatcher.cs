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
                try
                {
                    var attribs = new PhotoAttributes(file);

                    if (_byAttributes.ContainsKey(attribs))
                        _byAttributes[attribs].Add(file);
                    else
                        _byAttributes[attribs] = new List<FileInfo>() {file};

                    Logger.Debug($"Parsed and ordered file '{file.Name}' as '{attribs}");

                }
                catch (FormatException fe)
                {
                    Logger.Warn(fe, $"Some data was not recognized in {file.Name} ({file.DirectoryName}). Skipping");
                }
                catch (InvalidOperationException ioe)
                {
                    Logger.Warn(ioe, $"Invalid Op: Probably EXIF not found for {file.Name}.. Skipping");
                }
                catch (NotImplementedException nie)
                {
                    Logger.Warn(nie);
                }

                nbProcessedFiles ++;
                if (nbProcessedFiles % percentAdvance == 0)
                {
                    Logger.Info($"Processed {nbProcessedFiles} files ({100*nbProcessedFiles/_unsortedFiles.Count} %)");
                }

            }

            Logger.Info($"Finished sorting {_unsortedFiles.Count} files into images and XMP");
        }

 
  

        public CollisionsManager MakeCollisionsManager()
        {

            return new CollisionsManager(_byAttributes);
        }
    }

    
}
