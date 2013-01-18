import urllib2
import time as TM
import datetime as DT
import argparse
import sys

def run():
    stream = urllib2.urlopen(r'http://nextferry.appspot.com/stayawake/')
    print "server says: " + stream.read()

def runforever(interval):
    while True:
        try:
            run()
        except (urllib2.HTTPError, IOError) as err:
            print "Error occurred: " + str(err.reason)
        TM.sleep(interval * 60)
        
def runUntil(interval, endtime):
    curtime = DT.datetime.now().time()   
    while curtime < endtime:
        try:
            run()
        except (urllib2.HTTPError, IOError) as err:
            print "Error occurred: " + str(err.reason)
        TM.sleep(interval * 60)
        curtime = DT.datetime.now().time()
        

if __name__ == '__main__':
    parser = argparse.ArgumentParser(description='Ping NextFerry server at a given frequency until a given time of day')
    parser.add_argument('interval', nargs='?', default='2', type=float, help='how often to ping, in minutes')
    parser.add_argument('hour', nargs='?', default='22', type=int, help='hour to stop [0-23]' )
    parser.add_argument('minute', nargs='?', default='30', type=int, help='minute to stop [0-59]' )
    args = parser.parse_args()
    endtime = DT.time(args.hour,args.minute)
    #print repr(sys.argv)
    #print "Running with {0}, {1}".format(args.interval,endtime)
    runUntil(args.interval, endtime)