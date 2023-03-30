# https://kubernetes.io/docs/reference/generated/kubectl/kubectl-commands
list-contexts:
	kubectl config get-contexts
	@echo
	@echo "In order to switch contexts, use the following:"
	@echo "kubectl config use-context <NAME>"
list-deployments:
	kubectl get deployments
list-services:
	kubectl get services
list-pods:
	kubectl get pods
list-pvc:
	kubectl get pvc
list-config:
	cat ~/.kube/config


init-secrets:
	kubectl apply -f secrets.yaml


deploy-db:
	kubectl apply -f db-deployment.yaml
deploy-pvc-hg-repos:
	kubectl apply -f hg-repos-pvc.yaml
deploy-hgresume: deploy-pvc-hg-repos
	kubectl apply -f hgresume-deployment.yaml
deploy-hgkeeper: deploy-pvc-hg-repos
	kubectl apply -f hgkeeper-deployment.yaml


delete-db: # does NOT delete the volume, i.e., the data in the database
	kubectl delete deployment,service db
delete-pvc-db:
	kubectl delete pvc db-data
delete-hgresume: # does NOT delete the volume, i.e., the data in the database
	kubectl delete deployment,service hgresume
delete-pvc-hgresume-cache:
	kubectl delete pvc hgresume-cache
delete-pvc-hg-repos:
	kubectl delete pvc hg-repos
delete-hgkeeper:
	kubectl delete deployment,service hgkeeper


scale-down-db:
	kubectl scale deploy/db --replicas 0
scale-up-db:
	kubectl scale deploy/db --replicas 1
scale-down-hgresume:
	kubectl scale deploy/hgresume --replicas 0
scale-up-hgresume:
	kubectl scale deploy/hgresume --replicas 1
scale-down-hgkeeper:
	kubectl scale deploy/hgkeeper --replicas 0
scale-up-hgkeeper:
	kubectl scale deploy/hgkeeper --replicas 1
