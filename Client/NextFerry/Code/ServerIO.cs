using System;
using System.Net;
using System.Windows;
using System.IO;
using System.Threading;
using System.Text;
using System.Text.RegularExpressions;

namespace NextFerry
{
    /// <summary>
    /// Handle interactions with the server.
    /// The server supports the following kinds of interactions:
    ///     INIT:
    ///     Called when the application is entrered, or wakes up and finds it's state gone
    ///     
    ///     DISTANCE:
    ///     Called periodically with our current location to retrieve travel times to terminals.  
    /// 
    /// </summary>
    public static class ServerIO
    {
        private const string initURL = "http://server.nextferry.appspot.com/init";
        private const string travelURL = "http://server.nextferry.appspot.com/traveltimes";
        // How to test with debug server
        // private const string initURL = "http://localhost:8080/init";
        // private const string travelURL = "http://localhost:8080/traveltimes";

        public static void requestInitUpdate()
        {
            WebClient request = new WebClient();
            string appVersion = ((App)Application.Current).appVersion;
            // for debugging, this is sometimes useful: it makes this client unique to the server
            // appVersion = "debug";

            Uri uri = new Uri(String.Format("{0}/{1}/{2}", initURL, appVersion, AppSettings.cacheVersion));
            Log.write("Sending " + uri);

            try
            {
                request.DownloadStringCompleted += processResponse;
                request.DownloadStringAsync(uri);
            }
            catch (Exception e)
            {
                Log.write("Error accessing Server (init): " + e);
            }
        }


        public static void requestTravelTimes(string loc)
        {
            WebClient request = new WebClient();
            string appVersion = ((App)Application.Current).appVersion;

            Uri uri = new Uri(String.Format("{0}/{1}/{2}", travelURL, appVersion, loc));
            Log.write("Sending " + uri);

            try
            {
                request.DownloadStringCompleted += processResponse;
                request.DownloadStringAsync(uri);
            }
            catch (Exception e)
            {
                Log.write("Error accessing Server (travel): " + e);
            }
        }


        /// <summary>
        /// Common routine to handle the response from the server, for all requests.
        /// Note that a single request may get multiple kinds of responses.
        /// </summary>
        public static void processResponse(Object sender, DownloadStringCompletedEventArgs args)
        {
            try
            {
                if (args.Error != null)
                {
                    Log.write("fetch failed: " + args.Error.ToString());
                    return;
                }
                else if (args.Cancelled)
                {
                    Log.write("fetch cancelled");
                    // skip; it will be reread another time
                    return;
                }

                StringBuilder buffer = new StringBuilder();
                StringReader sr = new StringReader(args.Result);
                string controlLine = sr.ReadLine();
                while (controlLine != null)
                {
                    if (!controlLine.StartsWith("#"))
                    {
                        // abandon ship --- write the response to the log
                        Log.write("Error: expected control line, got ");
                        while (controlLine != null)
                        {
                            Log.write(controlLine);
                            controlLine = sr.ReadLine();
                        }
                        return;
                    }
                    if (controlLine.StartsWith("#done"))
                    {
                        Log.write("end of received");
                        return;
                    }

                    // else gather up the corresponding data block
                    buffer.Clear();
                    while (sr.Peek() != '#' && sr.Peek() != -1)
                    {
                        buffer.Append(sr.ReadLine());
                        buffer.Append('\n');
                    }

                    // and decide what to do with it
                    if (controlLine.StartsWith("#schedule"))
                    {
                        Log.write("received schedule");
                        string dataversion = controlLine.Substring("#schedule".Length + 1);
                        string newschedule = buffer.ToString();
                        bool success = RouteIO.deserialize(new StringReader(newschedule));
                        if (success)
                        {
                            // Write it out to cache, and store the version id
                            RouteIO.writeCache(newschedule);
                            AppSettings.cacheVersion = dataversion;
                        }
                        // if we weren't successful, we leave whatever we managed to read, but don't update
                        // the cache file.
                    }
                    else if (controlLine.StartsWith("#traveltimes"))
                    {
                        Log.write("received traveltimes");
                        LocationMonitor.processTravelTimes(buffer.ToString());
                    }
                    else
                    {
                        Log.write("received/ignoring: " + controlLine);
                    }

                    controlLine = sr.ReadLine();
                }
            }
            catch (Exception e)
            {
                Log.write("Unexpected exception in ServerIO " + e);
            }
        }
    }
}
