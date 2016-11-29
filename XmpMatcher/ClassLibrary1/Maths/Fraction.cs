using System;

namespace gbd.Tools.Maths
{
    public static class Fraction
    {
        public static double? Parse(string fraction)
        {
            if (fraction == null)
                return null;

            string[] parts = fraction.Split('/');

            if (parts.Length == 1)
                return double.Parse(parts[0]);

            else if (parts.Length == 2)
            {
                var p = double.Parse(parts[0]);
                var q = double.Parse(parts[1]);
                return p/q;
            }

            else // if (parts.Length > 2)
                throw new FormatException();
        }
    }
}