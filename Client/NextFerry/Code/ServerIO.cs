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
        private const string initURL = "http://nextferry.appspot.com/init";
        private const string distanceURL = "http://nextferry.appspot.com/traveltimes";

        public static void requestInitUpdate()
        {
            // if there's no network we don't do anything
            if (((App)Application.Current).usingNetwork)
            {
                WebClient request = new WebClient();
                string appVersion = ((App)Application.Current).appVersion;
                string cacheVersion = AppSettings.cacheVersion;
                Uri uri = new Uri(initURL + "/" + appVersion + "/" + cacheVersion);
                System.Diagnostics.Debug.WriteLine("Sending " + uri);

                try
                {
                    ManualResetEvent mre = new ManualResetEvent(false);
                    request.DownloadStringCompleted += processInitResponse;
                    request.DownloadStringAsync(uri, mre);
                    mre.WaitOne();  // wait until it completes --- this makes network activity sequential, which we want
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine("Error accessing Server: " + e.Message);
                }
            }
        }


        public static void processInitResponse(Object sender, DownloadStringCompletedEventArgs args)
        {
            try
            {
                if (args.Error != null)
                {
                    System.Diagnostics.Debug.WriteLine("fetch failed: " + args.Error.ToString());
                    return;
                }
                else if (args.Cancelled)
                {
                    System.Diagnostics.Debug.WriteLine("fetch cancelled");
                    // skip; it will be reread another time
                    return;
                }

                StringBuilder buffer = new StringBuilder();
                StringReader sr = new StringReader(args.Result);
                string controlLine = sr.ReadLine();
                while (controlLine != null)
                {
                    if (! controlLine.StartsWith("#"))
                    {
                        System.Diagnostics.Debug.WriteLine("Error reading result: expected control line, got " + controlLine);
                        // abandon ship.
                        return;
                    }
                    if (controlLine.StartsWith("#done"))
                    {
                        return;
                    }

                    // else gather up the corresponding data block
                    buffer.Clear();
                    while(sr.Peek() != '#'  &&  sr.Peek() != -1)
                    {
                        buffer.Append(sr.ReadLine());
                        buffer.Append('\n');
                    }

                    // and decide what to do with it
                    if (controlLine.StartsWith("#schedule"))
                    {
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
                    // else: do nothing: ignore unknown blocks

                    controlLine = sr.ReadLine();
                }
            }
            catch(Exception e)
            {
                System.Diagnostics.Debug.WriteLine("Unexpected exception " + e);
            }
            finally
            {
                ((ManualResetEvent)args.UserState).Set(); // tell original thread to continue.
            }
        }
    }
}
