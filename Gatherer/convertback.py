#!/usr/bin/env python
# Convert our compressed time format back to readable times.
# Used for debugging.
# Input: any text
# Convertback will split each line on commas and convert any all-digit sequence into
# a readable time
import fileinput
import re

def convertback(str):
	if re.match("\s*\d{3,4}\s*",str):
		ampm = "am"
		x = int(str)
		hours = x // 60
		minutes = x % 60
		if hours > 24:
			hours -= 24
		if hours > 12:
			hours -= 12
			ampm = "pm"
		if hours == 0:
			hours = 12
		return "{0:>2}:{1:>02}{2}".format(hours,minutes,ampm)
	else:
		return str

def main():
	for line in fileinput.input():
		print ",".join([ convertback(x) for x in line.split(",") ])

if __name__ == '__main__':
    main()