using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using gbd.XmpMatcher.Lib;
using NLog;

namespace gbd.XmpMatcher.App
{
    public class Program
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();


        private class OrderByFileName : IComparer<string>
        {
            private StringComparer _comparer = StringComparer.Create(CultureInfo.InvariantCulture, true);

            public int Compare(string x, string y)
            {
                if (x == null && y == null)
                    return 0;
                if (x == null || y == null)
                    return -1;

                var subX = x.Substring(x.LastIndexOf("\\") + 2, 7);
                var subY = y.Substring(y.LastIndexOf("\\") + 2, 7);
                return _comparer.Compare(subX, subY);
            }
        }


        public static void Main(string[] args)
        {
            Logger.Info("Starting Xmp Matcher App");

            var files = new List<string>(10000);
            ConfigurePaths(files);
            files.Sort(new OrderByFileName());
            

            var matcher = new Lib.XmpMatcher(files);
            matcher.DiscriminateFileTypes();

            Logger.Info("All files discriminated");

            var collisionMgr = matcher.MakeCollisionsManager();

            collisionMgr.DetectCollisions();
            collisionMgr.LinkXmpAndImagePairs(@"R:\StoreDisk recovery\RECOVERED critical\Relinked pairs");
            collisionMgr.ReportUnfixedCollisions();


        }

        private static void ConfigurePaths(List<string> files)
        {
            //var baseDir = @"R:\StoreDisk recovery\RECOVERED critical\run 2016-10-26 from dd try 000";
            var extensions = FileDiscriminator.IMAGE_EXTENSIONS;


            //files.AddRange(IncludeFilesIn(@"D:\StoreDiskRecovery (more files on another disk)\try 001 (no mpx-flac)\0000-0150\recup_dir.10", "*.xmp", extensions));


            // /*  REAL CASE SEARCHING
            files.AddRange(IncludeFilesIn(@"R:\StoreDisk recovery\RECOVERED critical", "*.xmp", extensions));
            files.AddRange(IncludeFilesIn(@"D:\StoreDiskRecovery (more files on another disk)", "*.xmp", extensions));
            //*/

            /* Test with already matching files */
            var path = @"D:\Photo\TempStoreWhileStoreDriveInMaintenance";
            //var path = @"D:\Photo\TempStoreWhileStoreDriveInMaintenance\2016\08 - Asco";
            //var path = @"D:\Users\pipo\Dropbox\IsoFiling\Dev & Geek\XmpMatcher\XmpMatcher\SampleData";
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

            foreach (var curMaskBase in masks)
            {
                var mask = curMaskBase.StartsWith("*") ? curMaskBase : "*" + curMaskBase;

                Logger.Debug($"searching for {mask} in directory {path}");
                var curFiles = Directory.GetFiles(path, mask, SearchOption.AllDirectories);
                files.AddRange(curFiles);
                Logger.Info($"Added {curFiles.Length} files '{mask}' in {path}");
            }

            
            return files;
        }
    }
}
