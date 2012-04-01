
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.TestTools.Common;
using Microsoft.VisualStudio.TestTools.Exceptions;
using Xunit;

namespace XUnitForVS
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
    internal sealed class UnitTestTip : Tip
    {
        public UnitTestTip(ITmi tmi)
        { }

        /// <summary>
        /// Returns TestType object for given TIP. There is 1:1 between TIPs and TestTypes
        /// </summary>
        /// <value>The Test Type that this TIP is for. This value is never null</value>
        public override TestType TestType
        {
            get { return UnitTest.UnitTestType; }
        }

        /// <summary>
        /// Loads elements from specified location into memory
        /// 
        /// This method uses exceptions for error conditions -- Not return values.
        /// </summary>
        /// <param name="location">Location to load tests from.</param>
        /// <param name="projectData">Project information object.</param>
        /// <param name="warningHandler">Warning handler that processes warnings.</param>
        /// <returns>The data that has been loaded</returns>
        public override ICollection Load(string location, ProjectData projectData, IWarningHandler warningHandler)
        {
            Guard.StringNotNullOrEmpty(location, "location");

            Environment.SetEnvironmentVariable("XUnitForceLegacyCallback", "True");
            IExecutorWrapper executor = null;
            try
            {
                executor = new ExecutorWrapper(location, configFilename: null, shadowCopy: true);
            }
            catch (ArgumentException ex)
            {
                Trace.WriteLine("No xUnit tests found in '" + location + "':");
                Trace.WriteLine(ex);
            }
#if DEBUG
            catch (Exception ex)
            {
                Guard.Fail(ex, "DEBUG: UnitTestTip.Load");
                throw;
            }
#endif

            var tests = new List<ITestElement>(); // Collection of tests loaded from disk
            if (executor != null)
            {
                using (executor)
                {
                    var testAssembly = TestAssemblyBuilder.Build(executor);
                    var testMethods = testAssembly.EnumerateTestMethods();
                    var unitTests = testMethods.Select(m => (ITestElement)new UnitTest(m, location, projectData));
                    tests.AddRange(unitTests);
                }
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
            throw new SaveNotSupportedException();
        }

    }
}
