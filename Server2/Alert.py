#!/usr/bin/env python
import re
import logging
import webapp2
import email
import datetime as dt
from google.appengine.ext.webapp.mail_handlers import InboundMailHandler
from google.appengine.ext import db
import WSF
import pst
import unicodedata

class Alert(db.Model):
    body = db.TextProperty()      # alert content
    routes = db.IntegerProperty()   # routes affected by this alert
    posted = db.DateTimeProperty(auto_now_add=True)
    expires = db.DateTimeProperty()

    # Note: posted and expires are both stored as explicit UTC times, but all the calculations with
    # them are done in Pacific times --- hence the large amount of conversion in the code.

    def __str__(self):
        # not a general purpose format...
        localtime = pst.toPacific(self.posted).time()
        return "__ %s %d\n%s\n" % (str(localtime), self.routes, self.body)


def hasAlerts():
    return Alert.all().count(limit=1) > 0

def allAlerts():
    return Alert.all().run(limit=10)

def dailyCleanup():
    """ Remove all alerts that have expired """
    for a in db.Query(Alert).filter('expires <',dt.datetime.now(pst.utc)).run():
        logging.info("deleting " + str(a.posted))
        a.delete()


# Temporarily, at least, keep mail we error out on, so we can figure out what happened.
class BadMail(db.Model):
    content = db.TextProperty()

def postBadMail(message):
    nm = BadMail()
    nm.content = message.original.as_string(True)
    nm.put()


class NewAlertHandler(InboundMailHandler):

    # Me and WSDOT.
    KnownSenderList = ['Denise Draper <draperd@acm.org>',
                       'Washington State Ferries <WSFAlert@wsdot.wa.gov>']


    def receive(self, message):
        if message.sender not in NewAlertHandler.KnownSenderList:
            logging.warn("Ignoring message from unexpected sender |%s|", message.sender)
            postBadMail(message)
            return

        alert = self.parseMessage(message)
        if alert != None:
            alert.put();


    def parseMessage(self, message):
        """
        Rudimentary processing of the message: really all we are doing is extracting the route
        information and stripping off the boilerplate.

        Ideally we would *like* to be able to tell which sailings are affected, but the
        format of the messages are too irregular to do that robustly.
        """
        try:
            # get rid of initial string on subject line
            subject = re.sub('Ferry Alert: ', '', message.subject)
            msgbody = ""
            # there should only be one segment, but let's be proper about it
            for (btype, bcontent) in message.bodies('text/plain'):
                msgbody += re.sub('\r','',bcontent.decode())

            # divide the real content from the boilerplate
            # the boilerplate is set off by three (or more) newlines
            parts = re.split('\n\n\n+', msgbody)
            logging.debug("Email has %d parts", len(parts))

            # now extract out the route name from the boilerplate
            boilerplate = parts[-1]
            routes = 0
            for routestring in WSF.AlertStringCode.keys():
                if routestring in boilerplate:
                    routes |= WSF.AlertStringCode[routestring]
            if routes == 0:
                logging.error("Received message that did not match any routes")
                postBadMail(message)
                return None

            # so far so good.
            newAlert = Alert()
            body = "\n".join( [subject] + parts[:-1] )
            newAlert.body = unicodedata.normalize('NFKD', body).encode('ascii','ignore')
            newAlert.routes = routes
            # by default, alerts expire at 11pm the same day they are posted.
            # but note, we clean them out at 2am, so really they stay around until then.
            newAlert.expires = pst.toUTC( dt.datetime.combine( dt.datetime.now(pst.pacific), dt.time(23) ))

            # check to see if this is a special alert
            # if so, allow an explicit expiration date
            if "Denise's Override" in boilerplate:
                dtpat = re.compile('\d\d/\d\d/\d\d\d\d')
                dates = dtpat.findall(boilerplate)
                if len(dates) > 0:
                    try:
                        expires = dt.datetime.strptime(dates[0],"%m/%d/%Y")
                        newAlert.expires = pst.toUTC( dt.datetime.combine( expires, dt.time(23) ))
                    except:
                        logging.error("Bad parse of " + dates[0])

            return newAlert

        except ValueError as e:
            logging.error("error " + str(e))
            postBadMail(message)
            return None


"""
****************************************
Example full message:
****************************************
From:	Washington State Ferries <WSFAlert@wsdot.wa.gov>
Sent:	Monday, February 25, 2013 3:28 PM
To:	draperd@acm.org
Subject:    Ferry Alert: PT/Coup - 3:45pm from PT & 4:30pm from Coup Cancelled due to Inclement Weather.

The 3:45pm sailing from Port Townsend, and the 4:30pm from Coupville have been cancelled due
to high winds and rough seas. Updates will occur as conditions change.


This alert was sent on 2/25/2013 at 3:27PM to subscribers of the Port Townsend / Coupeville route.

Our Web Site is at http://www.wsdot.wa.gov/ferries

You can change your account, anytime, at: https://secure1.wsdot.wa.gov/ferries/account

Please send any comments or suggestions you may have to WSFAlert@wsdot.wa.gov

****************************************
Other bodies, without the boilerplate (last 4 lines)
****************************************

The Kennewick is back in service after weather cancellations with the 1:15 p.m. Coupeville to
Port Townsend departure.

****************************************

Due to necessary repairs to the Evergreen State, the 9:55 a.m. Lopez to Shaw, Orcas, and
Friday Harbor sailing is cancelled. Maintenance crews have been dispatched to address the
problem. The Chelan and the Sealth will make unscheduled stops on Lopez. Updates will
occur as more information becomes available.

****************************************
<<Note: in this case critical information is in the subject line, in addition to the body>>
Subject:	Ferry Alert: CLINTON / MUKILTEO - 12:30 AM SAILING DELAYED 28 MINUTES

The 12:30 AM departure from Clinton to Mukilteo has returned to Clinton due to a medical emergency.
This will affect the 1:05 AM departure from Mukilteo.

****************************************
<<This message arrived a day ahead.  However they also sent a follow-up reminder the next day>>
<<So probably we could let this message expire>>

On WEDNESDAY NIGHT, Jan. 16, the Tacoma will be taken out of service for scheduled required
maintenance.  This will cancel the 8:55pm Bainbridge Island departure and the 10:05pm Seattle
departure.  The Wenatchee will sail as scheduled for the remainder of the evening.

****************************************
<<This is the most problematic message, due to it's length.  But there was really
  only one like this, so I guess we'll treat it like any other>>

Due to repair needs on three vessels, ferry service  on the Seattle/Bremerton route will be
reduced for the week of Dec. 10-14.<p>

Seattle/Bremerton: The 124-car Kitsap and the 64-car Salish will provide service
supported by Kitsap Transit's passenger only ferry, Rich Passage 1 which carries
118 people. This PO ferry will provide two extra sailings during the morning and
evening commutes.  The slower Salish carries 750 passengers and requires a longer
transit so the 12:20 p.m. sailing from Bremerton and the 1:30 p.m. sailing from Seattle
will be canceled this week. The Kitsap Transit PO Ferry sails from Pier 50 in Seattle
and the passenger ferry terminal in Bremerton. PO sailings will be 6:20 a.m. and
7:55 a.m. from Seattle, 5:35 a.m. and 7:12 a.m. from Bremerton. Evening PO sailings
will be  5:07 p.m. and 6:43 p.m. from Seattle and 4:25 p.m. and 5:55 p.m. from Bremerton.<p>

Fauntleroy/Vashon/Southworth: This route will be on a two boat schedule with the
124 car Issaquah and the 87-car Tillikum. This service will be supplemented with
unscheduled sailings by the 34-car Hiyu.

****************************************
GRR, now they don't put route info in!
****************************************
ALL ROUTES: The Klahowya will be out of service on Monday, March 11 due to necessary
repairs to the vessel. Fauntleroy, Vashon and Southworth will be operating on a two
boat schedule. Updates will occur as more information becomes available.

This alert was sent on 3/10/2013 at 8:33PM.

Our Web Site is at http://www.wsdot.wa.gov/ferries

You can change your account, anytime, at: https://secure1.wsdot.wa.gov/ferries/account

Please send any comments or suggestions you may have to WSFAlert@wsdot.wa.gov
"""


