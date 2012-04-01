
using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TestTools.Tips.TuipPackage;

namespace XUnitForVS
{
    using UnitTestResult = Microsoft.VisualStudio.TestTools.Common.TestResult;

    /// <summary>
    /// XUnit Test result details view tool window implementation.
    /// This class is simply a host for the control that does
    /// the heavy lifting for displaying the result details.
    /// </summary>
    [Guid("59F33709-25B1-4997-9221-D3EEA4835F02")]
    internal sealed class UnitTestResultViewWindow : ToolWindowPane
    {
        /// <summary>
        /// Default constructor. Calls base class with Package Instance
        /// </summary>
        public UnitTestResultViewWindow()    
            : base(UnitTestPackage.Instance as IServiceProvider)
        { }
    
        /// <summary>
        /// Called when VS is closed. This cleans up any Result windows in the background and
        /// removes any mappings from the list of open windows
        /// </summary>
        protected override void OnClose()
        {
            if (_result != null)
            {
                // Remove the window mapping
                UnitTestTuip.ResultWindowMapping.Remove(_result.Id);
            }

            base.OnClose();
        }

        // Holds the result for this window to ensure
        // we dont reload the result if it's the one
        // we already loaded
        UnitTestResult _result = null;

        DetailedResultsControl _resultControl = null;
        DetailedResultsControl ResultControl { get { return _resultControl ?? (_resultControl = new DetailedResultsControl()); } }

        UnitTestResultDetailsControl _detailsControl = null;
        UnitTestResultDetailsControl DetailsControl { get { return _detailsControl ?? (_detailsControl = new UnitTestResultDetailsControl()); } }

        /// <summary>
        /// This returns the actual Win32 Window that hosts the control. It is this
        /// window that Visual Studio hosts in a document window to display the result
        /// </summary>
        public override IWin32Window Window
        {
            get { return ResultControl; }
        }

        /// <summary>
        /// Loads the result into the controls that do the leg work
        /// </summary>
        /// <param name="result">The Test Result to load</param>
        public void Init(UnitTestResult result)
        {
            // Check that we are not going to try and load a result we've already loaded
            if (_result != null && _result.Id.ExecutionId.Id == result.Id.ExecutionId.Id)
                return;

            _result = (UnitTestResult)result.Clone();
            try
            {
                // Load the result into the custom control, and then initialise common header control
                ResultControl.Init(result, UnitTestPackage.Instance, this, DetailsControl);
            }
            catch (NullReferenceException ex)
            {
                Guard.Fail(ex, "Failure initializing results window");
                _resultControl = null;
                ResultControl.Init(result, UnitTestPackage.Instance, this, null);
            }
        }
    }
}
