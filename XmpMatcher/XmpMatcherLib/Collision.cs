using System;
using System.Collections.Generic;
using System.IO;

namespace gbd.XmpMatcher.Lib
{
    internal class Collision
    {
        public PhotoAttributes Attribs;
        public List<FileInfo> Xmp = new List<FileInfo>();
        public List<FileInfo> Images = new List<FileInfo>();

        internal List<FileInfo> FilesInternal;
        public ICollection<FileInfo> Files
        {
            get
            {
                if (FilesInternal == null)
                {
                    FilesInternal = new List<FileInfo>(Xmp.Count + Images.Count);
                    FilesInternal.AddRange(Xmp);
                    FilesInternal.AddRange(Images);
                }

                return FilesInternal;
            }
        }


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

        public Description Desc => new Description() { XmpAmount = Xmp.Count, ImagesAmount = Images.Count };

        public Collision(KeyValuePair<PhotoAttributes, ICollection<FileInfo>> kvp)
        {
            Attribs = kvp.Key;

            foreach (var fileInfo in kvp.Value)
            {
                switch (FileDiscriminator.Process(fileInfo))
                {
                    case FileType.Raw:
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

        public Collision(Collision c)
        {
            Xmp = new List<FileInfo>(c.Xmp);
            Images = new List<FileInfo>(c.Images);
            Attribs = new PhotoAttributes(c.Attribs);
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
}