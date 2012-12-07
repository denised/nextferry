using System;
using System.Device.Location;
using System.Windows;
using System.Windows.Threading;


namespace NextFerry
{
    public static class LocationMonitor
    {
        private static DateTime lastupdate;

        public static void go()
        {
            lastupdate = DateTime.Now;
            Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    Log.write("Adding location check");
                    ((App)Application.Current).theTimer.Tick += checkNow;
                });
            // do one call immediately
            checkNow(null, null);
        }

        // Call when update has been confirmed.
        public static void confirm()
        {
            lastupdate = DateTime.Now;
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                ((App)Application.Current).theMainPage.removeWarning();
            });
        }

        public static void checkNow(Object sender, EventArgs args)
        {
            Log.write(String.Format("checkNow @ {0:t}", DateTime.Now));
            if (AppSettings.useLocation && ((App)Application.Current).usingNetwork)
            {
                //Log.write("getting location");
                ImmediateLocation loc = new ImmediateLocation(consumeLocation);
                loc.GetLocation();
            }

            // Also check that the times are not getting stale
            if (AppSettings.useLocation)
            {
                int age = (int)(DateTime.Now - lastupdate).TotalMinutes;
                Log.write("tt age is " + age);
                if (age > 10)
                {
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                        {
                            Terminal.clearTravelTimes();
                            ((App)Application.Current).theMainPage.addWarning("Unable to get travel times");
                        });
                }
                else if (age > 5)
                {
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                        {
                            ((App)Application.Current).theMainPage.addWarning("Travel times are stale");
                        });
                }
            }
        }

        public static void consumeLocation(GeoCoordinate gc)
        {
            //Log.write("tock: got {0} at {1:t}", gc.ToString(), DateTime.Now);
            if (!gc.IsUnknown)
            {
                ServerIO.requestTravelTimes(String.Format("{0:F6},{1:F6}", gc.Latitude, gc.Longitude));
            }
        }
    }


    // Thank you Don Kackman
    // http://www.codeproject.com/Articles/134982/A-helper-class-to-get-the-current-location-on-a-Wi

    public class ImmediateLocation : IDisposable
    {
        private GeoCoordinateWatcher _watcher;
        private Action<GeoCoordinate> _action;

        public ImmediateLocation(Action<GeoCoordinate> a)
        {
            _action = a;
        }

        public void GetLocation()
        {
            if (_watcher == null)
            {
                _watcher = new GeoCoordinateWatcher(GeoPositionAccuracy.Default);
                _watcher.MovementThreshold = 1000;

                _watcher.PositionChanged += new
                    EventHandler<GeoPositionChangedEventArgs<GeoCoordinate>>
                    (_watcher_PositionChanged);
                _watcher.StatusChanged += new
                    EventHandler<GeoPositionStatusChangedEventArgs>
                    (_watcher_StatusChanged);

                _watcher.Start(false);

                if (_watcher.Status == GeoPositionStatus.Disabled
                    || _watcher.Permission == GeoPositionPermission.Denied)
                    Dispose();
            }
        }

        void _watcher_StatusChanged(object sender,
            GeoPositionStatusChangedEventArgs e)
        {
            if (e.Status == GeoPositionStatus.Disabled
                || _watcher.Permission == GeoPositionPermission.Denied)
                Dispose();
        }

        void _watcher_PositionChanged(object sender,
            GeoPositionChangedEventArgs<GeoCoordinate> e)
        {
            _action(e.Position.Location);
            Dispose();
        }

        public void Dispose()
        {
            if (_watcher != null)
            {
                _watcher.Stop();
                _watcher.PositionChanged -= new
                    EventHandler<GeoPositionChangedEventArgs<GeoCoordinate>>
                    (_watcher_PositionChanged);
                _watcher.StatusChanged -= new
                    EventHandler<GeoPositionStatusChangedEventArgs>
                    (_watcher_StatusChanged);
                _watcher.Dispose();
            }
            _watcher = null;
            _action = null;
        }
    }
}
