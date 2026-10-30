#!/bin/sh
set -e

# The runtime dir is an emptyDir that outlives container restarts; mod_cgid can't
# bind over a stale socket and never retries, so every CGI request would 503.
rm -f /usr/local/apache2/logs/cgisock.* /usr/local/apache2/logs/wsgi.*.sock

exec httpd-foreground "$@"
