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

namespace AutoMappic.Generator.CodeFixes
{
    /// <summary>
    ///   Provides a Roslyn CodeFix for AM0015 to automatically map a suggested property name.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SmartMatchCodeFixProvider)), Shared]
    internal sealed class SmartMatchCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create("AM0015");

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null) return;

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Read from diagnostic properties
            if (!diagnostic.Properties.TryGetValue("SuggestedName", out var suggestedName) || string.IsNullOrEmpty(suggestedName))
                return;

            // Find the property declaration identified by the diagnostic.
            var propertyToken = root.FindToken(diagnosticSpan.Start);
            var propertyDeclaration = propertyToken.Parent?.AncestorsAndSelf().OfType<PropertyDeclarationSyntax>().FirstOrDefault();

            if (propertyDeclaration == null) return;

            // Register a code action that will add the [MapProperty] attribute.
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: $"Map from '{suggestedName}'",
                    createChangedDocument: c => AddMapPropertyAttributeAsync(context.Document, propertyDeclaration, suggestedName!, c),
                    equivalenceKey: nameof(SmartMatchCodeFixProvider)),
                diagnostic);
        }

        private async Task<Document> AddMapPropertyAttributeAsync(Document document, PropertyDeclarationSyntax property, string suggestedName, CancellationToken cancellationToken)
        {
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

            // Define the [MapProperty("SuggestedName")] attribute.
            var attribute = editor.Generator.Attribute("MapProperty", editor.Generator.LiteralExpression(suggestedName));

            // Add the attribute to the property declaration.
            editor.AddAttribute(property, attribute);

            return editor.GetChangedDocument();
        }
    }
}
