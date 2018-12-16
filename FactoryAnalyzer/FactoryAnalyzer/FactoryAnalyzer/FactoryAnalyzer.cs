using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FactoryAnalyzer
{


    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class FactoryAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "FactoryOnlyClass";

        private static readonly string Title = "This class should be created by factory method";
        private static readonly string MessageFormat = "This class should be created by factory method Create of class {0}";
        private static readonly string Description = "This class should be created by factory method Create of class {0}";
        private const string Category = "Creation";

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterOperationAction(AnalyzeSymbol, OperationKind.ObjectCreation);
        }

        private static void AnalyzeSymbol(OperationAnalysisContext context)
        {
            var factoryMethod = GetBaseFactoryMethod(context.Operation.Type);

            if (factoryMethod != null)
            {
                var prop = ImmutableDictionary.CreateBuilder<string, string>();
                prop.Add("Name", factoryMethod.ContainingType.Name);

                var diagnostic = Diagnostic.Create(Rule,context.Operation.Syntax.GetLocation(),prop.ToImmutable(),factoryMethod.ContainingType.Name);

                context.ReportDiagnostic(diagnostic);
            }
        }

        public static IMethodSymbol GetBaseFactoryMethod(ITypeSymbol type)
        {
            var baseType = type.BaseType;
            if (baseType == null)
                return null;

            return baseType.OriginalDefinition
                                        .GetMembers("Create")
                                        .Where(x => x.Kind == SymbolKind.Method)
                                        .OfType<IMethodSymbol>()
                                        .FirstOrDefault(x => x.IsStatic 
                                                    && x.IsGenericMethod 
                                                    && x.TypeParameters.Length == 1 
                                                    && x.TypeParameters[0].HasConstructorConstraint 
                                                    && x.TypeParameters[0].ConstraintTypes.Any(t => t.Name == baseType.Name)
                                                    && Equals(x.ReturnType, x.TypeParameters[0])
                                               ) 
                   ?? GetBaseFactoryMethod(baseType);
                            
            
        }
    }
}
