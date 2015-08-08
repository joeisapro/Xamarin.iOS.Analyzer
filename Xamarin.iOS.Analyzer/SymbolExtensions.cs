using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Xamarin.iOS.Analyzer
{
    public static class SymbolExtensions
    {
        public static bool IsSubclassOf(this ITypeSymbol typeSymbol, Type type)
        {
            //not supported at the moment
            if (typeSymbol == null || typeSymbol.Kind == SymbolKind.ArrayType)
                return false;

            var asm = typeSymbol.ContainingAssembly.Identity.Name;
            var baseType = typeSymbol;
            var typeName = typeSymbol.Name;
            bool found = false;

            var tmpAssName = type.Assembly.CustomAttributes.Where(p => p.AttributeType.Name == "AssemblyTitleAttribute").SingleOrDefault();
            var assName = tmpAssName.ConstructorArguments.First().Value.ToString();

            ITypeSymbol tmpType = null;
            while (baseType != null && !found)
            {
                tmpType = baseType;
                asm = baseType.ContainingAssembly.Identity.Name;
                typeName = baseType.Name;
                found = asm == assName && typeName == type.Name;

                baseType = baseType.BaseType;
            }

            return found;
        }

        public static object GetAttribute(this ISymbol symbol, Type type, MemberTypes memberType, bool doNotTraverseHierarchy = false)
        {
            var asm = symbol.ContainingType.ContainingAssembly.Identity.Name;
            var baseType = symbol.ContainingType;
            bool found = false;
            INamedTypeSymbol tmpType = null;

            var tmpAssName = type.Assembly.CustomAttributes.Where(p => p.AttributeType.Name == "AssemblyTitleAttribute").SingleOrDefault();
            var assName = tmpAssName.ConstructorArguments.First().Value.ToString();

            while (baseType != null && !found && !doNotTraverseHierarchy)
            {
                tmpType = baseType;
                asm = baseType.ContainingAssembly.Identity.Name;
                found = asm == assName;

                baseType = baseType.BaseType;
            }

            if (found || doNotTraverseHierarchy)
            {
                string name = tmpType.ToDisplayString();
                var mytype = type.Assembly.GetType(name);
                if (mytype != null)
                {
                    var mi = mytype.GetMember(symbol.Name, memberType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).FirstOrDefault();
                    if (mi != null)
                    {
                        var ca = mi.GetCustomAttributes(type, true);
                        if (ca != null && ca.Length > 0)
                            return ca.FirstOrDefault();
                    }
                }
            }

            return null;
        }

        public static object GetAttribute(this IMethodSymbol symbol, Type type, bool doNotTraverseHierarchy = false)
        {
            return symbol.GetAttribute(type, MemberTypes.Method);
        }

        public static object GetAttribute(this IPropertySymbol symbol, Type type, bool doNotTraverseHierarchy = false)
        {
            return symbol.GetAttribute(type, MemberTypes.Property);
        }

        public static object GetAttribute(this ITypeSymbol symbol, Type type, bool doNotTraverseHierarchy = false)
        {
            var asm = symbol.ContainingAssembly.Identity.Name;
            var baseType = symbol;
            bool found = false;
            ITypeSymbol tmpType = null;

            var tmpAssName = type.Assembly.CustomAttributes.Where(p => p.AttributeType.Name == "AssemblyTitleAttribute").SingleOrDefault();
            var assName = tmpAssName.ConstructorArguments.First().Value.ToString();

            while (baseType != null && !found && !doNotTraverseHierarchy)
            {
                tmpType = baseType;
                asm = baseType.ContainingAssembly.Identity.Name;
                found = asm == assName;

                baseType = baseType.BaseType;
            }

            if (found || doNotTraverseHierarchy)
            {
                string name = tmpType.ToDisplayString();
                var mytype = type.Assembly.GetType(name);
                var ca = mytype.GetCustomAttributes(type, true);
                if (ca != null && ca.Length > 0)
                    return ca.FirstOrDefault();
            }

            return null;
        }
    }
}
