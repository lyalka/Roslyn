using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;

namespace FactoryAnalyzer
{
    
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(FactoryAnalyzerCodeFixProvider)), Shared]
    public class FactoryAnalyzerCodeFixProvider : CodeFixProvider
    {
        private const string title = "Use factory method";

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(FactoryAnalyzer.DiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override Task  RegisterCodeFixesAsync(CodeFixContext context)
        {            
            var diagnostic = context.Diagnostics.First();
            
            context.RegisterCodeFix(CodeAction.Create(title, c => ReplaceWithFactoryMethod(context), title), diagnostic);

            return Task.CompletedTask;
        }

        private async Task<Document> ReplaceWithFactoryMethod(CodeFixContext context)
        {
            try
            {
                var root = await context.Document.GetSyntaxRootAsync();

                var location = context.Diagnostics[0].Location;
                var create = location.SourceTree
                                     .GetRoot()
                                     .FindNode(location.SourceSpan, false, true);

                var sourceType = (create as ObjectCreationExpressionSyntax)?.Type;


                var a = SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                SyntaxFactory.IdentifierName(context.Diagnostics[0].Properties["Name"]),
                                SyntaxFactory.GenericName(@"Create").AddTypeArgumentListArguments(sourceType)
                            )
                            .WithOperatorToken(SyntaxFactory.Token(SyntaxKind.DotToken)))
                    .WithArgumentList(SyntaxFactory.ArgumentList()
                        .WithOpenParenToken(SyntaxFactory.Token(SyntaxKind.OpenParenToken))
                        .WithCloseParenToken(SyntaxFactory.Token(SyntaxKind.CloseParenToken)));

                return context.Document.WithSyntaxRoot(root.ReplaceNode(create, a));
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
