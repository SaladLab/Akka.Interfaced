import fnmatch
import os
import re

re_reference = re.compile(r'\<Reference Include=\"([\w\.]+)\,.*?\<\/Reference\>', re.DOTALL | re.MULTILINE)

def process_csproj(file, delete_package):
    proj = open(file).read()
    def repl(mo):
        if mo.group(1) == delete_package:
            return ""
        else:
            return mo.group(0)
    proj_new = re_reference.sub(repl, proj)
    if proj != proj_new:
        print "!", file
        open(file, "wb").write(proj_new)

re_package = re.compile(r'\<package id\="([\w\.]*)".*?\/\>')

def process_package(file, delete_package):
    pack = open(file).read()
    def repl(mo):
        if mo.group(1) == delete_package:
            return ""
        else:
            return mo.group(0)
    pack_new = re_package.sub(repl, pack)
    if pack != pack_new:
        print "!", file
        open(file, "wb").write(pack_new)

def do_recursive(start_path, delete_package):
    projs = []
    packages = []
    for root, dirnames, filenames in os.walk('../../'):
      for filename in fnmatch.filter(filenames, '*.csproj'):
          process_csproj(os.path.join(root, filename), delete_package)
      for filename in fnmatch.filter(filenames, 'packages.config'):
          process_package(os.path.join(root, filename), delete_package)

#do_recursive('../../', 'NetLegacySupport.Action')
#do_recursive('../../', 'NetLegacySupport.ConcurrentDictionary')
#do_recursive('../../', 'NetLegacySupport.Tuple')
#do_recursive('../../', 'TypeAlias')
