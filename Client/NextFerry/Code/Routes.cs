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
    public static class Routes
    {
        /// <summary>
        /// The primary application state: a list of all the ferry routes and their schedules.
        /// </summary>
        public static ObservableCollection<Route> AllRoutes = new ObservableCollection<Route>
        {
            new Route("wb", "bainbridge", 7, 3 ), 
            new Route("eb", "bainbridge", 3, 7 ),
            new Route("wb", "edmonds", 8, 12 ),
            new Route("eb", "edmonds", 12, 8 ),
            new Route("wb", "mukilteo", 14, 5 ),
            new Route("eb", "mukilteo", 5, 14 ),
            new Route("wb", "pt townsend", 11, 17 ),
            new Route("eb", "pt townsend", 17, 11 ),
            new Route("wb", "fauntleroy-southworth", 9, 20 ),
            new Route("eb", "fauntleroy-southworth", 20, 9 ),
            new Route("wb", "fauntleroy-vashon", 9, 22 ),
            new Route("eb", "fauntleroy-vashon", 22, 9 ),
            new Route("wb", "vashon-southworth", 22, 20 ),
            new Route("eb", "vashon-southworth", 20, 22 ),
            new Route("wb", "bremerton", 7, 4 ),
            new Route("eb", "bremerton", 4, 7 ),
            new Route("wb", "pt defiance", 21, 16 ),  // OK, so WB/EB doesn't make sense in this case...
            new Route("eb", "pt defiance", 16, 21 ),
            new Route("wb", "friday harbor", 1, 10 ),   // these aren't perfect either, but hopefully useful enough
            new Route("eb", "friday harbor", 10, 1 ),   
            new Route("wb", "orcas", 1, 15 ),
            new Route("eb", "orcas", 15, 1 )
        };

        /// <summary>
        /// Return route by preferred name.
        /// </summary>
        public static Route getRoute(string name, string dir)
        {
            foreach (Route r in AllRoutes)
                if (r.name.CompareTo(name) == 0 && r.direction.CompareTo(dir) == 0)
                    return r;
            return null;
        }

        /// <summary>
        /// Search by source and destination codes.
        /// </summary>
        public static Route getRoute(int sCode, int dCode)
        {
            foreach (Route r in AllRoutes)
                if (r.sourceCode == sCode && r.destCode == dCode)
                    return r;
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
                if (r.display && r.weekday.times.Count > 0)
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
                    r.stateRefresh();
            }
        }
    }
}
