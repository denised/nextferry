using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Phone.Tasks;


namespace NextFerry
{
    /// <summary>
    /// Represents the end-point of a ferry route.   
    /// </summary>
    public class Terminal
    {
        public int code { get; private set; }   // codes as used in the WSDOT's web services.
        public string name { get; private set; }
        public string loc { get; private set; } // in string lat,long form
        private List<string> counties; // which counties could one reasonably drive to this terminal from?

        private Terminal(int c, string n, string l, List<String> lc)
        {
            code = c;
            name = n;
            loc = l;
            counties = lc;
        }

        public static Terminal lookup(int code)
        {
            return null;
        }

        public static Terminal lookup(string name)
        {
            return null;
        }

        public static List<Terminal> AllTerminals = new List<Terminal>
        {
            new Terminal(1, "Anacortes", "48.502220, -122.679455", new List<string> {"Whatcom", "Skagit", "Snohomish", "King"}),
            new Terminal(3, "Bainbridge", "47.623046, -122.511377", new List<string> {"Jefferson", "Kitsap", "Mason"}),
            new Terminal(4, "Bremerton", "47.564990, -122.627012", new List<string> {"Mason","Kitsap","Thurston", "Pierce"}),
            new Terminal(5, "Clinton", "47.974785, -122.352139", new List<string> {"Island"}),
            new Terminal(8, "Edmonds", "47.811240, -122.382631", new List<string> {"Skagit", "Snohomish", "King"}),
            new Terminal(9, "Fauntleroy", "47.523115, -122.392952", new List<string> {"Snohomish","King","Pierce"}),
            new Terminal(10, "Friday Harbor", "48.535010, -123.014645", new List<string> {"San Juan"}),
            new Terminal(11, "Keystone", "48.160592, -122.674305", new List<string> {"Island"}),
            new Terminal(12, "Kingston", "47.796943, -122.496785", new List<string> {"Jefferson","Mason","Kitsap"}),
            new Terminal(13, "Lopez Island", "48.570447, -122.883646", new List<string> {"San Juan"}),
            new Terminal(14, "Mukilteo", "47.947758, -122.304138", new List<string> {"Skagit","Snohomish","King"}),
            new Terminal(15, "Orcas Island", "48.597971, -122.943985", new List<string>{"San Juan"}),
            new Terminal(16, "Point Defiance", "47.305414, -122.514123", new List<string>{"Thurston","Pierce","King"}),
            new Terminal(17, "Port Townsend", "48.112648, -122.760715", new List<string>{"Clallam","Jefferson","Mason","Kitsap"}),
            new Terminal( 7, "Seattle", "47.601767, -122.336089", new List<string>{"Skagit","Snohomish","King","Pierce"}),
            new Terminal(18, "Shaw Island", "48.583991, -122.929351", new List<string>{"San Juan"}),
            new Terminal(20, "Southworth", "47.512130, -122.500970", new List<string>{"Jefferson","Mason","Thurston","Kitsap","Pierce"}),
            new Terminal(21, "Tahlequah", "47.333023, -122.506999", new List<string>{"King"}),
            new Terminal(22, "Vashon Island", "47.508616, -122.464127", new List<string>{"King"})
        };


        public static void testTerms()
        {
            List<string> counties = new List<string> {"Whatcom","Skagit","Snohomish","King","Pierce","Thurston","Mason",
                                        "Kitsap","Jefferson","Clallam","Island","San Juan"};

            string url = "http://www.mapquestapi.com/staticmap/v3/getmap?key=Fmjtd%7Cluuan9u12u%2Cas%3Do5-96rl0f&center=47.587642,-122.431641&size=1400,1200&type=map&imagetype=jpeg&pois=";
            //OMG it worked.  Such a pretty little map.
 
            foreach (Terminal t in AllTerminals)
            {
                url += t.name.ToLower()[0] + "," + t.loc.Replace(" ","") + "|";
                
                foreach (string c in t.counties)
                {
                    if (!counties.Contains(c))
                        throw new Exception("Misspelled county name: " + c);
                }
            }

            System.Diagnostics.Debug.WriteLine(url);
            WebBrowserTask webBrowserTask = new WebBrowserTask();
            webBrowserTask.Uri = new Uri(url, UriKind.Absolute);
            webBrowserTask.Show();

        }
    }
}
