using System.Collections.Generic;
using System.IO;
using NLog;

namespace gbd.XmpMatcher.Lib
{
    public class XmpMatcher
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();


        private ICollection<FileInfo> _unsortedFiles;
        private ICollection<FileInfo> _xmpFiles;
        private ICollection<FileInfo> _imgFiles;



        public XmpMatcher(FileInfo[] inputFiles)
        {
            Logger.Info($"Creating XMP Matcher with {inputFiles.Length} files");
            _unsortedFiles = inputFiles;
        }

        public void SortFiles()
        {
            Logger.Info($"Sorting {_unsortedFiles.Count} files into images and XMP");

            foreach (var file in _unsortedFiles)
            {
                switch (FileDiscriminator.Process(file))
                {
                    case FileType.Xmp:
                        _xmpFiles.Add(file);
                        break;

                    case FileType.Image:
                        _imgFiles.Add(file);
                        break;

                    default:
                        Logger.Warn($"File '{file.Name}' is not recognized");
                        break;
                }
            }
        }



    }
}
