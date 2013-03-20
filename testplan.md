<!-- vim: set filetype=markdown : -->

Server-Client Interaction
=========================

Test in this order:
* Check that new client works with new server in emulation mode
* During downtime (e.g. at night), upload new server.  Check that old client (on phone) works with
  new server.
* Check that old client updates to new client
* Check that new client works with new server

Unfortunately, there appears to be no way to test upgrades of ApplicationSettings (there are tools
that work for Windows 8/Windows Phone 8 only).

Test Cases for Client
=====================

## Upgrade

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
* Does the "East" / "West" text fade in when the toggle is used?
* Does each navigation item on MainPage take you to the appropriate page?
  * [i][!] button to the corresponding RoutePage?
  * Application bar icon to ChooseRoutes?
  * Application bar icon to Settings?
  * Application bar icon to Info?
* Does the "back" button return to MainPage in each case?

## RoutePage / AlertsPage

* Each route has it's own unique page.  Verify that sequences of selecting route pages
  produce the correct route page.
* Verify that re-visiting the same route page works.
* ...Interleaved with other route pages.
* Verify that re-visiting a route page works if MainPage has been switched between East & West
  (especially for routes whose names change).

* Does Route Page look correct?
* Verify that the correct times are in the correct boxes for at least one Route Page
* Verify that every Route Page has content in every box (unless legitimately empty), and
  that the formatting looks correct in every case.
* Does Route Page scroll vertically when content is longer than screen?

## Choose Routes Page

* Does the full list of routes show?
* Is it possible to check and uncheck each route?
* Check all routes and return to main page.  Are all routes present?
* Uncheck all routes and return to main page.  Does everything still work (though screen is empty)?
* Check and uncheck a single route multiple times.  Verify that there are no strange behaviors when
  returning to MainPage

## Info Page

* Typos?

## Settings Page

* Does the 12/24 hour setting correctly change the display on both Main page and Route page?
* Try above when re-visiting a previously-visited Route Page
* Does the buffer time slider work properly?
* Does the Location on/off toggle work properly?
* Does a double-tap on the blank part of the screen bring up the Debug toggle and link?
* Make changes, exit the app, re-enter and verify that the changes persisted.
(Refresh schedules handled below).

## Loading Schedules

* Does the "Refresh Schedules" button cause the explanatory text to change to reload app?
* With an internet connection, click "Refresh Schedules", and exit and re-enter app.  Do schedules
  load quickly?
* Without an internet connection, click, "Refresh Schedules", and exit and re-enter app.
  * Does a "downloading..." dialog appear almost immediately?
  * Navigate (quickly) to a Route page.  Verify that nothing breaks (though boxes are empty).
  * Return to Main Page.  Verify that "no network connection" popup appears within a few minutes.
  * Exit the app, restart the internet connection, and re-enter the app.  Do the schedules now load?
* Go through above process (until the nonetwork popup is showing).  Exit and re-enter app without
  an internet connection.  Verify that nonetwork popup returns.

## Special Schedules

* Engineer a special schedule to be sent to the test app only.  Verify that the app can receive
  and display a special schedule that affects one or multiple routes.
* Verify that the special schedule is not cached by turning the special schedule off on the server
  and restarting the app.  The special schedule should be gone.

## Travel Times

* Basically, make sure they look right.   Drive around with the app and observe changing impact on
  travel times.
* Verify that setting buffer time has the impact you expect on the travel times.
* Verify that waiting for travel times messages display appropriately: appear after a few seconds of
  waiting when there is no network access, and then turn off again when travel times are delivered.

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

## Logging & Bugsense

* Verify that with logging turned on, you can go to the Log page.
* Verify that with logging turned on, no other changes are visible in the app (the original logging
  code would put stuff on all pages).
* Verify that clearing and emailing the log files works.
* Verify that a thrown exception is caught by BugSense and displayed on the BugSense console.

## Application Life Cycle

* For each page type, use the Windows Phone button to start a new application.  Verify that the back
  button returns to the same location in the app, and that everything works (no data is lost).
  Especially do this for the Route/Alert page and verify that back returns to where the user left, and
  that back again returns to the main page.
* Do the same process in the emulator with the "tombstone on exit" option set, to verify the same
  behavior when the application gets tombstoned.
* With location and logging both on, from the Main Page use the Windows Phone button to start a new
  application.  Use the phone "lightly" (only small applications, so the NextFerry app is *not* 
  tombstoned).  Leave the phone in this state for some time.  Return to the NextFerry app and verify
  that it has not been working (the log contains no notices of timer or other activity).   This is 
  not a completely accurate test, since the log itself may have been disabled, but I don't have
  any better ideas.

## WP7 Integration
* Verify that the tile looks correct, both in the Applications list, and if placed on the main screen.
* Verify that changing the light/dark theme and the theme colors produces correct behavior (especially
  on Route Pages and highlighting on MainPage).
* Verify that a phone call occuring while the NextFerry app is open works: the phone call goes through
  and the app returns correctly afterwords.
