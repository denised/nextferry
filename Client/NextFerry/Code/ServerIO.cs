using System;
using System.Net;
using System.Windows;
using System.IO;
using System.Threading;

namespace NextFerry
{
    /// <summary>
    /// Handle interactions with the server.
    /// </summary>
    public static class ServerIO
    {
        private const string scheduleURL = "http://nextferry.appspot.com/schedule";

        public static void getScheduleUpdate()
        {
            // if there's no network we don't do anything
            if (((App)Application.Current).usingNetwork)
            {
                WebClient request = new WebClient();
                string appVersion = ((App)Application.Current).appVersion;
                string cacheVersion = AppSettings.cacheVersion;
                Uri uri = new Uri(scheduleURL + "/" + appVersion + "/" + cacheVersion);
                System.Diagnostics.Debug.WriteLine("Sending " + uri);

                try
                {
                    ManualResetEvent mre = new ManualResetEvent(false);
                    request.DownloadStringCompleted += processServerSchedule;
                    request.DownloadStringAsync(uri, mre);
                    mre.WaitOne();  // wait until it completes --- this makes network activity sequential, which we want
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine("Error accessing Server: " + e.Message);
                }
            }
        }


        public static void processServerSchedule(Object sender, DownloadStringCompletedEventArgs args)
        {
            int count = 0;
            try
            {
                if (args.Error != null)
                {
                    System.Diagnostics.Debug.WriteLine("fetch failed: " + args.Error.ToString());
                }
                else if (args.Cancelled)
                {
                    System.Diagnostics.Debug.WriteLine("fetch cancelled");
                    // skip; it will be reread another time
                }
                else
                {
                    // Try to parse it.
                    count = RouteIO.deserialize(new StringReader(args.Result));

                    System.Diagnostics.Debug.WriteLine("Read " + count + " records from Ferry Server");
                    if (count == Routes.AllRoutes.Count * 2)
                    {
                        // complete read: save a local copy and update our state.
                        RouteIO.writeCache(args.Result);
                        AppSettings.cacheVersion = dataVersionString(); // TODO
                    }
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("Parse error in Ferry Server response: " + e.Message);

                // Something went wrong. Restore the old cached value and exit.
                // TODO: ...and if there was no cached value?  could end up with partial junk?
                if (count > 0)
                {
                    RouteIO.readCache();
                }
            }
            finally
            {
                ((ManualResetEvent)args.UserState).Set(); // tell original thread to continue.
            }
        }

        private static string dataVersionString()
        {
            // The server expects to know the date we last downloaded the schedule in this format
            return DateTime.Today.ToString("yyyy.MM.dd");
        }
    }
}
