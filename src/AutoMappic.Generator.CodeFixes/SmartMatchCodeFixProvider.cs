using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Text;

namespace AutoMappic.Generator.CodeFixes;

/// <summary>
///   Provides a Roslyn CodeFix for AM0015 to automatically map a suggested property name.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SmartMatchCodeFixProvider)), Shared]
internal sealed class SmartMatchCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ["AM0015"];

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

        // Read from diagnostic properties
        if (!diagnostic.Properties.TryGetValue("SuggestedName", out string? suggestedName) || string.IsNullOrEmpty(suggestedName))
        {
            return;
        }

        // Find the identifying node.
        SyntaxToken token = root.FindToken(diagnosticSpan.Start);
        SyntaxNode? node = token.Parent;

        // 1. Standalone Property - Add [MapProperty]
        PropertyDeclarationSyntax? propertyDecl = node?.AncestorsAndSelf().OfType<PropertyDeclarationSyntax>().FirstOrDefault();
        if (propertyDecl != null)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: $"Map from '{suggestedName}'",
                    createChangedDocument: c => AddMapPropertyAttributeAsync(context.Document, propertyDecl, suggestedName!, c),
                    equivalenceKey: nameof(SmartMatchCodeFixProvider)),
                diagnostic);
            return;
        }

        // 2. Profile CreateMap Call - Chain .ForMember()
        InvocationExpressionSyntax? invocation = node?.AncestorsAndSelf().OfType<InvocationExpressionSyntax>().FirstOrDefault();
        if (invocation != null && diagnostic.Properties.TryGetValue("TargetProperty", out string? targetProp))
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: $"Map from '{suggestedName}' in Profile",
                    createChangedDocument: c => AddForMemberMapAsync(context.Document, invocation, targetProp!, suggestedName!, c),
                    equivalenceKey: "MapInProfile"),
                diagnostic);
        }
    }

    private async Task<Document> AddMapPropertyAttributeAsync(Document document, PropertyDeclarationSyntax property, string suggestedName, CancellationToken cancellationToken)
    {
        DocumentEditor editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        SyntaxNode attribute = editor.Generator.Attribute("MapProperty", editor.Generator.LiteralExpression(suggestedName));
        editor.AddAttribute(property, attribute);
        return editor.GetChangedDocument();
    }

    private async Task<Document> AddForMemberMapAsync(Document document, InvocationExpressionSyntax invocation, string targetProp, string suggestedName, CancellationToken cancellationToken)
    {
        DocumentEditor editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

        InvocationExpressionSyntax updated = SyntaxFactory.InvocationExpression(
           SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, invocation, SyntaxFactory.IdentifierName("ForMember")),
           SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList([
                SyntaxFactory.Argument(SyntaxFactory.ParseExpression($"dest => dest.{targetProp}")),
                SyntaxFactory.Argument(SyntaxFactory.ParseExpression($"opt => opt.MapFrom(src => src.{suggestedName})"))
           ])));

        editor.ReplaceNode(invocation, updated);
        return editor.GetChangedDocument();
    }

}
