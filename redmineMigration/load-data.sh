#!/bin/bash
set -e
./loadSql/getSqlFromServer.sh
./loadSql/loadSql.sh
