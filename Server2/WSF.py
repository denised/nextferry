#!/usr/bin/env python
import logging

class terminal(object):
    """WSDOT code, name, location, [list of counties from which it makes sense to drive]"""
    def __init__(self,code,name,location,counties):
        self.code = code
        self.name = name
        self.location = location
        self.counties = counties
        
    @staticmethod
    def lookup(datum):
        """Return the terminal corresponding to the supplied datum (which may be name or code)."""
        for term in Terminals:
            if term.name == datum or term.code == datum:
                return term
        logging.warn("Attempted to look up a non-existent terminal field! " + repr(datum))
        return None

Terminals = (
    terminal(1, "Anacortes", (48.502220,-122.679455), ("Whatcom", "Skagit", "Snohomish", "King")),
    terminal(3, "Bainbridge", (47.623046,-122.511377), ("Jefferson", "Kitsap", "Mason")),
    terminal(4, "Bremerton", (47.564990,-122.627012), ("Mason","Kitsap","Thurston", "Pierce")),
    terminal(5, "Clinton", (47.974785,-122.352139), ("Island")),
    terminal(8, "Edmonds", (47.811240,-122.382631), ("Skagit", "Snohomish", "King")),
    terminal(9, "Fauntleroy", (47.523115,-122.392952), ("Snohomish","King","Pierce")),
    terminal(10, "Friday Harbor", (48.535010,-123.014645), ("San Juan")),
    terminal(11, "Keystone", (48.160592,-122.674305), ("Island")),
    terminal(12, "Kingston", (47.796943,-122.496785), ("Jefferson","Mason","Kitsap")),
    terminal(13, "Lopez Island", (48.570447,-122.883646), ("San Juan")),
    terminal(14, "Mukilteo", (47.947758,-122.304138), ("Skagit","Snohomish","King")),
    terminal(15, "Orcas Island", (48.597971,-122.943985), ("San Juan")),
    terminal(16, "Point Defiance", (47.305414,-122.514123), ("Thurston","Pierce","King")),
    terminal(17, "Port Townsend", (48.112648,-122.760715), ("Clallam","Jefferson","Mason","Kitsap")),
    terminal( 7, "Seattle", (47.601767,-122.336089), ("Skagit","Snohomish","King","Pierce")),
    terminal(18, "Shaw Island", (48.583991,-122.929351), ("San Juan")),
    terminal(20, "Southworth", (47.512130,-122.500970), ("Jefferson","Mason","Thurston","Kitsap","Pierce")),
    terminal(21, "Tahlequah", (47.333023,-122.506999), ("King")),
    terminal(22, "Vashon Island", (47.508616,-122.464127), ("King"))   
)


class route(object):
    """These are mostly *our* names and codes, not WSF's."""
    def __init__(self,name,code,t1,t2):
        self.name = name
        self.code = code    # bits for creating bitmaps
        self.term1 = t1
        self.term2 = t2
    
    @staticmethod
    def lookup(datum):
        """look up a route by it's name or code"""
        for route in Routes:
            if route.name == datum or route.code == datum:
                return route
        logging.warn("Attempted to look up a non-existent route field! " + repr(datum))
    
    @staticmethod
    def findall(term):
        """Yield all routes that depart from a given terminal"""
        for route in Routes:
            if term.name == route.term1 or term.name == route.term2:
                yield route
            

Routes = (
    route('bainbridge',1,'Seattle','Bainbridge'),
    route('edmonds',1<<2,'Edmonds','Kingston'),
    route('mukilteo',1<<3,'Mukilteo','Clinton'),
    route('pt townsend',1<<4,'Port Townsend','Keystone'),
    route('fauntleroy-southworth',1<<5,'Fauntleroy','Southworth'),
    route('fauntleroy-vashon', 1<<6,'Fauntleroy','Vashon Island'),
    route('vashon-southworth',1<<7,'Vashon Island','Southworth'),
    route('bremerton',1<<8,'Seattle','Bremerton'),
    route('vashon-pt defiance',1<<9,'Point Defiance','Tahlequah'),
    route('friday harbor',1<<10,'Anacortes','Friday Harbor'),
    route('orcas',1<<11,'Anacortes','Orcas Island')
)

# this is text WSF uses to identify routes in alerts.
# we're mapping them onto bitmaps.
# note the mapping is not strictly 1:1
AlertStringCode = {
    "Seattle / Bainbridge Island" :
        route.lookup('bainbridge').code,
    "Edmonds / Kingston" :
        route.lookup('edmonds').code,
    "Mukilteo / Clinton" :
        route.lookup('mukilteo').code,
    "Port Townsend / Coupeville" :
        route.lookup('pt townsend').code,
    "Fauntleroy (West Seattle) / Vashon / Southworth" :
        route.lookup('fauntleroy-southworth').code |
        route.lookup('fauntleroy-vashon').code |
        route.lookup('vashon-southworth').code,
    "Seattle / Bremerton" :
        route.lookup('bremerton').code,
    "Pt. Defiance / Tahlequah" :
        route.lookup('vashon-pt defiance').code,
    "Anacortes / San Juan Islands" :
        route.lookup('friday harbor').code |
        route.lookup('orcas').code
}


    







