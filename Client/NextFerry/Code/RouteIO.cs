using System;
using System.Windows;
using System.IO;
using System.IO.IsolatedStorage;

namespace NextFerry
{
    /// <summary>
    /// Parse schedules from text, and read/write IsolatedStorage copy.
    /// </summary>
    public static class RouteIO
    {
        // We get the ferrry schedule from a web service, and store it locally.
        // The schedule format is simple text: a sequence of lines of the form:
        //
        //      bainbridge,wd,330,370,...
        //
        // This is the the route name, two chars telling if this is a west/east and weekday/weekend schedule,
        // and then a list of departure times (in minutes past midnight).
        //
        // When we read the schedule file, we update AllRoutes accordingly.

        private readonly static IsolatedStorageFile myStore = IsolatedStorageFile.GetUserStoreForApplication();
        private const string scheduleFile = "CachedFerrySchedules.txt";  // where on disk to store it.

        /// <summary>
        /// Keep a local copy of the new schedule.
        /// </summary>
        public static void writeCache(string newtext)
        {
            Log.write("writing cache");
            using (IsolatedStorageFileStream stream = new IsolatedStorageFileStream(scheduleFile, FileMode.Create, myStore))
            {
                using (StreamWriter writer = new StreamWriter(stream))
                {
                    writer.Write(newtext);
                }
            }
        }

        /// <summary>
        /// Reads cached state and updates AllRoutes accordingly.
        /// </summary>
        public static void readCache()
        {
            if (myStore.FileExists(scheduleFile))
            {
                Log.write("reading cache");
                try
                {
                    using (var isoFileStream = new IsolatedStorageFileStream(scheduleFile, FileMode.Open, myStore))
                    {
                        using (var isoFileReader = new StreamReader(isoFileStream))
                        {
                            deserialize(isoFileReader);
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.write("readCache failed: " + e);
                }
            }
        }


        /// <summary>
        /// Parse a schedule.   We trap exceptions here.
        /// </summary>
        /// <returns>True if we successfully parsed all routes.</returns>
        public static bool deserialize(TextReader s)
        {
            try
            {
                int count = 0;
                while (true)
                {
                    string line = s.ReadLine();
                    if (line == null) break;
                    //Log.write("deserialize: |" + line + "|");
                    // Skip comments and empty lines.
                    if (line.Length < 2) continue;
                    if (line.StartsWith("//")) continue;

                    parseLine(line);
                    count++;
                }
                Log.write("Deserialize successful (" + count + ")");
                return (count == RouteManager.AllRoutes.Count * 2);
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

            if (len < 5) // heuristic: all ferry lines have at least 3 departures per day
                throw new ArgumentException("line too short (" + len + ")");

            string name = data[0];
            string code = data[1];

            if (code.Length != 2)
                throw new ArgumentException("invalid code '" + code + "'");
            bool iswest = (code[0] == 'w');
            bool isweekend = (code[1] == 'e');

            Schedule news = new Schedule(isweekend);

            // unpack the times
            for (int i = 4; i < len; i++)
                news.times.Add(new DepartureTime(int.Parse(data[i])));

            Route r = RouteManager.getRoute(name, iswest ? "wb" : "eb");
            if (r == null)
                throw new ArgumentException("unexpected route name " + name);

            // Update AllRoutes.
            //Log.write("updating " + r.name + "/" + r.direction + "/" + news.isWeekend);
            r.setScheduleMT(news, true);
        }

        /// <summary>
        /// Delete the cache.  Does <strong>not</strong> clear data from AllRoutes.
        /// </summary>
        public static void deleteCache()
        {
            try
            {
                AppSettings.cacheVersion = "";
                myStore.DeleteFile(scheduleFile);
            }
            catch (Exception e)
            {
                Log.write("Unable to clear cache: " + e);
            }
        }

        public static string cacheStatus()
        {
            if (myStore.FileExists(scheduleFile))
                return "Cached as of " + myStore.GetCreationTime(scheduleFile).ToString("G");
            else
                return "Cache not present.";
        }
    }
}
