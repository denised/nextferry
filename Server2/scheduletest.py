import CurrentSchedule
import CalcSchedule

# I tried using unittest, wasn't working probably due to python version issues.
# rather than debug, just manually hack together sufficient for now
# TODO: convert to real testing platform, stop overwriting global vars in other files...

# two schedule items, one weekday one weekend, one east one west
smallist = (CurrentSchedule.schedule('bainbridge','e',6,'320,425,525,575,625'), CurrentSchedule.schedule('edmonds','w',0,'335,380,430'))
# ditto, with pt defiance
ptdeflist = (CurrentSchedule.schedule('vashon-pt defiance','w',6,'380,430'), CurrentSchedule.schedule('pt defiance-vashon','e',0,'305,355'))
# bigger list with complete dow for two routes
biglist = (
    CurrentSchedule.schedule('bainbridge','w',0,'330,370,425'),
    CurrentSchedule.schedule('bainbridge','w',1,'475,525,575'),
    CurrentSchedule.schedule('bainbridge','w',2,'575,640'),
    CurrentSchedule.schedule('bainbridge','w',3,'640,685,740'),
    CurrentSchedule.schedule('bainbridge','w',4,'790,845,900'),
    CurrentSchedule.schedule('bainbridge','w',5,'900,945,1000'),
    CurrentSchedule.schedule('bainbridge','w',6,'1000,1050,1100'),
    CurrentSchedule.schedule('bainbridge','e',0,'285,320,380'),
    CurrentSchedule.schedule('bainbridge','e',1,'425,475'),
    CurrentSchedule.schedule('bainbridge','e',2,'525,580,625'),
    CurrentSchedule.schedule('bainbridge','e',3,'625,690'),
    CurrentSchedule.schedule('bainbridge','e',4,'740,790'),
    CurrentSchedule.schedule('bainbridge','e',5,'790,845,895,950'),
    CurrentSchedule.schedule('bainbridge','e',6,'995,1050,1110'),
    CurrentSchedule.schedule('edmonds','w',0,'335,380,430'),
    CurrentSchedule.schedule('edmonds','w',1,'475,530,580'),
    CurrentSchedule.schedule('edmonds','w',2,'630,670'),
    CurrentSchedule.schedule('edmonds','w',3,'725,760,820'),
    CurrentSchedule.schedule('edmonds','w',4,'820,865,915'),
    CurrentSchedule.schedule('edmonds','w',5,'915,955'),
    CurrentSchedule.schedule('edmonds','w',6,'1005,1045'),
    CurrentSchedule.schedule('edmonds','e',0,'295,335'),
    CurrentSchedule.schedule('edmonds','e',1,'385,425'),
    CurrentSchedule.schedule('edmonds','e',2,'475,520,580'),
    CurrentSchedule.schedule('edmonds','e',3,'625,675,715,770'),
    CurrentSchedule.schedule('edmonds','e',4,'810,870,910'),
    CurrentSchedule.schedule('edmonds','e',5,'960,1000,1050'),
    CurrentSchedule.schedule('edmonds','e',6,'1090,1140'))


canonicalresult = """bainbridge,wd,330,370,425
bainbridge,we,1000,1050,1100
bainbridge,ed,285,320,380
bainbridge,ee,995,1050,1110
edmonds,wd,335,380,430
edmonds,we,1005,1045
edmonds,ed,295,335
edmonds,ee,1090,1140
"""

def main():
	test_textify()
	test_versionify()
	test_canonical()
	test_special()

def test_textify():
	result = CalcSchedule.textify(smallist,True)
	assert result == "bainbridge,ee,320,425,525,575,625\nedmonds,wd,335,380,430\n", "Checking canonical result"
	result = CalcSchedule.textify(smallist,False)
	assert result == "bainbridge,es,320,425,525,575,625\nedmonds,ws,335,380,430\n", "Checking daily result"
	print "test_textify passed"

def test_versionify():
	result = CalcSchedule.textify(CalcSchedule.versionify(smallist,"2.0"),False)
	assert result=="bainbridge,es,200,201,320,425,525,575,625\nedmonds,ws,200,201,335,380,430\n", "Checking v2 added times"
	result = CalcSchedule.textify(CalcSchedule.versionify(ptdeflist,"4.0"), False)
	assert result=="vashon-pt defiance,es,380,430\npt defiance-vashon,ws,305,355\n", "Checking pt defiance turn-around"
	print "test_versionify passed"

def test_canonical():
	CurrentSchedule.CurrentSchedule = biglist # DANGER, DESTRUCTIVE, DO NOT TRY THIS AT HOME
	result = CalcSchedule.getSchedule("3.0")
	assert result == canonicalresult, "Checking canonical result"
	print "test_canonical passed"

def test_special():
	# we really need mocks to test this.  hand tested by setting today to be holiday in CurrentSchedule.py
    #CurrentSchedule.CurrentSchedule = biglist
    #print CalcSchedule.getSpecial("3.0")
    print "test_special not implemented"

if __name__ == "__main__":
	main()
