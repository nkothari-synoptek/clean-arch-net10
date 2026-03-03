#!/bin/bash

# Digital Inspection System - Kubernetes Cleanup Script
# This script removes all Kubernetes resources

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

confirm_deletion() {
    print_warning "This will delete ALL resources in the $NAMESPACE namespace!"
    print_warning "This action cannot be undone."
    echo ""
    read -p "Are you sure you want to continue? (yes/no) " -r
    echo
    if [[ ! $REPLY =~ ^[Yy][Ee][Ss]$ ]]; then
        print_info "Cleanup cancelled."
        exit 0
    fi
}

delete_resources() {
    print_info "Deleting Kubernetes resources..."
    
    # Delete in reverse order of creation
    print_info "Deleting API Gateway resources..."
    $KUBECTL delete -f api-gateway-ingress.yaml --ignore-not-found=true
    $KUBECTL delete -f api-gateway-service.yaml --ignore-not-found=true
    $KUBECTL delete -f api-gateway-deployment.yaml --ignore-not-found=true
    
    print_info "Deleting Inspection Service resources..."
    $KUBECTL delete -f inspection-service-hpa.yaml --ignore-not-found=true
    $KUBECTL delete -f inspection-service-service.yaml --ignore-not-found=true
    $KUBECTL delete -f inspection-service-deployment.yaml --ignore-not-found=true
    
    print_info "Deleting ConfigMaps..."
    $KUBECTL delete -f api-gateway-configmap.yaml --ignore-not-found=true
    $KUBECTL delete -f inspection-service-configmap.yaml --ignore-not-found=true
    
    print_info "Checking for secrets..."
    if $KUBECTL get secret inspection-service-secrets -n $NAMESPACE &> /dev/null; then
        read -p "Delete Inspection Service secrets? (y/n) " -n 1 -r
        echo
        if [[ $REPLY =~ ^[Yy]$ ]]; then
            $KUBECTL delete secret inspection-service-secrets -n $NAMESPACE
        fi
    fi
    
    if $KUBECTL get secret api-gateway-secrets -n $NAMESPACE &> /dev/null; then
        read -p "Delete API Gateway secrets? (y/n) " -n 1 -r
        echo
        if [[ $REPLY =~ ^[Yy]$ ]]; then
            $KUBECTL delete secret api-gateway-secrets -n $NAMESPACE
        fi
    fi
}

delete_namespace() {
    print_info "Deleting namespace: $NAMESPACE"
    read -p "Delete the entire namespace? (y/n) " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        $KUBECTL delete namespace $NAMESPACE --ignore-not-found=true
        print_info "Namespace deleted."
    else
        print_info "Namespace preserved."
    fi
}

verify_cleanup() {
    print_info "Verifying cleanup..."
    
    if $KUBECTL get namespace $NAMESPACE &> /dev/null; then
        echo ""
        print_info "Remaining resources in namespace:"
        $KUBECTL get all -n $NAMESPACE
    else
        print_info "Namespace $NAMESPACE has been deleted."
    fi
}

# Main execution
main() {
    print_info "Digital Inspection System - Cleanup Script"
    echo ""
    
    if ! command -v kubectl &> /dev/null; then
        print_error "kubectl is not installed."
        exit 1
    fi
    
    if ! $KUBECTL get namespace $NAMESPACE &> /dev/null; then
        print_warning "Namespace $NAMESPACE does not exist."
        exit 0
    fi
    
    confirm_deletion
    delete_resources
    delete_namespace
    verify_cleanup
    
    echo ""
    print_info "Cleanup completed!"
}

# Run main function
main
