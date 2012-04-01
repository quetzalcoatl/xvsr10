using System;
using System.Collections.Concurrent;
using Microsoft.VisualStudio.TestTools.Common;
using Microsoft.VisualStudio.TestTools.Execution;
using Microsoft.VisualStudio.TestTools.TestAdapter;

namespace Xunit.Runner.VisualStudio.VS2010
{
    // FYI: the internal classes like Microsoft.VisualStudio.TestTools.TestTypes.Unit.UnitTestElement
    // cannot be referenced directly, hence, a set of aliases is defined and their nearest BASE CLASSES
    // are used instead.
    using MSVST4U_UnitTestElement = Microsoft.VisualStudio.TestTools.Common.TestElement; // surrogate for Microsoft.VisualStudio.TestTools.TestTypes.Unit.UnitTestElement

    /// <summary>
    /// XUnit Test ITestAdapter implementation.
    /// The important methods to implement for your adapter are:
    /// - ITestAdapter.Initialize: it's useful to store the run id for the test run that this adapter
    ///   is being instantiated for so that it can be used later for result reporting.
    /// - IBaseAdapter.Run: the method that actually runs the test and reports results.
    /// </summary>
    public class XUnitTestAdapter : ITestAdapter
    {
        private IRunContext _runContext;
        private Guid _runId;
        private readonly ConcurrentDictionary<string, IExecutorWrapper> _executors = new ConcurrentDictionary<string, IExecutorWrapper>();

        /// <inheritdoc/>
        public void Initialize(IRunContext runContext)
        {
            _runContext = runContext;
            _runId = _runContext.RunConfig.TestRun.Id;
        }

        /// <inheritdoc/>
        public void Run(ITestElement testElement, ITestContext testContext)
        {
            try
            {
                var test = testElement as MSVST4U_UnitTestElement;
                if (test == null) throw new NotImplementedException("XUnitTestAdapter does not currently know how to run tests of type: " + (test == null ? "(null)" : test.GetType().FullName));

                var executor = _executors.GetOrAdd(testElement.Storage, s => new ExecutorWrapper(s, configFilename: null, shadowCopy: true));

                var result = XUnitTestRunner.ExecuteTest(executor, _runId, test);
                testContext.ResultSink.AddResult(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
#if DEBUG
                Guard.Fail(ex, "DEBUG: UnitTestAdapter.Run");
#endif
                throw;
            }
        }

        /// <inheritdoc/>
        public void Cleanup()
        {
            foreach (var executor in _executors.Values)
                executor.Dispose();
            _executors.Clear();
        }

        /// <inheritdoc/>
        public void ReceiveMessage(object obj) { } // currently, not needed

        /// <inheritdoc/>
        public void StopTestRun() { } // currently, not implemented

        /// <inheritdoc/>
        public void AbortTestRun() { } // currently, not implemented

        /// <inheritdoc/>
        public void PauseTestRun() { } // currently, not implemented

        /// <inheritdoc/>
        public void ResumeTestRun() { } // currently, not implemented

        /// <inheritdoc/>
        public void PreTestRunFinished(IRunContext runContext) { } // currently, not needed

    }
}
