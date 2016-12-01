using NLog;

namespace gbd.XmpMatcher.Lib
{
    internal static class CollisionExtension
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public static bool TryApproxMerge(this Collision me, Collision candidate)
        {
            if (me == null || candidate == null)
                return false;

            if (candidate.Attribs.CloseEnoughTo(me.Attribs) == false)
                return false;


            Logger.Debug($"Approximation found: merging {candidate} into {me}");
            foreach (var file in candidate.Files)
            {
                Logger.Debug($"  - merged file {file.Name}    \t {candidate.Attribs}");
            }


            me.FilesInternal = null;

            me.Xmp.AddRange(candidate.Xmp);
            me.Images.AddRange(candidate.Images);
            return true;
        }


        public static void DescribeToLogger(this Collision me, LogLevel level)
        {
            Logger.Log(level, $"Collision {me.Desc.Balance}: {me.Files.Count} files @ {me.Attribs}");
            foreach (var file in me.Files)
            {
                Logger.Log(level,$"  * {file.Name}  \t- ({file.DirectoryName}\\{file.Name}");
            }
        }

    }
}