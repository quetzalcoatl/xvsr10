using System;
//using System.ComponentModel.Design;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TestTools.Vsip;

namespace Xunit.Runner.VisualStudio.VS2010
{
    /// <summary>
    /// XUnit Test package implementation.
    /// Implement a package when you need to host your own editor, result details viewer, or new test wizard.
    /// Packages are standard VSIP packages and derive from Microsoft.VisualStudio.Shell.Package.
    /// There are a number of attributes to help with registration, including
    /// RegisterTestType, ProvideServiceForTestType, ProvideToolWindow, and AddNewItemTemplates.
    /// </summary>

    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is a package.
    [PackageRegistration(UseManagedResourcesOnly = true, RegisterUsing = RegistrationMethod.CodeBase)]
    [Guid("EC512710-795B-47E9-A0FF-050CF6E2EF0D")]

    // This attribute is used to register the informations needed to show the this package in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#101", "#102", "2.0", IconResourceID = 104)]

    [DefaultRegistryRoot(@"Software\Microsoft\VisualStudio\10.0")]
    [ProvideLoadKey("Professional", "2.0", "xUnit test runner for VS2010", "quetzalcoatl", (short)ResourceIds.TestPackageLoadKey)]
    [RegisterTestTypeNoEditor(typeof(XUnitDummyTest), typeof(XUnitTestTip), new string[] { ".dll", ".exe" }, new int[] { (int)ResourceIds.TestIcon, (int)ResourceIds.TestIcon }, (int)ResourceIds.TestName)]
    // [ProvideServiceForTestType(typeof(XUnitDummyTest), typeof(SXUnitTestService))]
    // [ProvideToolWindow(typeof(XUnitTestResultViewWindow), Orientation = ToolWindowOrientation.Left, Style = VsDockStyle.MDI)]
    public sealed class XUnitTestPackage : Package
    {
        /// <summary>
        /// Constructor for the package. Setup the required Service and associated callback
        /// </summary>
        public XUnitTestPackage()
        {
            //ServiceCreatorCallback callback = new ServiceCreatorCallback(this.OnCreateService);
            //IServiceContainer container = GetService(typeof(IServiceContainer)) as IServiceContainer;

            //Debug.Assert(container != null, "Package didn't provide IServiceContainer so we cannot provide services");

            //if (container != null)
            //    container.AddService(typeof(SXUnitTestService), callback, true); // For every TUIP, add its service here.

            m_package = this;
        }

        /// <value>
        /// The Instance of the package that allows other classess to access the packages services
        /// </value>
        public static XUnitTestPackage Instance { get { Debug.Assert(m_package != null, "Package needs to be created before Instance is called."); return m_package; } } private static XUnitTestPackage m_package;

        /// <summary>
        ///// Callback from IServiceContainer to demand create our services.
        ///// </summary>
        ///// <param name="container">service container</param>
        ///// <param name="serviceType">type of service</param>
        ///// <returns>the service provider object</returns>
        //private object OnCreateService(IServiceContainer container, Type serviceType)
        //{
        //    Guard.ParameterNotNull(container, "container");
        //    Guard.ParameterNotNull(serviceType, "serviceType");

        //    //Hint: return your service from here
        //    if (serviceType == typeof(SXUnitTestService)) return new XUnitTestTuip(this);

        //    Debug.Fail("service container requested unsupported service: " + serviceType.FullName);
        //    return null;
        //}
    }
}
