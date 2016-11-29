using System;
using gbd.Tools.Maths;
using gbd.Tools.Time;
using JetBrains.Annotations;

namespace gbd.XmpMatcher.Lib
{
    public class PhotoAttributes: IEquatable<PhotoAttributes>
    {
        
        public readonly DateTime? DateShutter;
        public readonly double? FocalPlaneXResolution;
        public readonly double? FocalPlaneYResolution;
        public readonly double? FNumber;
        public readonly double? ExposureTime;
        public readonly double? FocalLength;

        public PhotoAttributes(DateTime? dateShutter, double? fNumber, double? exposureTime, double? focalLength, double? focalPlaneXResolution, double? focalPlaneYResolution)
        {
            DateShutter = dateShutter;
            FocalPlaneXResolution = focalPlaneXResolution;
            FocalPlaneYResolution = focalPlaneYResolution;
            FNumber = fNumber;
            ExposureTime = exposureTime;
            FocalLength = focalLength;
        }


 

        public override string ToString()
        {
            return $"({DateShutter}, f/{FocalLength:0.000}, {1000*ExposureTime:0.000}ms, [{DateShutter.Value.Millisecond}])";
        }

        public override int GetHashCode()
        {
            var flooredDate = (DateShutter == null) ? 
                    (DateTime?) null: 
                    new DateTime(
                        DateShutter.Value.Year,
                        DateShutter.Value.Month,
                        DateShutter.Value.Day,
                        DateShutter.Value.Hour,
                        DateShutter.Value.Minute,
                        DateShutter.Value.Second  );

            int a = (flooredDate?.GetHashCode() ?? 0);
            int b = (FNumber?.GetHashCode() ?? 0);
            int c =  (ExposureTime?.GetHashCode() ?? 0);

            return a + b + c;
        }



        public bool Equals(PhotoAttributes other)
        {
            if (other == null)
                return false;

            bool match = DateTimeExtensions.Equals(DateShutter, other.DateShutter, DateTimeExtensions.Field.SecondAndAbove)
                         && FocalLength.Equals(other.FocalLength)
                         && ExposureTime.Equals(other.ExposureTime)
                         && FNumber.EqualsWithError(other.FNumber, 0.1);

            return match;
        }


        public override bool Equals(object obj)
        {
            if (obj is PhotoAttributes == false)
                return false;

            return Equals((PhotoAttributes) obj);
        }
    }
}