#!/bin/bash

ssh languagedepot ls -1 /var/vcs/public | sort > public.folders
ssh languagedepot ls -1 /var/vcs/private | sort > private.folders
cat public.folders private.folders | sort > all.folders.sorted
cat all.folders.sorted | uniq > all.folders.sorted.uniq
diff all.folders.sorted all.folders.sorted.uniq | grep "^<" | sed "s/^< //" | tee folders.inboth
echo There are $(wc folders.inboth|awk '{print $1}') folders that occur in both public and private