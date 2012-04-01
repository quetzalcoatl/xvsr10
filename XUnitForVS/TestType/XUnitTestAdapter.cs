
using System;
using System.Collections.Concurrent;
using Microsoft.VisualStudio.TestTools.Common;
using Microsoft.VisualStudio.TestTools.Execution;
using Microsoft.VisualStudio.TestTools.TestAdapter;
using Xunit;

namespace XUnitForVS
{
    /// <summary>
    /// XUnit Test ITestAdapter implementation.
    /// The important methods to implement for your adapter are:
    /// - ITestAdapter.Initialize: it's useful to store the run id
    ///   for the test run that this adapter is being instantiated for
    ///   so that it can be used later for result reporting.
    /// - IBaseAdapter.Run: the method that actually runs the test
    ///   and reports results.
    /// </summary>
    internal sealed class UnitTestAdapter: ITestAdapter
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public UnitTestAdapter()
        { }

        /// <summary>
        /// ITestAdapter method: called to initialize run context for this adapter.
        /// </summary>
        /// <param name="runContext">The run context to be used for this run</param>
        public void Initialize(IRunContext runContext)
        {
            _runContext = runContext;
            _runId = _runContext.RunConfig.TestRun.Id;
        }

        /// <summary>
        /// ITestAdapter method: called when a message is sent from the UI or the controller.
        /// </summary>
        /// <param name="obj">The message object</param>
        public void ReceiveMessage(object obj)
        {
        }

        /// <summary>
        /// ITestAdapter method: called just before the test run finishes and
        /// gives the adapter a chance to do any clean-up.
        /// </summary>
        /// <param name="runContext">The run context for this run</param>
        public void PreTestRunFinished(IRunContext runContext)
        {
        }

        /// <summary>
        /// IBaseAdapter method: called to execute a test.
        /// </summary>
        /// <param name="testElement">The test object to run</param>
        /// <param name="testContext">The Test conext for this test invocation</param>
        public void Run(ITestElement testElement, ITestContext testContext)
        {
            try
            {
                var test = testElement as UnitTest
                        ?? new UnitTest((TestElement)testElement);

                var executor = _executors.GetOrAdd(testElement.Storage, s => new ExecutorWrapper(s, configFilename: null, shadowCopy: true));
                var result = UnitTestRunner.ExecuteTest(executor, _runId, test);
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

        /// <summary>
        /// IBaseAdapter method: called when the test run is complete.
        /// </summary>
        public void Cleanup()
        {
            foreach (var executor in _executors.Values)
                executor.Dispose();
            _executors.Clear();
        }

        /// <summary>
        /// IBaseAdapter method: called when the user stops the test run.
        /// </summary>
        public void StopTestRun()
        {
        }

        /// <summary>
        /// IBaseAdapter method: called when the test run is aborted.
        /// </summary>
        public void AbortTestRun()
        {
        }

        /// <summary>
        /// IBaseAdapter method: called when the user pauses the test run.
        /// </summary>
        public void PauseTestRun()
        {
        }

        /// <summary>
        /// IBaseAdapter method: called when the user resumes a paused test run.
        /// </summary>
        public void ResumeTestRun()
        {
        }

        // Run context
        private IRunContext _runContext;
        private Guid _runId;
        private readonly ConcurrentDictionary<string, IExecutorWrapper> _executors = new ConcurrentDictionary<string, IExecutorWrapper>();
    }
}
