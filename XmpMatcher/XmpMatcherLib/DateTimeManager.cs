using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using NLog;

namespace gbd.XmpMatcher.Lib
{
    public static class DateTimeManager
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();




        public static DateTime Parse(string dateTimeRepresentation)
        {
            string[] possibleDateFormats = new[]
            {
                "yyyy-MM-dd'T'HH:mm:ss",
                "yyyy-MM-dd'T'HH:mm:ss.FF",
                "yyyy-MM-dd'T'HH:mm:sszzz",
                "yyyy-MM-dd'T'HH:mm:ss.FFzzz",

                "yyyy:MM:dd HH:mm:ss",
                "yyyy:MM:dd HH:mm:ss.FF",
            };

            DateTime result;

            if (DateTime.TryParseExact(     dateTimeRepresentation,
                                            possibleDateFormats,
                                            CultureInfo.InvariantCulture,
                                            DateTimeStyles.AllowWhiteSpaces,
                                            out result))
            {
                return result;
            }


            throw new FormatException();
        }

        public static DateTime FindAndParse(string container, string linePrePattern)
        {
            string pattern = linePrePattern + @".?(\d+.*\d+).?";
            var regex = new Regex(pattern, RegexOptions.Multiline);
            var match = regex.Match(container);

            if (match.Success == false)
            {
                throw new FormatException();
            }

            return Parse(match.Groups[1].Value);

            // var regexDateTime = new Regex("exif:DateTimeOriginal=.((\\d{4}).(\\d+).(\\d+)\\D+(\\d+).(\\d+).(\\d+)(\\.\\d+)).$", RegexOptions.Multiline);
        }


        private static DateTime TryParseExifDateTime(string dateTag, FileInfo file)
        {
            DateTime parsedExifOriginalTime;
            try
            {
                parsedExifOriginalTime = DateTime.ParseExact(dateTag, "yyyy:MM:dd HH:mm:ss",
                    CultureInfo.InvariantCulture);
            }
            catch (FormatException fe)
            {
                Logger.Warn($"Problem parsing dateTime '{dateTag}' for file {file.Name} ... Trying fallbacks", fe);
                try
                {
                    var dateFormatReader = new Regex(@"(\d+):(\d+):(\d+)\s+(\d+):(\d+):(\d+)");
                    var regexGroups = dateFormatReader.Match(dateTag).Groups;
                    parsedExifOriginalTime = new DateTime(int.Parse(regexGroups[1].Value),
                        int.Parse(regexGroups[2].Value),
                        int.Parse(regexGroups[3].Value),
                        int.Parse(regexGroups[4].Value),
                        int.Parse(regexGroups[5].Value),
                        int.Parse(regexGroups[6].Value));
                }
                catch (Exception e)
                {
                    Logger.Error(e, $"DateTime decoding failed twice '{dateTag}' for file {file.Name} ");
                    throw;
                }
            }
            return parsedExifOriginalTime;
        }

        private static DateTime MakeDateTimeFromExifFormat(string xmp)
        {
            string pattern = @"exif:DateTimeOriginal=.(.*?).\\s+$";
            string[] possibleDateFormats = new[]
            {
                "yyyy-MM-dd'T'HH:mm:ss",
                "yyyy-MM-dd'T'HH:mm:ss.FF",
                "yyyy-MM-dd'T'HH:mm:sszzz",
                "yyyy-MM-dd'T'HH:mm:ss.FFzzz",
            };

            DateTime result;

            var regexFirstTry = new Regex(pattern, RegexOptions.Multiline);
            var match = regexFirstTry.Match(xmp);

            if (match.Success && DateTime.TryParseExact(match.Groups[1].Value,
                                                            possibleDateFormats,
                                                            CultureInfo.InvariantCulture,
                                                            DateTimeStyles.AllowWhiteSpaces,
                                                            out result))
            {
                return result;
            }

            else
            {
                Logger.Debug($"Trying manual regex datetime construction");
                try
                {
                    var regexLastChance = new Regex("exif:DateTimeOriginal=.((\\d{4}).(\\d+).(\\d+)\\D+(\\d+).(\\d+).(\\d+)).$", RegexOptions.Multiline);
                    var match2 = regexLastChance.Match(xmp);
                    var toreturn = new DateTime(int.Parse(match2.Groups[2].Value),
                                                    int.Parse(match2.Groups[3].Value),
                                                    int.Parse(match2.Groups[4].Value),
                                                    int.Parse(match2.Groups[5].Value),
                                                    int.Parse(match2.Groups[6].Value),
                                                    int.Parse(match2.Groups[7].Value));

                    return toreturn;
                }
                catch (Exception)
                {
                    Logger.Error($"Cannot parse a date (tried twice) from XMP");
                    throw;
                }
            }
        }


    }
}