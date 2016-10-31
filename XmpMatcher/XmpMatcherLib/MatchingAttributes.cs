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

        public override int GetHashCode()
        {
            return DateShutter.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is MatchingAttributes == false)
                return false;

            return DateShutter.Equals(((MatchingAttributes) obj).DateShutter);
        }
    }
}