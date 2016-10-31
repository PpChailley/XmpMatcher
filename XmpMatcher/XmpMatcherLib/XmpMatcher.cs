using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NLog;

namespace gbd.XmpMatcher.Lib
{
    public class XmpMatcher
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();


        private ICollection<FileInfo> _unsortedFiles;
        private ICollection<KeyValuePair<FileInfo, MatchingAttributes> _xmpFiles = new List<KeyValuePair<FileInfo, MatchingAttributes>(10000);
        private ICollection<KeyValuePair<FileInfo, MatchingAttributes> _imgFiles = new List<KeyValuePair<FileInfo, MatchingAttributes>(10000);


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
                        _xmpFiles.Add(new KeyValuePair<FileInfo, MatchingAttributes>(file, (MatchingAttributes) null));
                        break;

                    case FileType.Image:
                        _imgFiles.Add(new KeyValuePair<FileInfo, MatchingAttributes>(file, (MatchingAttributes) null));
                        break;

                    case FileType.Unknown:
                    default:
                        Logger.Warn($"File '{file.Name}' is not recognized");
                        break;
                }
            }
        }

        public void ExtractInfoFromFiles()
        {
            foreach (var filePair in _imgFiles)
            {
                filePair.Value = GetImageAttribute(filePair.Key);
            }

            foreach (var filePair in _xmpFiles)
            {
                filePair.Value = GetXmpAttribute(filePair.Key);
            }
        }
    }

    public class MatchingAttributes
    {
        public DateTime DateShutter;
        public string FocalPlaneXResolution;
        public string FocalPlaneYResolution;

    }
}