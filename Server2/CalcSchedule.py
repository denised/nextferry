#!/usr/bin/env python
from datetime import date
import CurrentSchedule

def getSchedule(version):
	"""Return our 'canonical' schedule which shows weekday/weekend times"""
	# pull out the Sunday and Monday schedules as our canonical examples
	slist = [ x for x in CurrentSchedule.CurrentSchedule if x.dow == 0 or x.dow == 6 ]
	return textify(versionify(slist, version), True)


def getSpecial(version):
	"""Return a schedule just for today.  Originally this feature only returned special schedules
	on holidays, but now we return today's schedule every day, which allows us to correctly show times
	for non-canonical days."""
	# Further note: we could check whether today's schedule really is special (is different from the canonical)
	# but why bother?  We just return it.

	dow = date.today().weekday()
	holidayRoutes = CurrentSchedule.holidayRoutes()
	specialList = []
	for x in CurrentSchedule.CurrentSchedule:
		if x.name in holidayRoutes:  # for holiday routes, use the Sunday schedule
			if x.dow == 6:
				specialList.append(x)
		elif x.dow == dow:
			specialList.append(x)

	return textify(versionify(specialList, version), False)


def versionify(list,version):
    """Correct schedule details for different client versions"""
    if version == "1.0":
        return list
    elif version == "2.0":
        return v2interpolate(list)
    elif version == "3.0":
        return list
    else: # V4 and higher
        return v4plus(list)

def v2interpolate(list):
    """V2 clients leave off the first two times, so put dummy ones in"""
    return [ CurrentSchedule.schedule(x.name,x.direction,x.dow, "200,201," + x.text) for x in list ]

def v4plus(list):
    """At V4 we turned the direction of pt defiance around"""
    newlist = []
    for x in list:
    	if x.name in ("pt defiance-vashon", "vashon-pt defiance"):
    		newlist.append( CurrentSchedule.schedule(x.name, "w" if x.direction == "e" else "e", x.dow, x.text) )
    	else:
    		newlist.append( x )
    return newlist


def textify(list,asCanonical):
	"""Convert list of routes to string expected by client"""
	result = ""
	for x in list:
		type = "s"
		if asCanonical:
			type = "d" if x.dow < 5 else "e"   # weekDay or weekEnd

		result += x.name + "," + x.direction + type + "," + x.text + "\n"
	return result
