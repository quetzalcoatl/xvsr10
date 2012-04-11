using System;

namespace Xunit.Runner.VisualStudio.VS2010
{
    public static class TestGuids // used by TestElement
    {
        public const string DummyTestTypeID_S = "4D2F9CCB-49D3-4CAA-9A29-BEFFC604075A";
        public static readonly Guid DummyTestTypeID = new Guid(DummyTestTypeID_S);
    }
}
