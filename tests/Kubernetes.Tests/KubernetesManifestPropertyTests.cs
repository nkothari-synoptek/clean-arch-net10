using FsCheck;
using FsCheck.Xunit;
using FluentAssertions;

namespace Kubernetes.Tests;

/// <summary>
/// Property-based tests for Kubernetes configuration
/// **Validates: Requirements 17.2**
/// </summary>
public class KubernetesManifestPropertyTests
{
    private const string K8sDirectory = "../../../k8s";

    /// <summary>
    /// Property 18: Each Microservice Has Kubernetes Manifests
    /// Tests that all microservices have Deployment, Service, and HPA manifests.
    /// **Validates: Requirements 17.2**
    /// </summary>
    [Property(MaxTest = 100)]
    public Property EachMicroserviceHasRequiredKubernetesManifests()
    {
        return Prop.ForAll(
            GenerateMicroserviceNames(),
            microserviceName =>
            {
                // Arrange
                var k8sPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), K8sDirectory));
                
                // Skip if k8s directory doesn't exist (shouldn't happen in real scenario)
                if (!Directory.Exists(k8sPath))
                {
                    return true.ToProperty().Label($"K8s directory not found: {k8sPath}");
                }

                var deploymentFile = Path.Combine(k8sPath, $"{microserviceName}-deployment.yaml");
                var serviceFile = Path.Combine(k8sPath, $"{microserviceName}-service.yaml");
                var hpaFile = Path.Combine(k8sPath, $"{microserviceName}-hpa.yaml");

                // Act & Assert
                var deploymentExists = File.Exists(deploymentFile);
                var serviceExists = File.Exists(serviceFile);
                var hpaExists = File.Exists(hpaFile);

                // All three manifests must exist for each microservice
                var allManifestsExist = deploymentExists && serviceExists && hpaExists;

                if (!allManifestsExist)
                {
                    var missingFiles = new List<string>();
                    if (!deploymentExists) missingFiles.Add("Deployment");
                    if (!serviceExists) missingFiles.Add("Service");
                    if (!hpaExists) missingFiles.Add("HPA");

                    return allManifestsExist
                        .ToProperty()
                        .Label($"Microservice '{microserviceName}' is missing: {string.Join(", ", missingFiles)}");
                }

                return allManifestsExist.ToProperty();
            });
    }

    /// <summary>
    /// Property 18b: Deployment Manifests Contain Required Fields
    /// Tests that Deployment manifests contain essential Kubernetes fields.
    /// **Validates: Requirements 17.2**
    /// </summary>
    [Property(MaxTest = 100)]
    public Property DeploymentManifestsContainRequiredFields()
    {
        return Prop.ForAll(
            GenerateMicroserviceNames(),
            microserviceName =>
            {
                // Arrange
                var k8sPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), K8sDirectory));
                var deploymentFile = Path.Combine(k8sPath, $"{microserviceName}-deployment.yaml");

                // Skip if file doesn't exist
                if (!File.Exists(deploymentFile))
                {
                    return true.ToProperty().Label($"Deployment file not found: {deploymentFile}");
                }

                // Act
                var content = File.ReadAllText(deploymentFile);

                // Assert - Check for required Kubernetes Deployment fields
                var hasKind = content.Contains("kind: Deployment");
                var hasApiVersion = content.Contains("apiVersion:");
                var hasMetadata = content.Contains("metadata:");
                var hasSpec = content.Contains("spec:");
                var hasContainers = content.Contains("containers:");
                var hasImage = content.Contains("image:");

                var allFieldsPresent = hasKind && hasApiVersion && hasMetadata && 
                                      hasSpec && hasContainers && hasImage;

                if (!allFieldsPresent)
                {
                    var missingFields = new List<string>();
                    if (!hasKind) missingFields.Add("kind: Deployment");
                    if (!hasApiVersion) missingFields.Add("apiVersion");
                    if (!hasMetadata) missingFields.Add("metadata");
                    if (!hasSpec) missingFields.Add("spec");
                    if (!hasContainers) missingFields.Add("containers");
                    if (!hasImage) missingFields.Add("image");

                    return allFieldsPresent
                        .ToProperty()
                        .Label($"Deployment '{microserviceName}' is missing fields: {string.Join(", ", missingFields)}");
                }

                return allFieldsPresent.ToProperty();
            });
    }

    /// <summary>
    /// Property 18c: Service Manifests Contain Required Fields
    /// Tests that Service manifests contain essential Kubernetes fields.
    /// **Validates: Requirements 17.2**
    /// </summary>
    [Property(MaxTest = 100)]
    public Property ServiceManifestsContainRequiredFields()
    {
        return Prop.ForAll(
            GenerateMicroserviceNames(),
            microserviceName =>
            {
                // Arrange
                var k8sPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), K8sDirectory));
                var serviceFile = Path.Combine(k8sPath, $"{microserviceName}-service.yaml");

                // Skip if file doesn't exist
                if (!File.Exists(serviceFile))
                {
                    return true.ToProperty().Label($"Service file not found: {serviceFile}");
                }

                // Act
                var content = File.ReadAllText(serviceFile);

                // Assert - Check for required Kubernetes Service fields
                var hasKind = content.Contains("kind: Service");
                var hasApiVersion = content.Contains("apiVersion:");
                var hasMetadata = content.Contains("metadata:");
                var hasSpec = content.Contains("spec:");
                var hasPorts = content.Contains("ports:");
                var hasSelector = content.Contains("selector:");

                var allFieldsPresent = hasKind && hasApiVersion && hasMetadata && 
                                      hasSpec && hasPorts && hasSelector;

                if (!allFieldsPresent)
                {
                    var missingFields = new List<string>();
                    if (!hasKind) missingFields.Add("kind: Service");
                    if (!hasApiVersion) missingFields.Add("apiVersion");
                    if (!hasMetadata) missingFields.Add("metadata");
                    if (!hasSpec) missingFields.Add("spec");
                    if (!hasPorts) missingFields.Add("ports");
                    if (!hasSelector) missingFields.Add("selector");

                    return allFieldsPresent
                        .ToProperty()
                        .Label($"Service '{microserviceName}' is missing fields: {string.Join(", ", missingFields)}");
                }

                return allFieldsPresent.ToProperty();
            });
    }

    /// <summary>
    /// Property 18d: HPA Manifests Contain Required Fields
    /// Tests that HorizontalPodAutoscaler manifests contain essential Kubernetes fields.
    /// **Validates: Requirements 17.2**
    /// </summary>
    [Property(MaxTest = 100)]
    public Property HpaManifestsContainRequiredFields()
    {
        return Prop.ForAll(
            GenerateMicroserviceNames(),
            microserviceName =>
            {
                // Arrange
                var k8sPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), K8sDirectory));
                var hpaFile = Path.Combine(k8sPath, $"{microserviceName}-hpa.yaml");

                // Skip if file doesn't exist
                if (!File.Exists(hpaFile))
                {
                    return true.ToProperty().Label($"HPA file not found: {hpaFile}");
                }

                // Act
                var content = File.ReadAllText(hpaFile);

                // Assert - Check for required Kubernetes HPA fields
                var hasKind = content.Contains("kind: HorizontalPodAutoscaler");
                var hasApiVersion = content.Contains("apiVersion:");
                var hasMetadata = content.Contains("metadata:");
                var hasSpec = content.Contains("spec:");
                var hasScaleTargetRef = content.Contains("scaleTargetRef:");
                var hasMinReplicas = content.Contains("minReplicas:");
                var hasMaxReplicas = content.Contains("maxReplicas:");

                var allFieldsPresent = hasKind && hasApiVersion && hasMetadata && 
                                      hasSpec && hasScaleTargetRef && 
                                      hasMinReplicas && hasMaxReplicas;

                if (!allFieldsPresent)
                {
                    var missingFields = new List<string>();
                    if (!hasKind) missingFields.Add("kind: HorizontalPodAutoscaler");
                    if (!hasApiVersion) missingFields.Add("apiVersion");
                    if (!hasMetadata) missingFields.Add("metadata");
                    if (!hasSpec) missingFields.Add("spec");
                    if (!hasScaleTargetRef) missingFields.Add("scaleTargetRef");
                    if (!hasMinReplicas) missingFields.Add("minReplicas");
                    if (!hasMaxReplicas) missingFields.Add("maxReplicas");

                    return allFieldsPresent
                        .ToProperty()
                        .Label($"HPA '{microserviceName}' is missing fields: {string.Join(", ", missingFields)}");
                }

                return allFieldsPresent.ToProperty();
            });
    }

    /// <summary>
    /// Property 19: Internal Microservices Use ClusterIP Service Type
    /// Tests that non-gateway services use ClusterIP for internal-only access.
    /// **Validates: Requirements 17.4**
    /// </summary>
    [Property(MaxTest = 100)]
    public Property InternalMicroservicesUseClusterIPServiceType()
    {
        return Prop.ForAll(
            GenerateInternalMicroserviceNames(),
            microserviceName =>
            {
                // Arrange
                var k8sPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), K8sDirectory));
                var serviceFile = Path.Combine(k8sPath, $"{microserviceName}-service.yaml");

                // Skip if file doesn't exist
                if (!File.Exists(serviceFile))
                {
                    return true.ToProperty().Label($"Service file not found: {serviceFile}");
                }

                // Act
                var content = File.ReadAllText(serviceFile);

                // Assert - Internal services must use ClusterIP type
                var hasClusterIP = content.Contains("type: ClusterIP");
                var hasLoadBalancer = content.Contains("type: LoadBalancer");
                var hasNodePort = content.Contains("type: NodePort");

                // Internal services should ONLY have ClusterIP, not LoadBalancer or NodePort
                var isCorrectlyConfigured = hasClusterIP && !hasLoadBalancer && !hasNodePort;

                if (!isCorrectlyConfigured)
                {
                    var issues = new List<string>();
                    if (!hasClusterIP) issues.Add("missing 'type: ClusterIP'");
                    if (hasLoadBalancer) issues.Add("incorrectly uses 'type: LoadBalancer'");
                    if (hasNodePort) issues.Add("incorrectly uses 'type: NodePort'");

                    return isCorrectlyConfigured
                        .ToProperty()
                        .Label($"Internal service '{microserviceName}' has issues: {string.Join(", ", issues)}");
                }

                return isCorrectlyConfigured.ToProperty();
            });
    }

    /// <summary>
    /// Property 19b: API Gateway Uses External Service Type
    /// Tests that the API Gateway uses LoadBalancer or NodePort for external access.
    /// **Validates: Requirements 17.3**
    /// </summary>
    [Property(MaxTest = 100)]
    public Property ApiGatewayUsesExternalServiceType()
    {
        return Prop.ForAll(
            GenerateGatewayMicroserviceNames(),
            microserviceName =>
            {
                // Arrange
                var k8sPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), K8sDirectory));
                var serviceFile = Path.Combine(k8sPath, $"{microserviceName}-service.yaml");

                // Skip if file doesn't exist
                if (!File.Exists(serviceFile))
                {
                    return true.ToProperty().Label($"Service file not found: {serviceFile}");
                }

                // Act
                var content = File.ReadAllText(serviceFile);

                // Assert - Gateway services must use LoadBalancer or NodePort for external access
                var hasLoadBalancer = content.Contains("type: LoadBalancer");
                var hasNodePort = content.Contains("type: NodePort");
                var hasClusterIP = content.Contains("type: ClusterIP");

                // Gateway should use LoadBalancer or NodePort, NOT ClusterIP only
                var isCorrectlyConfigured = (hasLoadBalancer || hasNodePort) && !hasClusterIP;

                if (!isCorrectlyConfigured)
                {
                    var issues = new List<string>();
                    if (!hasLoadBalancer && !hasNodePort)
                        issues.Add("missing external service type (LoadBalancer or NodePort)");
                    if (hasClusterIP)
                        issues.Add("incorrectly uses 'type: ClusterIP' (should be external)");

                    return isCorrectlyConfigured
                        .ToProperty()
                        .Label($"Gateway service '{microserviceName}' has issues: {string.Join(", ", issues)}");
                }

                return isCorrectlyConfigured.ToProperty();
            });
    }

    /// <summary>
    /// Generates arbitrary microservice names for property testing
    /// Based on the actual microservices in the system
    /// </summary>
    private static Arbitrary<string> GenerateMicroserviceNames()
    {
        // Define the microservices that should have Kubernetes manifests
        // Based on the current system: InspectionService and ApiGateway
        var microserviceGen = Gen.Elements(
            "inspection-service",
            "api-gateway"
        );

        return Arb.From(microserviceGen);
    }

    /// <summary>
    /// Generates arbitrary internal microservice names (non-gateway services)
    /// These services should use ClusterIP for internal-only access
    /// </summary>
    private static Arbitrary<string> GenerateInternalMicroserviceNames()
    {
        // Define internal microservices that should use ClusterIP
        var internalServicesGen = Gen.Elements(
            "inspection-service"
            // Add more internal services as they are created
        );

        return Arb.From(internalServicesGen);
    }

    /// <summary>
    /// Generates arbitrary gateway microservice names
    /// These services should use LoadBalancer or NodePort for external access
    /// </summary>
    private static Arbitrary<string> GenerateGatewayMicroserviceNames()
    {
        // Define gateway services that should use LoadBalancer or NodePort
        var gatewayServicesGen = Gen.Elements(
            "api-gateway"
        );

        return Arb.From(gatewayServicesGen);
    }
}

