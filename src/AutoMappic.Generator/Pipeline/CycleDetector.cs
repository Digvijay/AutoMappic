using System.Collections.Generic;
using AutoMappic.Generator.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace AutoMappic.Generator.Pipeline;

internal static class CycleDetector
{
    public static IEnumerable<Diagnostic> Detect(IEnumerable<MappingModel> models)
    {
        var modelMap = new Dictionary<string, MappingModel>(System.StringComparer.Ordinal);
        foreach (var m in models)
        {
            var key = GetKey(m.SourceTypeFullName, m.DestinationTypeFullName);
            if (!modelMap.ContainsKey(key)) modelMap[key] = m;
        }

        var visited = new HashSet<string>(System.StringComparer.Ordinal);
        var stack = new HashSet<string>(System.StringComparer.Ordinal);
        var diagnostics = new List<Diagnostic>();

        foreach (var key in modelMap.Keys)
        {
            if (!visited.Contains(key))
            {
                DFS(key, modelMap, visited, stack, diagnostics);
            }
        }

        return diagnostics;
    }

    private static void DFS(
        string currentKey,
        Dictionary<string, MappingModel> modelMap,
        HashSet<string> visited,
        HashSet<string> stack,
        List<Diagnostic> diagnostics)
    {
        visited.Add(currentKey);
        stack.Add(currentKey);

        if (modelMap.TryGetValue(currentKey, out var model))
        {
            foreach (var prop in model.Properties)
            {
                if (prop.NestedSourceTypeFullName != null && prop.NestedDestTypeFullName != null)
                {
                    var childKey = GetKey(prop.NestedSourceTypeFullName, prop.NestedDestTypeFullName);

                    if (stack.Contains(childKey))
                    {
                        // Cycle detected!
                        var location = Location.Create(
                            model.FilePath ?? string.Empty,
                            new TextSpan(),
                            new LinePositionSpan(
                                new LinePosition(model.Line - 1, model.Column - 1),
                                new LinePosition(model.Line - 1, model.Column - 1)));

                        diagnostics.Add(Diagnostic.Create(
                            AutoMappicDiagnostics.CircularReference,
                            location,
                            model.SourceTypeFullName,
                            model.DestinationTypeFullName));

                        // Break to avoid multiple reports for the same model in one DFS run
                        break;
                    }

                    if (!visited.Contains(childKey))
                    {
                        DFS(childKey, modelMap, visited, stack, diagnostics);
                    }
                }
            }
        }

        stack.Remove(currentKey);
    }

    private static string GetKey(string source, string dest) => $"{source}_To_{dest}";
}
