using System;
using System.Windows;
using System.IO;
using System.IO.IsolatedStorage;


namespace NextFerry
{
    /// <summary>
    /// Parse schedules from text, and read/write IsolatedStorage copy.
    /// </summary>
    public static class ScheduleIO
    {
        // We get the ferrry schedule from a web service, and store it locally.
        // The schedule format is simple text: a sequence of lines of the form:
        //
        //      bainbridge,wd,330,370,...
        //
        // This is (one of the) route names, two chars telling if this is a west/east and weekday/weekend schedule,
        // and then a list of departure times (in minutes past midnight).
        //
        // When we read the schedule file, we update AllRoutes accordingly.


        private const string scheduleFile = "CachedFerrySchedules.txt";  // where on disk to store it.

        /// <summary>
        /// Keep a local copy of the new schedule.
        /// </summary>
        public static void writeCache(string newtext)
        {
            Log.write("writing cache");
            Util.writeText(scheduleFile, newtext);
        }

        /// <summary>
        /// Reads cached state and updates AllRoutes accordingly.
        /// </summary>
        public static bool readCache()
        {
            Log.write("reading cache");
            String cache = Util.readText(scheduleFile);
            return (cache != null) && deserialize(cache);
        }


        /// <summary>
        /// Parse a schedule, putting the values into the appropriate field in Routes.
        /// </summary>
        /// <returns>True if we successfully parsed all routes.</returns>
        public static bool deserialize(string s)
        {
            try
            {
                StringReader sr = new StringReader(s);
                int count = 0;
                while (true)
                {
                    string line = sr.ReadLine();
                    if (line == null) break;
                    //Log.write("deserialize: |" + line + "|");
                    // Skip comments and empty lines.
                    if (line.Length < 2) continue;
                    if (line.StartsWith("//")) continue;

                    parseLine(line);
                    count++;
                }
                Log.write("Deserialize successful (" + count + ")");
                return (count == RouteManager.AllRoutes.Count * 4);  // four departurelists per route
            }
            catch (Exception e)
            {
                Log.write("Unexpected exception in Route deserialize " + e);
                return false;
            }
        }


        private static void parseLine(string line)
        {
            // name, code, departure times...
            string[] data = line.Split(',');
            int len = data.Length;

            // We used to have a bunch of error checking code in here, but I've removed it,
            // as (a) it seems that the only error we are likely to see is truncated data
            // and (b) we don't do anything clever to recover anyway.
            // Most forms of error should cause an exception to be thrown (NullValue or Parse
            // exception), which will be caught above.

            string name = data[0];
            string code = data[1];

            Boolean isWest = (code[0] == 'w');
            Boolean isSpecial = (code[1] == 's');
            Boolean isWeekend = (code[1] == 'e');

            int[] times = new int[len - 2];
            for (int i = 2; i < len; i++)
                times[i - 2] = int.Parse(data[i]);

            Route r = RouteManager.lookup(name);
            r.setTimeList(isSpecial, isWeekend, isWest, times);
        }

        /// <summary>
        /// Delete the cache.  Does <strong>not</strong> clear data from AllRoutes.
        /// </summary>
        public static void deleteCache()
        {
            try
            {
                AppSettings.cacheVersion = "";
                IsolatedStorageFile.GetUserStoreForApplication().DeleteFile(scheduleFile);
            }
            catch (Exception e)
            {
                Log.write("Unable to clear cache: " + e);
            }
        }

        public static string cacheStatus()
        {
            IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication();
            if (store.FileExists(scheduleFile))
                return "Cached as of " + store.GetCreationTime(scheduleFile).ToString("G");
            else
                return "Cache not present.";
        }
    }
}
