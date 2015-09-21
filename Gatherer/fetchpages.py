#!/usr/bin/env python
#
# Get and parse the pages from the WSDOT site, in particular from it's "mobile"
# pages, which are much smaller and more succinct than the more general schedules.
# (Still all the errors that can come from scraping web pages can happen here, of course)
import urllib2
import re
import os
import logging

# The WSDOT ferry routes.  The tuples represent (route name, terminal1, terminal2)
# The name is our choice.  WSDOT only ever names routes by both
# endpoints (e.g. Seattle/Bainbridge).  The choice of all lower case for the
# name is because that is how they are displayed in the phone app (an egregious
# example of design decisions in the wrong place, I know).
# The terminals are constants used by WSDOT in their web pages (see table below)
# The order of the terminals in a route is significant --- we call end1->end2 a
# "westbound" route and end2->end1 an "eastbound" route.   This is accurate for all
# routes except pt. defiance which is north/south.
# We simplify the San Juan routes by only considering two pairs:  Anacortes/Friday Harbor
# and Anacortes/Orcas.  Shaw and Lopez are left out, as are all inter-island routes.

# If storepages is set, it must be a path name to a directory in which to put
# copies of the downloaded pages, for logging/debugging purposes
storepages = None
logger = logging.getLogger(__name__)

routes = [
    ("bainbridge", 7, 3, "w" ),
    ("bainbridge", 3, 7, "e" ),
    ("edmonds", 8, 12, "w" ),
    ("edmonds", 12, 8, "e" ),
    ("mukilteo", 14, 5, "w" ),
    ("mukilteo", 5, 14, "e" ),
    ("pt townsend", 11, 17, "w" ),
    ("pt townsend", 17, 11, "e" ),
    ("fauntleroy-southworth", 9, 20, "w" ),
    ("southworth-fauntleroy", 20, 9, "e" ),
    ("fauntleroy-vashon", 9, 22, "w" ),
    ("vashon-fauntleroy", 22, 9, "e" ),
    ("vashon-southworth", 22, 20, "w" ),
    ("southworth-vashon", 20, 22, "e" ),
    ("bremerton", 7, 4, "w" ),
    ("bremerton", 4, 7, "e" ),
    ("vashon-pt defiance", 21, 16, "w" ),
    ("pt defiance-vashon", 16, 21, "e" ),
    ("friday harbor", 1, 10, "w" ),
    ("friday harbor", 10, 1, "e" ),
    ("orcas", 1, 15, "w" ),
    ("orcas", 15, 1, "e" )
]

# translation of the constants:
#terminals = {
#    1:  'Anacortes',
#    3:  'Bainbridge',
#    4:  'Bremerton',
#    5:  'Clinton',
#    11: 'Coupville',
#    8:  'Edmonds',
#    9:  'Fauntleroy',
#    10: 'Friday Harbor',
#    12: 'Kingston',
#    13: 'Lopez Island',
#    14: 'Mukilteo',
#    15: 'Orcas Island',
#    16: 'Point Defiance',
#    17: 'Port Townsend',
#    7:  'Seattle',
#    18: 'Shaw Island',
#    20: 'Southworth',
#    21: 'Tahlequah',
#    22: 'Vashon Island'
#}

def fetch(fromterm, toterm, dow):
    """Read the WSDOT web page for this route and return the page as a string.
    fromterm and toterm are terminal identifiers as given above,
    and dow is the day of the week as a capitalized string.
    """
    uri = "http://www.wsdot.com/Ferries/Schedule/Small/ScheduleDetail.aspx?tripday={0}&departingterm={1}&arrivingterm={2}".format(dow,fromterm,toterm)
    page = urllib2.urlopen(uri).read()
    logpage(page,fromterm,toterm,dow)
    return page

def parse(text):
    """Parse the departure times from a WSDOT schedule page."""
    # See a page samples at the bottom of this source file
    # to keep this sort of robust, we don't actually parse the HTML, but just look for
    # the departure times with regular expressions.
    # We represent times by minutes past midnight (a simple and compact format).
    # WSDOT reports sequences of times up to and then past midnight, ala
    # 5:30AM, 6:30AM, ..., 10:50PM, 11:15PM, 12:10AM, 1:30AM
    # we want to turn this into a simple increasing sequence.
    # This means our time measures may be > 24*60.
    # To do this we first correctly calculate minutes past (the most recent previous)
    # midnight, then find the place in the sequence where that value decreases, and
    # add an extra 24 hours to the reported times after that.

    logger.debug("Beginning to parse page")
    aftermidnight = False
    last = 0
    times = []
    for m in re.finditer(r"(\d\d?):(\d\d) (A|P)M", text):
        hours = int(m.group(1))
        minutes = int(m.group(2))
        eve = (m.group(3) == 'P')
        if (hours == 12):
            hours = 0
        time = hours * 60 + minutes
        if (eve):
            time += 12*60
        if (not aftermidnight and time < last):
            aftermidnight = True
        if aftermidnight:
            time += 24*60
        last = time
        #print "convert " + m.group(0) + " --> " + str(time)
        times.append(time)
    logger.debug("Parse found %d times from %d to %d", len(times), times[0], times[-1])
    # return it as a csv string
    return ",".join(map(str,times))

def allschedules():
    """Return all schedules for our tracked WSDOT ferry routes as an array of strings"""
    result = [];
    template = "schedule('{name}','{direction}',{dow},'{times}'),\n"

    for (name, terma, termb, direction) in routes:
        for (dow,dname) in ((0,"Monday"),(1,"Tuesday"),(2,"Wednesday"),
                            (3,"Thursday"),(4,"Friday"),(5,"Saturday"),(6,"Sunday")):
            logger.info("fetching %s %s %s", name, direction, dname)
            times = parse(fetch(terma,termb,dname))
            result.append(template.format(name=name,direction=direction,dow=dow,times=times))
    return result


def logpage(text,fromterm,toterm,dow):
    if storepages <> None:
        filename = "{0}\q{1}_{2}_{3}.html".format(storepages,fromterm,toterm,dow)
        logger.info("Storing raw file %s", filename)
        with open(filename,"w") as f:
            f.write(text)

# page sample
"""
<html><body>
<form id="frmMobileSchedule" name="frmMobileSchedule" method="post" action="ScheduleDetail.aspx?__ufps=044919&tripday=Mon&departingterm=7&arrivingterm=3&pdaformat=true">
<input type="hidden" name="__VIEWSTATE" value="/wEXAQUDX19QD2QPBufDT29Vh8+IZqELY3k4/ybBHh7ti2iwXYhLgnPe">
<font color="Black">WSF Small Schedule<br>
Fall 2012<br>
9/23/2012 - 12/29/2012<br>



        <br>
 Fri, 11/2/2012<br>
Seattle to Bainbridge Island<br>
<a href="/Ferries/Schedule/Small/RouteAlerts.aspx?tripday=Friday&departingterm=7&arrivingterm=3&pdaformat=True">Alerts</a><br>
5:30 AM<br>
6:10 AM<br>
7:05 AM<br>
7:55 AM<br>
8:45 AM<br>
9:35 AM<br>
10:40 AM<br>
11:25 AM<br>
12:20 PM<br>
1:10 PM<br>
2:05 PM<br>
3:00 PM<br>
3:45 PM<br>
4:40 PM<br>
5:30 PM<br>
6:20 PM<br>
7:20 PM<br>
8:10 PM<br>
9:00 PM<br>
10:05 PM<br>
10:55 PM<br>
12:15 AM [11/3/2012]<br>
1:35 AM [11/3/2012]<br>


        <br>
 The Seattle-Bainbridge Island schedule is presented as a sailing day which begins with the first printed sailing time for that day and progresses consecutively through the last printed sailing time even though the last sailing may be past midnight and technically on the following day. Please pay special attention to annotations next to sailing times.<br>


        <br>
 <input name="cmdBack" type="submit" value="Back"/><br>


        <br>
 Additional Info 1-800-843-3779<br>

        WSDOT &#169; 2012</font></form></body></html>'
"""

# older page sample (try to keep the code able to handle both)
"""
<html><body>
<form id="frmMobileSchedule" name="frmMobileSchedule" method="post" action="ScheduleDetail.aspx?__ufps=489068&tripday=Thursday&departingterm=1&arrivingterm=10&pdaformat=true">
<input type="hidden" name="__VIEWSTATE" value="/wEXAQUDX19QD2QPBjMOZaW5gc+IAgjwBzW3kBOun6iKTbmYA6wHVV/wVA==">
<input type="hidden" name="__EVENTTARGET" value="">
<input type="hidden" name="__EVENTARGUMENT" value="">
<script language=javascript><!--
function __doPostBack(target, argument){
  var theform = document.frmMobileSchedule
  theform.__EVENTTARGET.value = target
  theform.__EVENTARGUMENT.value = argument
  theform.submit()
}
// -->
</script>
<font size="-1" color="Black" face="Arial"><b>WSF Small Schedule</b><br>
Fall 2012<br>
9/23/2012 - 12/29/2012<br>



        <br>
 <b>Thu, 11/1/2012<br>
Anacortes to Friday Harbor</b><br>
<a href="/Ferries/Schedule/Small/RouteAlerts.aspx?tripday=Thursday&departingterm=1&arrivingterm=10&pdaformat=True">Alerts</a><br>
<table>
<tr><td><font size="-1" color="Black" face="Arial">4:20 AM</font></font></td></tr>
<tr><td><font size="-1" color="Black" face="Arial">6:20 AM</font></td></tr>
<tr><td><font size="-1" color="Black" face="Arial">8:30 AM (1)</font></td></tr>
<tr><td><font size="-1" color="Black" face="Arial">9:30 AM</font></td></tr>
<tr><td><font size="-1" color="Black" face="Arial">2:40 PM</font></td></tr>
<tr><td><font size="-1" color="Black" face="Arial">4:30 PM</font></td></tr>
<tr><td><font size="-1" color="Black" face="Arial">6:00 PM</font></td></tr>
<tr><td><font size="-1" color="Black" face="Arial">8:25 PM</font></td></tr>
</table>
<table>
<tr><td><font size="-1" color="Black" face="Arial">(1) Priority for Sidney BC vehicles ticketed and in line no later than 8:00am</font></td></tr>
</table>
<font size="-1" color="Black" face="Arial">

        <br>
 Preservation projects will require temporary closures of the Orcas and Lopez terminals during September-October. For updated information and dates, visit:
www.wsdot.wa.gov/projects/sr20/orcastransferspan
www.wsdot.wa.gov/projects/sr20/lopeztrestlerehab<br>


        <br>
 <input name="cmdBack" type="submit" value="Back"/><br>


        <br>
 Additional Info 1-800-843-3779<br>

        WSDOT &#169; 2012<br>

                        <script type="text/javascript" src="http://www.wsdot.wa.gov/media/scripts/analytics.js"></script>
                    </font></form></body></html>
"""

#End of page samples
