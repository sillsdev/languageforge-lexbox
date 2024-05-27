#!/usr/bin/env python3

import os
import sys

def usage():
    name = os.path.basename(sys.argv[0])
    print(f"Usage: {name} file key value")
    print("")
    print(f"E.g., {name} local.env HONEYCOMB_API_KEY __REPLACE__")

def ensure(fname, search_str, replace_str):
    # Ensure search_str is present. If not found, add replace_str to end of file
    found = False
    try:
        with open(fname, encoding='utf-8') as f:
            lines = f.readlines()
            for line in lines:
                if search_str in line:
                    found = True
                    break
    except FileNotFoundError:
        lines = []
    need_extra_newline = False
    if lines and lines[-1] and not lines[-1].endswith('\n'):
        need_extra_newline = True
    if not found:
        if need_extra_newline:
            lines[-1] = lines[-1] + '\n'
        lines.append(replace_str + '\n')
        with open(fname, 'w', encoding='utf-8') as f:
            f.writelines(lines)

if __name__ == '__main__':
    if (len(sys.argv) < 4):
        usage()
    (fname, key, value) = sys.argv[1:4]
    search_str = f"{key}="
    replace_str = f"{key}={value}"
    ensure(fname, search_str, replace_str)
