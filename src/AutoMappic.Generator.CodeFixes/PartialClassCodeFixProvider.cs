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
    ///   Provides a Roslyn CodeFix for AM0018 to automatically add the 'partial' keyword to a class.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(PartialClassCodeFixProvider)), Shared]
    internal sealed class PartialClassCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create("AM0018");

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null) return;

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the class declaration identified by the diagnostic.
            var token = root.FindToken(diagnosticSpan.Start);
            var classDeclaration = token.Parent?.AncestorsAndSelf().OfType<ClassDeclarationSyntax>().FirstOrDefault();

            if (classDeclaration == null) return;

            // Register a code action that will add the 'partial' keyword.
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "Make class partial",
                    createChangedDocument: c => AddPartialKeywordAsync(context.Document, classDeclaration, c),
                    equivalenceKey: nameof(PartialClassCodeFixProvider)),
                diagnostic);
        }

        private async Task<Document> AddPartialKeywordAsync(Document document, ClassDeclarationSyntax classDeclaration, CancellationToken cancellationToken)
        {
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

            // Add 'partial' modifier.
            var partialKeyword = SyntaxFactory.Token(SyntaxKind.PartialKeyword);

            // SyntaxFactory.PartialClass is high-level but let's use the editor for safety.
            var updated = classDeclaration.AddModifiers(partialKeyword);

            editor.ReplaceNode(classDeclaration, updated);

            return editor.GetChangedDocument();
        }
    }
}
