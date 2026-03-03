# Kubernetes Deployment Manifests

This directory contains Kubernetes manifests for deploying the Digital Inspection System to Azure Kubernetes Service (AKS).

## Architecture Overview

The system consists of:
- **API Gateway**: External-facing gateway with authentication and routing (LoadBalancer)
- **Inspection Service**: Internal microservice for inspection management (ClusterIP)

## Prerequisites

- Azure Kubernetes Service (AKS) cluster
- kubectl configured to access your cluster
- Azure Container Registry (ACR) for container images
- Azure Entra ID (formerly Azure AD) configured for authentication
- Azure resources: PostgreSQL, Redis, Service Bus, Application Insights

## Directory Structure

```
k8s/
├── namespace.yaml                          # Namespace definition
├── inspection-service-deployment.yaml      # Inspection Service deployment
├── inspection-service-service.yaml         # Inspection Service ClusterIP service
├── inspection-service-hpa.yaml             # Inspection Service autoscaler
├── inspection-service-configmap.yaml       # Inspection Service configuration
├── inspection-service-secrets.yaml         # Inspection Service secrets (template)
├── api-gateway-deployment.yaml             # API Gateway deployment
├── api-gateway-service.yaml                # API Gateway LoadBalancer service
├── api-gateway-ingress.yaml                # API Gateway Ingress with TLS
├── api-gateway-configmap.yaml              # API Gateway configuration
├── api-gateway-secrets.yaml                # API Gateway secrets (template)
└── README.md                               # This file
```

## Deployment Instructions

### 1. Create Namespace

```bash
kubectl apply -f namespace.yaml
```

### 2. Configure Secrets

**IMPORTANT**: Do NOT commit actual secrets to version control. Use one of these approaches:

#### Option A: Azure Key Vault with CSI Driver (Recommended)
```bash
# Install Azure Key Vault CSI driver
helm repo add csi-secrets-store-provider-azure https://azure.github.io/secrets-store-csi-driver-provider-azure/charts
helm install csi csi-secrets-store-provider-azure/csi-secrets-store-provider-azure
```

#### Option B: Manual Secrets Creation
```bash
# Create Inspection Service secrets
kubectl create secret generic inspection-service-secrets \
  --from-literal=database-connection-string="YOUR_DB_CONNECTION_STRING" \
  --from-literal=redis-connection-string="YOUR_REDIS_CONNECTION_STRING" \
  --from-literal=servicebus-connection-string="YOUR_SERVICEBUS_CONNECTION_STRING" \
  --from-literal=appinsights-connection-string="YOUR_APPINSIGHTS_CONNECTION_STRING" \
  --namespace=digital-inspection

# Create API Gateway secrets
kubectl create secret generic api-gateway-secrets \
  --from-literal=appinsights-connection-string="YOUR_APPINSIGHTS_CONNECTION_STRING" \
  --namespace=digital-inspection
```

### 3. Configure ConfigMaps

Update the ConfigMap files with your actual values:
- `inspection-service-configmap.yaml`: Update Azure AD tenant and client IDs
- `api-gateway-configmap.yaml`: Update Azure AD authority, tenant, and client IDs

```bash
kubectl apply -f inspection-service-configmap.yaml
kubectl apply -f api-gateway-configmap.yaml
```

### 4. Deploy Inspection Service

```bash
# Deploy the service
kubectl apply -f inspection-service-deployment.yaml
kubectl apply -f inspection-service-service.yaml
kubectl apply -f inspection-service-hpa.yaml

# Verify deployment
kubectl get pods -n digital-inspection -l app=inspection-service
kubectl get svc -n digital-inspection inspection-service
kubectl get hpa -n digital-inspection inspection-service-hpa
```

### 5. Deploy API Gateway

```bash
# Deploy the gateway
kubectl apply -f api-gateway-deployment.yaml
kubectl apply -f api-gateway-service.yaml
kubectl apply -f api-gateway-ingress.yaml

# Verify deployment
kubectl get pods -n digital-inspection -l app=api-gateway
kubectl get svc -n digital-inspection api-gateway
kubectl get ingress -n digital-inspection api-gateway-ingress
```

### 6. Verify Deployment

```bash
# Check all resources
kubectl get all -n digital-inspection

# Check pod logs
kubectl logs -n digital-inspection -l app=inspection-service --tail=50
kubectl logs -n digital-inspection -l app=api-gateway --tail=50

# Check health endpoints
kubectl port-forward -n digital-inspection svc/inspection-service 8080:80
curl http://localhost:8080/health/ready
```

## Configuration Details

### Inspection Service

**Deployment Specifications:**
- Replicas: 3 (minimum)
- Resource Requests: 256Mi memory, 250m CPU
- Resource Limits: 512Mi memory, 500m CPU
- Health Checks: Liveness and readiness probes on `/health/live` and `/health/ready`

**Autoscaling:**
- Min Replicas: 3
- Max Replicas: 10
- CPU Target: 70% utilization
- Memory Target: 80% utilization

**Service Type:** ClusterIP (internal only)

### API Gateway

**Deployment Specifications:**
- Replicas: 3 (minimum)
- Resource Requests: 128Mi memory, 100m CPU
- Resource Limits: 256Mi memory, 250m CPU
- Health Checks: Liveness and readiness probes on `/health/live` and `/health/ready`

**Service Type:** LoadBalancer (external access)

**Ingress:**
- TLS enabled with cert-manager
- Rate limiting: 100 requests per minute
- Host: `api.digitalinspection.example.com` (update with your domain)

## Security Considerations

1. **Secrets Management**: Use Azure Key Vault or Kubernetes Secrets with encryption at rest
2. **Network Policies**: Consider implementing network policies to restrict pod-to-pod communication
3. **RBAC**: Configure Role-Based Access Control for cluster access
4. **Pod Security**: Containers run as non-root users with dropped capabilities
5. **TLS**: All external traffic uses HTTPS via Ingress with TLS certificates

## Monitoring and Observability

All services are configured with:
- **Application Insights**: Distributed tracing and metrics
- **Health Checks**: Kubernetes liveness and readiness probes
- **Logging**: Structured logging to stdout (collected by AKS)
- **Metrics**: Exposed for Prometheus scraping (if configured)

## Scaling

### Manual Scaling
```bash
# Scale Inspection Service
kubectl scale deployment inspection-service --replicas=5 -n digital-inspection

# Scale API Gateway
kubectl scale deployment api-gateway --replicas=5 -n digital-inspection
```

### Autoscaling
The HorizontalPodAutoscaler automatically scales based on CPU and memory metrics.

## Troubleshooting

### Pod Not Starting
```bash
kubectl describe pod <pod-name> -n digital-inspection
kubectl logs <pod-name> -n digital-inspection
```

### Service Not Accessible
```bash
kubectl get endpoints -n digital-inspection
kubectl describe svc <service-name> -n digital-inspection
```

### Ingress Issues
```bash
kubectl describe ingress api-gateway-ingress -n digital-inspection
kubectl logs -n ingress-nginx -l app.kubernetes.io/name=ingress-nginx
```

## Cleanup

To remove all resources:
```bash
kubectl delete namespace digital-inspection
```

## Additional Resources

- [Azure Kubernetes Service Documentation](https://docs.microsoft.com/en-us/azure/aks/)
- [Kubernetes Documentation](https://kubernetes.io/docs/)
- [YARP Reverse Proxy](https://microsoft.github.io/reverse-proxy/)
- [Azure Entra ID Authentication](https://docs.microsoft.com/en-us/azure/active-directory/)
