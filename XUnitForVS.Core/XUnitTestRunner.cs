using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.Common;

namespace Xunit.Runner.VisualStudio.VS2010
{
    // FYI: the internal classes like Microsoft.VisualStudio.TestTools.TestTypes.Unit.UnitTestElement
    // cannot be referenced directly, hence, a set of aliases is defined and their nearest BASE CLASSES
    // are used instead.
    using MSVST4U_UnitTestElement = Microsoft.VisualStudio.TestTools.Common.TestElement; // surrogate for Microsoft.VisualStudio.TestTools.TestTypes.Unit.UnitTestElement
    using MSVST4U_UnitTestResult = Microsoft.VisualStudio.TestTools.Common.TestResultAggregation; // surrogate for Microsoft.VisualStudio.TestTools.TestTypes.Unit.UnitTestResult

    /// <summary>
    /// Class responsible for running a test
    /// </summary>
    public class XUnitTestRunner : IRunnerLogger
    {
        private readonly Guid _runId;
        private readonly ITestElement _test;
        private readonly List<MSVST4U_UnitTestResult> _results = new List<MSVST4U_UnitTestResult>();
        private bool _isTheory;

        private XUnitTestRunner(Guid runId, ITestElement test)
        {
            _runId = runId;
            _test = test;
        }

        public static MSVST4U_UnitTestResult ExecuteTest(IExecutorWrapper executor, Guid runId, MSVST4U_UnitTestElement testElement)
        {
            var logger = new XUnitTestRunner(runId, testElement);
            var runner = new TestRunner(executor, logger);

            string fclassname = MSVST4U_Access.GetFullyQualifiedClassName(testElement);
            if (fclassname == null) throw new NotImplementedException("xUnit runner does not currently know how to extract a ClassName from test type: " + (testElement == null ? "(null)" : testElement.GetType().FullName));

            // The testElement is the original UnitTestElement, so the result is the original UnitTestResult.
            // However, due to the nature of some unit test types, we could have gathered many results, and must
            // now group them together. 

            runner.RunTest(fclassname, testElement.Name);

            MSVST4U_UnitTestResult result;

            if (logger._isTheory)
            {
                Debug.Assert(logger._results.Count > 0, "Expected at least one result for Theory test " + testElement.Name + ": " + logger._results.Count);

                MSVST4U_UnitTestResult[] innerResults = logger._results.ToArray();
                TestOutcome outcome = TestOutcomeHelper.GetAggregationOutcome(innerResults);

                // for a Theory test (a DataDriven test in the original VS nomenclature), the logger gathered
                // multiple responses and they should be packed up together in a "TestAggregateResult". Actually,
                // a original UnitTestResult already implements that and it is available as a constructor overload. 
                result = MSVST4U_Access.New_MSVST4U_UnitTestResult_DataDriven(runId, testElement, outcome, null, innerResults);
            }
            else
            {
                Debug.Assert(logger._results.Count == 1, "Expected one result for non-Theory test " + testElement.Name + ": " + logger._results.Count);

                // for a Fact test (a NotDataDriven test in the original VS nomenclature), the logger gathered
                // a single response; it is completely valid to simply return it 
                result = logger._results[0];
            }

            return result;
        }

        private MSVST4U_UnitTestResult TestComplete(string name, TestOutcome outcome, double duration, string output, TestResultErrorInfo errorInfo)
        {
            MSVST4U_UnitTestResult result;
            int dataIndex = name.IndexOf('(') + 1;

            if (dataIndex > 0)
            {
                _isTheory = true;
                string data = name.Substring(dataIndex, name.Length - dataIndex - 1);
                var id = new TestResultId(_runId, new TestExecId(), _test.ExecutionId, _test.Id);
                result = MSVST4U_Access.New_MSVST4U_UnitTestResult_DataDrivenRow(id, _test, data);
            }
            else
            {
                result = MSVST4U_Access.New_MSVST4U_UnitTestResult_Standard(_runId, _test);
            }

            _results.Add(result);

            result.EndTime = DateTime.Now;
            result.Duration = TimeSpan.FromSeconds(duration);
            result.StartTime = result.EndTime - result.Duration;

            result.Outcome = outcome;
            result.StdOut = output ?? "";

            result.ErrorInfo = errorInfo;

            return result;
        }

        #region IRunnerLogger Members - callbacks providing info on the test run state changes

        void IRunnerLogger.AssemblyStart(string assemblyFilename, string configFilename, string xUnitVersion)
        {
            Trace.TraceInformation("UnitTestRunner.AssemblyStart was called.");
        }

        void IRunnerLogger.ExceptionThrown(string assemblyFilename, Exception exception)
        {
            Trace.TraceInformation("UnitTestRunner.ExceptionThrown was called.");

            ((IRunnerLogger)this).TestFailed("", "", "", 0.0, null, exception.GetType().FullName, exception.Message, exception.StackTrace);
        }

        void IRunnerLogger.AssemblyFinished(string assemblyFilename, int total, int failed, int skipped, double time)
        {
            Trace.TraceInformation("UnitTestRunner.AssemblyFinished was called.");
        }

        bool IRunnerLogger.ClassFailed(string className, string exceptionType, string message, string stackTrace)
        {
            Trace.TraceInformation("UnitTestRunner.ClassFailed was called.");

            return true;
        }

        bool IRunnerLogger.TestStart(string name, string type, string method)
        {
            Trace.TraceInformation("UnitTestRunner.TestStart was called.");

            return true;
        }

        void IRunnerLogger.TestPassed(string name, string type, string method, double duration, string output)
        {
            Trace.TraceInformation("UnitTestRunner.TestPassed was called.");

            TestComplete(name, TestOutcome.Passed, duration, output, null);
        }

        void IRunnerLogger.TestFailed(string name, string type, string method, double duration, string output, string exceptionType, string message, string stackTrace)
        {
            Trace.TraceInformation("UnitTestRunner.TestFailed was called.");

            if (message == null || !message.Contains(exceptionType))
                message = exceptionType + ": " + message;

            var errorInfo = new TestResultErrorInfo(message) { StackTrace = stackTrace };

            TestComplete(name, TestOutcome.Failed, duration, output, errorInfo);
        }

        void IRunnerLogger.TestSkipped(string name, string type, string method, string reason)
        {
            Trace.TraceInformation("UnitTestRunner.TestSkipped was called.");

            var errorInfo = new TestResultErrorInfo(reason);

            var result = TestComplete(name, TestOutcome.Completed, 0, null, errorInfo);
            //result.AddTextMessage(reason);
        }

        bool IRunnerLogger.TestFinished(string name, string type, string method)
        {
            Trace.TraceInformation("UnitTestRunner.TestFinished was called.");

            return true;
        }

        #endregion
    }
}
