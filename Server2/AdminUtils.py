import logging
import sys
import inspect
from google.appengine.api import mail


logging.getLogger().setLevel(logging.INFO)

# Keep this updated to some reasonable string
# Usually it should be the first part of the output of 'git describe --tags'
# together with a verbal description.   Wish I could automate this...
appversion = "V4"

notifyfrom = "error@nextferry.appspotmail.com"
notifylist = ["draperd@acm.org"]
notifysubject = "NextFerry error"
notifybody = """Dear NextFerry Administrator:
Something bad is happening.  Please check it out asap.
%s
%s
"""


def handleError():
    """Respond to an unexpected exception, by logging information and sending email to denise"""
    try:
        err = str(sys.exc_info()[1])
        info = repr(inspect.getframeinfo(sys.exc_info()[2]))
        logging.error("Unexpected exception: %s\n%s", err)
        logging.error(info)
        for recipient in notifylist:
            mail.send_mail(notifyfrom,recipient,notifysubject,notifybody % (err,info))
    except:
        # we don't want recursiveness here, so minimally indicate a real problem
        logging.error("Recursive error %s, bailing", str(sys.exc_info()[1]))
    finally:
        #sys.exc_clear()
        pass
       