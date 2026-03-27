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

namespace AutoMappic.Generator
{
    /// <summary>
    ///   Provides a Roslyn CodeFix to suppress unmapped property errors by adding [AutoMappicIgnore].
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UnmappedPropertyCodeFixProvider)), Shared]
    internal sealed class UnmappedPropertyCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create("AM0001");

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null) return;

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the property declaration identified by the diagnostic.
            var propertyToken = root.FindToken(diagnosticSpan.Start);
            var propertyDeclaration = propertyToken.Parent?.AncestorsAndSelf().OfType<PropertyDeclarationSyntax>().FirstOrDefault();

            if (propertyDeclaration == null) return;

            // Register a code action that will add the [AutoMappicIgnore] attribute.
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "Ignore property in AutoMappic",
                    createChangedDocument: c => AddIgnoreAttributeAsync(context.Document, propertyDeclaration, c),
                    equivalenceKey: nameof(UnmappedPropertyCodeFixProvider)),
                diagnostic);
        }

        private async Task<Document> AddIgnoreAttributeAsync(Document document, PropertyDeclarationSyntax property, CancellationToken cancellationToken)
        {
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

            // Define the [AutoMappicIgnore] attribute.
            var attribute = editor.Generator.Attribute("AutoMappicIgnore");

            // Add the attribute to the property declaration.
            editor.AddAttribute(property, attribute);

            return editor.GetChangedDocument();
        }
    }
}
