#!/bin/bash

docker stop mariadb-server || true
docker run --name mariadb-server --detach -e MARIADB_ALLOW_EMPTY_ROOT_PASSWORD=yes -dp 3306:3306 --mount type=bind,src=`pwd`/dump.sql,target=/docker-entrypoint-initdb.d/dump.sql mariadb:latest
