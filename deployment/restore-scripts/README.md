after bringing up a fresh instance on a cluster

restore the backup secret from production into the target cluster, or build it from the config and password.

setup restore pod:
```bash
kubectl --context dallas-stage --namespace languagedepot-dev apply -f .\kopia-maint.yaml
```
connect to the kopia pod
```bash
kubectl --context dallas-stage --namespace languagedepot-dev exec -it pod/kopia-maint -c kopia -- /bin/bash
```


helpful kopia commands:
```bash
kopia repostory status
kopia snapshot list -al
# list files in a snapshot
kopia ls {snapshot id}
```

## Postgrest restore
postgres database restore
```bash
kopai restore {snapshot id} /dump/restore-$(date -I)
```
now you need to switch to the pg container to run the restore. 
connect to the pg pod
```bash
kubectl --context dallas-stage --namespace languagedepot-dev exec -it pod/kopia-maint -c pg -- /bin/bash
```
run the restore, note the /dump path is mounted in both containers
```bash
pg_restore -U postgres -v -h db.languagedepot -d $PGDATABASE /dump/restore-{date from previous step}
```
note: include `-c` when doing the restore to clear the current DB first

## HG restore
Restore from a specific spanshot directly into hg repos
```bash
kopia restore k4de77b4722704a4db051e8929df64ac0/e/elawa200810 /hg-repos/e/elawa200810 --no-overwrite-files --no-overwrite-directories
```
