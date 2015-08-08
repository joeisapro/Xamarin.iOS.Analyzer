using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Text;
using NDesk.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Xamarin.iOS.Analyzer
{
    class Program
    {
        private static Assembly _xamAsm = null;
        private static Type _nsobject = null;
        private static Type _availability = null;
        private static Type _platform = null;
        private static string _solutionPath = string.Empty;
        private static ulong? _sdkVersion = null;
        private static TextWriter writer = null;

        static void Main(string[] args)
        {
            if (ParseCommandLineArgs(args))
            {
                var ws = MSBuildWorkspace.Create();
                var solution = ws.OpenSolutionAsync(_solutionPath).Result;

                //add reference to xamarin.ios.dll 
                foreach (var project in solution.Projects)
                    solution = solution.AddMetadataReference(project.Id, MetadataReference.CreateFromFile(_xamAsm.Location));

                //loop through all projects
                foreach (var project in solution.Projects)
                {
                    Console.WriteLine("Analyzing project {0}", project.Name);
                    var compilation = project.GetCompilationAsync().Result;

                    foreach (var tree in compilation.SyntaxTrees)
                    {
                        var model = compilation.GetSemanticModel(tree);

                        var nsobjects = new NSObjectSubclassAnalyzer(tree, model) { Availability = _availability, NsObject = _nsobject, Platform = _platform, SdkVersion = _sdkVersion.Value };
                        nsobjects.Analyze();
                        nsobjects.OutputDeprecatedApiWarnings(Console.Out);

                        var variables = new VariableDeclarationAnalyzer(tree, model) { Availability = _availability, NsObject = _nsobject, Platform = _platform, SdkVersion = _sdkVersion.Value };
                        variables.Analyze();
                        variables.OutputDeprecatedApiWarnings(Console.Out);

                        var invocations = new MemberInvocationAnalyzer(tree, model) { Availability = _availability, NsObject = _nsobject, Platform = _platform, SdkVersion = _sdkVersion.Value };
                        invocations.Analyze();
                        invocations.OutputDeprecatedApiWarnings(Console.Out);

                        var identifiers = new IdentifierAnalyzer(tree, model) { Availability = _availability, NsObject = _nsobject, Platform = _platform, SdkVersion = _sdkVersion.Value };
                        identifiers.Analyze();

                        //this last analyzer picks up a lot of symbols already caught by the previous 2
                        //let's get rid of some duplicates if we can
                        identifiers.RemoveDuplicates(invocations.DeprecatedElements);
                        identifiers.RemoveDuplicates(variables.DeprecatedElements);
                        identifiers.OutputDeprecatedApiWarnings(Console.Out);
                    }
                }
            }
        }

        private static bool ParseCommandLineArgs(string[] args)
        {
            string sdkVersion = string.Empty;
            string output = string.Empty;
            bool showHelp = false;

            var options = new OptionSet()
                .Add("r=|reference=", "Path to Xamarin.iOS.dll - Optional", r => _xamAsm = Assembly.LoadFrom(r))
                .Add("s=|solution=", "Path to the solution to be analyzed - Required", s => _solutionPath = s)
                .Add("v=|version=", "Target SDK version (ie. 7.0) - Optional", v => sdkVersion = v)
                .Add("o=|out=", "Output filename - Optional (not implemented yet)", o => output = o)
                .Add("?|h|help", "Display this usage message", h => showHelp = true);

            options.Parse(args);

            if (showHelp)
            {
                ShowHelp(options);
                return false;
            }

            if (string.IsNullOrWhiteSpace(_solutionPath))
            {
                Console.WriteLine("Missing required option -s\nTry 'xamarin.ios.analyzer --help' for more information.");
                return false;
            }

            if (_xamAsm == null)
            {
                Console.WriteLine("Option -r not supplied.  Will attempt to auto-locate xamarin.ios.dll");
                _xamAsm = Assembly.LoadFrom(AutoLocateXamarinAssembly());
            }

            if (_xamAsm == null)
            {
                Console.WriteLine("Could not load xamarin.ios.dll\nTry 'xamarin.ios.analyzer --help' for more information.");
                return false;
            }

            _nsobject = _xamAsm.GetType("Foundation.NSObject", false, true);
            _availability = _xamAsm.GetType("ObjCRuntime.AvailabilityAttribute", false, true);
            _platform = _xamAsm.GetType("ObjCRuntime.Platform", false, true);

            if (_nsobject == null || _availability == null || _platform == null)
            {
                Console.WriteLine("ERROR: Unable to load expected types from Xamarin.iOS.dll");
                return false;
            }

            //load all possible sdk versions from assembly
            var pvs = Enum.GetValues(_platform)
                .OfType<object>()
                .Where(p => Enum.GetName(_platform, p).StartsWith("iOS_"))
                .Where(p => Enum.GetName(_platform, p).Length > 4)
                .Where(p => Char.IsDigit(Enum.GetName(_platform, p)[4]))
                .ToList();

            if (string.IsNullOrEmpty(sdkVersion))
            {
                _sdkVersion = pvs.Cast<ulong>().Max();
                Console.WriteLine("Option -v not supplied.  Defaulting to max available SDK version");
            }
            else
            {
                var sdk = "iOS_" + sdkVersion.Replace('.', '_');
                _sdkVersion = pvs.Where(p => Enum.GetName(_platform, p) == sdk)
                    .Cast<ulong>()
                    .SingleOrDefault();

                if (!_sdkVersion.HasValue)
                {
                    Console.WriteLine("Invalid SDK vesion value\nTry 'xamarin.ios.analyzer --help' for more information.");
                    return false;
                }
            }

            Console.WriteLine("The following parameters will be used for this session:");
            Console.WriteLine("\tDLL='{0}'", _xamAsm.Location);
            Console.WriteLine("\tSLN='{0}'", _solutionPath);
            Console.WriteLine("\tSDK={0}", Enum.GetName(_platform, _sdkVersion.Value).Substring(4).Replace('_', '.'));
            Console.WriteLine("==========================================================================================");

            return true;
        }

        public static string AutoLocateXamarinAssembly()
        {
            string name = "Xamarin.iOS.dll";

            //search current location first
            if (File.Exists(name))
                return name;

            //check reference assembly folder
            var path = string.Format(@"{0}\{1}\{2}", Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                @"Reference Assemblies\Microsoft\Framework\Xamarin.iOS\v1.0", name);

            if (File.Exists(path))
                return path;
            else
                return string.Empty;
        }

        private static void ShowHelp(OptionSet os)
        {
            Console.WriteLine("Syntax: xamarin.ios.analyzer [Options]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            os.WriteOptionDescriptions(Console.Out);
        }
    }
}
