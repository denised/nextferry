using System;
using System.IO;
using System.Net;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;

namespace NextFerry
{
    /// <summary>
    /// The collection of all routes.
    /// </summary>
    public static class RouteManager
    {
        /// <summary>
        /// The primary application state: a list of all the ferry routes and their schedules.
        /// </summary>
        public static ObservableCollection<Route> AllRoutes = new ObservableCollection<Route>()
        {
            // In theory we don't need to have this list here --- it could be obtained from
            // the server.  But duplicating it here makes the app start faster.

            new Route(1,     7,  3, "bainbridge","bainbridge"),
            new Route(1<<2,  8, 12, "edmonds","edmonds"),
            new Route(1<<3, 14,  5, "mukilteo","mukilteo"),
            new Route(1<<4, 11, 17, "pt townsend","pt townsend"),
            new Route(1<<5,  9, 20, "fauntleroy-southworth","southworth-fauntleroy"),
            new Route(1<<6,  9, 22, "fauntleroy-vashon","vashon-fauntleroy"),
            new Route(1<<7, 22, 20, "vashon-southworth","southworth-vashon"),
            new Route(1<<8,  7,  4, "bremerton","bremerton"),
            new Route(1<<9, 21, 16, "vashon-pt defiance","pt defiance-vashon"),
            new Route(1<<10, 1, 10, "friday harbor","friday harbor"),
            new Route(1<<11, 1, 15, "orcas","orcas")
        };


        /// <summary>
        /// Return the route that corresponds to this name (which may be either a wb or eb name)
        /// </summary>
        public static Route lookup(string name)
        {
            foreach (Route r in AllRoutes)
            {
                if (r.ebName == name || r.wbName == name)
                {
                    return r;
                }
            }
            return null;
        }

        /// <summary>
        /// Return the route that corresponds to this route code.
        /// </summary>
        public static Route lookup(int code)
        {
            foreach (Route r in AllRoutes)
            {
                if (r.routeCode == code)
                    return r;
            }
            return null;
        }

        /// <summary>
        /// Remove all the current schedules from the routes.   Used to completely refresh schedule state.
        /// </summary>
        public static void clearSchedules()
        {
            foreach (Route r in AllRoutes)
                r.clearSchedules();
        }

        /// <summary>
        /// Return true if we have schedules.   We cheat and say yes if we have *any* schedules
        /// </summary>
        /// <returns></returns>
        public static bool haveSchedules()
        {
            foreach (Route r in AllRoutes)
            {
                if (r.display && (!r.weekday.isEmpty()))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Perform update action on all routes that are displayed.
        /// </summary>
        public static void updateDisplay()
        {
            foreach (Route r in AllRoutes)
            {
                if (r.display)
                    r.updateGoodness();
            }
        }
    }
}
