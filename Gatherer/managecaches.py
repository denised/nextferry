#!/usr/bin/env python
#
# Manaage the cached ferry schedules
#
# Caches are kept in a fixed directory and they are named nfcache[yyyy_mm_dd]_[id].txt
# The one with the highest name (that is, latest date and highest id number) is the
# currently active cache.
#
# The code in this file handles the following:
#   1.  Do we need a new cache (there is none, or it is expired)?
#   2.  If so, try to create a new one.
#   3.  Does the new one smell good?
#   4.  If so, install the new one
#   ### 5.  And send it to the server.   TODO
#
# Various things are checked and logged, and suspicious things cause notifications.
#
import os
import re
import glob
import datetime
import time
import logging
import stat
import fetchpages
 
cachdir = "D:\\NextFerry\\Cache\\"
fetchpages.storepages = "D:\\NextFerry\\Cache\\Raw"

logging.captureWarnings(True)
logging.basicConfig(filename='D:\\NextFerry\\Cache\\Log\\Gatherer.log',\
                format='%(process)d:%(name)s:%(levelno)s:    %(message)s',\
                level=logging.DEBUG)
logger = logging.getLogger(__name__)


def getwsdotexpiration():
    """Return the current schedule expiration date as a date.
    
    The date is retrieved from one of the schedules on the WSDOT web site."""
    text = fetchpages.fetch(7,3,"Mon")
    # the schedule has the range of dates  [from] - [to]
    # we extract the 2nd one.
    datepat = re.search(r"\d\d?/\d\d?/\d\d\d\d +- +(\d\d?)/(\d\d?)/(\d\d\d\d)",text)
    if datepat != None:
        logging.info("WSDOT schedules expire at %s", datepat.group(0))
        return datetime.date(int(datepat.group(3)),int(datepat.group(1)),int(datepat.group(2)))
    else:
        logging.error("Unable to obtain expiration from WSDOT")
        logging.error(text)
        raise RuntimeError("Unable to obtain expiration from WSDOT")


def getcurrentcache():
    """Return the filename of the current cache, or None if there is no cache"""
    files = glob.glob(cachdir + r"cache*.txt")
    if len(files) > 0:
        return max(files)
    else:
        logger.info("No cache found")

def getcachedexpiration(filename):
    """Extract the expiration date from the filename, and return it as a date"""
    match = re.search("(\d\d\d\d)_(\d\d)_(\d\d)",filename)
    expiration = datetime.date(match.group(1),match.group(2),match.group(3))
    logger.info("Found cache for " + expiration)
    return expiration

def install(candidate, expdate):
    """Store the candidate as the current cache"""
    filename = "{}\cache_{:%Y_%m_%d}.txt".format(cachdir,expdate)
    logger.info("Creating cache file %s", filename)
    with open(filename,"w") as f:
            for line in candidate:
                f.write(line + "\n")
    confirm = os.stat(filename)
    logger.info("Created: %s, %d bytes", time.ctime(confirm.st_mtime), confirm.st_size )

# Main
# Note that the only exception catching occurs here.  Basically, we don't recover from any
# errors, we simply note that they occurred and abort.   Since this code needs to
# run four times a year, manual recovery is sufficient.

def main(force=False):
    bestcache = None
    try:
        bestcache = getcurrentcache()
        if not force and bestcache <> None and getcachedexpiration(bestcache) >= datetime.date.today():
            logger.info("Current cache still good; exiting")
        else:
            # we need a new cache
            logger.info("Start: create a new cache")
            candidate = fetchpages.allschedules()
            #sanitysniff(candidate)
            #if bestcache <> None:
            #    sanitydiff(bestcache,candidate)
            #logger.info("Current candidate passes; installing")
            install(candidate,getwsdotexpiration())
            #logger.info("Candidate installation successful; exiting")
    except Exception as e:
        logger.exception("Exception raised %s", repr(e))

    
if __name__ == '__main__':
    main()


    


