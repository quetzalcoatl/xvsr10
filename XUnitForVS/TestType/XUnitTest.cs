using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.Common;

namespace Xunit.Runner.VisualStudio.VS2010
{
    /// <summary>
    /// This class is truly unused, but must exist because the registration of the TIP
    /// through the attribute 'RegisterTestTypeNoEditor' simply requires a test
    /// type to be provided.
    /// </summary>
    [Guid(TestTypeID), Serializable]
    public sealed class XUnitDummyTest : TestElement
    {
        // Each test type has a GUID that identifies it.
        public const string TestTypeID = "4D2F9CCB-49D3-4CAA-9A29-BEFFC604075A";
        public static readonly Guid Guid = new Guid(TestTypeID);
        public static readonly TestType UnitTestType = new TestType(XUnitDummyTest.Guid);

        private XUnitDummyTest() { }
        public override string Adapter { get { throw new NotImplementedException(); } }
        public override bool CanBeAggregated { get { throw new NotImplementedException(); } }
        public override object Clone() { throw new NotImplementedException(); }
        public override string ControllerPlugin { get { throw new NotImplementedException(); } }
        public override bool ReadOnly { get { throw new NotImplementedException(); } set { throw new NotImplementedException(); } }
        public override TestType TestType { get { throw new NotImplementedException(); } }
    }
}
