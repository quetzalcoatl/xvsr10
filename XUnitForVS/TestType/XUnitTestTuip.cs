//using System;
//using System.Collections.Generic;
//using Microsoft.VisualStudio.Shell;
//using Microsoft.VisualStudio.Shell.Interop;
//using Microsoft.VisualStudio.TestTools.Common;
//using Microsoft.VisualStudio.TestTools.Exceptions;
//using Microsoft.VisualStudio.TestTools.Vsip;

//namespace Xunit.Runner.VisualStudio.VS2010
//{
//    // FYI: the internal classes like Microsoft.VisualStudio.TestTools.TestTypes.Unit.UnitTestElement
//    // cannot be referenced directly, hence, a set of aliases is defined and their nearest BASE CLASSES
//    // are used instead.
//    using MSUnitTestResult = Microsoft.VisualStudio.TestTools.Common.TestResult; // surrogate for Microsoft.VisualStudio.TestTools.TestTypes.Unit.UnitTestResult

//    /// <summary>
//    /// XUnit Test ITuip, ICommandProvider, and service implementation.
//    /// The important methods to implement for ITuip are:
//    /// - InvokeEditor: this method is called to invoke an editor from
//    ///   the Test View or Test Manager window for your test.
//    ///   InvokeEditor can be used to invoke a complete GUI editor or
//    ///   use VS services on existing editors (such as opening a source
//    ///   file and positioning the caret on a particular line).
//    /// - InvokeResultViewer: this method is called to invoke a result
//    ///   details viewer for your test results. InvokeResultViewer can
//    ///   be used to show test result-specific information stored in your
//    ///   derived TestResult class.
//    /// </summary>
//    public class XUnitTestTuip : BaseTuip, SXUnitTestService
//    {
//        /// <summary>
//        /// The Basic constructor for the TUIP. This takes the service provider to be
//        /// used by the TUIP and and associated classes. This implementation just calls
//        /// the BaseTuip constructor
//        /// </summary>
//        /// <param name="serviceProvider">The Service Provider to be used</param>
//        public XUnitTestTuip(IServiceProvider serviceProvider) : base(serviceProvider) { }

//        /// <summary>
//        /// Invoke the result viewer for XUnit Test. In this method, we need to keep references
//        /// to previously opened windows to ensure that we dont recreate the window needlessly.
//        /// </summary>
//        /// <param name="resultMessage">The result to be viewed</param>
//        public override void InvokeResultViewer(TestResultMessage resultMessage)
//        {
//            var result = resultMessage as MSUnitTestResult;

//            if (result == null || result.Test == null)
//                throw new ArgumentException("resultMessage");

//            // The ID of the window for the result
//            int idWindow;

//            // Bring up the existing tool window if it is open
//            if (!ResultWindowMapping.TryGetValue(result.Id, out idWindow))
//            {
//                // There wasn't already a window for this result
//                // so get another ID -- any old number will do
//                idWindow = m_idNext++;
//            }

//            // Get/create the window for the result. The underlying package will create the window if it
//            // doesnt already exist.
//            ToolWindowPane toolWindowPane = XUnitTestPackage.Instance.FindToolWindow(typeof(XUnitTestResultViewWindow), idWindow, true);
//            var windowResult = toolWindowPane as XUnitTestResultViewWindow; // Cast to the correct type

//            if (windowResult == null || windowResult.Frame == null)
//            {
//                throw new EqtException("The Result window could not be created. Try restarting Visual Studio and trying again.");
//            }
//            else
//            {
//                try
//                {
//                    windowResult.Init(result); // Load the result into the window
//                }
//                catch (Exception ex) { Guard.Fail(ex, "Failure initializing results window"); }
//            }

//            // The "right" way to set the caption of the window is this:
//            //          windowResult.Caption = result.Test.Name;
//            // Unfortunately, a bug prevents that from working, so we do this for now:
//            ((IVsWindowFrame)windowResult.Frame).SetProperty((int)__VSFPROPID.VSFPROPID_Caption, result.Test.Name);
//            ((IVsWindowFrame)windowResult.Frame).Show();

//            // Add mapping after tool window is opened successfully
//            if (!ResultWindowMapping.ContainsKey(result.Id))
//                ResultWindowMapping.Add(result.Id, idWindow);
//        }

//        /// <summary>
//        /// Closes the detailed results viewer for a specific result
//        /// </summary>
//        /// <param name="resultMessage">The result for which to close the window</param>
//        public override void CloseResultViewer(TestResultMessage resultMessage)
//        {
//            var result = resultMessage as MSUnitTestResult;

//            if (result == null || result.Test == null)
//                throw new ArgumentException("resultMessage");

//            // If there's no window for the result ID no need to close it
//            if (ResultWindowMapping.ContainsKey(result.Id))
//            {
//                // Retrieve the Window ID for the result
//                int toolWindowId = (int)ResultWindowMapping[result.Id];

//                // Get Hold of the window
//                ToolWindowPane toolWindowPane = XUnitTestPackage.Instance.FindToolWindow(typeof(XUnitTestResultViewWindow), toolWindowId, false);
//                var resultWindow = toolWindowPane as XUnitTestResultViewWindow;

//                // Close the window
//                if (resultWindow != null)
//                    ((IVsWindowFrame)resultWindow.Frame).CloseFrame(0);
//            }
//        }

//        /// <summary>
//        /// User Control to show in the Run Config dialog tab for this Test Type. 
//        /// We have not implemented one here. Returning null signifies this no special editor
//        /// </summary>
//        public override IRunConfigurationCustomEditor RunConfigurationEditor
//        {
//            get { return null; }
//        }

//        // This maintains the Window mapping between a result ID and a Window ID
//        internal static Dictionary<TestResultId, int> ResultWindowMapping = new Dictionary<TestResultId, int>();
//        private static int m_idNext = 0; // id of the next window to create
//    }
//}
