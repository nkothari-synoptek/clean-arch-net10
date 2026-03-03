# API Gateway

The API Gateway serves as the single entry point for all external client requests to the Digital Inspection System. It uses YARP (Yet Another Reverse Proxy) for routing and provides authentication, authorization, rate limiting, and request logging.

## Features

### 1. Azure Entra External Authentication
- JWT Bearer token validation
- Configured with Azure Entra ID (formerly Azure AD)
- Validates issuer, audience, lifetime, and signing keys
- Logs authentication events

### 2. Authorization Policies
The gateway enforces the following authorization policies:

- **CanCreateInspection**: Requires `Inspector` or `Admin` role
  - Used for: POST /api/inspections, PUT /api/inspections/{id}, DELETE /api/inspections/{id}
  
- **CanCompleteInspection**: Requires `Inspector` role
  - Used for: Completing inspection operations
  
- **CanViewAllInspections**: Requires `Admin` or `Supervisor` role
  - Used for: Viewing all inspections across the system

### 3. YARP Reverse Proxy Routing
Routes are configured to forward requests to the Inspection Service:

- **POST /api/inspections** → Inspection Service (requires CanCreateInspection)
- **PUT /api/inspections/{id}** → Inspection Service (requires CanCreateInspection)
- **DELETE /api/inspections/{id}** → Inspection Service (requires CanCreateInspection)
- **GET /api/inspections/{**catch-all}** → Inspection Service (authenticated users)

### 4. Rate Limiting
- Fixed window rate limiter: 100 requests per minute per user
- Uses user identity for authenticated requests
- Falls back to IP address for anonymous requests
- Returns 429 Too Many Requests when limit exceeded

### 5. Request Logging with Serilog
Structured logging includes:
- HTTP method, path, status code, and elapsed time
- Request host, scheme, and remote IP address
- User agent and authenticated user name
- Logs written to console and rolling file (logs/apigateway-{date}.log)

### 6. Health Checks
- Endpoint: `/health`
- Monitors gateway availability
- Used by Kubernetes liveness and readiness probes

## Configuration

### Azure Entra ID Setup

Update `appsettings.json` with your Azure Entra ID configuration:

```json
{
  "AzureAd": {
    "Authority": "https://login.microsoftonline.com/{your-tenant-id}",
    "ClientId": "{your-client-id}",
    "TenantId": "{your-tenant-id}"
  }
}
```

### YARP Routing Configuration

The reverse proxy configuration is in `appsettings.json` under the `ReverseProxy` section:

```json
{
  "ReverseProxy": {
    "Routes": {
      "inspection-create-route": {
        "ClusterId": "inspection-cluster",
        "AuthorizationPolicy": "CanCreateInspection",
        "Match": {
          "Path": "/api/inspections",
          "Methods": [ "POST" ]
        }
      }
    },
    "Clusters": {
      "inspection-cluster": {
        "Destinations": {
          "destination1": {
            "Address": "http://inspection-service:80"
          }
        }
      }
    }
  }
}
```

### Development Configuration

For local development, update `appsettings.Development.json`:

```json
{
  "ReverseProxy": {
    "Clusters": {
      "inspection-cluster": {
        "Destinations": {
          "destination1": {
            "Address": "http://localhost:5001"
          }
        }
      }
    }
  }
}
```

## Running the Gateway

### Local Development

```bash
cd src/ApiGateway
dotnet run
```

The gateway will start on `https://localhost:5000` (or the port specified in launchSettings.json).

### Docker

```bash
docker build -t apigateway:latest -f src/ApiGateway/Dockerfile .
docker run -p 8080:80 apigateway:latest
```

### Kubernetes

Deploy using the Kubernetes manifests (to be created in Task 11):

```bash
kubectl apply -f k8s/apigateway-deployment.yaml
kubectl apply -f k8s/apigateway-service.yaml
kubectl apply -f k8s/apigateway-ingress.yaml
```

## Testing Authentication

### Obtain a JWT Token

1. Register your application in Azure Entra ID
2. Configure redirect URIs and API permissions
3. Use OAuth 2.0 authorization code flow or client credentials flow to obtain a token

### Make Authenticated Requests

```bash
# Example: Create an inspection
curl -X POST https://localhost:5000/api/inspections \
  -H "Authorization: Bearer {your-jwt-token}" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Building Safety Inspection",
    "description": "Annual safety inspection",
    "inspectorId": "00000000-0000-0000-0000-000000000000",
    "scheduledDate": "2024-12-31T10:00:00Z",
    "items": [
      {
        "itemName": "Fire Extinguisher",
        "checklistCriteria": "Check expiration date and pressure"
      }
    ]
  }'
```

## Monitoring

### Logs

Logs are written to:
- Console (stdout) - for container environments
- File: `logs/apigateway-{date}.log` - rolling daily logs

### Health Checks

Check gateway health:

```bash
curl https://localhost:5000/health
```

## Security Considerations

1. **HTTPS Only**: The gateway enforces HTTPS redirection in production
2. **Token Validation**: All JWT tokens are validated against Azure Entra ID
3. **Rate Limiting**: Prevents abuse with per-user rate limits
4. **Internal Services**: Backend services (Inspection Service) are not exposed externally
5. **Least Privilege**: Authorization policies enforce role-based access control

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     External Clients                         │
│                  (Web, Mobile, Desktop)                      │
└────────────────────────┬────────────────────────────────────┘
                         │ HTTPS + JWT
                         ▼
┌─────────────────────────────────────────────────────────────┐
│                    API Gateway (YARP)                        │
│  ┌──────────────────────────────────────────────────────┐   │
│  │ 1. Authentication (Azure Entra External)             │   │
│  │ 2. Authorization (Role-based policies)               │   │
│  │ 3. Rate Limiting (100 req/min per user)              │   │
│  │ 4. Request Logging (Serilog)                         │   │
│  │ 5. Reverse Proxy Routing (YARP)                      │   │
│  └──────────────────────────────────────────────────────┘   │
└────────────────────────┬────────────────────────────────────┘
                         │ Internal HTTP (No Auth)
                         ▼
┌─────────────────────────────────────────────────────────────┐
│                    Inspection Service                        │
│                  (Internal ClusterIP Service)                │
└─────────────────────────────────────────────────────────────┘
```

## Next Steps

- Task 11: Create Kubernetes deployment manifests for the API Gateway
- Configure TLS certificates for production
- Set up Azure Application Insights for monitoring
- Configure additional routes for other microservices (Reporting, Master Data)
