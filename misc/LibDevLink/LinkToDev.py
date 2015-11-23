import sys
import os
import glob

build_conf = "Debug"
base_path = os.path.split(__file__)[0]
devlib_root = os.path.abspath(os.path.join(base_path, r"../../core"))
lib_prefix = "packages\Akka.Interfaced"

def find_devlib_path(dll_name):
    paths = glob.glob(devlib_root + "/*/*/" + build_conf + "/" + dll_name)
    if len(paths) == 0: return None
    return paths[0]
    

def link(csproj):
    lines = []
    dirty = False
    for line in open(csproj):
        si = line.find("<HintPath>")
        ei = line.find("</HintPath>")
        if si != -1 and ei != -1:
            x = line[si+10:ei]
            if lib_prefix in x:
                devlib = find_devlib_path(os.path.split(x)[1])
                if devlib:
                    print x, "=>", devlib
                    line = line[:si+10] + devlib + line[ei:ei+11] + " <!--?" + x + "?-->" + line[ei+11:]
                    dirty = True
        lines.append(line)
    if dirty:
        open(csproj, "wb").write(''.join(lines))
    

def unlink(csproj):
    lines = []
    dirty = False
    for line in open(csproj):
        si = line.find("<HintPath>")
        ei = line.find("</HintPath>")
        sk = line.find("<!--?")
        ek = line.find("?-->")
        if si != -1 and ei != -1 and sk != -1 and ek != -1:
            x = line[si+10:ei]
            y = line[sk+5:ek]
            line = line[:si+10] + y + line[ei:ei+11] + line[ek+4:]
            dirty = True
            print x, "=>", y
        lines.append(line)
    if dirty:
        open(csproj, "wb").write(''.join(lines))


def show_usage():
    print "[USAGE] command csproj"
    print "  l   link dev dll to csproj"
    print "  u   unlink dev dll from csproj"

    
def main():
    if len(sys.argv) <= 1:
        show_usage()
        sys.exit(1)

    if sys.argv[1] == "l" or sys.argv[1] == "L":
        link(sys.argv[2])
    elif sys.argv[1] == "u" or sys.argv[1] == "U":
        unlink(sys.argv[2])
    else:
        show_usage()
        sys.exit(1)       


main()
