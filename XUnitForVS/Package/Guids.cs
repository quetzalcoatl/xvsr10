using System;

namespace Xunit.Runner.VisualStudio.VS2010
{
    public static class Guids // used by VSCT
    {
        public const string PackageKey_S = "EC512710-795B-47E9-A0FF-050CF6E2EF0D";
        public static readonly Guid PackageKey = new Guid(PackageKey_S);

        public const string QualityToolsPackageKey_S = "A9405AE6-9AC6-4f0e-A03F-7AFE45F6FCB7";
        public static readonly Guid QualityToolsPackageKey = new Guid(QualityToolsPackageKey_S);

        public const string ProjectToolsKey_S = "106645C8-ED0B-40FE-9BF9-8DA4EF20ED0E";
        public static readonly Guid ProjectToolsKey = new Guid(ProjectToolsKey_S);

        public const string IDETestToolsKey_S = "7E495D98-0A46-4802-BD8F-952B4E12D40B";
        public static readonly Guid IDETestToolsKey = new Guid(IDETestToolsKey_S);
    }
}
