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
deploy-hgweb: deploy-pvc-hg-repos
	kubectl apply -f hgweb-deployment.yaml
deploy-hasura: deploy-db
	kubectl apply -f hasura-deployment.yaml
deploy-api: deploy-db deploy-hgresume deploy-hgweb deploy-hasura
	kubectl apply -f lexbox-api-deployment.yaml


delete-db: # does NOT delete the volume, i.e., the data in the database
	kubectl delete deployment,service db
delete-pvc-db:
	kubectl delete pvc db-data
delete-hgresume:
	kubectl delete deployment,service hgresume
delete-pvc-hgresume-cache:
	kubectl delete pvc hgresume-cache
delete-pvc-hg-repos:
	kubectl delete pvc hg-repos
delete-hgweb:
	kubectl delete deployment,service hgweb
delete-hasura:
	kubectl delete deployment,service hasura
delete-api:
	kubectl delete deployment,service lexbox-api


scale-down-db:
	kubectl scale deploy/db --replicas 0
scale-up-db:
	kubectl scale deploy/db --replicas 1
scale-down-hgresume:
	kubectl scale deploy/hgresume --replicas 0
scale-up-hgresume:
	kubectl scale deploy/hgresume --replicas 1
scale-down-hgweb:
	kubectl scale deploy/hgweb --replicas 0
scale-up-hgweb:
	kubectl scale deploy/hgweb --replicas 1
scale-down-hasura:
	kubectl scale deploy/hasura --replicas 0
scale-up-hasura:
	kubectl scale deploy/hasura --replicas 1
scale-down-api:
	kubectl scale deploy/lexbox-api --replicas 0
scale-up-api:
	kubectl scale deploy/lexbox-api --replicas 1
