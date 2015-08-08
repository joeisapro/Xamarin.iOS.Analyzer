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
    public class MemberInvocationAnalyzer : DeprecatedApiAnalyzer
    {
        public MemberInvocationAnalyzer(SyntaxTree tree, SemanticModel model)
            : base(tree, model)
        { }

        public override void Analyze()
        {
            var mas = _tree
                .GetRoot()
                .DescendantNodes()
                .OfType<MemberAccessExpressionSyntax>()
                .Select(p => new { SyntaxNode = p, Symbol = _model.GetSymbolInfo(p) })
                .Where(p => p.Symbol.Symbol != null);

            foreach (var ma in mas)
            {
                if (ma.Symbol.Symbol.ContainingType.IsSubclassOf(NsObject))
                {
                    object availability = null;
                    if (ma.Symbol.Symbol.Kind == SymbolKind.Property)
                        availability = ma.Symbol.Symbol.GetAttribute(Availability, MemberTypes.Property);
                    else
                        availability = ma.Symbol.Symbol.GetAttribute(Availability, MemberTypes.Method);

                    if (availability != null)
                    {
                        ulong version = GetDeprecatedVersion(availability);
                        if (version != 0 && version <= SdkVersion)
                            DeprecatedElements.Add(new Tuple<CSharpSyntaxNode, ISymbol, ulong>(ma.SyntaxNode, ma.Symbol.Symbol, version));
                    }
                }
            }
        }
    }
}
