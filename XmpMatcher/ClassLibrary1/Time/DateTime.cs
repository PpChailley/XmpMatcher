

using System;

namespace gbd.Tools.Time
{
    public static class DateTimeExtensions
    {

        public enum Field
        {
            Year = 1,
            Month = 2,
            Day = 4,
            Hour = 8,
            Minute = 0x10,
            Second = 0x20,
            Millisec = 0x40,

            SecondAndAbove = 0x3F

        }

        public static bool Equals(this DateTime? me, DateTime? you, Field mask)
        {
            if (me == you)
                return true;

            if ((me == null) != (you == null))
                return false;


            if (mask.HasFlag(Field.Year) && me.Value.Year != you.Value.Year)
                return false;

            if (mask.HasFlag(Field.Month) && me.Value.Month != you.Value.Month)
                return false;

            if (mask.HasFlag(Field.Day) && me.Value.Day != you.Value.Day)
                return false;

            if (mask.HasFlag(Field.Hour) && me.Value.Hour != you.Value.Hour)
                return false;

            if (mask.HasFlag(Field.Minute) && me.Value.Minute != you.Value.Minute)
                return false;

            if (mask.HasFlag(Field.Second) && me.Value.Second != you.Value.Second)
                return false;

            if (mask.HasFlag(Field.Millisec) && me.Value.Millisecond != you.Value.Millisecond)
                return false;


            return true;
        }


    }
}
