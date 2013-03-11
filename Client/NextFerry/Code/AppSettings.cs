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
        public const string KcacheVersion = "cacheVersion";
        public const string KuseLocation = "useLocation";
        public const string Kdebug = "debug";
        public const string KlastAppVersion = "lastAppVersion";

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

        private static string _lastAppVersion = "0.0";
        /// <summary>
        /// The last version of the application to run before this invocation.
        /// This allows us to check for upgrades and perform one-time behaviors.
        /// </summary>
        public static string lastAppVersion
        {
            get { return _lastAppVersion; }
            set
            {
                _lastAppVersion = value;
                IsolatedStorageSettings.ApplicationSettings[KlastAppVersion] = value;
                OnChanged(KlastAppVersion);
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
            init<string>(KlastAppVersion);
            recoverDisplaySettings();
            Log.write("Application Settings restored");
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
            storeDisplaySettings();
        }

        #region route display preferences
        // Route display is handled a bit differently: we load and store route display
        // information on startup and exit rather than on each change.
        // Note we store information for which routes are *not* displayed.  That way
        // if there should be any new routes introduced, they will default to display=true.

        public static void storeDisplaySettings()
        {
            foreach (Route r in RouteManager.AllRoutes)
            {
                string key = r.routeCode.ToString();
                if (r.display)
                    IsolatedStorageSettings.ApplicationSettings.Remove(key);
                else
                    IsolatedStorageSettings.ApplicationSettings[key] = "false";
            }
        }

        public static void recoverDisplaySettings()
        {
            int count = 0;
            foreach (Route r in RouteManager.AllRoutes)
            {
                string key = r.routeCode.ToString();
                r.display = (!IsolatedStorageSettings.ApplicationSettings.Contains(key));
                if (r.display)
                    count++;
            }
        }
        #endregion

        #region legacy class
        // We need this here to be able to read settings as stored in V2.0
        // Lesson learned: don't store class objects unless you really, really, need to.
        public class RouteSetting
        {
            public string wbname { get; set; }
            private bool _display;
            public bool display
            {
                get { return _display; }
                set { _display = value; }
            }

            /// <summary>
            /// Convert V2.0 settings to V3.0 format
            /// </summary>
            public static void upgrade()
            {
                IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;
                List<AppSettings.RouteSetting> dlist;
                if (settings.TryGetValue<List<AppSettings.RouteSetting>>("displaySettings", out dlist))
                {
                    foreach (AppSettings.RouteSetting rs in dlist)
                    {
                        // actually set the route, which in turn will cause the settings to be changed.
                        // this way no chance of inconsistency between the two.
                        Route r = RouteManager.lookup(rs.wbname);
                        r.display = rs.display;
                    }
                    settings.Remove("displaySettings");
                }
            }
        }
        #endregion
    }
}
