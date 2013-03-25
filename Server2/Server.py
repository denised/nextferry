#!/usr/bin/env python
import re
import logging
import webapp2
from datetime import date
import CurrentSchedule
import MapQuestTT
import Alert

logging.getLogger().setLevel(logging.INFO)

# protocol:
# commit the server code.
# issue command "git describe --tags"
# put the result in to this string
# upload that
# grr. automate grr.
appversion = "V2x-10 unmodified: will send alerts, so backend only"
    
class GetInitUpdate(webapp2.RequestHandler):
    """
    The client calls init to get whatever information it might need.
    Currently we send new schedules and/or alerts, as needed.
    """
    def get(self, clientversion, year=None, month=None, day=None):
        self.response.headers['Content-Type'] = 'text/plain'
        if needschedule(year,month,day):
            self.response.out.write('#schedule {:%Y.%m.%d}\n'.format(date.today()))
            schedule = CurrentSchedule.text()
            if clientversion == '2.0':
                schedule = CurrentSchedule.v2interpolate(schedule)
            self.response.out.write(schedule)
        if CurrentSchedule.isHoliday():
            self.response.out.write("#special\n")
            schedule = CurrentSchedule.holidaySchedule()
            if clientversion == '2.0':
                schedule = CurrentSchedule.v2interpolate(schedule)
            self.response.out.write(schedule)
        if Alert.hasAlerts():
            self.response.out.write('#allalerts\n')
            for alert in Alert.allAlerts():
                self.response.out.write(str(alert))
            self.response.out.write('__\n')
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
    """
    Travel times are computed from the client's location to each of the ferry
    terminals.  We also return any alerts that have occurred in the last five minutes.
    """
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

        
class Version(webapp2.RequestHandler):
    def get(self):
        self.response.headers['Content-Type'] = 'text/plain'
        self.response.out.write('#version: |' + appversion + '|\n#done\n')

class DailyCleanup(webapp2.RequestHandler):
    def get(self):
        Alert.dailyCleanup()
        pass


app = webapp2.WSGIApplication(debug=True)
# note the notation {3,20} means between 3 and 20 charcaters.
# I'm adding it as a DOS defense, but I don't know if it is needed or not.
app.router.add((r'/init/(.{3,20}?)/(\d\d\d\d).(\d\d).(\d\d)', GetInitUpdate))
app.router.add((r'/init/(.{3,20}?)/', GetInitUpdate))
app.router.add((r'/traveltimes/(.{3,20}?)/([+-]?[\d.]{3,11}),([+-]?[\d.]{3,11})', GetTravelTimes))
app.router.add((r'/_ah/mail/alert@nextferry.appspotmail.com', Alert.NewAlertHandler))
app.router.add((r'/version',Version))
app.router.add((r'/tasks/dailycleanup',DailyCleanup))

if __name__ == '__main__':
    app.run()
