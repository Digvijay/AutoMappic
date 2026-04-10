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
    ///   Provides a Roslyn CodeFix for AM0005 to automatically add a public parameterless constructor.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MissingConstructorCodeFixProvider)), Shared]
    internal sealed class MissingConstructorCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create("AM0005");

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null) return;

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the class declaration.
            var token = root.FindToken(diagnosticSpan.Start);
            var classDeclaration = token.Parent?.AncestorsAndSelf().OfType<ClassDeclarationSyntax>().FirstOrDefault();

            if (classDeclaration == null) return;

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "Add parameterless constructor",
                    createChangedDocument: c => AddConstructorAsync(context.Document, classDeclaration, c),
                    equivalenceKey: nameof(MissingConstructorCodeFixProvider)),
                diagnostic);
        }

        private async Task<Document> AddConstructorAsync(Document document, ClassDeclarationSyntax classDeclaration, CancellationToken cancellationToken)
        {
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

            // Create a public parameterless constructor.
            var constructor = SyntaxFactory.ConstructorDeclaration(classDeclaration.Identifier)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .WithBody(SyntaxFactory.Block());

            var updated = classDeclaration.AddMembers(constructor);
            editor.ReplaceNode(classDeclaration, updated);

            return editor.GetChangedDocument();
        }
    }
}
