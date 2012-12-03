#!/usr/bin/env python
import urllib2
import logging
import json

"""
Determine the travel times to ferry terminals from the client's current location.
"""
# The mapquest routematrix service takes a list of locations as an argument and returns a list of distances
# (and travel times) from the first location to each of the others in the list.
# So we supply the client's location as the first location, and all the terminals as the remaining.
# Some of the results will be unreasonable/uninteresting (for example if the client is in Seattle,
# it does not make sense to report the travel time to the Bainbridge terminal, since a ferry ride
# is required to get there).   We use a set of heuristics to filter out the unreasonable ones,
# and generally reduce the result to the minimal information the client needs.

# Terminals:  WSDOT code, name, location, [list of counties from which it makes sense to drive]
terminals = (
    (1, "Anacortes", (48.502220,-122.679455), ("Whatcom", "Skagit", "Snohomish", "King")),
    (3, "Bainbridge", (47.623046,-122.511377), ("Jefferson", "Kitsap", "Mason")),
    (4, "Bremerton", (47.564990,-122.627012), ("Mason","Kitsap","Thurston", "Pierce")),
    (5, "Clinton", (47.974785,-122.352139), ("Island")),
    (8, "Edmonds", (47.811240,-122.382631), ("Skagit", "Snohomish", "King")),
    (9, "Fauntleroy", (47.523115,-122.392952), ("Snohomish","King","Pierce")),
    (10, "Friday Harbor", (48.535010,-123.014645), ("San Juan")),
    (11, "Keystone", (48.160592,-122.674305), ("Island")),
    (12, "Kingston", (47.796943,-122.496785), ("Jefferson","Mason","Kitsap")),
    (13, "Lopez Island", (48.570447,-122.883646), ("San Juan")),
    (14, "Mukilteo", (47.947758,-122.304138), ("Skagit","Snohomish","King")),
    (15, "Orcas Island", (48.597971,-122.943985), ("San Juan")),
    (16, "Point Defiance", (47.305414,-122.514123), ("Thurston","Pierce","King")),
    (17, "Port Townsend", (48.112648,-122.760715), ("Clallam","Jefferson","Mason","Kitsap")),
    ( 7, "Seattle", (47.601767,-122.336089), ("Skagit","Snohomish","King","Pierce")),
    (18, "Shaw Island", (48.583991,-122.929351), ("San Juan")),
    (20, "Southworth", (47.512130,-122.500970), ("Jefferson","Mason","Thurston","Kitsap","Pierce")),
    (21, "Tahlequah", (47.333023,-122.506999), ("King")),
    (22, "Vashon Island", (47.508616,-122.464127), ("King"))   
)

def codeof(terminal):
    return terminal[0]

def nameof(terminal):
    return terminal[1]

def locationof(terminal):
    return terminal[2]

def drivingcounties(terminal):
    return terminal[3]

def reverselookup(datum):
    """Return the terminal corresponding to the supplied datum (which may be name or code)."""
    for term in terminals:
        if datum in term:
            return term
    logging.warn("Attempted to look up a non-existent terminal field! " + repr(datum))
    return None



def getTravelTimes(lat,lon):
    """Return a set of travel times from the given lat, lon position.
    The return value is a json-able object
    """
    
    ## Build the URL
    mqURIprefix = r'http://www.mapquestapi.com/directions/v1/routematrix?key=Fmjtd%7Cluuan9u12u%2Cas%3Do5-96rl0f'
    uri = '{0}&json={{locations:["{1},{2}"'.format(mqURIprefix,lat,lon)
    # add terminals that are within "reasonable range"
    keep = []
    for term in terminals:
        if closeEnough(lat,lon,locationof(term)):
            uri += ',"{0[0]},{0[1]}"'.format(locationof(term))
            keep.append(codeof(term))
        else:
            logging.debug("determined {} out of range", nameof(term))
    if len(keep) == 0:   # bail, nothing to do here
        logging.info("client too far away to estimate: {}, {}", lat, lon)
        return "";
    uri += "]}"
    logging.debug("mq url is: " + repr(uri))
    
    ## Get the response from MapQuest
    try:
        stream = urllib2.urlopen(uri)
        mqresponse = json.load(stream)
    except (IOError, HTTPError) as e:
        logging.warn("Access to mapquest failed: " + repr(e))
        return ""
    except ValueError as e:
        logging.error("error parsing mapquest response: " + repr(e))
        return ""
      
    ## Parse the response from MapQuest  
    # see http://www.mapquestapi.com/directions/#matrix for information on the result format   
    try:
        times = mqresponse["time"]
        distances = mqresponse["distance"]
        locations = mqresponse["locations"]
        clientloc = locations[0]
        clientcounty = clientloc["adminArea4"]
        clientcity = clientloc["adminArea5"]
    except (KeyError, IndexError):
        logging.error("Mapquest reponse format unexpected")
        logging.error(json.dumps(mqresponse))
        return ""
    logging.info("Client called from " + repr(clientcity) + " / " + repr(clientcounty))
    logging.debug("mq returned times: " + repr(times))
    
    ## Extract the bits we want and build our own response from that
    ourresponse = ""
    for i in range(0,len(keep)):
        term = reverselookup(keep[i])
        
        # determine what is on the same side of the water:
        # generally we use counties to determine reachability, but there are a few special cases:
        # Vashon is in King county, and Pierce reaches over to Gig Harbor
        # Other special cases we don't handle (yet):
        #    The San Juan Islands are not contiguous
        #    I haven't double checked that "Vashon" is the only city name on Vashon Island
        #    Or that there isn't another city in between / around Tacoma and Gig Harbor
        if nameof(term) in ["Vashon Island","Tahlequah"]:
            if clientcity != "Vashon":
                continue
        elif clientcity == "Vashon":
            if nameof(term) not in ["Vashon Island","Tahlequah"]:
                continue
        elif nameof(term) == "Southworth" and clientcounty == "Pierce":
            if clientcity not in ["Gig Harbor","Tacoma"]:
                continue
        elif clientcounty not in drivingcounties(term):
            continue            
        
        #if we get here, we want to return this value.
        ourresponse += "{}:{}\n".format(codeof(term), int(times[i+1]/60))
    
    ## Return our response
    return ourresponse

def closeEnough(lat1,lon1,loc2):
    """Return true if the two points are within approx 50 miles of each other
    This only measures x & y delta (not true distance)
    A degree latitude ~ 69 miles
    A degree longitude (at this latitude) ~ 47 miles
    """
    deltax = lat1 - loc2[0]
    deltay = lon1 - loc2[1]
    return -0.7 < deltax < 0.7 and -1.0 < deltay < 1.0

