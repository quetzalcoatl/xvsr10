using System;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;

// Consistency.MayCorruptAppDomain: An exceptional condition while executing
// the code in this assembly may corrupt the app domain where this code is
// running. No guarantee is made that app domain state is valid in an
// exceptional condition.
// Cer.None: This code knows nothing about CERs.
// [assembly: ReliabilityContract(Consistency.MayCorruptAppDomain, Cer.None)]
[assembly: AssemblyTitle("xunit.runner.visualstudio.vs2010")]
[assembly: AssemblyDescription("xUnit test runner for Visual Studio 2010")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("quetzalcoatl")]
[assembly: AssemblyProduct("xunit.runner.visualstudio.vs2010")]
[assembly: AssemblyCopyright("Copyright ©  2012")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
//[assembly: ComVisible(false)] // copied from vs package samples
//[assembly: CLSCompliant(false)] // copied from vs package samples
//[assembly: NeutralResourcesLanguage("en-US")] // copied from vs package samples

[assembly: AssemblyVersion("2.1.1.2")]
[assembly: AssemblyFileVersion("2.1.1.2")]
