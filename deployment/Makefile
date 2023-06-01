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
list-ingress:
	kubectl get ingress
list-config:
	cat ~/.kube/config


init-secrets:
	kubectl apply -f secrets.yaml

create-otel-config:
    kubectl create configmap otel-config --from-file=../otel/collector-config.yaml


deploy-db:
	kubectl apply -f db-deployment.yaml
deploy-pvc-hg-repos:
	kubectl apply -f hg-repos-pvc.yaml
deploy-app: deploy-db
	kubectl apply -f lexbox-deployment.yaml
deploy-hg:
	kubectl apply -f hg-deployment.yaml
deploy-proxy: deploy-app deploy-hg
	kubectl apply -f proxy-deployment.yaml


delete-db: # does NOT delete the volume, i.e., the data in the database
	kubectl delete deployment,service db
delete-pvc-db:
	kubectl delete pvc db-data
delete-hgresumable:
	kubectl delete deployment,service hgresumable
delete-pvc-hgresumable-cache:
	kubectl delete pvc hgresumable-cache
delete-pvc-hg-repos:
	kubectl delete pvc hg-repos
delete-hgweb:
	kubectl delete deployment,service hgweb
delete-hasura:
	kubectl delete deployment,service hasura
delete-otel-collector:
	kubectl delete deployment,service otel-collector
delete-api:
	kubectl delete deployment,service lexbox-api
delete-ui:
	kubectl delete deployment,service ui
delete-proxy:
	kubectl delete ingress proxy


scale-down-db:
	kubectl scale deploy/db --replicas 0
scale-up-db:
	kubectl scale deploy/db --replicas 1
scale-down-hgresumable:
	kubectl scale deploy/hgresumable --replicas 0
scale-up-hgresumable:
	kubectl scale deploy/hgresumable --replicas 1
scale-down-hgweb:
	kubectl scale deploy/hgweb --replicas 0
scale-up-hgweb:
	kubectl scale deploy/hgweb --replicas 1
scale-down-hasura:
	kubectl scale deploy/hasura --replicas 0
scale-up-hasura:
	kubectl scale deploy/hasura --replicas 1
scale-down-otel-collector:
	kubectl scale deploy/otel-collector --replicas 0
scale-up-otel-collector:
	kubectl scale deploy/otel-collector --replicas 1
scale-down-api:
	kubectl scale deploy/lexbox-api --replicas 0
scale-up-api:
	kubectl scale deploy/lexbox-api --replicas 1
scale-down-ui:
	kubectl scale deploy/ui --replicas 0
scale-up-ui:
	kubectl scale deploy/ui --replicas 1
