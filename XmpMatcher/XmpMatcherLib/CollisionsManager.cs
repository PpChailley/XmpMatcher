using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLog;

namespace gbd.XmpMatcher.Lib
{
    public class CollisionsManager
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();


        internal class Collision
        {
            public MatchingAttributes Attribs;
            public ICollection<FileInfo> Xmp = new List<FileInfo>();
            public ICollection<FileInfo> Images = new List<FileInfo>();

            public class Description
            {
                public int XmpAmount = 0;
                public int ImagesAmount = 0;

                public BalanceLevel Balance
                {
                    get
                    {
                        if (ImagesAmount == 0 && XmpAmount == 0)
                            return BalanceLevel.Empty;
                        else if (ImagesAmount > 0 && XmpAmount == 0)
                            return BalanceLevel.ImagesOnly;
                        else if (ImagesAmount == 0 && XmpAmount > 0)
                            return BalanceLevel.XmpOnly;
                        else if (ImagesAmount == 1 && XmpAmount == 1)
                            return BalanceLevel.Balanced;
                        else if (ImagesAmount == 1 && XmpAmount > 1)
                            return BalanceLevel.SeveralXmpForOneImage;
                        else if (ImagesAmount > 1)
                            return BalanceLevel.SeveralImages;
                        else
                            return BalanceLevel.Unknown;
            }
        }
            }

            public Description Desc => new Description() {XmpAmount = Xmp.Count, ImagesAmount = Images.Count};

            public Collision(KeyValuePair<MatchingAttributes, ICollection<FileInfo>> kvp)
            {
                Attribs = kvp.Key;

                foreach (var fileInfo in kvp.Value)
                {
                    switch (FileDiscriminator.Process(fileInfo))
                    {
                        case FileType.Image:
                            Images.Add(fileInfo);
                            break;

                        case FileType.Xmp:
                            Xmp.Add(fileInfo);
                            break;

                        case FileType.Unknown:
                        default:
                            throw new InvalidOperationException();
                    }
                }
            }

            public enum BalanceLevel
            {
                Empty,
                ImagesOnly,
                XmpOnly,
                Balanced,
                SeveralImages,
                SeveralXmpForOneImage,
                Unknown,
            }
        }



        internal IDictionary<MatchingAttributes, Collision> ByAttributes;
        internal IDictionary<Collision.BalanceLevel, List<Collision>> ByContentsBalance;


        internal CollisionsManager(IDictionary<MatchingAttributes, ICollection<FileInfo>> byAttributes)
        {
            ByAttributes = new Dictionary<MatchingAttributes, Collision>();
            ByContentsBalance = new Dictionary<Collision.BalanceLevel, List<Collision>>();
            foreach (var kvp in byAttributes)
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

            var collisionsToProcess = new List<Collision>();
            if (ByContentsBalance.ContainsKey(Collision.BalanceLevel.Balanced))
                collisionsToProcess.AddRange(ByContentsBalance[Collision.BalanceLevel.Balanced]);

            foreach (var collision in collisionsToProcess)
            {
                try
                {

                    var xmpOldName = collision.Xmp.Single().Name;
                    var xmpOldPath = collision.Xmp.Single().Directory;
                    var imageOldName = collision.Images.Single().Name;
                    var imageOldPath = collision.Images.Single().Directory;

                    collision.Images.Single().MoveTo(destinationPath);
                    collision.Xmp.Single().MoveTo($"{destinationPath}\\{imageOldName}.xmp");

                    Logger.Info($"Moved together {xmpOldName} ({xmpOldPath}) and {imageOldName} ({imageOldPath})");
                }
                catch (Exception e)
                {
                    Logger.Warn(e, $"Failed moving {collision.Xmp.FirstOrDefault()?.Name} or {collision.Images.FirstOrDefault()?.Name}");
                    // _moveFileFailed.Add(kvp);
                }
            }
        }

        public void ReportUnfixedCollisions()
        {
            throw new NotImplementedException();
        }
    }
}