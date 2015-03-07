
Operations
==========

The ferry schedule is maintained manually.  It is updated four times per year.
To update the schedule, do the following:

* Wait until the first day of the new schedule!  The API we use can only fetch current schedule times.
* Run Gatherer/managecaches.py.  It automatically downloads all the day schedules and puts the resulting data in a file in ../Cache/
* Edit CurrentSchedule.py, and make the following changes:
**  Set mindate to the first date of this schedule period.
**  Set the schedule name to the proper name of the current schedule (as seen on wsdot.gov)
**  Replace the schedule content with the newly downloaded content.
**  Update the holiday schedules:
*** Find out the holiday schedule dates for this schedule, and which routes are affected.
*** Set the holidays at the bottom of CurrentSchedule.py.
*** For each holiday route, copy the weekend schedule times to the holiday schedule and replace the "e" (weekend designation) with an "s" (special schedule designation).
* Upload the app.   (No version change just for a schedule change.)
* Check that the new schedule times actually appear.
