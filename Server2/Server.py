#!/usr/bin/env python
import re
import logging
import webapp2
from datetime import date
import CurrentSchedule
import MapQuestTT

logging.getLogger().setLevel(logging.INFO)
    
class GetInitUpdate(webapp2.RequestHandler):
    def get(self, clientversion, year=None, month=None, day=None):
        self.response.headers['Content-Type'] = 'text/plain'
        if needschedule(year,month,day):
            self.response.out.write('#schedule {:%Y.%m.%d}\n'.format(date.today()))
            self.response.out.write(CurrentSchedule.text())
        self.response.out.write('#done\n')

def needschedule(year,month,day):
    """
    Return true if the client needs a new version of the scheule.
    The date passed in is the date the client received it's version.
    If the version the client has is still good, return false
    Otherwise (including if the arguments are garbled or missing) return true
    """
    if year == None:
        return True
    try:
        return date(int(year),int(month),int(day)) < CurrentSchedule.mindate
    except (ValueError, TypeError):
        logging.warn('Garbled data version caught: %s/%s/%s', year, month, day)
        return True


class GetTravelTimes(webapp2.RequestHandler):
    def get(self, clientversion, lat, lon):         
        self.response.headers['Content-Type'] = 'text/plain'
        try:
            flat = float(lat)
            flon = float(lon)
        except (ValueError, TypeError):
            logging.error('GetTravelTime received bad args: %s, %s', lat, lon)
        else:
            self.response.out.write('#traveltimes\n')
            self.response.out.write(MapQuestTT.getTravelTimes(flat,flon))
        self.response.out.write('#done\n')


class IamAwake(webapp2.RequestHandler):
    def get(self):
        self.response.headers['Content-Type'] = 'text/plain'
        self.response.out.write('#I am awake\n')
        

app = webapp2.WSGIApplication(debug=True)
# note the notation {3,20} means between 3 and 20 charcaters.
# I'm adding it as a DOS defense, but I don't know if it is needed or not.
app.router.add((r'/init/(.{3,20}?)/(\d\d\d\d).(\d\d).(\d\d)', GetInitUpdate))
app.router.add((r'/init/(.{3,20}?)/', GetInitUpdate))
app.router.add((r'/traveltimes/(.{3,20}?)/([+-]?[\d.]{3,11}),([+-]?[\d.]{3,11})', GetTravelTimes))

if __name__ == '__main__':
    app.run()
