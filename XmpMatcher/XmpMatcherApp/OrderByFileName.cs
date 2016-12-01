using System;
using System.Collections.Generic;
using System.Globalization;

namespace gbd.XmpMatcher.App
{
    internal class OrderByFileName : IComparer<string>
    {
        private StringComparer _comparer = StringComparer.Create(CultureInfo.InvariantCulture, true);

        public int Compare(string x, string y)
        {
            if (x == null && y == null)
                return 0;
            if (x == null || y == null)
                return -1;

            var subX = x.Substring(x.LastIndexOf("\\") + 2, 7);
            var subY = y.Substring(y.LastIndexOf("\\") + 2, 7);
            return _comparer.Compare(subX, subY);
        }
    }
}