namespace Xunit.Runner.VisualStudio.VS2010
{
    /// <summary>
    /// XUnit Test resource ids. These are exactly the same values that
    /// are in the satellite DLL's resource.h.
    /// </summary>
    public enum ResourceIds : int
    {
        TestName = 101, // used by XUnitTestPackage
        TestDescription = 102, // used by XUnitTestPackage
        //TestBaseName = 103,
        TestIcon = 104, // used by XUnitTestPackage and XUnitTest.vstemplate
        //TestEditorCaption = 105,
        //TestNewItemLocation = 106,
        //TestNewItemBaseName = 107,

        //TestTemplateName = 108 // used by XUnitTest.vstemplate

        TestPackageLoadKey = 500, // used by XUnitTestPackage
    }
}
