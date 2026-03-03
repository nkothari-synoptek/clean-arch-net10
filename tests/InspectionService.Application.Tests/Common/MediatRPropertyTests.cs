using FsCheck;
using FsCheck.Xunit;
using InspectionService.Application.Inspections.Commands.CreateInspection;
using InspectionService.Application.Inspections.Commands.DeleteInspection;
using InspectionService.Application.Inspections.Commands.UpdateInspection;
using InspectionService.Application.Inspections.Queries.GetInspectionById;
using InspectionService.Application.Inspections.Queries.ListInspections;
using MediatR;
using System.Reflection;

namespace InspectionService.Application.Tests.Common;

/// <summary>
/// Property-based tests for MediatR command and query handling
/// **Validates: Requirements 3.2, 3.3**
/// </summary>
public class MediatRPropertyTests
{
    /// <summary>
    /// Property 3: MediatR Processes All Commands and Queries
    /// Tests that all commands are processed through dedicated command handlers
    /// and all queries are processed through dedicated query handlers.
    /// **Validates: Requirements 3.2, 3.3**
    /// </summary>
    [Property(MaxTest = 100)]
    public Property AllCommandsAndQueriesAreProcessedByHandlers()
    {
        return Prop.ForAll(
            GenerateCommandOrQueryScenario(),
            scenario =>
            {
                // Get the Application assembly
                var applicationAssembly = typeof(CreateInspectionCommand).Assembly;
                
                // Get the request type from the scenario
                var requestType = GetRequestType(scenario);
                
                if (requestType == null)
                    return false;
                
                // Find the handler for this request type
                var handlerFound = FindHandlerForRequest(applicationAssembly, requestType);
                
                // The property passes if a handler is found for the request
                return handlerFound;
            });
    }
    
    /// <summary>
    /// Gets the request type from the scenario
    /// </summary>
    private static Type? GetRequestType(CommandOrQueryScenario scenario)
    {
        return scenario.Type switch
        {
            ScenarioType.CreateCommand => typeof(CreateInspectionCommand),
            ScenarioType.UpdateCommand => typeof(UpdateInspectionCommand),
            ScenarioType.DeleteCommand => typeof(DeleteInspectionCommand),
            ScenarioType.GetByIdQuery => typeof(GetInspectionByIdQuery),
            ScenarioType.ListQuery => typeof(ListInspectionsQuery),
            _ => null
        };
    }
    
    /// <summary>
    /// Finds a handler for the given request type in the assembly
    /// </summary>
    private static bool FindHandlerForRequest(Assembly assembly, Type requestType)
    {
        // Get all types in the assembly
        var allTypes = assembly.GetTypes();
        
        // Find the IRequest<TResponse> interface implemented by the request type
        var requestInterface = requestType.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>));
        
        if (requestInterface == null)
            return false;
        
        // Get the response type
        var responseType = requestInterface.GetGenericArguments()[0];
        
        // Look for a handler that implements IRequestHandler<TRequest, TResponse>
        var handlerInterfaceType = typeof(IRequestHandler<,>).MakeGenericType(requestType, responseType);
        
        // Check if any type in the assembly implements this handler interface
        var handlerExists = allTypes.Any(t =>
            t.IsClass &&
            !t.IsAbstract &&
            t.GetInterfaces().Any(i =>
                i.IsGenericType &&
                i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>) &&
                i.GetGenericArguments()[0] == requestType &&
                i.GetGenericArguments()[1] == responseType));
        
        return handlerExists;
    }
    
    /// <summary>
    /// Generates arbitrary command or query scenarios for property testing
    /// </summary>
    private static Arbitrary<CommandOrQueryScenario> GenerateCommandOrQueryScenario()
    {
        var createCommandGen = Gen.Constant(new CommandOrQueryScenario { Type = ScenarioType.CreateCommand });
        var updateCommandGen = Gen.Constant(new CommandOrQueryScenario { Type = ScenarioType.UpdateCommand });
        var deleteCommandGen = Gen.Constant(new CommandOrQueryScenario { Type = ScenarioType.DeleteCommand });
        var getByIdQueryGen = Gen.Constant(new CommandOrQueryScenario { Type = ScenarioType.GetByIdQuery });
        var listQueryGen = Gen.Constant(new CommandOrQueryScenario { Type = ScenarioType.ListQuery });
        
        var allScenarios = Gen.OneOf(
            createCommandGen,
            updateCommandGen,
            deleteCommandGen,
            getByIdQueryGen,
            listQueryGen
        );
        
        return Arb.From(allScenarios);
    }
}

/// <summary>
/// Represents a command or query scenario for property testing
/// </summary>
public class CommandOrQueryScenario
{
    public ScenarioType Type { get; set; }
}

/// <summary>
/// Types of scenarios for testing
/// </summary>
public enum ScenarioType
{
    CreateCommand,
    UpdateCommand,
    DeleteCommand,
    GetByIdQuery,
    ListQuery
}
