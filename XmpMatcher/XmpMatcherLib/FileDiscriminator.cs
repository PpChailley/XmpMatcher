using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLog;

namespace gbd.XmpMatcher.Lib
{
    public class FileDiscriminator
    {

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private FileInfo _f;
        private FileType _guessed;

        public static readonly string[] IMAGE_EXTENSIONS = {    ".cr2", 
                                                                ".tif", 
                                                                ".dng", 
                                                                ".png", 
                                                                ".gif", 
        };

        public static readonly string[] JPG_EXTENSIONS = {    ".jpg", ".jpeg" };

        private static readonly IEqualityComparer<string> stringCaseIgnoreComparer = new StringIgnoreCaseComparer();


        private FileDiscriminator(FileInfo f)
        {
            _f = f;
        }

        public static FileType Process(FileInfo file)
        {
            var discriminator = new FileDiscriminator(file);
            discriminator.GuessType();
            return discriminator.AssertType();
        }

        private FileType AssertType()
        {
            if (IsOfType(_guessed))
                return _guessed;
            else
                foreach (var type in Enum.GetValues(typeof(FileType)).Cast<FileType>())
                {
                    if (type == _guessed)
                        continue;

                    if (IsOfType(type))
                        return type;
                }

            // throw new InvalidOperationException($"Unable to te  the type of {_f.Name}");
            return FileType.Unknown;
        }

        private bool IsOfType(FileType type)
        {
            bool allTestsPass = true;

            switch (type)
            {
                case FileType.Raw:
                        allTestsPass &= IMAGE_EXTENSIONS.Contains(_f.Extension, stringCaseIgnoreComparer);
                    break;

                case FileType.Jpg:
                    allTestsPass &= JPG_EXTENSIONS.Contains(_f.Extension, stringCaseIgnoreComparer);
                    break;

                case FileType.Xmp:
                        allTestsPass &= _f.Extension.Equals(".xmp", StringComparison.InvariantCultureIgnoreCase);
                    break;

                case FileType.Unknown:
                    break;

                default:
                    allTestsPass = false;
                    break;
            }


            return allTestsPass;
        }

        private void GuessType()
        {
            var size = _f.Length;

            if (size > 1*1024*1024 && size < 40*1024*1024)
                _guessed = FileType.Raw;

            else if (size < 30*1024 && _f.Extension.Equals(".xmp"))
                _guessed = FileType.Xmp;
            else
                _guessed = FileType.Raw;
        }
    }
}