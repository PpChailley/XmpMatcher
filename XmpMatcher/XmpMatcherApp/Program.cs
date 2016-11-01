using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using gbd.XmpMatcher.Lib;
using NLog;

namespace gbd.XmpMatcher.App
{
    public class Program
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public static void Main(string[] args)
        {
            Logger.Info("Starting Xmp Matcher App");

            var files = new List<string>(10000);
            ConfigurePaths(files);

            var matcher = new Lib.XmpMatcher(files);
            matcher.SortFiles();

            Logger.Info("All files sorted");

            var collisionMgr = matcher.MakeCollisionsManager();

            collisionMgr.DetectCollisions();
            collisionMgr.LinkXmpAndImagePairs(@"R:\StoreDisk recovery\RECOVERED critical\Relinked pairs");
            collisionMgr.ReportUnfixedCollisions();


        }

        private static void ConfigurePaths(List<string> files)
        {
            var baseDir = @"R:\StoreDisk recovery\RECOVERED critical\run 2016-10-26 from dd try 000";
            var extensions = FileDiscriminator.IMAGE_EXTENSIONS;


            //files.AddRange(IncludeFilesIn(@"D:\StoreDiskRecovery (more files on another disk)\try 001 (no mpx-flac)\0000-0150\recup_dir.10", "*.xmp", extensions));


            /*  REAL CASE SEARCHING
            files.AddRange(IncludeFilesIn(@"R:\StoreDisk recovery\RECOVERED critical", extensions));
            files.AddRange(IncludeFilesIn(@"R:\StoreDisk recovery\RECOVERED critical", new string[]{"*.xmp"} ));

            files.AddRange(IncludeFilesIn(@"D:\StoreDiskRecovery (more files on another disk)", extensions));
            files.AddRange(IncludeFilesIn(@"D:\StoreDiskRecovery (more files on another disk)", new string[] { "*.xmp" }));
            */

            /* Test with already matching files */
            var path = @"D:\Photo\TempStoreWhileStoreDriveInMaintenance";
            files.AddRange(IncludeFilesIn(path, "*.xmp", extensions));


            /*files.AddRange(IncludeFilesIn(baseDir + "\\recup_dir.144", "*.xmp"));
           files.AddRange(IncludeFilesIn(baseDir + "\\recup_dir.77", "*.xmp"));
           files.AddRange(IncludeFilesIn(baseDir + "\\recup_dir.77", "*.jpg"));
           files.AddRange(IncludeFilesIn(baseDir + "\\recup_dir.77", "*.cr2"));
           */
        }

        private static ICollection<string> IncludeFilesIn(string path, string mask, string [] otherMasks = null)
        {
            var allMasks = new List<string> {mask};

            if (otherMasks != null && otherMasks.Length > 0)
                allMasks.AddRange(otherMasks);

            return IncludeFilesIn(path, allMasks.ToArray());
        }

        private static ICollection<string> IncludeFilesIn(string path, string[] masks)
        {
            var files = new List<string>(1000);

            foreach (var mask in masks)
            {

                Logger.Debug($"searching for {mask} in directory {path}");
                var curFiles = Directory.GetFiles(path, mask, SearchOption.AllDirectories);
                files.AddRange(curFiles);
                Logger.Info($"Added {curFiles.Length} files '{mask}' in {path}");
            }

            
            return files;
        }
    }
}
