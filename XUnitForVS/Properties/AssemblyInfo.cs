
using System.Runtime.ConstrainedExecution;
using System.Reflection;

// Consistency.MayCorruptAppDomain: An exceptional condition while executing
// the code in this assembly may corrupt the app domain where this code is
// running. No guarantee is made that app domain state is valid in an
// exceptional condition.
// Cer.None: This code knows nothing about CERs.
[assembly: ReliabilityContract(Consistency.MayCorruptAppDomain, Cer.None)]
[assembly: AssemblyTitleAttribute("xunit.runner.visualstudio.vs2010")]
[assembly: AssemblyDescriptionAttribute("xUnit test runner for Visual Studio 2010")]
[assembly: AssemblyCompanyAttribute("quetzalcoatl")]
[assembly: AssemblyCopyrightAttribute("2012")]
[assembly: AssemblyVersionAttribute("2.0.0.0")]
[assembly: AssemblyFileVersionAttribute("2.0.0.0")]
