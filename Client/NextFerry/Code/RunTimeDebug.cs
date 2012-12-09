namespace NextFerry
{
    using System.Collections.Generic;

    public static class Log
    {
        public static void write(object o)
        {
            System.Diagnostics.Debug.WriteLine(o);
            if (AppSettings.debug)
            {
                System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    WP7Contrib.Diagnostics.RuntimeDebug.Write(o);
                });
            }
        }

        public static void writeSeq<X>(IEnumerable<X> seq)
        {
            foreach (X item in seq)
            {
                Log.write("> " + item.ToString());
            }
        }

        public static void writeDict<X, Y>(IDictionary<X, Y> d)
        {
            foreach (KeyValuePair<X, Y> item in d)
            {
                Log.write("> " + item.Key.ToString() + ": " + item.Value.ToString());
            }
        }
    }
}


namespace WP7Contrib.Diagnostics
{
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.IO.IsolatedStorage;
    using System.Windows.Threading;
    using System.Linq;
    using Microsoft.Phone.Tasks;
    using System.Text;
    using Microsoft.Phone.Controls;

    /// <summary>
    /// utility which aides in debugging of phone applications durring runtime.  Can be used in a silent manor by by initializing w/ 
    /// IsVisual = false or using the showdebug() HideDebug() methods, or used with a visual which is designed to show the real time 
    /// log, but not interfer with user interactions.  Logs can also be emailed
    /// or cleared at will.
    /// Example:
    /// public MainPage()
    /// {
    ///    InitializeComponent();
    ///    WP7Contrib.Diagnostics.RunTimeDebug.Initialize(true,true);
    ///    this.Loaded += new RoutedEventHandler(MainPage_Loaded);
    ///     
    ///  }
    ///    
    /// void MainPage_Loaded(object sender, RoutedEventArgs e)
    /// {
    ///    WP7Contrib.Diagnostics.RunTimeDebug.Write("loaded");
    /// }
    /// </summary>
    public static class RuntimeDebug
    {

        #region fields
        private static ObservableCollection<string> _output;
        private static string DEBUGFILENAME = "WP7CONTRIB-RUNTIME-DIAG.txt";
        #endregion

        #region properties
        /// <summary>
        /// should the visual debug controls show
        /// </summary>
        public static bool IsVisual {set;get;}


        /// <summary>
        /// email to address
        /// </summary>
        public static string EmailTo { set; get; }

        /// <summary>
        /// email text intro
        /// </summary>
        public static string EmailIntro { set; get; }

        /// <summary>
        /// the current debug statements
        /// </summary>
        public static ObservableCollection<string> DebugStack
        {
            get { return _output; }
        }
        #endregion

        #region inits
        /// <summary>
        /// initialize the debug controls
        /// </summary>
        public static void Initialize()
        {
            Initialize(false, true);
        }

        /// <summary>
        /// intialize w/ default to and intro
        /// </summary>
        /// <param name="writeexceptions"></param>
        /// <param name="visual"></param>
        public static void Initialize(bool writeexceptions, bool visual)
        {
            Initialize(writeexceptions, visual, EmailTo, EmailIntro);
        }
        
        /// <summary>
        /// initialize the debug controls, and log any uncaught exception
        /// </summary>
        /// <param name="writeexceptions"></param>
        /// <param name="visual"></param>
        /// <param name="to"></param>
        /// <param name="body"></param>
        public static void Initialize(bool writeexceptions, bool visual,string to,string body)
        {
            EmailTo = to;
            EmailIntro = body;

            if (_output == null)
            {
                IsVisual = visual;

                _output = new ObservableCollection<string>();
                _output = GetLog();
                if (writeexceptions)
                {
                    Application.Current.UnhandledException += new EventHandler<ApplicationUnhandledExceptionEventArgs>(Current_UnhandledException);
                }



                // if the host is ready wire up the navigate and add the visual
                // add control on each navigate
                Microsoft.Phone.Controls.PhoneApplicationFrame frame = (Microsoft.Phone.Controls.PhoneApplicationFrame)Application.Current.RootVisual;
                if (frame != null)
                {
                    frame.Navigated += new System.Windows.Navigation.NavigatedEventHandler(frame_Navigated);
                    AddVisual();

                }
                else
                {
                    // controls arent available yet, so wait for the root to be ready
                    // then wire up the visual
                    DispatcherTimer t = new DispatcherTimer();
                    t.Interval = TimeSpan.FromSeconds(1);
                    t.Tick += delegate(object sender, EventArgs e)
                    {
                        if (Application.Current.RootVisual != null)
                        {
                            ((DispatcherTimer)sender).Stop();
                            Microsoft.Phone.Controls.PhoneApplicationFrame found = (Microsoft.Phone.Controls.PhoneApplicationFrame)Application.Current.RootVisual;
                            found.Navigated += new System.Windows.Navigation.NavigatedEventHandler(frame_Navigated);
                            AddVisual();
                        }
                    };

                    t.Start();
                }
            }
        }

        #endregion

        #region methods

        #region iso helper

        /// <summary>
        /// save to iso
        /// </summary>
        /// <param name="logline"></param>
        public static void SaveLog(string logline)
        {
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
            {


                using (IsolatedStorageFileStream isfs = new IsolatedStorageFileStream(DEBUGFILENAME, FileMode.Append, isf))
                {
                    using (StreamWriter writer = new StreamWriter(isfs))
                    {
                        writer.WriteLine(logline);
                        writer.Close();
                    }
                }
            }
        }

        /// <summary>
        /// read from iso
        /// </summary>
        /// <returns></returns>
        public static ObservableCollection<string> GetLog()
        {
            object locked = new object();
            ObservableCollection<string> data = new ObservableCollection<string>();
            lock (locked)
            {
                using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if (isf.FileExists(DEBUGFILENAME))
                    {
                        using (IsolatedStorageFileStream isfs = new IsolatedStorageFileStream(DEBUGFILENAME, FileMode.Open, isf))
                        {
                            using (StreamReader reader = new StreamReader(isfs))
                            {
                                string line;
                                while ((line = reader.ReadLine()) != null)
                                {
                                    data.Insert(0, line);
                                }

                            }
                        }
                    }
                }
            }

            return data;
        }

        #endregion

        /// <summary>
        /// send email w/ a specific intro and to
        /// </summary>
        /// <param name="to"></param>
        /// <param name="intro"></param>
        public static void SendEmail(String to, string intro)
        {
            EmailTo = to;
            EmailIntro = intro;

            SendEmail();
        }

        /// <summary>
        /// send log by email
        /// </summary>
        public static void SendEmail()
        {
            if (_output.Count > 0)
            {
                EmailComposeTask task = new EmailComposeTask();
                StringBuilder sb = new StringBuilder();
                if (EmailIntro != string.Empty)
                    sb.Append(EmailIntro);

                sb.AppendLine(string.Format("memory use: {0}", MemoryStats.DebugString));
                
                foreach (string s in _output)
                {
                    sb.AppendLine(s);
                }

                task.To = EmailTo;
                task.Body = sb.ToString();
                task.Subject="log data";
                task.Show();
            }
        }

        /// <summary>
        /// write the object to the log
        /// </summary>
        /// <param name="o"></param>
        public static void Write(object o)
        {
            Initialize();

            string output = Format(o.ToString());
            _output.Insert(0, output);
            SaveLog(output);
        }

        /// <summary>
        /// clear the log
        /// </summary>
        public static void Clear()
        {
            _output.Clear();
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
            {
                isf.DeleteFile(DEBUGFILENAME);
            }

        }

        /// <summary>
        /// helpder to find the top level grid
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static T GetChild<T>(DependencyObject obj) where T : DependencyObject
        {
            DependencyObject child = null;
            for (Int32 i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                child = VisualTreeHelper.GetChild(obj, i);
                if (child != null && child.GetType() == typeof(T))
                {
                    break;
                }
                else if (child != null)
                {
                    child = GetChild<T>(child);
                    if (child != null && child.GetType() == typeof(T))
                    {
                        break;
                    }
                }
            }
            return child as T;
        }

        /// <summary>
        /// on frame navigation add the visual
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static void frame_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            AddVisual();
        }

        /// <summary>
        /// add the visual to the tree
        /// </summary>
        private static void AddVisual()
        {
            if (IsVisual)
            {
                PhoneApplicationFrame frame = (PhoneApplicationFrame)Application.Current.RootVisual;
                PhoneApplicationPage page = (PhoneApplicationPage)frame.Content;
                Grid g = GetChild<Grid>(page);
                if (g != null)
                {

                    RunTimeDebugDisplay display = new RunTimeDebugDisplay();
                    display.ItemSource = _output;
                    g.Children.Add(display);
                }
            }
        }

        /// <summary>
        /// Remove this child
        /// </summary>
        private static void RemoveVisual()
        {
            PhoneApplicationFrame frame = (PhoneApplicationFrame)Application.Current.RootVisual;
            PhoneApplicationPage page = (PhoneApplicationPage)frame.Content;

            Grid g = GetChild<Grid>(page);
            if (g != null)
            {
                if (g.Children.Where(w => w.GetType() == typeof(RunTimeDebugDisplay)).Count() > 0)
                {
                    g.Children.Remove(g.Children.Where(w => w.GetType() == typeof(RunTimeDebugDisplay)).ElementAt(0));
                }
            }
        }

        /// <summary>
        /// show the debug visual
        /// </summary>
        public static void ShowDebug()
        {
            IsVisual = true;
            RemoveVisual();
            AddVisual();
        }


        /// <summary>
        /// hide the debug visual
        /// </summary>
        public static void HideDebug()
        {
            IsVisual = false;
            RemoveVisual();
        }

        /// <summary>
        /// on unhandeled exceptions write to the log
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static void Current_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            Write(string.Format("error: {0}", MemoryStats.DebugString));
            Write(e.ExceptionObject.Message);
        }

        /// <summary>
        /// format the strings
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        private static string Format(string s)
        {
            return string.Format("{0} - {1}", DateTime.Now.ToString("H:mm:ss"), s);
        }

        #endregion

    }
}
