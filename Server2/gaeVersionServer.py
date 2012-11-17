#!/usr/bin/env python
import re
import logging
import webapp2
from datetime import date
import CurrentSchedule

#logging.captureWarnings(True)
#logging.basicConfig(filename='D:\NextFerry\Logs\server.log',\
#                format='%(process)d|%(asctime)s:%(msecs)04d|s%(levelno)s|%(name)s|    %(message)s',\
#                datefmt='%Y %b %d %H:%M:%S',\
#                level=logging.DEBUG)
#logger = logging.getLogger(__name__)  
    
class GetSchedule(webapp2.RequestHandler):
    def get(self, clientversion, year=None, month=None, day=None):
        self.response.headers['Content-Type'] = 'text/plain'
        if needschedule(year,month,day):
           self.response.out.write(CurrentSchedule.text())
        else:
           self.response.out.write("//No Changes Required\n")

def needschedule(year,month,day):
    """
    Return true if the client needs a new version of the scheule.
    The date passed in is the date of the clients version.
    If the version the client has is still good, return false
    Otherwise (including if the arguments are garbled or missing) return true
    """
    if year == None:
        return True
    try:
        return date(int(year),int(month),int(day)) < CurrentSchedule.mindate
    except (ValueError, TypeError):
        #logger.warn('  Garbled data version caught: "' + year + '/' + month + '/' + day + '"')
        return True
 

app = webapp2.WSGIApplication(debug=True)
app.router.add((r'/schedule/(.+?)/(\d\d\d\d).(\d\d).(\d\d)', GetSchedule))
app.router.add((r'/schedule/(.+?)/', GetSchedule))

if __name__ == '__main__':
    app.run()
