using System.Collections.Generic;

namespace NextFerry
{
    /// <summary>
    /// Everything we know about a terminal.
    /// This includes static information such as it's name and location, and dynamic information, such as the
    /// expected travel time to the terminal from the user's current location.
    /// </summary>
    public class Terminal
    {
        public int code { get; private set; }   // codes as used in the WSDOT's web services.
        public string name { get; private set; }
        public string loc { get; private set; } // in string lat,long form.  currently unused.
        public bool hasTT { get; private set; } // is a travel time defined for this terminal?
        public int tt { get; private set; } // if hasTT, then tt is the estimated driving time in minutes

        private Terminal(int c, string n, string l)
        {
            code = c;
            name = n;
            loc = l;
            hasTT = false;
        }

        /// <summary>
        /// Update the estimated travel time to this terminal.
        /// </summary>
        /// <param name="estimatedDelta"></param>
        public void setTT(int estimatedTT)
        {
            hasTT = true;
            tt = estimatedTT;
        }

        /// <summary>
        /// Remove estimate of travel time to this terminal.
        /// </summary>
        public void clearTT()
        {
            hasTT = false;
        }

        #region AllTerminals

        public static List<Terminal> AllTerminals = new List<Terminal>
        {
            new Terminal(1, "Anacortes", "48.502220, -122.679455"),
            new Terminal(3, "Bainbridge Island", "47.623046, -122.511377"),
            new Terminal(4, "Bremerton", "47.564990, -122.627012"),
            new Terminal(5, "Clinton", "47.974785, -122.352139"),
            new Terminal(8, "Edmonds", "47.811240, -122.382631"),
            new Terminal(9, "Fauntleroy", "47.523115, -122.392952"),
            new Terminal(10, "Friday Harbor", "48.535010, -123.014645"),
            new Terminal(11, "Coupeville", "48.160592, -122.674305"),
            new Terminal(12, "Kingston", "47.796943, -122.496785"),
            new Terminal(13, "Lopez Island", "48.570447, -122.883646"),
            new Terminal(14, "Mukilteo", "47.947758, -122.304138"),
            new Terminal(15, "Orcas Island", "48.597971, -122.943985"),
            new Terminal(16, "Point Defiance", "47.305414, -122.514123"),
            new Terminal(17, "Port Townsend", "48.112648, -122.760715"),
            new Terminal( 7, "Seattle", "47.601767, -122.336089"),
            new Terminal(18, "Shaw Island", "48.583991, -122.929351"),
            new Terminal(20, "Southworth", "47.512130, -122.500970"),
            new Terminal(21, "Tahlequah", "47.333023, -122.506999"),
            new Terminal(22, "Vashon Island", "47.508616, -122.464127")
        };


        /// <summary>
        /// Return terminal from code.
        /// </summary>
        public static Terminal lookup(int code)
        {
            foreach (Terminal t in AllTerminals)
            {
                if (t.code == code)
                    return t;
            }
            return null;
        }

        /// <summary>
        /// Return terminal from name
        /// </summary>
        public static Terminal lookup(string name)
        {
            foreach (Terminal t in AllTerminals)
            {
                if (t.name == name)
                    return t;
            }
            return null;
        }

        /// <summary>
        /// Clear all travel times.
        /// </summary>
        public static void clearTravelTimes()
        {
            foreach (Terminal t in AllTerminals)
            {
                t.clearTT();
            }
        }
        #endregion
    }
}
