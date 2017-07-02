
Operations
==========

The ferry schedule is maintained manually.  It is updated four times per year.
To update the schedule, do the following:

* Wait until the first day of the new schedule!  The API we use can only fetch current schedule times.
* Run Gatherer/managecaches.py.  It automatically downloads all the day schedules and puts the resulting data in a file in ../Cache/
* Edit CurrentSchedule.py, and make the following changes:
**  Set mindate to the first date of this schedule period, or to the date the schedule is actually uploaded, if that is later (shame!)
**  Set the schedule name to the proper name of the current schedule (as seen on wsdot.gov)
**  Set the holidays and affected routes
**  Replace the schedule content with the newly downloaded content.  Be sure to remove the trailing ","
* Upload the app.   (No version change just for a schedule change.)
* If there was a version change, you will need to go to the app dashboard and make the new version the default.
* Check that the new schedule times actually appear.

Running and Deploying
=====================

We have now switched to using gcloud.  To run locally:
* Open google cloud console
* `Cd <this directory>`
* `dev_appserver.py .`

To deploy
* `gcloud app deploy --project nextferry --version <version> [--no-promote] `
Notes:
The `no-promote` flag prevents a new version from becoming the default version automatically.
If the `--version` flag is omitted, a new date-based version is created automatically.  I think we'll go with this approach from now on.
