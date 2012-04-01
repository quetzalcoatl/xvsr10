//using System;
//using System.Runtime.InteropServices;
//using System.Windows.Forms;
//using Microsoft.VisualStudio.Shell;
//using Microsoft.VisualStudio.TestTools.Tips.TuipPackage;

//namespace Xunit.Runner.VisualStudio.VS2010
//{
//    // FYI: the internal classes like Microsoft.VisualStudio.TestTools.TestTypes.Unit.UnitTestElement
//    // cannot be referenced directly, hence, a set of aliases is defined and their nearest BASE CLASSES
//    // are used instead.
//    using MSVST4U_UnitTestResult = Microsoft.VisualStudio.TestTools.Common.TestResult; // surrogate for Microsoft.VisualStudio.TestTools.TestTypes.Unit.UnitTestResult

//    /// <summary>
//    /// XUnit Test result details view tool window implementation.
//    /// This class is simply a host for the control that does
//    /// the heavy lifting for displaying the result details.
//    /// </summary>
//    [Guid("496888E5-D7EA-4458-ADE3-C99248C9EDDF")]
//    public class XUnitTestResultViewWindow : ToolWindowPane
//    {
//        /// <summary>
//        /// Default constructor. Calls base class with Package Instance
//        /// </summary>
//        public XUnitTestResultViewWindow() : base(XUnitTestPackage.Instance as IServiceProvider) { }

//        /// <summary>
//        /// Called when VS is closed. This cleans up any Result windows in the background and
//        /// removes any mappings from the list of open windows
//        /// </summary>
//        protected override void OnClose()
//        {
//            if (_result != null)
//                XUnitTestTuip.ResultWindowMapping.Remove(_result.Id); // Remove the window mapping

//            base.OnClose();
//        }

//        // Holds the result for this window to ensure
//        // we dont reload the result if it's the one
//        // we already loaded
//        private MSVST4U_UnitTestResult _result = null;

//        private DetailedResultsControl _resultControl = null;
//        private DetailedResultsControl ResultControl { get { return _resultControl ?? (_resultControl = new DetailedResultsControl()); } }

//        private UnitTestResultDetailsControl _detailsControl = null;
//        private UnitTestResultDetailsControl DetailsControl { get { return _detailsControl ?? (_detailsControl = new UnitTestResultDetailsControl()); } }

//        /// <summary>
//        /// This returns the actual Win32 Window that hosts the control. It is this
//        /// window that Visual Studio hosts in a document window to display the result
//        /// </summary>
//        public override IWin32Window Window
//        {
//            get { return ResultControl; }
//        }

//        /// <summary>
//        /// Loads the result into the controls that do the leg work
//        /// </summary>
//        /// <param name="result">The Test Result to load</param>
//        public void Init(MSVST4U_UnitTestResult result)
//        {
//            // Check that we are not going to try and load a result we've already loaded
//            if (_result != null && _result.Id.ExecutionId.Id == result.Id.ExecutionId.Id)
//                return;

//            _result = (MSVST4U_UnitTestResult)result.Clone();
//            try
//            {
//                // Load the result into the custom control, and then initialise common header control
//                ResultControl.Init(result, XUnitTestPackage.Instance, this, DetailsControl);
//            }
//            catch (NullReferenceException ex)
//            {
//                Guard.Fail(ex, "Failure initializing results window");
//                _resultControl = null;
//                ResultControl.Init(result, XUnitTestPackage.Instance, this, null);
//            }
//        }
//    }
//}
