import logging
import sys
import traceback
import re
import StringIO
from datetime import date, timedelta, datetime
from google.appengine.api import mail
from google.appengine.api.logservice import logservice

logging.getLogger().setLevel(logging.INFO)

# Keep this updated to some reasonable string
# Usually it should match the version in the app.yaml file
appversion = "V9 Redo log/stat processing"

notifyfrom = "error@nextferry.appspotmail.com"
notifylist = ["draperd@acm.org"]


def handleError():
    """Respond to an unexpected exception, by logging information and sending email to denise"""

    notifysubject = "NextFerry Error"
    notifybody = """Dear NextFerry Administrator:
    Something bad is happening.  Please check it out asap.
    """

    try:
        logging.error("NextFerry exception: %s\n%s",
                      str(sys.exc_info()[1]),
                      repr(traceback.extract_tb(sys.exc_info()[2])))
        for recipient in notifylist:
            mail.send_mail(notifyfrom,recipient,notifysubject, notifybody + traceback.format_exc())
    except:
        # we don't want recursiveness here, so minimally indicate a real problem
        logging.error("Recursive error %s, bailing", str(sys.exc_info()[1]))
    finally:
        #sys.exc_clear()
        pass


def mailstats():
    mailbody = dologs()
    for recipient in notifylist:
            mail.send_mail(notifyfrom,recipient,"NextFerry Stats",mailbody)


# anonymize ip addresses
idcount = 1
ipmap = {}
def getId(ip):
    global idcount
    if not (ip in ipmap):
        ipmap[ip] = idcount
        idcount+=1
    return ipmap[ip]

def dologs():
    """Process the last weeks worth of logs, build a digested version"""
    # format: time, req-type, version, client, id, err, lat, long
    # where
    #    time = time of the call
    #    type = new (a new init), call (a repeat call), travel (a travel time request)
    #    version = client version of nextquery
    #    id = a unique id (for tracking multiple use by clients, without recording ip address, etc.)
    #    lat, long = only present if this is a travel request.
    weekago = totimestamp(datetime.now() - timedelta(days=7))
    countaccess = 0
    counterror = 0
    countnew = 0
    output = StringIO.StringIO()

    print >> output, "Time,Type,NFVersion,ID,Lat,Long\n"

    for record in logservice.fetch(start_time=weekago):
        urlbits = record.resource.split('/')

        if urlbits[1] in ("_ah", "tasks"):
            continue # we don't care about these

        calltype = "other"
        if urlbits[1] == "init":
            calltype = ("new" if len(urlbits) == 3 else "call")
        elif urlbits[1] == "traveltimes":
            calltype = "travel"

        print >>output, datetime.fromtimestamp(record.start_time), ",", calltype, ",",
        print >>output, (urlbits[2] if len(urlbits)>2 else ""), ",", getId(record.ip),
        if calltype == "travel":
            print >>output, ",", (urlbits[3] if len(urlbits)>3 else "")
        else:
            print >>output, ",,"

        if calltype == "new":
            countnew+=1
        if calltype != "other":
            countaccess+=1

    for record in logservice.fetch(start_time=weekago,minimum_log_level=logservice.LOG_LEVEL_WARNING):
        urlbits = record.resource.split('/')

        if urlbits[1] in ("init","traveltimes","tasks"):
            counterror+=1

    print >> output, "\nAccesses: ", countaccess
    print >> output, "Unique: ", len(ipmap.keys())
    print >> output, "New: ", countnew
    print >> output, "Errors: ", counterror

    return output.getvalue()

# gag
def totimestamp(dt, epoch=datetime(1970,1,1)):
    td = dt - epoch
    # return td.total_seconds()
    return (td.microseconds + (td.seconds + td.days * 24 * 3600) * 10**6) / 1e6
