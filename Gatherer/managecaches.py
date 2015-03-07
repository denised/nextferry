#!/usr/bin/env python
#
# Create a new ferry schedule
#
# Various things are done with caching raw text, etc., but the really important bit is
# writing a new cache*.txt file with the new schedule in it.
#
# This code should be run just after a new schedule comes online.
#

import os
import re
import glob
import datetime
import time
import logging
import stat
import fetchpages

cachdir = "D:\\Projects\\NextFerry\\Cache\\"
fetchpages.storepages = "D:\\Projects\\NextFerry\\Cache\\Raw"

logging.captureWarnings(True)
logging.basicConfig(filename='D:\\Projects\NextFerry\\Cache\\Log\\Gatherer.log',\
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
        logger.info("WSDOT schedules cover %s", datepat.group(0))
        return datetime.date(int(datepat.group(3)),int(datepat.group(1)),int(datepat.group(2)))
    else:
        logger.error("Unable to obtain expiration from WSDOT")
        logger.error(text)
        raise RuntimeError("Unable to obtain expiration from WSDOT")

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

def main():
    try:
        candidate = fetchpages.allschedules()
        install(candidate,getwsdotexpiration())
    except Exception as e:
        logger.exception("Exception raised %s", repr(e))


if __name__ == '__main__':
    main()





