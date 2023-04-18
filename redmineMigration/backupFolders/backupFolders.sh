#!/bin/bash

#cd /var/vcs/public
#BACKUP_DIR=/mnt/backups/orphanedFolders/2023-03-21/public
BACKUP_DIR=../backups
cd test
#for d in `cat ~/public.projects.notInDbInFolder.10`; do
for d in `cat ../../compareSql/public.projects.notInDbInFolder`; do
    if [ -d "$d" ]; then
        echo "Processing $d"
        BACKUP_FILE="$d.tgz"
        tar -czvf $BACKUP_FILE "$d"
        mv $BACKUP_FILE $BACKUP_DIR
        rm -r "$d"

    else
        echo "Error: Directory $d was not found"
    fi
done