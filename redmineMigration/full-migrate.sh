#!/bin/bash
set -e
./loadSql/getSqlFromServer.sh
./loadSql/loadSql.sh
prod_context=aws-rke
prod_db_pw=****
kubectl port-forward service/db 5433:5432 -n languagedepot --context $prod_context &

#startup dotnet and configure migration db
export DbConfig__LexBoxConnectionString="Host=localhost;Port=5433;Username=postgres;Password=$prod_db_pw;Database=lexbox;Include Error Detail=true"
export DbConfig__RedmineConnectionString="server=localhost;database=languagedepot;user=root;"
dotnet run --no-build --project ./../backend/LexBoxApi/LexBoxApi.csproj migrate-mysql
pkill -f 'kubectl'
echo migration finished!
