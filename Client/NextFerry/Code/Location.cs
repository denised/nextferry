using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Device.Location;
using System.Windows;
using System.ComponentModel;
using System.Windows.Threading;


namespace NextFerry
{
    /// <summary>
    /// Notifies that new travel times have been received.
    /// </summary>
    public class TravelTimeEventArgs : EventArgs
    {
        /// <summary>
        /// A dictionary mapping terminal-id to travel time (in minutes).
        /// </summary>
        public Dictionary<int, int> traveltimes { get; set; }

        public TravelTimeEventArgs(Dictionary<int, int> d)
        {
            traveltimes = d;
        }
    }

    /// <summary>
    /// Handles logic for retrieving locations as well as turning locations into travel time information.
    /// </summary>
    public static class LocationMonitor
    {
        private static GeoCoordinateWatcher watcher = null;
        private static TimeSpan delayThreshold = new TimeSpan(0, 0, 3);
        private static BackgroundWorker loop = null;

        public delegate void TravelTimeEventHandler(TravelTimeEventArgs fe);
        public static event TravelTimeEventHandler NewTravelTimes;

        #region loop thread
        /// <summary>
        /// Start the travel time monitor background process.
        /// Has no impact if the monitor is already running.
        /// </summary>
        public static void start()
        {
            if (loop == null)
            {
                initloop();
                loop.RunWorkerAsync();
            }
        }

        /// <summary>
        /// Stop the background monitor process, and the GPS unit, if it is running.
        /// </summary>
        public static void stop()
        {
            if (watcher != null)
            {
                watcher.Stop();
            }
            if (loop != null)
            {
                loop.CancelAsync();
                loop = null;
            }
        }


        private static void initloop()
        {
            loop = new BackgroundWorker();
            loop.WorkerSupportsCancellation = true;
            Log.write("created new loop " + loop.GetHashCode());

            loop.DoWork += (o, a) =>
            {
                BackgroundWorker me = (BackgroundWorker)o;
                int counter = 0;
                while (!me.CancellationPending)
                {
                    counter++;
                    if (counter % 4 == 1)  // 80 seconds
                    {
                        Log.write("tick: " + me.GetHashCode());
                        if (AppSettings.useLocation)
                        {
                            checkTravelTimes();
                        }
                    }
                    Thread.Sleep(1000 * 20);   // 20 seconds
                }
                Log.write("loop " + me.GetHashCode() + " received cancellation");
            };
        }

        #endregion

        #region GPS interaction
        /// <summary>
        /// Return the current location, or null if unable to get it.
        /// This must not be called directly on the UI thread, as it blocks.
        /// </summary>
        public static GeoCoordinate getLocation()
        {
            if (!AppSettings.useLocation)
                return null;

            if (watcher == null)
            {
                watcher = new GeoCoordinateWatcher(GeoPositionAccuracy.High);
                watcher.MovementThreshold = 25; // in meters.  this is finer-grained than we actually care about
                                                // because we want to be sure the GPS is trying, if accuracy is poor.
            }

            // Note: we don't check status or permission, because TryStart checks for us
            if (!watcher.TryStart(false, delayThreshold))
                return null;

            // The watcher may return a stale location, which we
            // detect with the timestamp check.   In addition, it may return a bad reading (low accuracy)
            // So we want to repeat the check for a little while until we get it right, but not repeat it forever.
            // The GeoCoordinateWatcher interface is designed to use a change event to tell you when it has a new
            // value ready, but it is very awkward to do the counting and checking over arbitrarily many threads,
            // and we want a synchronous result anyway, so we use an old-fashioned sleep loop instead.

            GeoPosition<GeoCoordinate> place;
            DateTime limit = DateTime.Now.AddMinutes(1.0);
            DateTime minbar = DateTime.Now.AddMinutes(-2.0);
            int counter = 0;
            while (DateTime.Now < limit) // keep trying for up to one minute.
            {
                counter++;
                place = watcher.Position;

                if (place.Location.IsUnknown)  // someone turned off the GPS
                {
                    break;
                }

                //Log.write("GPS accuracy = " + place.Location.HorizontalAccuracy);
                if (place.Location.HorizontalAccuracy < 1000 ||  // accuracy in meters
                    place.Timestamp > minbar)  // recent enough
                {
                    watcher.Stop();
                    Log.write("Took " + counter + " tries to get GPS result");
                    return place.Location;
                }

                Thread.Sleep(500); // sleep for half a second
            }

            Log.write("Giving up on GPS result after " + counter + " tries.");
            watcher.Stop();
            return null;
        }
        #endregion

        #region travel times
        /// <summary>
        /// Put it together into a single call:  go get location, and then get the travel times from that.
        /// </summary>
        public static void checkTravelTimes()
        {
            GeoCoordinate loc = getLocation();
            if (loc != null)
            {
                ServerIO.requestTravelTimes(String.Format("{0:F6},{1:F6}", loc.Latitude, loc.Longitude));
                // The continuation calls processTravelTimes (see below)
            }
        }

        /// <summary>
        /// Parse the travel times as returned from the NextFerry service.
        /// When the parse is complete, the NewTravelTimes event is raised.
        /// </summary>
        public static void processTravelTimes(string textblock)
        {
            StringReader sr = new StringReader(textblock);
            Dictionary<int, int> parsed = new Dictionary<int, int>();
            while (true)
            {
                string line = sr.ReadLine();
                if (line == null) break;
                string[] ss = line.Split(':');
                if (ss.Length != 2)
                {
                    Log.write("Badly formatted travel time response?  " + line);
                    continue;
                }
                int code, val;
                bool success = true;
                success &= Int32.TryParse(ss[0], out code);
                success &= Int32.TryParse(ss[1], out val);
                if (success)
                {
                    parsed[code] = val;
                }
                else
                {
                    Log.write("Unable to parse travel time response " + line);
                    continue;
                }
            }

            if (NewTravelTimes != null)
            {
                NewTravelTimes(new TravelTimeEventArgs(parsed));
            }
        }
        #endregion
    }
}
