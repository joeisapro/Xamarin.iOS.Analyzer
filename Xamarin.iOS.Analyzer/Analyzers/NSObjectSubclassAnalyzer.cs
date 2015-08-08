using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Xamarin.iOS.Analyzer
{
    public class NSObjectSubclassAnalyzer : DeprecatedApiAnalyzer
    {
        public NSObjectSubclassAnalyzer(SyntaxTree tree, SemanticModel model)
            : base(tree, model)
        { }

        public override void Analyze()
        {
            //get all overriden properties/methods
            var properties = _tree
                .GetRoot()
                .DescendantNodes()
                .OfType<PropertyDeclarationSyntax>()
                .Select(p => new { SyntaxNode = p, Symbol = _model.GetDeclaredSymbol(p) as IPropertySymbol })
                .Where(p => p.Symbol.IsOverride && p.Symbol.ContainingType.IsSubclassOf(NsObject))
                .GroupBy(p => p.Symbol.ContainingType, p => p, (key, g) => new { Key = key, Symbols = g });
            var methods = _tree
                .GetRoot()
                .DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .Select(p => new { SyntaxNode = p, Symbol = _model.GetDeclaredSymbol(p) as IMethodSymbol })
                .Where(p => p.Symbol.IsOverride && p.Symbol.ContainingType.IsSubclassOf(NsObject))
                .GroupBy(p => p.Symbol.ContainingType, p => p, (key, g) => new { Key = key, Symbols = g });

            //group by types
            var types = properties.Select(p => p.Key)
                            .Concat(methods.Select(p => p.Key))
                            .Distinct()
                            .OrderBy(p => p.Name);

            foreach (var type in types)
            {
                string name = type.Name;

                var prop = properties.Where(p => p.Key == type).SingleOrDefault();
                if (prop != null)
                    foreach (var propSymbol in prop.Symbols)
                    {
                        var availability = propSymbol.Symbol.GetAttribute(Availability);
                        if (availability != null)
                        {
                            ulong version = GetDeprecatedVersion(availability);
                            if (version != 0 && version <= SdkVersion)
                                DeprecatedElements.Add(new Tuple<CSharpSyntaxNode, ISymbol, ulong>(propSymbol.SyntaxNode, propSymbol.Symbol, version));
                        }
                    }

                var meth = methods.Where(p => p.Key == type).SingleOrDefault();
                if (meth != null)
                    foreach (var methSymbol in meth.Symbols)
                    {
                        var availability = methSymbol.Symbol.GetAttribute(Availability);
                        if (availability != null)
                        {
                            ulong version = GetDeprecatedVersion(availability);
                            if (version != 0 && version <= SdkVersion)
                                DeprecatedElements.Add(new Tuple<CSharpSyntaxNode, ISymbol, ulong>(methSymbol.SyntaxNode, methSymbol.Symbol, version));
                        }
                    }
            }
        }
    }
}
