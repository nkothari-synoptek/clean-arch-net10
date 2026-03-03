#!/bin/bash

# Digital Inspection System - Kubernetes Deployment Script
# This script deploys all Kubernetes resources to AKS

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Configuration
NAMESPACE="digital-inspection"
KUBECTL="kubectl"

# Functions
print_info() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARN]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

check_prerequisites() {
    print_info "Checking prerequisites..."
    
    if ! command -v kubectl &> /dev/null; then
        print_error "kubectl is not installed. Please install kubectl first."
        exit 1
    fi
    
    if ! kubectl cluster-info &> /dev/null; then
        print_error "Cannot connect to Kubernetes cluster. Please configure kubectl."
        exit 1
    fi
    
    print_info "Prerequisites check passed."
}

create_namespace() {
    print_info "Creating namespace: $NAMESPACE"
    $KUBECTL apply -f namespace.yaml
}

deploy_configmaps() {
    print_info "Deploying ConfigMaps..."
    
    print_warning "Please ensure you have updated the ConfigMap files with your actual values!"
    read -p "Have you updated the ConfigMaps? (y/n) " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        print_error "Please update ConfigMaps before deploying."
        exit 1
    fi
    
    $KUBECTL apply -f inspection-service-configmap.yaml
    $KUBECTL apply -f api-gateway-configmap.yaml
}

deploy_secrets() {
    print_info "Checking secrets..."
    
    if $KUBECTL get secret inspection-service-secrets -n $NAMESPACE &> /dev/null; then
        print_info "Inspection Service secrets already exist."
    else
        print_warning "Inspection Service secrets not found!"
        print_warning "Please create secrets manually or use Azure Key Vault."
        print_warning "See README.md for instructions."
        read -p "Continue anyway? (y/n) " -n 1 -r
        echo
        if [[ ! $REPLY =~ ^[Yy]$ ]]; then
            exit 1
        fi
    fi
    
    if $KUBECTL get secret api-gateway-secrets -n $NAMESPACE &> /dev/null; then
        print_info "API Gateway secrets already exist."
    else
        print_warning "API Gateway secrets not found!"
        print_warning "Please create secrets manually or use Azure Key Vault."
        print_warning "See README.md for instructions."
        read -p "Continue anyway? (y/n) " -n 1 -r
        echo
        if [[ ! $REPLY =~ ^[Yy]$ ]]; then
            exit 1
        fi
    fi
}

deploy_inspection_service() {
    print_info "Deploying Inspection Service..."
    
    $KUBECTL apply -f inspection-service-deployment.yaml
    $KUBECTL apply -f inspection-service-service.yaml
    $KUBECTL apply -f inspection-service-hpa.yaml
    
    print_info "Waiting for Inspection Service to be ready..."
    $KUBECTL wait --for=condition=available --timeout=300s \
        deployment/inspection-service -n $NAMESPACE || true
}

deploy_api_gateway() {
    print_info "Deploying API Gateway..."
    
    $KUBECTL apply -f api-gateway-deployment.yaml
    $KUBECTL apply -f api-gateway-service.yaml
    $KUBECTL apply -f api-gateway-ingress.yaml
    
    print_info "Waiting for API Gateway to be ready..."
    $KUBECTL wait --for=condition=available --timeout=300s \
        deployment/api-gateway -n $NAMESPACE || true
}

verify_deployment() {
    print_info "Verifying deployment..."
    
    echo ""
    print_info "Pods:"
    $KUBECTL get pods -n $NAMESPACE
    
    echo ""
    print_info "Services:"
    $KUBECTL get svc -n $NAMESPACE
    
    echo ""
    print_info "Ingress:"
    $KUBECTL get ingress -n $NAMESPACE
    
    echo ""
    print_info "HPA:"
    $KUBECTL get hpa -n $NAMESPACE
}

show_access_info() {
    print_info "Deployment complete!"
    echo ""
    print_info "To access the API Gateway:"
    
    EXTERNAL_IP=$($KUBECTL get svc api-gateway -n $NAMESPACE -o jsonpath='{.status.loadBalancer.ingress[0].ip}' 2>/dev/null || echo "pending")
    
    if [ "$EXTERNAL_IP" != "pending" ] && [ -n "$EXTERNAL_IP" ]; then
        echo "  External IP: $EXTERNAL_IP"
        echo "  HTTP: http://$EXTERNAL_IP"
    else
        echo "  External IP is still pending. Run the following command to check:"
        echo "  kubectl get svc api-gateway -n $NAMESPACE"
    fi
    
    echo ""
    print_info "To view logs:"
    echo "  Inspection Service: kubectl logs -n $NAMESPACE -l app=inspection-service --tail=50"
    echo "  API Gateway: kubectl logs -n $NAMESPACE -l app=api-gateway --tail=50"
    
    echo ""
    print_info "To check health:"
    echo "  kubectl port-forward -n $NAMESPACE svc/inspection-service 8080:80"
    echo "  curl http://localhost:8080/health/ready"
}

# Main execution
main() {
    print_info "Starting deployment of Digital Inspection System to Kubernetes..."
    echo ""
    
    check_prerequisites
    create_namespace
    deploy_configmaps
    deploy_secrets
    deploy_inspection_service
    deploy_api_gateway
    verify_deployment
    show_access_info
    
    echo ""
    print_info "Deployment script completed successfully!"
}

# Run main function
main
