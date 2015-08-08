# Xamarin.iOS.Analyzer
Xamarin Studio and Visual Studio do not generate compiler warning when using deprecated iOS APIs, as opposed to Xcode.  Fortunately, Xamarin.iOS types and their members are annotated with 'AvailabilityAttribute', which is the managed equivalent of Clang's availability __attribute__, which is used by Apple to annotate the SDK.

This windows console application uses Roslyn to analyze a Xamarin.iOS solution for deprecated API usage.  This was quicly put together as a proof of concept and work out pretty well for my use case.  Hopefully it can be useful for others as well.

USAGE
-----
Xamarin.iOS.Analyzer -s="path-to-your-solution"

This will attempt auto-locating a copy of Xamarin.iOS.dll and use the most recent SDK version the dll is built for.

When more control is needed, the following command line options can be used:

  -r, --reference=VALUE      Path to Xamarin.iOS.dll - Optional<br/>
  -s, --solution=VALUE       Path to the solution to be analyzed - Required<br/>
  -v, --version=VALUE        Target SDK version (ie. 7.0) - Optional<br/>
  -o, --out=VALUE            Output filename - Optional (not implemented yet)<br/>
  -?, -h, --help             Display this usage message<br/>
