After bringing up a fresh instance on a cluster:

Restore the backup secret from production into the target cluster, or build it from the config and password.

Setup restore pod:
```bash
kubectl --context dallas-stage --namespace languagedepot-dev apply -f .\kopia-maint.yaml
```
Connect to the kopia pod:
```bash
kubectl --context dallas-stage --namespace languagedepot-dev exec -it pod/kopia-maint -c kopia -- /bin/bash
```


Helpful kopia commands:
```bash
kopia repostory status
kopia snapshot list -al
# list files in a snapshot
kopia ls {snapshot id}
```

## Postgrest restore
Postgres database restore:
```bash
kopai restore {snapshot id} /dump/restore-$(date -I)
```
Now you need to switch to the pg container to run the restore.

Connect to the pg pod:
```bash
kubectl --context dallas-stage --namespace languagedepot-dev exec -it pod/kopia-maint -c pg -- /bin/bash
```
Run the restore: (note that the /dump path is mounted in both containers)
```bash
pg_restore -U postgres -v -h db.languagedepot -d $PGDATABASE /dump/restore-{date from previous step}
```
Note: include `-c` when doing the restore to clear the current DB first.

## HG restore
Restore from a specific spanshot directly into hg repos:
```bash
kopia restore k4de77b4722704a4db051e8929df64ac0/e/elawa200810 /hg-repos/e/elawa200810 --no-overwrite-files --no-overwrite-directories
```
