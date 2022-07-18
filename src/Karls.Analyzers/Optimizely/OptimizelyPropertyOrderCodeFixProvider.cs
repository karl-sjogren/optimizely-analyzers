using System.Composition;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Roslynator.CSharp;

namespace Karls.Analyzers.Optimizely;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(OptimizelyPropertyOrderCodeFixProvider)), Shared]
public sealed class OptimizelyPropertyOrderCodeFixProvider : CodeFixProvider {
    public override ImmutableArray<string> FixableDiagnosticIds {
        get { return ImmutableArray.Create(DiagnosticIdentifiers.OptimizelyPropertyOrderShouldMatchSourceOrder); }
    }

    public override FixAllProvider? GetFixAllProvider() {
        return null;
    }

    public override async Task RegisterCodeFixesAsync(CodeFixContext context) {
        var root = await context.Document.GetSyntaxRootAsync();
        if(root == null)
            return;

        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        var declaration = root.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf().OfType<ClassDeclarationSyntax>().First();
        if(declaration == null)
            return;

        var classDeclarations = root.DescendantNodes().OfType<ClassDeclarationSyntax>();

        var title = "Order properties by display order";

        var codeAction = CodeAction.Create(
            title,
            cancellationToken => RefactorAsync(context.Document, declaration, cancellationToken),
            equivalenceKey: "ReorderPropertiesByDisplayOrder");

        context.RegisterCodeFix(codeAction, context.Diagnostics);
    }

    public static async Task<Solution> RefactorAsync(
            Document document,
            ClassDeclarationSyntax classDeclaration,
            CancellationToken cancellationToken) {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if(root == null) {
            throw new InvalidOperationException("Could not get syntax root");
        }

        var properties = OptimizelyPropertyOrderAnalyzer.GetOptimizelyPropertiesFromClass(classDeclaration);
        var orderedProperties = properties.OrderBy(x => x.Order).ToArray();

        var firstProperty = orderedProperties.First();

        var newClassDeclaration = firstProperty.PropertyDeclaration.Ancestors().OfType<ClassDeclarationSyntax>().First();
        newClassDeclaration = newClassDeclaration.InsertNodesBefore(newClassDeclaration.ChildNodes().OfType<PropertyDeclarationSyntax>().First(), orderedProperties.Select((x, index) => x.PropertyDeclaration.WithTriviaFrom(properties[index].PropertyDeclaration).WithFormatterAnnotation()));
        newClassDeclaration = newClassDeclaration.RemoveNodes(newClassDeclaration.ChildNodes().OfType<PropertyDeclarationSyntax>().Skip(orderedProperties.Length), SyntaxRemoveOptions.KeepNoTrivia);

        var newRoot = root;
        newRoot = newRoot!.ReplaceNode(classDeclaration, newClassDeclaration!);

        var newDocument = document.WithSyntaxRoot(newRoot);

        return newDocument.Project.Solution;
    }
}
