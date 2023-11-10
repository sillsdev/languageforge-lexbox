#!/bin/bash
set -e
./loadSql/getSqlFromServer.sh
./loadSql/loadSql.sh

kubectl port-forward service/db 5433:5432 -n languagedepot --context docker-desktop &
#startup dotnet and configure migration db
export DbConfig__LexBoxConnectionString="Host=localhost;Port=5433;Username=postgres;Password=972b722e63f549938d07bd8c4ee5086c;Database=lexbox-migration;Include Error Detail=true"
export DbConfig__RedmineConnectionString="server=localhost;database=languagedepot;user=root;"
dotnet run --no-build --project ./../backend/LexBoxApi/LexBoxApi.csproj migrate-mysql

#export pg db to file
kubectl --context docker-desktop -n languagedepot exec deployment/db -- bash -c 'pg_dump --disable-triggers -U postgres -d lexbox-migration -T __EFMigrationsHistory' > ./out.sql

prod_context=docker-desktop
prod_database=import-new
#import pg db to prod db
target_pod=$(kubectl --context docker-desktop -n languagedepot get pods -l app=db -o jsonpath='{.items[0].metadata.name}')
kubectl --context $prod_context -n languagedepot cp out.sql $target_pod:/tmp/out.sql
kubectl --context $prod_context -n languagedepot exec -it $target_pod -- psql -U postgres -d $prod_database -f /tmp/out.sql
#terminate port forward above
pkill -f 'kubectl'
