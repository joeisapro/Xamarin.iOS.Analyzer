using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xamarin.iOS.Analyzer
{
    public abstract class DeprecatedApiAnalyzer
    {
        protected SyntaxTree _tree;
        protected SemanticModel _model;

        public DeprecatedApiAnalyzer(SyntaxTree tree, SemanticModel model)
        {
            _tree = tree;
            _model = model;
        }

        public List<Tuple<CSharpSyntaxNode, ISymbol, ulong>> DeprecatedElements { get; } = new List<Tuple<CSharpSyntaxNode, ISymbol, ulong>>();
        public Type NsObject { get; set; }
        public Type Availability { get; set; }
        public Type Platform { get; set; }
        public ulong SdkVersion { get; set; }

        public abstract void Analyze();

        public virtual void OutputDeprecatedApiWarnings(TextWriter writer)
        {
            foreach (var element in DeprecatedElements)
            {
                //get line number
                var ls = _tree.GetLineSpan(element.Item1.Span);
                writer.WriteLine("{{{0}, {1}}} WARNING: {2} has been deprecated in {3}",
                                Path.GetFileName(_tree.FilePath), ls.StartLinePosition.Line, element.Item2.Name, Enum.GetName(Platform, element.Item3).Substring(4).Replace('_', '.'));
            }
        }

        protected ulong GetDeprecatedVersion(object availabilityAttribute)
        {
            var pi = availabilityAttribute.GetType().GetProperty("Deprecated");
            return (ulong)pi.GetValue(availabilityAttribute);
        }
    }
}
