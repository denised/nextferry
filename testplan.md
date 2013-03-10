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
  * [All] button to the corresponding RoutePage?
  * Application bar icon to ChooseRoutes?
  * Application bar icon to Settings?
  * Application bar icon to Help?
* Does the "back" button return to MainPage in each case?

## RoutePage

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
* Does the "Settings" navigation work?

## Choose Routes Page

* Does the full list of routes show?
* Is it possible to check and uncheck each route?
* Check all routes and return to main page.  Are all routes present?
* Uncheck all routes and return to main page.  Does everything still work (though screen is empty)?
* Check and uncheck a single route multiple times.  Verify that there are no strange behaviors when
  returning to MainPage

## Help Page

* Typos?

## Settings Page

* Does the 12/24 hour setting correctly change the display on both Main page and Route page?
* Try above when re-visiting a previously-visited Route Page
* Try above after navigating directly from Route Page, then returning with back arrow
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
  * Does a "loading schedules" dialog appear immediately?
  * Navigate (quickly) to a Route page.  Verify that nothing breaks (though boxes are empty).
  * Return to Main Page.  Verify that "no network connection" popup appears within a few minutes.
  * Exit the app, restart the internet connection, and re-enter the app.  Do the schedules now load?
* Go through above process (until the nonetwork popup is showing).  Exit and re-enter app without
  an internet connection.  Verify that nonetwork popup returns.

## Travel Times

## Logging

* Verify that with logging turned on, you can go to the Log page.
* Verify that with logging turned on, no other changes are visible in the app (the original logging
  code would put stuff on all pages).
* Verify that clearing and emailing the log files works.
* Verify that after clearing the log, the size on disk does actually go back down (how to do this)?

## Application Life Cycle

* For each page type, use the Windows Phone button to start a new application.  Verify that the back
  button returns to the same location in the app, and that everything works (no data is lost).
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
* Verify that changing the light/dark theme and the theme colors produces correct behavior (specifically
  on Route Pages and highlighting on MainPage).
* Verify that a phone call occuring while the NextFerry app is open works: the phone call goes through
  and the app returns correctly afterwords.
