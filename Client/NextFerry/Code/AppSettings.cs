using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.IsolatedStorage;
using System.Reflection;

namespace NextFerry
{
    /// <summary>
    /// Manage user settings.   Mostly uninteresting boilerplate.
    /// </summary>
    public static class AppSettings
    {
        // Use the same keys for the app settings and for OnChanged notification
        public const string KdisplayWB = "displayWB";
        public const string Kdisplay12hr = "display12hr";
        public const string KbufferTime = "bufferTime";
        public const string KdisplaySettings = "displaySettings";
        public const string KcacheVersion = "cacheVersion";
        public const string KuseLocation = "useLocation";
        public const string Kdebug = "debug";

        private static bool _displayWB = true;
        public static bool displayWB
        {
            get { return _displayWB; }
            set
            {
                _displayWB = value;
                IsolatedStorageSettings.ApplicationSettings[KdisplayWB] = value;
                OnChanged(KdisplayWB);
            }
        }
                    
        private static bool _display12hr = true;
        public static bool display12hr
        {
            get { return _display12hr; }
            set
            {
                _display12hr = value;
                IsolatedStorageSettings.ApplicationSettings[Kdisplay12hr] = value;
                OnChanged(Kdisplay12hr);
            }
        }

        private static int _bufferTime = 20;
        public static int bufferTime
        {
            get { return _bufferTime; }
            set
            {
                _bufferTime = value;
                IsolatedStorageSettings.ApplicationSettings[KbufferTime] = value;
                OnChanged(KbufferTime);
            }
        }

        // displaySettings changes are propagated differently --- no notification here.
        private static List<RouteSetting> _displaySettings = new List<RouteSetting>();
        public static List<RouteSetting> displaySettings
        {
            get { return _displaySettings; }
            private set { displaySettings = value; }
        }

        private static string _cacheVersion = "";
        public static string cacheVersion
        {
            get { return _cacheVersion; }
            set
            {
                _cacheVersion = value;
                IsolatedStorageSettings.ApplicationSettings[KcacheVersion] = value;
                OnChanged(KcacheVersion);
            }
        }

        private static bool _useLocation = false;
        public static bool useLocation
        {
            get { return _useLocation; }
            set
            {
                _useLocation = value;
                IsolatedStorageSettings.ApplicationSettings[KuseLocation] = value;
                OnChanged(KuseLocation);
            }
        }

        private static bool _debug = false;
        public static bool debug
        {
            get { return _debug; }
            set
            {
                _debug = value;
                IsolatedStorageSettings.ApplicationSettings[Kdebug] = value;
                OnChanged(Kdebug);
            }
        }

        public static event PropertyChangedEventHandler PropertyChanged;
        public static void OnChanged(string s)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(null, new PropertyChangedEventArgs(s));
            }
        }


        /// <summary>
        /// Read initial values from AppSettings, if present.
        /// Else initialize from defaults.
        /// </summary>
        public static void init()
        {
            init<bool>(KdisplayWB);
            init<bool>(Kdisplay12hr);
            init<int>(KbufferTime);
            init<string>(KcacheVersion);
            init<bool>(KuseLocation);
            init<bool>(Kdebug);
            init<List<RouteSetting>>(KdisplaySettings);
            RouteSetting.init(_displaySettings);
        }


        // Use reflection to generalize the process of getting IsolatedStorageSettings.
        private static void init<T>(string key)
        {
            string fieldName = "_" + key;
            FieldInfo theField = typeof(AppSettings).GetField(fieldName,BindingFlags.NonPublic|BindingFlags.Static);
            T value;

            if (IsolatedStorageSettings.ApplicationSettings.TryGetValue<T>(key, out value))
            {
                // set the field from the stored setting.
                theField.SetValue(null, value);
            }
            else
            {
                // initialize stored setting to the default value
                IsolatedStorageSettings.ApplicationSettings.Add(key, theField.GetValue(null));
            }
        }


        public static void close()
        {
            // Displaysettings is the only one we don't write atomically
            IsolatedStorageSettings.ApplicationSettings[KdisplaySettings] = displaySettings;
        }
    }



    public class RouteSetting
    {
        public string wbname { get; set; }
        private bool _display;
        public bool display
        {
            get { return _display; }
            set // set here, and also update the routes themselves.
            {
                _display = value;
                propagate();
            }
        }

        // Instead of a general eventing mechanism, we have this hard-wired:
        // when the display value is updated, set the corresponding values in the Routes.
        // Maybe someday rewrite as an event...
        public void propagate()
        {
            // when deserializing, sometimes name is not set yet.
            if (wbname != null)
            {
                Route r = Routes.getRoute(wbname, "wb");
                r.display = _display;
                r.sibling().display = _display;
            }
        }

        /// <summary>
        /// If dlist is empty, initialize it to the proper set of routes, and 
        /// set their display values appropriately.
        /// Also make sure that the display values are propagated to the routes
        /// themselves.
        /// </summary>
        public static void init(List<RouteSetting> dlist)
        {
            if (dlist.Count == 0)
            {
                Log.write("no display settings found");
                foreach (Route r in Routes.AllRoutes)
                {
                    if (String.Equals(r.direction, "wb")) // get only the westbound routes.
                        dlist.Add(new RouteSetting { wbname = r.name, display = false });
                }
                // First time display just a couple of routes to keep things simple
                // This happens to be Bainbridge and Edmonds, the most popular.
                dlist[0].display = true;
                dlist[1].display = true;
            }
            // Propagate (in case deserialization didn't do it properly).
            foreach (RouteSetting rs in dlist)
                rs.propagate();
        }
    }
}
