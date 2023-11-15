#!/bin/bash
set -e
docker stop mariadb-server || true
docker rm mariadb-server || true
docker run --name mariadb-server --detach -e MARIADB_ALLOW_EMPTY_ROOT_PASSWORD=yes -dp 3306:3306 mariadb:latest
sleep 10
docker exec -i mariadb-server mariadb -w -uroot < dump.sql
