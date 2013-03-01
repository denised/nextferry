#!/usr/bin/env python
import urllib2
import logging
import json
import WSF

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

def getTravelTimes(lat,lon):
    """Return a set of travel times from the given lat, lon position.
    The return value is a json-able object
    """
    
    ## Build the URL
    mqURIprefix = r'http://www.mapquestapi.com/directions/v1/routematrix?key=Fmjtd%7Cluuan9u12u%2Cas%3Do5-96rl0f'
    uri = '{0}&json={{locations:["{1},{2}"'.format(mqURIprefix,lat,lon)
    # add terminals that are within "reasonable range"
    keep = []
    for term in WSF.Terminals:
        if closeEnough(lat,lon,term.location):
            uri += ',"{0[0]},{0[1]}"'.format(term.location)
            keep.append(term.code)
        else:
            logging.debug("determined %s out of range", term.name)
    if len(keep) == 0:   # bail, nothing to do here
        logging.info("client too far away to estimate: %s, %s", lat, lon)
        return "";
    uri += "]}"
    logging.debug("mq url is: " + repr(uri))
    
    ## Get the response from MapQuest
    try:
        stream = urllib2.urlopen(uri)
        mqresponse = json.load(stream)
    except urllib2.URLError as e:
        logging.warn("Access to mapquest failed: " + e.reason)
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
        term = WSF.terminal.lookup(keep[i])
        
        # determine what is on the same side of the water:
        # generally we use counties to determine reachability, but there are a few special cases:
        # Vashon is in King county, and Pierce reaches over to Gig Harbor
        # Other special cases we don't handle (yet):
        #    The San Juan Islands are not contiguous
        #    I haven't double checked that "Vashon" is the only city name on Vashon Island
        #    Or that there isn't another city in between / around Tacoma and Gig Harbor
        if term.name in ["Vashon Island","Tahlequah"]:
            if clientcity != "Vashon":
                continue
        elif clientcity == "Vashon":
            if term.name not in ["Vashon Island","Tahlequah"]:
                continue
        elif term.name == "Southworth" and clientcounty == "Pierce":
            if clientcity not in ["Gig Harbor","Tacoma"]:
                continue
        elif clientcounty not in term.counties:
            continue            
        
        #if we get here, we want to return this value.
        ourresponse += "{}:{}\n".format(term.code, int(times[i+1]/60))
    
    ## Return our response
    return ourresponse

def closeEnough(lat1,lon1,loc2):
    """Return true if the two points are within approx 40 miles of each other
    This only measures x & y delta (not true distance)
    A degree latitude ~ 69 miles
    A degree longitude (at this latitude) ~ 47 miles
    """
    deltax = lat1 - loc2[0]
    deltay = lon1 - loc2[1]
    return -0.6 < deltax < 0.6 and -0.9 < deltay < 0.9

