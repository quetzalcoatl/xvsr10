
using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TestTools.Vsip;

namespace XUnitForVS
{
    /// <summary>
    /// XUnit Test package implementation.
    /// Implement a package when you need to host your own editor, result
    /// details viewer, or new test wizard.
    /// Packages are standard VSIP packages and derive from
    /// Microsoft.VisualStudio.Shell.Package.
    /// There are a number of attributes to help with registration, including
    /// RegisterTestType, ProvideServiceForTestType, ProvideToolWindow, and
    /// AddNewItemTemplates.
    /// </summary>
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is a package.
    [PackageRegistration(UseManagedResourcesOnly = true, RegisterUsing = RegistrationMethod.CodeBase)]
    // This attribute is used to register the informations needed to show the this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#101", "#102", "1.2", IconResourceID = 104)]
    [ProvideToolWindow(typeof(UnitTestResultViewWindow), Orientation = ToolWindowOrientation.Left, Style = VsDockStyle.MDI)]
    // TODO: Generate a unique GUID for your package. To get a valid package
    // load key for your package, you need to visit http://vsipdev.com and
    // request one. Use the ProvideLoadKey attribute to provide the PLK
    // information for registration with regpkg.
    [Guid("BA1248C1-49F2-4899-9A2C-548035755F2C")]
    [DefaultRegistryRoot(@"Software\Microsoft\VisualStudio\10.0")]
    [ProvideLoadKey("Professional", "1.2", "Team Test Integration Sample", "n/a", (short)ResourceIds.TestPackageLoadKey)]
    [RegisterTestTypeNoEditor(typeof(UnitTest), typeof(UnitTestTip), new string[] { ".dll", ".exe" }, new int[] { (int)ResourceIds.TestIcon, (int)ResourceIds.TestIcon }, (int)ResourceIds.TestName)]
    [ProvideServiceForTestType(typeof(UnitTest), typeof(SUnitTestService))]
    internal sealed class UnitTestPackage : Package
    {
        /// <summary>
        /// Constructor for the package. Setup the required Service and associated callback
        /// </summary>
        public UnitTestPackage()
            : base()
        {
            ServiceCreatorCallback callback = new ServiceCreatorCallback(this.OnCreateService);
            IServiceContainer container = GetService(typeof(IServiceContainer)) as IServiceContainer;

            Debug.Assert(container != null, "Package didn't provide IServiceContainer so we cannot provide services");

            if (container != null)
            {
                // For every TUIP, add its service here.
                container.AddService(typeof(SUnitTestService), callback, true);
            }

            m_package = this;
        }

        /// <value>
        /// The Instance of the package that allows other classess to access the packages services
        /// </value>
        internal static UnitTestPackage Instance
        {
            get { Debug.Assert(m_package != null, "Package needs to be created before Instance is called."); return m_package; }
        }

        /// <summary>
        /// Callback from IServiceContainer to demand create our services.
        /// </summary>
        /// <param name="container">service container</param>
        /// <param name="serviceType">type of service</param>
        /// <returns>the service provider object</returns>
        private object OnCreateService(IServiceContainer container, Type serviceType)
        {
            if (container == null)
            {
                throw new ArgumentNullException("container");
            }

            if (serviceType == null)
            {
                throw new ArgumentNullException("serviceType");
            }

            //Hint: return your service from here
            if (serviceType == typeof(SUnitTestService))
            {
                return new UnitTestTuip(this);
            }
            else
            {
                Debug.Fail("service container requested unsupported service: " + serviceType.FullName);
                return null;
            }
        }

        // The one instance of this package.
        private static UnitTestPackage m_package;
    }
}
