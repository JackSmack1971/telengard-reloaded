using Telengard.Core;
using Xunit;

namespace Telengard.Architecture.Tests;

public sealed class DependencyBoundaryTests
{
    [Fact]
    public void Core_does_not_reference_presentation_or_engine_assemblies()
    {
        var forbidden = new[] { "Godot", "Telengard.Terminal", "Telengard.Godot" };
        var references = typeof(AssemblyBoundary).Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty);

        Assert.DoesNotContain(references, reference =>
            forbidden.Any(name => reference.Contains(name, StringComparison.OrdinalIgnoreCase)));
    }
}
