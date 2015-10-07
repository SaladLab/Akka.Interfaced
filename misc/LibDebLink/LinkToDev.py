import os
import glob

build_conf = "Debug"
devlib_root = os.path.abspath(r"../../output")


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
			if "packages\Akka.Interfaced" in x:
				devlib = find_devlib_path(os.path.split(x)[1])
				if devlib:
					print x, "=>", devlib
					line = line[:si+10] + devlib + line[ei:]
					dirty = True
					print line
		lines.append(line)
	if dirty:
		open(csproj, "wb").write(''.join(lines))
	

def main():
	link("../BasicBenchmark/InterfacedPingpong/PingpongProgram/PingpongProgram.csproj")
	link("../BasicBenchmark/InterfacedPingpong/PingpongInterface/PingpongInterface.csproj")


main()
