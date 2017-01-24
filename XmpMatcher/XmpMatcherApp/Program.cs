using System;
using System.Collections.Generic;
using System.IO;
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
            files.Sort(new OrderByFileName());
            

            var matcher = new Lib.XmpMatcher(files);
            matcher.DiscriminateFileTypes();
            var collisionMgr = matcher.MakeCollisionsManager();

            collisionMgr.DetectCollisions();

            throw new Exception();

            collisionMgr.LinkXmpAndImagePairs(@"R:\StoreDisk recovery\RECOVERED critical\Relinked pairs");
            collisionMgr.GuessNextMatchings();


            collisionMgr.ReportUnfixedCollisions();


        }

        private static void ConfigurePaths(List<string> files)
        {
            var extensions = FileDiscriminator.IMAGE_EXTENSIONS;

            //ConfigurePathsLeftOversFromPreviousRun(files, extensions);
            //ConfigurePathsRealCase(files, extensions);
            //ConfigurePathsAlreadyMatching(files, extensions);
            ConfigurePathsTestData(files, extensions);
            //ConfigurePathsSmallSubset(files);
        }

        private static void ConfigurePathsTestData(List<string> files, string[] extensions)
        {
            files.AddRange(IncludeFilesIn(@"D:\Users\pipo\Dropbox\IsoFiling\Dev & Geek\XmpMatcher\XmpMatcher\XmpMatcher\TestData", "*.xmp", extensions));
        }

        private static void ConfigurePathsLeftOversFromPreviousRun(List<string> files, string[] extensions)
        {
            files.AddRange(IncludeFilesIn(@"D:\Photo\TempStoreWhileStoreDriveInMaintenance\2016\10 - Ariege", "*.xmp", extensions));
        }

        private static void ConfigurePathsSmallSubset(List<string> files)
        {
            var baseDir = @"R:\StoreDisk recovery\RECOVERED critical\run 2016-10-26 from dd try 000";
            files.AddRange(IncludeFilesIn(baseDir + "\\recup_dir.144", "*.xmp"));
            files.AddRange(IncludeFilesIn(baseDir + "\\recup_dir.77", "*.xmp"));
            files.AddRange(IncludeFilesIn(baseDir + "\\recup_dir.77", "*.jpg"));
            files.AddRange(IncludeFilesIn(baseDir + "\\recup_dir.77", "*.cr2"));
        }

        private static void ConfigurePathsAlreadyMatching(List<string> files, string[] extensions)
        {
            //var path = @"D:\Photo\TempStoreWhileStoreDriveInMaintenance";
            var path = @"D:\Photo\TempStoreWhileStoreDriveInMaintenance\2016\08 - Asco";

            files.AddRange(IncludeFilesIn(path, "*.xmp", extensions));
        }

        private static void ConfigurePathsRealCase(List<string> files, string[] extensions)
        {
            files.AddRange(IncludeFilesIn(@"R:\StoreDisk recovery\RECOVERED critical\Files", "*.xmp", extensions));
            files.AddRange(IncludeFilesIn(@"D:\StoreDiskRecovery (more files on another disk)", "*.xmp", extensions));
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
