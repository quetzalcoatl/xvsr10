
using System.Runtime.ConstrainedExecution;

// Consistency.MayCorruptAppDomain: An exceptional condition while executing
// the code in this assembly may corrupt the app domain where this code is
// running. No guarantee is made that app domain state is valid in an
// exceptional condition.
// Cer.None: This code knows nothing about CERs.
[assembly: ReliabilityContract(Consistency.MayCorruptAppDomain, Cer.None)]
