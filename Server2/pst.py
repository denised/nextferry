import datetime as dtm

class Pacific_tzinfo(dtm.tzinfo):
    """Implementation of the Pacific timezone."""
    def utcoffset(self, dt):
        return dtm.timedelta(hours=-8) + self.dst(dt)

    def _FirstSunday(self, dt):
        """First Sunday on or after dt."""
        return dt + dtm.timedelta(days=(6-dt.weekday()))

    def dst(self, dt):
        # 2 am on the second Sunday in March
        dst_start = self._FirstSunday(dtm.datetime(dt.year, 3, 8, 2))
        # 1 am on the first Sunday in November
        dst_end = self._FirstSunday(dtm.datetime(dt.year, 11, 1, 1))

        if dst_start <= dt.replace(tzinfo=None) < dst_end:
            return dtm.timedelta(hours=1)
        else:
            return dtm.timedelta(hours=0)
        
    def tzname(self, dt):
        if self.dst(dt) == dtm.timedelta(hours=0):
            return "PST"
        else:
            return "PDT"

class UTC_tzinfo(dtm.tzinfo):
    """Implementation of the UTC timezone."""
    def utcoffset(self, dt):
        return dtm.timedelta(0)

    def dst(self, dt):
        return dtm.timedelta(0)
        
    def tzname(self, dt):
        return "UTC"

pacific = Pacific_tzinfo()
utc = UTC_tzinfo()

def toPacific(dt):
    """convert time to pacific time, interpreting naive times as UTC"""
    if dt.tzinfo == None:
        dt = dt.replace(tzinfo=utc)
    return dt.astimezone(pacific)

def toUTC(dt):
    """convert time to UTC, interpreting naive times as pacific"""
    if dt.tzinfo == None:
        dt = dt.replace(tzinfo=pacific)
    return dt.astimezone(utc)
    
    
    
    
