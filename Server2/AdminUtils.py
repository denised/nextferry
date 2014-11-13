import logging
import sys
import traceback
import re
from google.appengine.api import mail
from google.appengine.api.logservice import logservice

logging.getLogger().setLevel(logging.INFO)

# Keep this updated to some reasonable string
# Usually it should match the version in the app.yaml file
appversion = "V6 removed restrictions on geo length"

notifyfrom = "error@nextferry.appspotmail.com"
notifylist = ["draperd@acm.org"]
notifysubject = "NextFerry error"
notifybody = """Dear NextFerry Administrator:
Something bad is happening.  Please check it out asap.
"""


def handleError():
    """Respond to an unexpected exception, by logging information and sending email to denise"""
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


def readclienthistory(stream):
    """Write client access data from logs"""
    # format: time, req-type, ip, lat, long
    # where req-type = {1=new_init, 2=re_init, 3=travel_time}
    # and lat, long are empty unless this is a travel request
    for record in logservice.fetch():
        request_type = record.resource # URL string

        if '/init' in request_type:
            isnew = (1 if re.search('/[0-9]',request_type) == None else 2)
            stream.write("%d, %d, %s,,\n" % (record.start_time, isnew, record.ip))
        elif '/travel' in request_type:
            mob = re.search('/([0-9.-]+),([0-9.-]+)',request_type)
            (l1,l2) = ((mob.group(1),mob.group(2)) if mob != None else ('',''))
            stream.write("%d, 3, %s, %s, %s\n" % (record.start_time, record.ip, l1, l2))
        else:
            pass # ignore all other message types

