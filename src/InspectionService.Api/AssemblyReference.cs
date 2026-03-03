using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("InspectionService.Api.Tests")]

namespace InspectionService.Api;

/// <summary>
/// Marker class for assembly reference in architecture tests
/// </summary>
public sealed class AssemblyReference
{
}
