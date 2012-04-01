
using System;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using System.Xml;
using Microsoft.VisualStudio.TestTools.Common;
using Microsoft.VisualStudio.TestTools.Common.Xml;
using Xunit;

namespace XUnitForVS
{
    /// <summary>
    /// XUnit Test ITestElement implementation.
    /// Deriving from TestElement provides base implementations for many
    /// of the required methods.
    /// The important things to implement for your own test element are:
    /// - TestType with a GUID that uniquely identifies your test type.
    /// - Copy constructor that implements a deep copy for cloning.
    /// - Adapter property that returns the assembly and the class that
    ///   implements ITestAdapter for this test type.
    /// </summary>
    [Guid("3451566B-E89A-4b09-AC24-6CB67CFB8A04")]
    [Serializable]
    internal sealed class UnitTest : TestElement, IXmlTestStoreCustom
    {
        // Each test type has a GUID that identifies it.
        public static readonly Guid Guid = new Guid("3451566B-E89A-4b09-AC24-6CB67CFB8A04");
        public static readonly TestType UnitTestType = new TestType(UnitTest.Guid);

        /// <summary>
        /// Constructor used to create a new test element. This constructor is
        /// typically called from the TIP.
        /// </summary>
        /// <param name="name">The name of the test</param>
        /// <param name="desc">The description of the test</param>
        /// <param name="strCommandLine">The command line for this test</param>
        public UnitTest(TestMethod test, string location, ProjectData projectData)
            : base(test.MethodName, test.DisplayName)
        {
            this.Owner = test.TestClass.TypeName;
            this.Storage = location;
            this.ProjectData = projectData;
        }

        /// <summary>
        /// Copy constructor. Useful for implementing the Clone method.
        /// </summary>
        /// <param name="copy">The test to copy from</param>
        public UnitTest(TestElement copy)
            : base(copy)
        {
        }

        /// <summary>
        /// Parameterless constructor needed for XML persistence.
        /// </summary>
        private UnitTest()
        {
        }

        /// <value>
        /// Indicate whether this test element is read-only or not.
        /// </value>
        public override bool ReadOnly
        {
            get { return false; }
            set { throw new InvalidOperationException(); }
        }

        /// <value>
        /// Return the test type associated with this test element.
        /// </value>
        public override TestType TestType
        {
            get { return UnitTestType; }
        }

        /// <summary>
        /// Because TestElements can eventually be handed off for execution,
        /// they must be capable of deep copies so that later edits don't
        /// mess up an in-progress execution.
        ///
        /// This means that all specific TestTypes (like UnitTest, Stress & Load Test, etc),
        /// must be capable of making independent clones of themselves. This interface is
        /// present to enforce this contract, but no default behavior is in place because there
        /// is no way for TestElement to instantiate a copy of itself (abstract class). 
        ///
        /// Nonetheless, a helper does exist for copying the methods of the TestElement class:
        /// the copy c'tor for this class. A derived class can create its own copy c'tor and 
        /// call the base c'tor during construction, and the appropriate methods will be copied.
        /// </summary>
        /// <returns></returns>
        public override object Clone()
        {
            return new UnitTest(this);
        }

        /// <summary>
        /// Specify where the adapter for this test element is located.
        /// </summary>
        public override string Adapter
        {
            get { return typeof(UnitTestAdapter).AssemblyQualifiedName; }
        }

        /// <summary>
        /// Indicate whether this test element can be aggregated by other types of test elements.
        /// One example of this is if this test element can be a part of an ordered test.
        /// </summary>
        /// <returns>True if this test element can be aggregated.</returns>
        public override bool CanBeAggregated
        {
            get { return true; }
        }

        /// <summary>
        /// Indicate whether this test element is a candidate for being included in a load test.
        /// </summary>
        /// <returns>True if this test element can be included in a load test.</returns>
        public override bool IsLoadTestCandidate
        {
            get { return false; }
        }

        public override string ControllerPlugin
        {
            get { return null; }
        }

        public override void Save(XmlElement element, XmlTestStoreParameters parameters)
        {
            base.Save(element, parameters);

            string ns = element.NamespaceURI;
            string prefix = element.GetPrefixOfNamespace(ns);
            var testMethod = element.OwnerDocument.CreateElement(prefix, "TestMethod", ns);

            testMethod.SetAttribute("codeBase", Storage);
            testMethod.SetAttribute("adapterTypeName", Adapter);
            testMethod.SetAttribute("className", Owner);
            testMethod.SetAttribute("name", Name);

            element.AppendChild(testMethod);
        }

        public override void Load(XmlElement element, XmlTestStoreParameters parameters)
        {
            base.Load(element, parameters);
        }

        #region IXmlTestStoreCustom Members

        public string ElementName
        {
            get { return "UnitTest"; }
        }

        public string NamespaceUri
        {
            get { return "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"; }
        }

        #endregion
    }
}
