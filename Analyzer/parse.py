#!/usr/bin/env python
import sys
import re
import glob
import email
import email.utils as utils
import email.iterators as iter
import csv
import StringIO
import datetime as dt
import pst

fields = ["ReqID", "Date", "Time","DOW","Type","NFVersion","ID","Lat","Long"]

def transcodefiles(pattern):
	"""Convert a set of eml files to csv output"""
	output = csv.DictWriter(sys.stdout, fields, lineterminator="\n")
	for file in glob.glob(pattern):
		with open( file, "r" ) as f:
			# get date header, and message body
			mail = email.message_from_file(f)
			date = utils.mktime_tz(utils.parsedate_tz(mail.get("Date")))
			datecode = date / (60 * 60 * 24 * 5) # unique per file over all time
			body = mail.get_payload()

			# Truncate the summary data at the end
			body = body[:body.find("Accesses")]

			# for whatever @#$#* reason, something inserts a newline in front of lat/long data sometimes.
			# remove those newlines
			pattern = re.compile(r"\n([\d.,\-]+)\n")
			body = pattern.sub(r"\1\n", body)

			# we also have spaces around commas, which can be problematic
			body = body.replace(" ,", ",")
			body = body.replace(", ", ",")

			# now use a dictreader to go through and do the following changes:
			#    change the timestamp to local time
			#    split out date, day of week and time of day into separate fields
			#    concatenate the datecode to the id, to make them unique across files
			#	 add a field for the request id to make this line unique
			# the result is printed to stdout, to be redirected appropriately
			reader = csv.DictReader(StringIO.StringIO(body))
			i = 1
			for row in reader:
				rowtime = pst.toPacific(dt.datetime.strptime(row["Time"],"%Y-%m-%d %X %a"))
				row["Date"] = dt.datetime.strftime(rowtime,"%Y-%m-%d")
				row["Time"] = dt.datetime.strftime(rowtime, "%X")
				row["DOW"] = dt.datetime.strftime(rowtime, "%w")
				row["ID"] = "{}_{}".format(datecode,row["ID"])
				row["ReqID"] = "{}_{:03}".format(datecode,i)
				output.writerow(row)
				i = i+1

def main():
	pattern = "./*.eml"
	if len(sys.argv) > 1:
		pattern = sys.argv[1]
	transcodefiles(pattern)

if __name__ == '__main__':
    main()
