using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLog;

namespace gbd.XmpMatcher.Lib
{
    public class CollisionsManager
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private struct ImageAndXmp
        {
            public FileInfo Image;
            public FileInfo Xmp;
        }



        public IDictionary<MatchingAttributes, ICollection<FileInfo>> ByAttributes;

        private ICollection<KeyValuePair<MatchingAttributes, ICollection<FileInfo>>> _collisions;
        private ICollection<KeyValuePair<MatchingAttributes, ICollection<FileInfo>>> _oneOnOneMatch;
        private ICollection<KeyValuePair<MatchingAttributes, ICollection<FileInfo>>> _moveFileFailed = new List<KeyValuePair<MatchingAttributes, ICollection<FileInfo>>>();
        private ICollection<KeyValuePair<MatchingAttributes, ICollection<FileInfo>>> _moveFileSucceeded = new List<KeyValuePair<MatchingAttributes, ICollection<FileInfo>>>();


        internal CollisionsManager(IDictionary<MatchingAttributes, ICollection<FileInfo>> byAttributes)
        {
            ByAttributes = byAttributes;
        }

        public void DetectCollisions()
        {
            Logger.Info($"Detecting collisions (files with same metadata)");

            _collisions = ByAttributes.Where(kvp => kvp.Value.Count >= 2).ToArray();
            Logger.Info($"Found {_collisions.Count()} group of colliding files out of {ByAttributes} file groups");

            _oneOnOneMatch = _collisions.Where( kvp => IsOneOnOne(kvp.Value)).ToArray();
            Logger.Info($"Found {_oneOnOneMatch.Count()} collisions with exactly one XMP and one image");

        }

        private bool IsOneOnOne(ICollection<FileInfo> files)
        {
            if (files.Count != 2)
                return false;

            bool foundXmp = false;
            bool foundImage = false;

            foreach (var file in files)
            {
                switch (FileDiscriminator.Process(file))
                {
                    case FileType.Image:
                        foundImage = true;
                        break;

                    case FileType.Xmp:
                        foundXmp = true;
                        break;
                }
            }

            return (foundImage && foundXmp);
        }


        private ImageAndXmp MakeImageAndXmpPair(IEnumerable<FileInfo> files)
        {
            var pair = new ImageAndXmp();

            foreach (var fileInfo in files)
            {
                switch (FileDiscriminator.Process(fileInfo))
                {
                    case FileType.Image:
                        if (pair.Image != null)
                            throw new InvalidOperationException();
                        pair.Image = fileInfo;
                        break;

                    case FileType.Xmp:
                        if (pair.Xmp != null)
                            throw new InvalidOperationException();
                        pair.Xmp = fileInfo;
                        break;

                    case FileType.Unknown:
                    default:
                        throw new InvalidOperationException();
                }
            }

            if (pair.Xmp == null || pair.Image == null)
                throw new InvalidOperationException();

            Logger.Debug($"Identified XMP/image pair: {pair.Xmp.Name} / {pair.Image.Name}");

            return pair;
        }

        public void LinkXmpAndImagePairs()
        {
            Logger.Info($"Start linking XMP and images");

            foreach (var kvp in _oneOnOneMatch)
            {
                try
                {
                    ImageAndXmp pair = MakeImageAndXmpPair(kvp.Value);

                    var oldName = pair.Xmp.Name;
                    var oldPath = pair.Xmp.Directory;
                    var newXmpName = pair.Image.FullName + ".xmp";

//                pair.Xmp.MoveTo(newXmpName);

                    _moveFileSucceeded.Add(kvp);
                    Logger.Info($"Moved {oldName} ({oldPath}) to {newXmpName}");
                }
                catch (Exception e)
                {
                    Logger.Warn(e, $"Failed moving {kvp.Value.FirstOrDefault()?.Name}");
                    _moveFileFailed.Add(kvp);
                }

            }
        }

        public void ReportUnfixedCollisions()
        {
            throw new NotImplementedException();
        }
    }
}