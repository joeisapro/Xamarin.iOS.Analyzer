using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Xamarin.iOS.Analyzer
{
    public class IdentifierAnalyzer : DeprecatedApiAnalyzer
    {
        public IdentifierAnalyzer(SyntaxTree tree, SemanticModel model)
            : base(tree, model)
        { }

        public override void Analyze()
        {
            var identifiers = _tree
                .GetRoot()
                .DescendantNodes()
                .OfType<IdentifierNameSyntax>()
                .Select(p => new { SyntaxNode = p, Symbol = _model.GetSymbolInfo(p) })
                .Select(p => new { p.SyntaxNode, Symbol = p.Symbol.Symbol != null ? p.Symbol.Symbol : p.Symbol.CandidateSymbols.FirstOrDefault() })
                .Where(p => p.Symbol != null)
                .Where(p => p.Symbol.Kind == SymbolKind.Property || p.Symbol.Kind == SymbolKind.Method);

            foreach (var identifier in identifiers)
            {
                if (identifier.Symbol.ContainingType.IsSubclassOf(NsObject))
                {
                    object availability = null;
                    if (identifier.Symbol.Kind == SymbolKind.Property)
                        availability = identifier.Symbol.GetAttribute(Availability, MemberTypes.Property);
                    else
                        availability = identifier.Symbol.GetAttribute(Availability, MemberTypes.Method);

                    if (availability != null)
                    {
                        ulong version = GetDeprecatedVersion(availability);
                        if (version != 0 && version <= SdkVersion)
                            DeprecatedElements.Add(new Tuple<CSharpSyntaxNode, ISymbol, ulong>(identifier.SyntaxNode, identifier.Symbol, version));
                    }
                }
            }
        }

        public void RemoveDuplicates(List<Tuple<CSharpSyntaxNode, ISymbol, ulong>> elements)
        {
            for (int i = DeprecatedElements.Count - 1; i >= 0; i--)
            {
                var element = DeprecatedElements[i];
                if (elements.Any(p => p.Item1 == element.Item1.Parent))
                    DeprecatedElements.RemoveAt(i);
            }
        }
    }
}
