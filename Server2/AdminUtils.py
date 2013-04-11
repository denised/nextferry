import logging
import sys
import traceback
from google.appengine.api import mail


logging.getLogger().setLevel(logging.INFO)

# Keep this updated to some reasonable string
# Usually it should be the first part of the output of 'git describe --tags'
# together with a verbal description.   Wish I could automate this...
appversion = "tag V3-3 (app V4 first bug fix)"

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
       
