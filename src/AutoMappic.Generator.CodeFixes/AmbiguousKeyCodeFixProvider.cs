using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Text;

namespace AutoMappic.Generator.CodeFixes;

/// <summary>
///   Provides a Roslyn CodeFix to resolve ambiguous entity keys by adding [AutoMappicKey].
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AmbiguousKeyCodeFixProvider)), Shared]
internal sealed class AmbiguousKeyCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ["AM0017"];

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        SyntaxNode? root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
        {
            return;
        }

        Diagnostic diagnostic = context.Diagnostics.First();
        TextSpan diagnosticSpan = diagnostic.Location.SourceSpan;

        // AM0017 is reported on the mapping site (CreateMap or [AutoMap]).
        SemanticModel? semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel == null)
        {
            return;
        }

        if (!diagnostic.Properties.TryGetValue("ItemTypeName", out string? itemTypeName) || string.IsNullOrEmpty(itemTypeName))
        {
            return;
        }

        // For now, let's just use the current document's symbols to find the type.
        INamedTypeSymbol typeSymbol = semanticModel.Compilation.GetSymbolsWithName(itemTypeName!, SymbolFilter.Type).OfType<INamedTypeSymbol>().FirstOrDefault();
        if (typeSymbol == null)
        {
            return;
        }

        // List candidate properties (ending in Id or named Key)
        var candidates = typeSymbol.GetMembers().OfType<IPropertySymbol>()
            .Where(p => p.Name.EndsWith("Id", System.StringComparison.OrdinalIgnoreCase) ||
                        p.Name.Equals("Key", System.StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (IPropertySymbol? candidate in candidates)
        {
            SyntaxReference? syntaxRef = candidate.DeclaringSyntaxReferences.FirstOrDefault();
            if (syntaxRef == null)
            {
                continue;
            }

            Document? candidateDoc = context.Document.Project.Solution.GetDocument(syntaxRef.SyntaxTree);
            if (candidateDoc == null)
            {
                continue;
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: $"Use '{candidate.Name}' as AutoMappicKey",
                    createChangedSolution: c => AddKeyAttributeAsync(context.Document.Project.Solution, syntaxRef, c),
                    equivalenceKey: $"UseKey_{candidate.Name}"),
                diagnostic);
        }
    }

    private async Task<Solution> AddKeyAttributeAsync(Solution solution, SyntaxReference syntaxRef, CancellationToken cancellationToken)
    {
        Document document = solution.GetDocument(syntaxRef.SyntaxTree)!;
        SyntaxNode root = await syntaxRef.SyntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);

        if (root.FindNode(syntaxRef.Span) is not PropertyDeclarationSyntax node)
        {
            return solution;
        }

        DocumentEditor editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

        // Ensure AutoMappic namespace is available or use full name
        editor.AddAttribute(node, editor.Generator.Attribute("global::AutoMappic.AutoMappicKey"));

        return editor.GetChangedDocument().Project.Solution;
    }
}
