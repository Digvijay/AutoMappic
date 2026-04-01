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

            // Find the identifying node.
            var token = root.FindToken(diagnosticSpan.Start);
            var node = token.Parent;

            // 1. Standalone Property - Add [AutoMappicIgnore]
            var propertyDecl = node?.AncestorsAndSelf().OfType<PropertyDeclarationSyntax>().FirstOrDefault();
            if (propertyDecl != null)
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: "Ignore property in AutoMappic",
                        createChangedDocument: c => AddIgnoreAttributeAsync(context.Document, propertyDecl, c),
                        equivalenceKey: nameof(UnmappedPropertyCodeFixProvider)),
                    diagnostic);
                return;
            }

            // 2. Profile CreateMap Call - Chain .ForMemberIgnore()
            var invocation = node?.AncestorsAndSelf().OfType<InvocationExpressionSyntax>().FirstOrDefault();
            if (invocation != null && diagnostic.Properties.TryGetValue("TargetProperty", out var targetProp))
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: $"Ignore '{targetProp}' in Profile",
                        createChangedDocument: c => AddForMemberIgnoreAsync(context.Document, invocation, targetProp!, c),
                        equivalenceKey: "IgnoreInProfile"),
                    diagnostic);
            }
        }

        private async Task<Document> AddIgnoreAttributeAsync(Document document, PropertyDeclarationSyntax property, CancellationToken cancellationToken)
        {
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            editor.AddAttribute(property, editor.Generator.Attribute("AutoMappicIgnore"));
            return editor.GetChangedDocument();
        }

        private async Task<Document> AddForMemberIgnoreAsync(Document document, InvocationExpressionSyntax invocation, string targetProp, CancellationToken cancellationToken)
        {
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

            var updated = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, invocation, SyntaxFactory.IdentifierName("ForMemberIgnore")),
                SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.Argument(SyntaxFactory.ParseExpression($"dest => dest.{targetProp}")))));

            editor.ReplaceNode(invocation, updated);
            return editor.GetChangedDocument();
        }

    }
}
