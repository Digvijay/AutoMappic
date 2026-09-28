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
///   Provides a Roslyn CodeFix for AM0018 to automatically add the 'partial' keyword to a class.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(PartialClassCodeFixProvider)), Shared]
internal sealed class PartialClassCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ["AM0018"];

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

        // Find the class declaration identified by the diagnostic.
        SyntaxToken token = root.FindToken(diagnosticSpan.Start);
        ClassDeclarationSyntax? classDeclaration = token.Parent?.AncestorsAndSelf().OfType<ClassDeclarationSyntax>().FirstOrDefault();

        if (classDeclaration == null)
        {
            return;
        }

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
        DocumentEditor editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

        // Add 'partial' modifier.
        SyntaxToken partialKeyword = SyntaxFactory.Token(SyntaxKind.PartialKeyword);

        // SyntaxFactory.PartialClass is high-level but let's use the editor for safety.
        ClassDeclarationSyntax updated = classDeclaration.AddModifiers(partialKeyword);

        editor.ReplaceNode(classDeclaration, updated);

        return editor.GetChangedDocument();
    }
}
