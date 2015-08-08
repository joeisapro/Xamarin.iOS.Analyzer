using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xamarin.iOS.Analyzer
{
    public class VariableDeclarationAnalyzer : DeprecatedApiAnalyzer
    {
        public VariableDeclarationAnalyzer(SyntaxTree tree, SemanticModel model)
            : base(tree, model)
        { }

        public override void Analyze()
        {
            var variables = _tree
                .GetRoot()
                .DescendantNodes()
                .OfType<VariableDeclaratorSyntax>()
                .Select(p => new { SyntaxNode = p, Symbol = _model.GetDeclaredSymbol(p) })
                .Where(p => p.Symbol != null);

            foreach (var variable in variables)
            {
                ILocalSymbol ils = variable.Symbol as ILocalSymbol;
                IFieldSymbol ifs = variable.Symbol as IFieldSymbol;

                if (ils != null || ifs != null)
                {
                    ITypeSymbol its = ils != null ? ils.Type : ifs.Type;

                    if (its.IsSubclassOf(NsObject))
                    {
                        var availability = its.GetAttribute(Availability);

                        if (availability != null)
                        {
                            ulong version = GetDeprecatedVersion(availability);
                            if (version != 0 && version <= SdkVersion)
                                DeprecatedElements.Add(new Tuple<CSharpSyntaxNode, ISymbol, ulong>(variable.SyntaxNode, its, version));
                        }
                    }
                }
            }
        }
    }
}
