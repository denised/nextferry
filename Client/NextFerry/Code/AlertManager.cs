using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Threading;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace NextFerry
{
    public class Alert
    {
        public string content { get; set; }
        public bool read { get; set; }
        public string id { get; set; }

        public bool isNew { get { return !read; } }
        public string posted { get { return "posted " + id.Substring(0, 5); } } // yeah, I know.
    }

    /// <summary>
    /// Manage the in-memory collection of alerts, and the caching of alerts in IsolatedStorage.
    /// <para>
    /// An alert is a message issued by WSF commenting on the schedule or other issues for 
    /// specific route(s) on a specific day.  An alert body is free-form text, typically 2-8 
    /// sentences long.  There is no structure to the text.   Often alerts refer to a specific 
    /// departure or set of departures, but due to the nature of the text, we cannot determine 
    /// that, so we simply allow alerts to have a full day lifetime.
    /// </para>
    /// <para>
    /// Alerts are displayed by the AlertPage.  The only interesting things we do with them
    /// is keep track of which one(s) users have read.
    /// </para>
    /// <para>
    /// As for schedules, we persist Alerts in isolated storage primarily as a form of caching,
    /// in case there are issues with connecting to the server when the application starts.
    /// </summary>
    public static class AlertManager
    {
        public static Dictionary<Route, List<Alert>> RouteAlerts = new Dictionary<Route, List<Alert>>();
        private static Dictionary<string, Alert> AllAlerts = new Dictionary<string, Alert>();
        private static Object lockable = new Object();
        private static DateTime lastReceived = DateTime.MinValue;

        public static event EventHandler newAlerts;

        #region caching or receiving
        private readonly static IsolatedStorageFile myStore = IsolatedStorageFile.GetUserStoreForApplication();
        private const string alertsFile = "CachedAlerts.txt";  // where on disk to store it.

        public static void recoverCache()
        {
            Util.Asynch(() =>
                {
                    // Wait awhile to see if init has returned new data anyway
                    Thread.Sleep(1000 * 60); // one minute
                    if (lastReceived == DateTime.MinValue &&   // no new data
                         myStore.FileExists(alertsFile) &&     // we have a cache
                         myStore.GetCreationTime(alertsFile).Date == DateTime.Today)  // and it is fresh
                    {
                        bool newones = false;
                        lock (lockable)
                        {
                            string alertsbody = Util.readText(alertsFile);
                            newones = parseAlerts(alertsbody);
                        }
                        if (newones && newAlerts != null)
                            newAlerts(null, null);
                    }
                }
            );
        }

        // called at shutdown; we keep track of the alerts that have been read.
        public static void save()
        {
            if (AllAlerts.Count > 0)
            {
                List<string> seen = new List<string>();
                foreach (Alert a in AllAlerts.Values)
                {
                    if (a.read)
                        seen.Add(a.id);
                }
                AppSettings.alertsSeen = seen;
            }
        }

        // Must be called from background threads.
        public static void receiveAlerts(string alertsbody, DateTime receivedStamp)
        {
            // it is possible for multiple threads to try to do this at the same time,
            // so acquire a lock.
            bool newones = false;
            lock (lockable)
            {
                if (receivedStamp > lastReceived)
                {
                    lastReceived = receivedStamp;
                    Util.writeText(alertsFile, alertsbody);  // cache it
                    newones = parseAlerts(alertsbody); // and parse it
                }
            }
            if (newones && newAlerts != null)
            {
                newAlerts(null, null);
            }
        }

        #endregion

        #region parsing and allocating

        // time for a regular expression!
        // example format in the comment below.
        private static string re = @"\G__ ([\d.:]+) (\d+)\n" + // beginning line: "__" <key> <routecodes>"\n""
                                   @"(.+?\n)(?=__)";           // content: everything up to the next "__"

        private static bool parseAlerts(string alertsbody)
        {
            int alertCount;
            bool newones = false;

            if (alertsbody.Length < 10)
            {
                Log.write("Received empty or truncated alerts: |" + alertsbody + "|");
                return newones;
            }

            // get count of how many we expect
            alertCount = Regex.Matches(alertsbody, "\n__").Count;
            
            // now do the real match
            MatchCollection matches = Regex.Matches(alertsbody,re,RegexOptions.Singleline);

            if (matches.Count != alertCount)
            {
                Log.write("Error parsing alerts: count does not match: " + matches.Count + "/" + alertCount);
                // go ahead and try anyway...
            }

            foreach (Match m in matches)
            {
                // skip if we've already got this one
                if (AllAlerts.ContainsKey(m.Groups[1].Value))
                    continue;

                Alert a = new Alert();
                a.id = m.Groups[1].Value;
                a.content = m.Groups[3].Value;
                a.read = AppSettings.alertsSeen.Contains(a.id);
                int rCodes = Int32.Parse(m.Groups[2].Value);
                bool foundone = false;

                AllAlerts[a.id] = a;
                foreach( Route r in RouteManager.bitRoutes( rCodes ))
                {
                    foundone = true;
                    RouteAlerts[r].Add(a);
                }

                if (!foundone)
                {
                    Log.write("Received alert that matched no routes! " + rCodes);
                }
                else
                {
                    newones = true;
                }
            }
            return newones;
        }
        #endregion
    }
}
