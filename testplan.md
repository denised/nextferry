<!-- vim: set filetype=markdown : -->

# Manual Test Plan for Nextferry server update

Update at night.
Check that these commands work and return sensible results:

    curl http://nextferry.appspot.com/version
    curl http://nextferry.appspot.com/init/2.0/
    curl http://server.nextferry.appspot.com/init/3.0/
    curl http://server.nextferry.appspot.com/init/3.0/yyyy.mm.dd
    # above should return empty if the date is above min date for current schedule
    # and return a full schedule otherwise
    curl http://nextferry.appspot.com/traveltimes/3.0/47.590417,-122.331688
    curl http://nextferry.appspot.com/getlogs

To test the alert mechanism, resend an old alert to alert@nextferry.appspotmail.com, then do one of the init's above and verify that the alert is included.  Then
manually remove it from the alert DB via the appengine console.   (Note: only send from draperd@acm.org; other mail sources will be rejected.)

# Manual Test Plan for NextFerry client update

Note the client also has automated tests that should be run first.  The following tests are mostly run on
the actual device.

## Client Server dual-update

If the update involves changes to both client and server, use this section.
If it only involves the client, skip to next section.

Test in this order:
* Check that new client works with new server in emulation mode
* During downtime (e.g. at night), upload new server.  Do server tests as above.
* Check that old client (on phone) works with new server.
* Do the Client Upgrade test, below.

## Client Upgrade

This comes first since you often only get a single shot to do it easily.

* Install application on phone that has existing application, with cache.

  * Does the cache work (are the schedules shown on MainPage) or, if appropriate, are new schedules downloaded?
  * Does the list of displayed routes match the previous settings?
  * Are other settings (Settings Page) preserved?
  * Does the URL sent by the client show the new version number (use log to verify)?

## Clean Install

We typically don't test this specially, because it gets tested automatically when using
the emulator.

## MainPage

* Does the application start with MainPage?  Does it look right?
* Do departure times scroll horizontally?
* Does the East / West toggle work?
* Does each navigation item on MainPage take you to the appropriate page?
  * Route name to the corresponding RoutePage?
  * Application bar icon to ChooseRoutes?
  * Application bar icon to Settings?
  * Application bar icon to Info?
* Does the "back" button return to MainPage in each case?

## RoutePage / AlertsPage

* Each route has it's own unique page.  Verify that sequences of selecting route pages
  produce the correct route page.
* Verify that re-visiting the same route page works.
* ...Interleaved with other route pages.
* Does Route Page look correct?
* Verify that the correct times are in the correct boxes for at least one Route Page
* Does Route Page scroll vertically when content is longer than screen?

## Choose Routes Page

* Does the full list of routes show?
* Is it possible to check and uncheck each route?
* Check all routes and return to main page.  Are all routes present?
* Uncheck all routes and return to main page.  Does everything still work (though screen is empty)?
* Check and uncheck a single route multiple times.  Verify that there are no strange behaviors when
  returning to MainPage

## Info Page

* Does the current schedule name display?
* Turn travel times settings on and off and verify that the info page message follows

## Settings Page

* Does the 12/24 hour setting correctly change the display on both Main page and Route page?
* Try above when re-visiting a previously-visited Route Page
* Does the buffer time slider work properly?
* Does the Location on/off toggle work properly?
* Make changes, exit the app, re-enter and verify that the changes persisted.

## Loading Schedules

* With an internet connection, click "Refresh Schedules", and exit and re-enter app.  Do schedules
  load quickly?
* Without an internet connection, all schedule pages will be blank, with no explanation.  This is
  a known issue with the current implementation.

## Special Schedules

* Engineer a special schedule to be sent to the test app only.  Verify that the app can receive
  and display a special schedule that affects one or multiple routes.
* Verify that the special schedule is not cached by turning the special schedule off on the server
  and restarting the app.  The special schedule should be gone.

## Travel Times

* Basically, make sure they look right.   Drive around with the app and observe changing impact on
  travel times.
* Verify that setting buffer time has the impact you expect on the travel times.

## Alerts

* Engineer special alert(s) to be sent to the test app only.  Verify that the app can receive and
  display them.  Verify that the app can display multiple alerts for the same route.
* Verify that new alerts show as unread (icon on Mainpage and "new" text on alert itself).
* Verify that once a RouteAlert page has been visited, those alerts are no longer marked unread.
* Special case: verify that an alert that affects multiple routes is marked as read on all those routes.
* Verify that alert caching works:  after alerts have been received, restart the application without
  network access.  Verity that the previous set of alerts are still present.
* Verify that alert update works: change the alerts sent to include new alerts and remove some old ones.
  Verify that the app shows exclusively the new set of alerts (old ones are gone).  Verify that only
  the new alerts are marked unread.

## Application Life Cycle

* For Android and WP, verify that the back button does the same thing as swipe left, and as a final step,
  exits the app.
* For all platforms, verify that directly exiting the app then returning returns you to the same page
  that you left (unless the app has been kicked out, in which case it should go to the main page).
