using System.IO;
using NLog;

namespace gbd.XmpMatcher.Lib
{
    public class FileDiscriminator
    {

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public static FileType Process(FileInfo file)
        {
            var size = file.Length;
            FileType guessedType;

            if ( size > 1*1024*1024 && size < 40*1024*1024)
                guessedType = FileType.Image;
            else if (size < 30*1024 && file.Extension.Equals("xmp"))
                guessedType = FileType.Xmp;
            else
                guessedType = FileType.Image;
            
        }
    }
}