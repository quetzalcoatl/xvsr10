using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.Common;
using Microsoft.VisualStudio.TestTools.Exceptions;

namespace Xunit.Runner.VisualStudio.VS2010
{
    /// <summary>
    /// XUnit Test ITip implementation.
    /// The important methods to implement for your TIP are:
    /// - TestType property get: return the test type defined in your
    ///   test element.
    /// - Load: Load is called by TMI when a project containing your test
    ///   types is loaded. Load create instances of your ITestElement and
    ///   returns them to TMI.
    /// 
    /// A TIP can only throw certain exceptions up to TMI. Here's the current
    /// list of valid exceptions out of Microsoft.VisualStudio.TestTools.Exceptions:
    /// 
    /// StorageNotAccessibleException
    /// InvalidDataInStorageException
    /// ErrorReadingStorageException
    /// DuplicateIdException
    /// IdNotFoundException
    /// InvalidTestObjectException
    /// SaveNotSupportedException
    /// CreateNewTestNotSupportedException
    /// WrongResultTypeException
    /// CorruptedResultException
    /// EqtDataException
    /// </summary>
    public class XUnitTestTip : Tip
    {
        /// <summary>Returns TestType object for given TIP. There is 1:1 between TIPs and TestTypes</summary>
        /// <value>The Test Type that this TIP is for. This value is never null</value>
        /// <remarks>
        ///   Actually, this value is bogus and it is provided only because the TIP registration seems to require it.
        ///   The XUnitVSRunner uses original Microsoft's UnitTestElements and UnitTestResults, and they will point
        ///   back to the proprietary internal UnitTestTip. This TIP is used essentialy only to discover and load the
        ///   tests, and the only thing it needs to override in the original is the TestElement's Adapter property.
        /// </remarks>
        public override TestType TestType { get { return XUnitDummyTest.UnitTestType; } }

        protected readonly ITmi tmi; // this member is currently unused, but it is the only significant normal link to VS test services, provided on startup.
        
        public XUnitTestTip(ITmi tmi)
        {
            this.tmi = tmi;
        }

        /// <summary>
        /// Loads elements from specified location into memory
        /// 
        /// This method uses exceptions for error conditions -- Not return values.
        /// </summary>
        /// <param name="assemblyFileLocation">Location to load tests from.</param>
        /// <param name="projectData">Project information object.</param>
        /// <param name="warningHandler">Warning handler that processes warnings.</param>
        /// <returns>The data that has been loaded</returns>
        public override ICollection Load(string assemblyFileLocation, ProjectData projectData, IWarningHandler warningHandler)
        {
            Guard.StringNotNullOrEmpty(assemblyFileLocation, "location");

            IExecutorWrapper executor = null;
            
            try
            {
                // The ExecutorWrapper is the xUnit's version-resilient layer for communicating with different
                // versions of the main xunit.dll. The XUnitVSRunner is thus ignorant about that module and will
                // try to communicate and use whatever version is actually referenced in the unit test's assembly.
                executor = new ExecutorWrapper(assemblyFileLocation, configFilename: null, shadowCopy: true);
            }
            catch (ArgumentException ex)
            {
                Trace.WriteLine("No xUnit tests found in '" + assemblyFileLocation + "':");
                Trace.WriteLine(ex);
            }
#if DEBUG
            catch (Exception ex)
            {
                Trace.TraceError("Error at XUnitTestTip.Load: " + ex.Message);
                throw;
            }
#endif

            var tests = new List<ITestElement>(); // Collection of tests loaded from disk
            if (executor == null) return tests;

            using (executor)
            {
                var testAssembly = TestAssemblyBuilder.Build(executor);

                // the magic is in this two-liner: we ask the xUnit to find all tests and then
                // with heavy use of Reflection we create the actual internal Microsoft's UnitTestElements
                // that will contain all required information about the test's location
                foreach (var xmethod in testAssembly.EnumerateTestMethods())
                    tests.Add(MSVST4U_Access.New_MSVST4U_UnitTestElement(
                        assemblyFileLocation,
                        xmethod.TestClass.TypeName,
                        xmethod.MethodName,
                        projectData));
            }

            return tests;
        }
        
        /// <summary>
        /// Saves specified set of tests in a specified location.
        /// Tests are to be written to the location in the order in which they appear in the array.
        /// </summary>
        /// <param name="tests">Tests to save. Storage parameter on tests must be ignored.</param>
        /// <param name="location">Location where tests are to be saved.</param>
        /// <param name="projectData">Project information object.</param>
        public override void Save(ITestElement[] tests, string location, ProjectData projectData)
        {
            // BTW. this might look strange, but the original Microsoft's code behaves in the exact same way
            // This method is probably provided as an option to write changes made to the test's setup through
            // the GUI Test's PropertyWindow back to the source code. Actually, I have no other ideas.
            throw new SaveNotSupportedException();
        }
    }
}
