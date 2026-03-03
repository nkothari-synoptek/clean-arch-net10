# Quick Start Guide

This guide provides the fastest path to deploying the Digital Inspection System to Kubernetes.

## Prerequisites Checklist

- [ ] Azure Kubernetes Service (AKS) cluster running
- [ ] kubectl installed and configured
- [ ] Azure Container Registry with images pushed
- [ ] Azure Entra ID configured
- [ ] Azure resources provisioned (PostgreSQL, Redis, Service Bus, App Insights)

## Quick Deploy (5 Steps)

### Step 1: Update Configuration

Edit these files with your actual values:
```bash
# Update Azure AD configuration
vi inspection-service-configmap.yaml
vi api-gateway-configmap.yaml
```

### Step 2: Create Secrets

```bash
# Create Inspection Service secrets
kubectl create secret generic inspection-service-secrets \
  --from-literal=database-connection-string="YOUR_DB_CONNECTION" \
  --from-literal=redis-connection-string="YOUR_REDIS_CONNECTION" \
  --from-literal=servicebus-connection-string="YOUR_SERVICEBUS_CONNECTION" \
  --from-literal=appinsights-connection-string="YOUR_APPINSIGHTS_CONNECTION" \
  --namespace=digital-inspection --dry-run=client -o yaml | kubectl apply -f -

# Create API Gateway secrets
kubectl create secret generic api-gateway-secrets \
  --from-literal=appinsights-connection-string="YOUR_APPINSIGHTS_CONNECTION" \
  --namespace=digital-inspection --dry-run=client -o yaml | kubectl apply -f -
```

### Step 3: Deploy Using Script

```bash
chmod +x deploy.sh
./deploy.sh
```

### Step 4: Verify Deployment

```bash
# Check all resources
kubectl get all -n digital-inspection

# Check pod status
kubectl get pods -n digital-inspection -w
```

### Step 5: Test Access

```bash
# Get external IP
kubectl get svc api-gateway -n digital-inspection

# Test health endpoint
curl http://<EXTERNAL_IP>/health/ready
```

## Alternative: Deploy with Kustomize

```bash
# Deploy all resources at once
kubectl apply -k .

# Verify
kubectl get all -n digital-inspection
```

## Alternative: Deploy Manually

```bash
# 1. Create namespace
kubectl apply -f namespace.yaml

# 2. Create ConfigMaps
kubectl apply -f inspection-service-configmap.yaml
kubectl apply -f api-gateway-configmap.yaml

# 3. Create secrets (see Step 2 above)

# 4. Deploy Inspection Service
kubectl apply -f inspection-service-deployment.yaml
kubectl apply -f inspection-service-service.yaml
kubectl apply -f inspection-service-hpa.yaml

# 5. Deploy API Gateway
kubectl apply -f api-gateway-deployment.yaml
kubectl apply -f api-gateway-service.yaml
kubectl apply -f api-gateway-ingress.yaml
```

## Common Commands

### View Logs
```bash
# Inspection Service
kubectl logs -n digital-inspection -l app=inspection-service --tail=100 -f

# API Gateway
kubectl logs -n digital-inspection -l app=api-gateway --tail=100 -f
```

### Scale Services
```bash
# Manual scaling
kubectl scale deployment inspection-service --replicas=5 -n digital-inspection
kubectl scale deployment api-gateway --replicas=5 -n digital-inspection
```

### Port Forward for Testing
```bash
# Forward Inspection Service
kubectl port-forward -n digital-inspection svc/inspection-service 8080:80

# Forward API Gateway
kubectl port-forward -n digital-inspection svc/api-gateway 8081:80
```

### Check Health
```bash
# Inspection Service
kubectl port-forward -n digital-inspection svc/inspection-service 8080:80 &
curl http://localhost:8080/health/ready
curl http://localhost:8080/health/live

# API Gateway
kubectl port-forward -n digital-inspection svc/api-gateway 8081:80 &
curl http://localhost:8081/health/ready
curl http://localhost:8081/health/live
```

### Restart Deployments
```bash
kubectl rollout restart deployment inspection-service -n digital-inspection
kubectl rollout restart deployment api-gateway -n digital-inspection
```

### View Events
```bash
kubectl get events -n digital-inspection --sort-by='.lastTimestamp'
```

## Troubleshooting Quick Fixes

### Pods Not Starting
```bash
# Describe pod to see events
kubectl describe pod <pod-name> -n digital-inspection

# Check logs
kubectl logs <pod-name> -n digital-inspection

# Check if secrets exist
kubectl get secrets -n digital-inspection
```

### Service Not Accessible
```bash
# Check endpoints
kubectl get endpoints -n digital-inspection

# Check service
kubectl describe svc <service-name> -n digital-inspection
```

### Image Pull Errors
```bash
# Check if image exists in ACR
az acr repository list --name <your-acr-name>

# Create image pull secret if needed
kubectl create secret docker-registry acr-secret \
  --docker-server=<your-acr-name>.azurecr.io \
  --docker-username=<service-principal-id> \
  --docker-password=<service-principal-password> \
  --namespace=digital-inspection
```

## Cleanup

```bash
# Using script
chmod +x cleanup.sh
./cleanup.sh

# Or manually
kubectl delete namespace digital-inspection
```

## Next Steps

1. Configure custom domain for Ingress
2. Set up cert-manager for automatic TLS certificates
3. Configure monitoring and alerting
4. Set up CI/CD pipeline for automated deployments
5. Implement network policies for enhanced security

## Support

For detailed documentation, see [README.md](README.md)
