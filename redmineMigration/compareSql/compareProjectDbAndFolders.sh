#!/bin/bash

echo -e "Public projects that exist in DB but have no corresponding folder\n"
diff public.project.codes ../compareFolders/public.folders | grep "<" | sed "s/^< //" | tee public.projects.inDbNotInFolder
echo -e "\n$(wc public.projects.inDbNotInFolder | awk '{print $1;}') items\n"

echo -e "Public projects that DO NOT exist in DB but have a folder\n"
diff public.project.codes ../compareFolders/public.folders | grep ">" | sed "s/^> //" | tee public.projects.notInDbInFolder
echo -e "\n$(wc public.projects.notInDbInFolder | awk '{print $1;}') items\n"

echo -e "Private projects that exist in DB but have no corresponding folder\n"
diff private.project.codes ../compareFolders/private.folders | grep "<" | sed "s/^< //" | tee private.projects.inDbNotInFolder
echo -e "\n$(wc private.projects.inDbNotInFolder | awk '{print $1;}') items\n"

echo -e "Private projects that DO NOT exist in DB but have a folder\n"
diff private.project.codes ../compareFolders/private.folders | grep ">" | sed "s/^> //" | tee private.projects.notInDbInFolder
echo -e "\n$(wc private.projects.notInDbInFolder | awk '{print $1;}') items\n"