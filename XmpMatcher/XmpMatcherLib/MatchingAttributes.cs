using System;

namespace gbd.XmpMatcher.Lib
{
    public class MatchingAttributes
    {
        public MatchingAttributes()
        { }

        public DateTime DateShutter;
        public string FocalPlaneXResolution;
        public string FocalPlaneYResolution;


        public override string ToString()
        {
            return $"({DateShutter}, {FocalPlaneXResolution}, {FocalPlaneYResolution})";
        }
    }
}