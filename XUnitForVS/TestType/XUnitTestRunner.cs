
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.VisualStudio.TestTools.Common;
using Xunit;

namespace XUnitForVS
{
    using UnitTestResult = TestResultAggregation;
    using VSTestResult = Microsoft.VisualStudio.TestTools.Common.TestResult;

    /// <summary>
    /// Class responsible for running a test
    /// </summary>
    class UnitTestRunner : IRunnerLogger
    {
        private static readonly Func<Guid, ITestElement, UnitTestResult> CreateTestResult;
        private static readonly Func<TestResultId, ITestElement, string, UnitTestResult> CreateDataTestResult;
        private static readonly Func<Guid, ITestElement, TestOutcome, TestResultCounter, VSTestResult[], UnitTestResult> CreateAggregateDataTestResult;

        private readonly Guid _runId;
        private readonly ITestElement _test;
        private readonly List<UnitTestResult> _results = new List<UnitTestResult>();
        private bool _isTheory;

        private UnitTestRunner(Guid runId, ITestElement test)
        {
            _runId = runId;
            _test = test;
        }

        static UnitTestRunner()
        {
            try
            {
                const string ResultTypeName = "Microsoft.VisualStudio.TestTools.TestTypes.Unit.UnitTestResult";
                const string ResultTypeAssembly = "Microsoft.VisualStudio.QualityTools.Tips.UnitTest.ObjectModel, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
                Type resultType = Type.GetType(ResultTypeName + ", " + ResultTypeAssembly, throwOnError: true, ignoreCase: true);

                var runId = Expression.Parameter(typeof(Guid), "runId");
                var test = Expression.Parameter(typeof(ITestElement), "test");
                var ctorTestResult = GetNewTestResultExpression(resultType, runId, test);
                CreateTestResultMethod(ctorTestResult, out CreateTestResult, runId, test);

                var id = Expression.Parameter(typeof(TestResultId), "id");
                var data = Expression.Parameter(typeof(string), "data");
                var ctorDataTestResult = GetNewTestResultExpression(resultType, id, test);
                var initDataRowInfo = Expression.Bind(resultType.GetProperty("DataRowInfo", typeof(string)), data);
                var initDataTestResult = Expression.MemberInit(ctorDataTestResult, initDataRowInfo);
                CreateTestResultMethod(initDataTestResult, out CreateDataTestResult, id, test, data);

                var outcome = Expression.Parameter(typeof(TestOutcome), "outcome");
                var counters = Expression.Parameter(typeof(TestResultCounter), "counters");
                var innerResults = Expression.Parameter(typeof(VSTestResult[]), "innerResults");
                var ctorAggregateDataTestResult = GetNewTestResultExpression(resultType, runId, test, outcome, counters, innerResults);
                CreateTestResultMethod(ctorAggregateDataTestResult, out CreateAggregateDataTestResult, runId, test, outcome, counters, innerResults);
            }
            catch (Exception ex)
            {
                Guard.Fail(ex, "Error retrieving result type.");
                throw;
            }
        }

        static NewExpression GetNewTestResultExpression(Type resultType, params ParameterExpression[] parameters)
        {
            Type[] types = Array.ConvertAll(parameters, p => p.Type);
            var ctor = resultType.GetConstructor(types);
            return Expression.New(ctor, parameters);
        }

        static void CreateTestResultMethod<TDelegate>(Expression body, out TDelegate method, params ParameterExpression[] parameters)
        {
            var lambda = Expression.Lambda<TDelegate>(body, parameters);
            method = lambda.Compile();
        }

        /// <summary>
        /// Executes the test.
        /// </summary>
        /// <param name="type">The test class type.</param>
        /// <param name="testClass">The test class.</param>
        /// <returns></returns>
        public static UnitTestResult ExecuteTest(IExecutorWrapper executor, Guid runId, UnitTest test)
        {
            var logger = new UnitTestRunner(runId, test);
            var runner = new TestRunner(executor, logger);

            runner.RunTest(test.Owner, test.Name);

            UnitTestResult result;
            if (logger._isTheory)
            {
                Debug.Assert(logger._results.Count > 0, "Expected at least one result for Theory test " + test.Name + ": " + logger._results.Count);
                UnitTestResult[] innerResults = logger._results.ToArray();
                TestOutcome outcome = TestOutcomeHelper.GetAggregationOutcome(innerResults);
                result = CreateAggregateDataTestResult(runId, test, outcome, null, innerResults);
            }
            else
            {
                Debug.Assert(logger._results.Count == 1, "Expected one result for non-Theory test " + test.Name + ": " + logger._results.Count);
                result = logger._results[0];
            }

            return result;
        }

        #region IRunnerLogger Members

        public void AssemblyStart(string assemblyFilename, string configFilename, string xUnitVersion)
        {
            Trace.TraceInformation("UnitTestRunner.AssemblyStart was called.");
        }

        public void ExceptionThrown(string assemblyFilename, Exception exception)
        {
            Trace.TraceInformation("UnitTestRunner.ExceptionThrown was called.");

            TestFailed("", "", "", 0.0, null, exception.GetType().FullName, exception.Message, exception.StackTrace);
        }

        public void AssemblyFinished(string assemblyFilename, int total, int failed, int skipped, double time)
        {
            Trace.TraceInformation("UnitTestRunner.AssemblyFinished was called.");
        }

        public bool ClassFailed(string className, string exceptionType, string message, string stackTrace)
        {
            Trace.TraceInformation("UnitTestRunner.ClassFailed was called.");

            return true;
        }

        public bool TestStart(string name, string type, string method)
        {
            Trace.TraceInformation("UnitTestRunner.TestStart was called.");

            return true;
        }

        public void TestPassed(string name, string type, string method, double duration, string output)
        {
            Trace.TraceInformation("UnitTestRunner.TestPassed was called.");

            var result = TestComplete(name, TestOutcome.Passed, duration, output);
        }

        public void TestFailed(string name, string type, string method, double duration, string output, string exceptionType, string message, string stackTrace)
        {
            Trace.TraceInformation("UnitTestRunner.TestFailed was called.");

            var result = TestComplete(name, TestOutcome.Failed, duration, output);
            result.ErrorInfo = new TestResultErrorInfo(exceptionType + ": " + message);
            result.ErrorStackTrace = stackTrace;
        }

        public void TestSkipped(string name, string type, string method, string reason)
        {
            Trace.TraceInformation("UnitTestRunner.TestSkipped was called.");

            var result = TestComplete(name, TestOutcome.Completed, 0, null);
            result.ErrorMessage = reason;
            //result.AddTextMessage(reason);
        }

        public bool TestFinished(string name, string type, string method)
        {
            Trace.TraceInformation("UnitTestRunner.TestFinished was called.");

            return true;
        }

        private UnitTestResult TestComplete(string name, TestOutcome outcome, double duration, string output)
        {
            UnitTestResult result;
            int dataIndex = name.IndexOf('(') + 1;
            if (dataIndex > 0)
            {
                _isTheory = true;
                string data = name.Substring(dataIndex, name.Length - dataIndex - 1);
                var id = new TestResultId(_runId, new TestExecId(), _test.ExecutionId, _test.Id);
                result = CreateDataTestResult(id, _test, data);
            }
            else
            {
                result = CreateTestResult(_runId, _test);
            }

            _results.Add(result);

            result.EndTime = DateTime.Now;
            result.Duration = TimeSpan.FromSeconds(duration);
            result.StartTime = result.EndTime - result.Duration;

            result.Outcome = outcome;
            result.StdOut = output ?? "";

            return result;
        }

        #endregion
    }
}
