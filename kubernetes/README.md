# SAV Pro local Kubernetes deployment with Docker Desktop

This folder deploys the SAV Pro backend stack locally on Docker Desktop Kubernetes.

## 1. Enable Kubernetes

Open Docker Desktop -> Settings -> Kubernetes -> Enable Kubernetes.

Verify:

```bash
kubectl get nodes
```

## 2. Build local Docker images

Run these commands from the project root, the same place where your `src` folder exists:

```bash
docker build -t sav-auth-service:local -f src/services/AuthService/AuthService.Api/Dockerfile src
docker build -t sav-reclamation-service:local -f src/services/ReclamationService/ReclamationService.Api/Dockerfile src
docker build -t sav-notification-service:local -f src/services/NotificationService/NotificationService.Api/Dockerfile src
docker build -t sav-intervention-service:local -f src/services/InterventionService/InterventionService.Api/Dockerfile src
docker build -t sav-api-gateway:local -f src/gateway/ApiGateway/Dockerfile src
```

If you have a frontend Dockerfile, build it and uncomment it in `kustomization.yaml`:

```bash
docker build -t sav-frontend:local -f frontend/Dockerfile frontend
```

If you do not have a frontend Dockerfile, keep running React locally with `npm run dev`.

## 3. Deploy backend stack

From the folder containing these YAML files:

```bash
kubectl apply -k .
```

Wait for pods:

```bash
kubectl get pods -n sav-pro -w
```

## 4. Open local ports

API Gateway:

```bash
kubectl port-forward -n sav-pro svc/api-gateway 5005:8080
```

Open:

```text
http://localhost:5005/health/ready
```

RabbitMQ management UI:

```bash
kubectl port-forward -n sav-pro svc/rabbitmq 15672:15672
```

Open:

```text
http://localhost:15672
Username: pfe
Password: PfeNetRabbit!2026
```

Optional SQL Server local access:

```bash
kubectl port-forward -n sav-pro svc/sqlserver 14333:1433
```

Connect with SSMS:

```text
Server: localhost,14333
User: sa
Password: PfeNet!2026Strong
Trust server certificate: true
```

Optional individual service Swagger ports:

```bash
kubectl port-forward -n sav-pro svc/auth-service 5001:8080
kubectl port-forward -n sav-pro svc/reclamation-service 5002:8080
kubectl port-forward -n sav-pro svc/notification-service 5003:8080
kubectl port-forward -n sav-pro svc/intervention-service 5004:8080
```

Then open:

```text
http://localhost:5001/swagger/index.html
http://localhost:5002/swagger/index.html
http://localhost:5003/swagger/index.html
http://localhost:5004/swagger/index.html
```

## 5. Useful commands

```bash
kubectl get all -n sav-pro
kubectl logs -n sav-pro deploy/auth-service
kubectl logs -n sav-pro deploy/reclamation-service
kubectl logs -n sav-pro deploy/notification-service
kubectl logs -n sav-pro deploy/intervention-service
kubectl logs -n sav-pro deploy/api-gateway
kubectl describe pod -n sav-pro <pod-name>
```

## 6. Reset everything

This deletes the namespace and all persistent volumes in this namespace:

```bash
kubectl delete namespace sav-pro
```

If Docker Desktop keeps local PV data, clean it from Docker Desktop volumes if needed.

## Notes

- The manifests use `imagePullPolicy: Never` for local images. Build the images before `kubectl apply`.
- The Kubernetes service names are intentionally the same as Docker Compose names: `sqlserver`, `rabbitmq`, `auth-service`, `reclamation-service`, `notification-service`, `intervention-service`.
- Keep `ASPNETCORE_ENVIRONMENT=Docker` in Kubernetes because your service-to-service configuration should use container/Kubernetes service names, not localhost.
- `RESET_DEMO_DATA=true` is useful for demos but can reset demo data after restarts. Set it to `false` after the first successful seed if you want persistence.
