import os
import sys
# An example WSGI for use with mod_wsgi, edit as necessary
# See https://mercurial-scm.org/wiki/modwsgi for more information
# mod_wsgi docs: https://modwsgi.readthedocs.io/en/master/

# Path to repo or hgweb config to serve (see 'hg help hgweb')
config = b"/var/hg/hgweb.hgrc"

# Uncomment and adjust if Mercurial is not installed system-wide
# (consult "installed modules" path from 'hg debuginstall'):
#import sys; sys.path.insert(0, "/path/to/python/lib")

# Uncomment to send python tracebacks to the browser if an error occurs:
#import cgitb; cgitb.enable()

# enable demandloading to reduce startup time
from mercurial import demandimport;

# enable demandloading to reduce startup time
if os.getenv('ENABLE_DEMAND_IMPORT', 'false').lower() in ['1', 'true', 'yes']:
    demandimport.enable()
else:
    demandimport.disable()

from mercurial.hgweb import hgweb
application = hgweb(config)
