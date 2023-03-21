#!/bin/bash

# assume running docker container mariadb-server
docker exec mariadb-server mariadb languagedepot -e "select identifier from projects" | sort > public.project.codes
docker exec mariadb-server mariadb languagedepotpvt -e "select identifier from projects" | sort > private.project.codes