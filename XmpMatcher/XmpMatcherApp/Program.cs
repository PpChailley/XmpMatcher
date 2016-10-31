using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gbd.XmpMatcher.App
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var files = new List<string>(10000);

            files.AddRange(Directory.GetFiles(@"D:\StoreDiskRecovery(more files on another disk)", "*.*", SearchOption.AllDirectories));
            files.AddRange(Directory.GetFiles(@"R:\StoreDisk recovery\RECOVERED critical\run 2016-10-26 from dd try 000", "*.*", SearchOption.AllDirectories));


            var matcher = new Lib.XmpMatcher(files);
        }
    }
}
