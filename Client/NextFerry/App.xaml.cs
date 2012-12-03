using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.IO.IsolatedStorage;
using System.Net.NetworkInformation;
using System.Windows.Threading;
using System.Reflection;


namespace NextFerry
{
    public partial class App : Application
    {
        public PhoneApplicationFrame RootFrame { get; private set; }
        public bool usingNetwork = false;
        public string appVersion = "2.0";  // TODO: set via reflection?  (didn't work?)
        public MainPage theMainPage = null;
        public DispatcherTimer theTimer = null;

        public App()
        {
            UnhandledException += Application_UnhandledException;
            theTimer = new DispatcherTimer();
            theTimer.Interval = new TimeSpan(0, 0, 20);

            InitializeComponent();
            InitializePhoneApplication();
        }
     
        // Code to execute when the application is launching (eg, from Start)
        // This code will not execute when the application is reactivated
        private void Application_Launching(object sender, LaunchingEventArgs e)
        {
            // call order matters.
            AppSettings.init();
            startThreads(true);
        }


        // Code to execute when the application is activated (brought to foreground)
        // This code will not execute when the application is first launched
        private void Application_Activated(object sender, ActivatedEventArgs e)
        {
            if (e.IsApplicationInstancePreserved)
            {
                // restart network requests only
                startThreads(false);
            }
            else // start over again
            {
                AppSettings.init();
                startThreads(true);
            }
        }

        // Code to execute when the application is deactivated (sent to background)
        // This code will not execute when the application is closing
        private void Application_Deactivated(object sender, DeactivatedEventArgs e)
        {
            AppSettings.close();
            theTimer.Stop();
        }

        // Code to execute when the application is closing (eg, user hit Back)
        // This code will not execute when the application is deactivated
        private void Application_Closing(object sender, ClosingEventArgs e)
        {
            AppSettings.close();
        }

        #region error handlers

        // Code to execute if a navigation fails
        private void RootFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // A navigation has failed; break into the debugger
                System.Diagnostics.Debugger.Break();
            }
        }

        // Code to execute on Unhandled Exceptions
        private void Application_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            // This is a big hammer, but it seems better than trying to be more delicate:
            // *Any* error that happens because we try to communicate with a server is caught and "disappeared" here.
            // That is okay because the app is architected to not need server response.
            // NB: we have to do this because the service reference code can cause exceptions on 
            // other threads that we can't put a try/catch around.
            if (e.ExceptionObject is System.ServiceModel.CommunicationException)
            {
                System.Diagnostics.Debug.WriteLine("Server communication failure: " + e.ExceptionObject.Message);
                e.Handled = true;
            }
            else if (System.Diagnostics.Debugger.IsAttached)
            {
                // An unhandled exception has occurred; break into the debugger
                System.Diagnostics.Debugger.Break();
            }
        }

        #endregion

        #region boiler plate application initialization

        // Avoid double-initialization
        private bool phoneApplicationInitialized = false;

        // Do not add any additional code to this method
        private void InitializePhoneApplication()
        {
            if (phoneApplicationInitialized)
                return;

            // Create the frame but don't set it as RootVisual yet; this allows the splash
            // screen to remain active until the application is ready to render.
            RootFrame = new PhoneApplicationFrame();
            RootFrame.Navigated += CompleteInitializePhoneApplication;

            // Handle navigation failures
            RootFrame.NavigationFailed += RootFrame_NavigationFailed;

            // Ensure we don't initialize again
            phoneApplicationInitialized = true;
        }

        // Do not add any additional code to this method
        private void CompleteInitializePhoneApplication(object sender, NavigationEventArgs e)
        {
            // Set the root visual to allow the application to render
            if (RootVisual != RootFrame)
                RootVisual = RootFrame;

            // Remove this handler since it is no longer needed
            RootFrame.Navigated -= CompleteInitializePhoneApplication;
        }

        #endregion

        #region background threads

        private PhaseTasker pt;

        private void startThreads(bool freshStart)
        {
            pt = new PhaseTasker();

            // do tasks in three phases.  Each phase must complete before the next begins.
            // note this means that the the tasks themselves have to behave synchronously.
            if (freshStart)
            {
                // do only once
                pt.addAction(1, RouteIO.readCache);
                pt.addAction(3, LocationMonitor.go);
                theTimer.Tick += checkNetwork;
            }

            pt.addAction(1, findNetwork);
            pt.addAction(2, ServerIO.requestInitUpdate);

            //pt.addAction(3, bar); // Display "no can do" if we have no data at all.
            
            pt.go();
            theTimer.Start();
        }

        /// <summary>
        /// Determine if the network is available.   This is done on a background thread because
        /// the system will wait on the network availability for a certain amount of time before
        /// timing out.
        /// </summary>
        public void findNetwork()
        {
            usingNetwork = NetworkInterface.GetIsNetworkAvailable();
            // NB: BeginInvoke not needed, since this isn't UI state and there's no race conditions.
        }

        private static int counter = 0;
        public void checkNetwork(Object o, EventArgs a)
        {
            counter++;
            if (counter % 5 == 0)
            {
                if (!usingNetwork)
                {
                    System.Diagnostics.Debug.WriteLine("rechecking network");
                    findNetwork();
                }
            }
        }

        #endregion
    }
}