using System;

namespace gbd.Tools.Maths
{
    public static class Double
    {

        /// <summary>
        /// Compares two nullable doubles with a specified allowance
        /// following "null == null" rule
        /// True if both numbers are null OR both are non-null within a range of (allowance * me)
        /// </summary>
        /// <param name="me"></param>
        /// <param name="you"></param>
        /// <param name="allowance"></param>
        /// <returns></returns>
        public static bool EqualsWithError(this double? me, double? you, double allowance = 0.01)
        {
            if (me == null && you == null)
                return true;

            if ((me == null) != (you == null))
                return false;

            var delta = Math.Abs(you.Value - me.Value);
            var allowedDelta = allowance*me;

            return ( delta <= allowedDelta );

        }

    }
}
