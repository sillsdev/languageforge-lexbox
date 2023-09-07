#!/bin/bash

ssh -t languagedepot "sudo mysqldump --databases --add-drop-database languagedepot languagedepotpvt | gzip > dump.sql.gz"
scp languagedepot:dump.sql.gz .
ssh languagedepot "rm dump.sql.gz"
gunzip dump.sql.gz -k
