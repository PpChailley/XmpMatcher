using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using NLog;

namespace gbd.XmpMatcher.Lib
{
    public class CollisionsManager
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();



        internal IDictionary<PhotoAttributes, Collision> ByAttributes;
        internal IDictionary<Collision.BalanceLevel, List<Collision>> ByContentsBalance;
        internal List<Collision> BalancedApproximations;


        internal CollisionsManager(IDictionary<PhotoAttributes, ICollection<FileInfo>> inputCollection)
        {
            ByAttributes = new Dictionary<PhotoAttributes, Collision>();
            ByContentsBalance = new Dictionary<Collision.BalanceLevel, List<Collision>>();
            foreach (var kvp in inputCollection)
            {
                ByAttributes[kvp.Key] = new Collision(kvp);
            }
        }

        public void DetectCollisions()
        {
            Logger.Info($"Detecting collisions (files with same metadata)");


            // see http://stackoverflow.com/a/40365237/1121983
            ByContentsBalance  = ByAttributes.Values
                            .GroupBy(coll => coll.Desc.Balance)
                            .ToDictionary( group => group.Key, group => group.ToList());

            foreach (var contentBalance in ByContentsBalance.Keys)
            {
                Logger.Info($"Found {ByContentsBalance[contentBalance].Count} groups of files {contentBalance}");
            }

        }

        public void LinkXmpAndImagePairs(string destinationPath)
        {
            Logger.Info($"Start linking XMP and images");

            var balancedCollisionsToProcess = new List<Collision>();
            if (ByContentsBalance.ContainsKey(Collision.BalanceLevel.Balanced))
                balancedCollisionsToProcess.AddRange(ByContentsBalance[Collision.BalanceLevel.Balanced]);

            foreach (var collision in balancedCollisionsToProcess)
            {
                try
                {
                    MoveFilesTogether(collision.Xmp.Single(), collision.Images.Single(), destinationPath);
                }
                catch (Exception e)
                {
                    Logger.Warn(e, $"Failed moving {collision.Xmp.FirstOrDefault()?.Name} or {collision.Images.FirstOrDefault()?.Name}");
                    // _moveFileFailed.Add(kvp);
                }
            }
        }

        private static void MoveFilesTogether(FileInfo xmp, FileInfo image, string destinationPath)
        {
            var xmpOldName = xmp.Name;
            var xmpOldPath = xmp.Directory;
            var imageOldName = image.Name;
            var imageOldPath = image.Directory;

            var xmpNameNoExt = Path.GetFileNameWithoutExtension(xmpOldName);
            var imageNameNoExt = Path.GetFileNameWithoutExtension(imageOldName);

            if (xmpOldPath.Equals(imageOldPath) && xmpNameNoExt.Equals(imageNameNoExt))
            {
                Logger.Info($"Matching files {imageOldName} already together. Skipping.");
                return;
            }

            string newName = $"{imageNameNoExt}";

            Logger.Debug($"Should move together {xmpOldName} and {imageOldName} ({xmpOldPath} AND {imageOldPath})");
            image.MoveTo($"{destinationPath}\\{newName}{image.Extension}");
            xmp.MoveTo($"{destinationPath}\\{newName}.xmp");

            Logger.Info($"Moved together {xmpOldName} ({xmpOldPath}) and {imageOldName} ({imageOldPath})");

        }

        public void ReportUnfixedCollisions()
        {
           // throw new NotImplementedException();

            int i = 0;
        }

        public void GuessNextMatchings()
        {
            var singles = ByAttributes.Where(att => att.Value.Desc.Balance == Collision.BalanceLevel.ImagesOnly ||
                                                    att.Value.Desc.Balance == Collision.BalanceLevel.XmpOnly)
                .OrderBy(kvp => kvp.Key.DateShutter).ToList();

            Collision currentApproxTry = null;
            BalancedApproximations =  new List<Collision>(singles.Count);

            foreach (var kvp in singles)
            {
                try
                {
                    if (currentApproxTry.TryApproxMerge(kvp.Value))
                        continue;
                     
                    if (currentApproxTry != null && currentApproxTry.Files.Count > 1)
                    {
                        Logger.Debug($"Approximate collision built (see below)");
                        currentApproxTry.DescribeToLogger(LogLevel.Debug);
                        BalancedApproximations.Add(currentApproxTry);
                        Logger.Debug("");
                    }

                        currentApproxTry = new Collision(kvp.Value);
                }
                catch (NullReferenceException nre)
                {
                    Logger.Warn(nre, $"Null pointer when processing collision {kvp.Key}");
                    kvp.Value.DescribeToLogger(LogLevel.Warn);
                    throw;
                }
            } 
        }
    }
}