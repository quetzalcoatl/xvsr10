using System;
using System.Linq.Expressions;
using System.Reflection;
//using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
//using System.Xml;
using Microsoft.VisualStudio.TestTools.Common;
using Microsoft.VisualStudio.TestTools.Common.Xml;

namespace Xunit.Runner.VisualStudio.VS2010
{
    // FYI: the internal classes like Microsoft.VisualStudio.TestTools.TestTypes.Unit.UnitTestElement
    // cannot be referenced directly, hence, a set of aliases is defined and their nearest BASE CLASSES
    // are used instead.
    using MSVST4U_TestMethod = Microsoft.VisualStudio.TestTools.Common.Xml.IXmlTestStore; // surrogate for Microsoft.VisualStudio.TestTools.TestTypes.Unit.TestMethod
    using MSVST4U_UnitTestElement = Microsoft.VisualStudio.TestTools.Common.TestElement; // surrogate for Microsoft.VisualStudio.TestTools.TestTypes.Unit.UnitTestElement
    using MSVST4U_UnitTestResult = Microsoft.VisualStudio.TestTools.Common.TestResultAggregation; // surrogate for Microsoft.VisualStudio.TestTools.TestTypes.Unit.UnitTestResult
    using MSVSTTC_TestResult = Microsoft.VisualStudio.TestTools.Common.TestResult;// btw. this one must be aliased too, because it clashes with Xunit.TestResult

    /// <summary>
    /// This class provides access to all the needed functionality of hidden Microsoft.VisualStudio.TestTools.TestTypes.Unit namespace.
    /// </summary>
    public static class MSVST4U_Access
    {
        // this method is really handy
        public static ITestElement New_MSVST4U_UnitTestElement(string assemblyFileLocation, string fullClassName, string methodName, ProjectData projectData)
        {
            // The UnitTestElement constructor needs a some basic information, most notably, the "TestMethod" instance
            // that wraps FullClassName and MethodName. The ugly part is, that even the TestMethod class is internal too.

            // Ah, and one more thing. When the Microsoft's internals somethimes try to quickly match or find tests with 
            // the use customized generation of the test's IDs. It important to mimic those signature-like behaviour, just
            // to be sure that the test will not be improperly thought to be missing in some contexts.
            // For a "member-level" tests (that is, a test method or a test property), the keygen algorithm uses
            // a "full member name" as the generator's seed
            Guid testid = MSVST4U_Tunnels.EqtHash.GuidFromString(fullClassName + "." + methodName);
            // For a 'class-level' tests it would be a "full class name".

            var vsmethod = MSVST4U_Tunnels.CreateTestMethod(methodName, fullClassName);

            // This is the important trick: although we create the actual internal UnitTestElement, it is important
            // to set the test adapter class name to OURS. From the VisulStudio's point of view, the test adapter is
            // the entity responsible for actually running the test and for publishing its results. It not the TestElement,
            // but the Adapter is the perfect place to actually takeover the control!
            var ts = MSVST4U_Tunnels.CreateTestElement(testid, methodName, typeof(XUnitTestAdapter).AssemblyQualifiedName, vsmethod);

            // We are not living in the perfect world, so of course, the internal UnitTestElement's constructor had
            // to skip a few important parts: the Storage must be set manually to CodeBase/Location of the assembly with tests,
            // and also the CodeBase must be set, too. If left empty or null, the test.IsValid would turn to false and the
            // TMI would warn and ignore them as not runnable.
            // Another thing that must be set is the ProjectData property. Without it the UnitTest would be accepted,
            // but its "project name", "solution name", and a few other properties would be missing, and it would
            // render a few features unavailable (like double clicking on the test in a not-yet-run test list and
            // jupming to the test's source)

            // BTW. see Microsoft.VisualStudio.TestTools.TestTypes.Unit.VSTypeEnumerator.AddTest for full test construction
            // workflow, both for CF and .Net and ASP tests.

            // BTW. if a UnitTestElement is to be returned, why not to call that method?????

            MSVST4U_Tunnels.TS_AssignCodeBase(ts, assemblyFileLocation);
            ts.ProjectData = projectData;
            ts.Storage = assemblyFileLocation;

            // and the last thing - setting the Storage and ProjectData has marked the test as 'Modified', we don't
            // want that - all in all, the test is fresly loaded.
            // BTW. the original Microsoft's code behaves in the exact same way: sets Storage, clears IsModified :)
            ts.IsModified = false; // reset the change marker

            return ts;
        }
        public static string GetFullyQualifiedClassName(MSVST4U_UnitTestElement testElement) { return MSVST4U_Tunnels.TS_FullyQualifiedClassName(testElement); }

        // and the following ones are here just to record the parameter names (missing in the delegates in MSVSAccess)
        public static MSVST4U_UnitTestResult New_MSVST4U_UnitTestResult_Standard(Guid runId, ITestElement test) { return MSVST4U_Tunnels.CreateTestResult(runId, test); }
        public static MSVST4U_UnitTestResult New_MSVST4U_UnitTestResult_DataDrivenRow(TestResultId id, ITestElement test, string dataRowInfo) { return MSVST4U_Tunnels.CreateDataTestResult(id, test, dataRowInfo); }
        public static MSVST4U_UnitTestResult New_MSVST4U_UnitTestResult_DataDriven(Guid runId, ITestElement test, TestOutcome outcome, TestResultCounter counters, MSVSTTC_TestResult[] innerResults) { return MSVST4U_Tunnels.CreateAggregateDataTestResult(runId, test, outcome, counters, innerResults); }
    }

    /// <summary>
    /// This class provides all the ugly internals of accessing the hidden classes and methods
    /// </summary>
    public static class MSVST4U_Tunnels
    {
        public static readonly Type resultType;
        public static readonly Func<Guid, ITestElement, MSVST4U_UnitTestResult> CreateTestResult;
        public static readonly Func<TestResultId, ITestElement, string, MSVST4U_UnitTestResult> CreateDataTestResult;
        public static readonly Func<Guid, ITestElement, TestOutcome, TestResultCounter, MSVSTTC_TestResult[], MSVST4U_UnitTestResult> CreateAggregateDataTestResult;

        public static readonly Type elementType;
        public static readonly Func<Guid, string, string, MSVST4U_TestMethod, MSVST4U_UnitTestElement> CreateTestElement;
        public static readonly Action<MSVST4U_UnitTestElement, string> TS_AssignCodeBase;
        public static readonly Func<MSVST4U_UnitTestElement, string> TS_FullyQualifiedClassName;

        public static readonly Type testmethodType;
        public static readonly Func<string, string, MSVST4U_TestMethod> CreateTestMethod;

        //public static readonly Type shelpType;
        //public static readonly Func<SerializationInfo, int, object> CreateSerializationHelper; - not needed, the custom TestElement has been refactored out
        //public static readonly Action<object> SH_BeginDeserialization;
        //public static readonly Func<object, string> SH_GetString;
        //public static readonly Func<object, Type, object, object> SH_GetField;
        //public static readonly Action<object> SH_EndDeserialization;
        //public static readonly Action<object> SH_BeginSerialization;
        //public static readonly Action<object, string> SH_AddString;
        //public static readonly Action<object, object, object, Type> SH_AddField;
        //public static readonly Action<object> SH_EndSerialization;

        //public static readonly Type xpersType;
        //public static readonly Func<object> CreateXmlPersistence; - not needed, the custom TestElement has been refactored out
        //public static readonly Action<XmlElement, object, Type, XmlTestStoreParameters> XP_LoadUsingReflection;
        //public static readonly Func<object, XmlElement, string, Type, object, object> XP_LoadSimpleField;
        //public static readonly Func<object, XmlElement, string, XmlTestStoreParameters, Type, object> XP_LoadIXmlTestStore;
        //public static readonly Action<object, XmlElement, string, object, object> XP_SaveSimpleField;
        //public static readonly Action<object, object, XmlElement, string, XmlTestStoreParameters> XP_SaveObject;

        static MSVST4U_Tunnels()
        {
            try
            {
                {
                    const string ResultTypeAssembly = "Microsoft.VisualStudio.QualityTools.Tips.UnitTest.ObjectModel, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
                    const string ResultTypeName = "Microsoft.VisualStudio.TestTools.TestTypes.Unit.UnitTestResult";
                    const string ElementTypeName = "Microsoft.VisualStudio.TestTools.TestTypes.Unit.UnitTestElement";
                    const string TestMethodTypeName = "Microsoft.VisualStudio.TestTools.TestTypes.Unit.TestMethod";
                    resultType = Type.GetType(ResultTypeName + ", " + ResultTypeAssembly, throwOnError: true, ignoreCase: true);
                    elementType = Type.GetType(ElementTypeName + ", " + ResultTypeAssembly, throwOnError: true, ignoreCase: true);
                    testmethodType = Type.GetType(TestMethodTypeName + ", " + ResultTypeAssembly, throwOnError: true, ignoreCase: true);

                    InitNewLambda(out CreateTestResult, resultType, typeof(Guid), typeof(ITestElement)); // <<-- NotDataDriven

                    var id = Expression.Parameter(typeof(TestResultId), "id");
                    var test = Expression.Parameter(typeof(ITestElement), "test");
                    var data = Expression.Parameter(typeof(string), "data");
                    var ctorDataTestResult = GetNewExpression(resultType, id, test);  // <<-- DataDrivenDataRow
                    var initDataRowInfo = Expression.Bind(resultType.GetProperty("DataRowInfo", typeof(string)), data);
                    var initDataTestResult = Expression.MemberInit(ctorDataTestResult, initDataRowInfo);
                    CreateMethod(initDataTestResult, out CreateDataTestResult, id, test, data);

                    InitNewLambda(out CreateAggregateDataTestResult, resultType, typeof(Guid), typeof(ITestElement), typeof(TestOutcome), typeof(TestResultCounter), typeof(MSVSTTC_TestResult[])); // <<-- DataDriven

                    var gid = Expression.Parameter(typeof(Guid), "id");
                    var name = Expression.Parameter(typeof(string), "name");
                    var adapter = Expression.Parameter(typeof(string), "adapterTypeName");
                    var _testmethod = Expression.Parameter(typeof(IXmlTestStore), "testMethod");
                    var testmethod = Expression.Convert(_testmethod, testmethodType);
                    var ctorTestElement = GetNewExpression(elementType, gid, name, adapter, testmethod);
                    CreateMethod(ctorTestElement, out CreateTestElement, gid, name, adapter, _testmethod);
                    InitCallLambda(out TS_AssignCodeBase, elementType.GetMethod("AssignCodeBase", BindingFlags.Instance | BindingFlags.Public), typeof(string));
                    InitCallLambda(out TS_FullyQualifiedClassName, elementType.GetProperty("FullyQualifiedClassName", BindingFlags.Instance | BindingFlags.Public).GetGetMethod());
                    
                    InitNewLambda(out CreateTestMethod, testmethodType, typeof(string), typeof(string)); // name classname
                }
                //{
                //    const string HelpersTypeAssembly = "Microsoft.VisualStudio.QualityTools.Common, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
                //    const string SHelpTypeName = "Microsoft.VisualStudio.TestTools.Common.SerializationHelper";
                //    const string XPersTypeName = "Microsoft.VisualStudio.TestTools.Common.Xml.XmlPersistence";
                //    shelpType = Type.GetType(SHelpTypeName + ", " + HelpersTypeAssembly, throwOnError: true, ignoreCase: true);
                //    xpersType = Type.GetType(XPersTypeName + ", " + HelpersTypeAssembly, throwOnError: true, ignoreCase: true);

                //    InitNewLambda(out CreateSerializationHelper, shelpType, typeof(SerializationInfo), typeof(int));

                //    InitCallLambda(out SH_BeginDeserialization, shelpType.GetMethod("BeginDeserialization", BindingFlags.Instance | BindingFlags.Public, null, Type.EmptyTypes, null));
                //    InitCallLambda(out SH_GetString, shelpType.GetMethod("GetString", BindingFlags.Instance | BindingFlags.Public, null, Type.EmptyTypes, null));
                //    InitCallLambda(out SH_GetField, shelpType.GetMethod("GetField", BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(Type), typeof(object) }, null), typeof(Type), typeof(object));
                //    InitCallLambda(out SH_EndDeserialization, shelpType.GetMethod("EndDeserialization", BindingFlags.Instance | BindingFlags.Public, null, Type.EmptyTypes, null));

                //    InitCallLambda(out SH_BeginSerialization, shelpType.GetMethod("BeginSerialization", BindingFlags.Instance | BindingFlags.Public, null, Type.EmptyTypes, null));
                //    InitCallLambda(out SH_AddString, shelpType.GetMethod("AddString", BindingFlags.Instance | BindingFlags.Public, null, new[] { typeof(string) }, null), typeof(string));
                //    InitCallLambda(out SH_AddField, shelpType.GetMethod("AddField", BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(object), typeof(object), typeof(Type) }, null), typeof(object), typeof(object), typeof(Type));
                //    InitCallLambda(out SH_EndSerialization, shelpType.GetMethod("EndSerialization", BindingFlags.Instance | BindingFlags.Public, null, Type.EmptyTypes, null));

                //    InitNewLambda(out CreateXmlPersistence, xpersType);

                //    InitSCallLambda(out XP_LoadUsingReflection, xpersType.GetMethod("LoadUsingReflection", BindingFlags.Static | BindingFlags.NonPublic, null, new[] { typeof(XmlElement), typeof(object), typeof(Type), typeof(XmlTestStoreParameters) }, null), typeof(XmlElement), typeof(object), typeof(Type), typeof(XmlTestStoreParameters));

                //    InitCallLambda(out XP_LoadSimpleField, xpersType.GetMethod("LoadSimpleField", BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(XmlElement), typeof(string), typeof(Type), typeof(object) }, null), typeof(XmlElement), typeof(string), typeof(Type), typeof(object));
                //    InitCallLambda(out XP_LoadIXmlTestStore, xpersType.GetMethod("LoadIXmlTestStore", BindingFlags.Instance | BindingFlags.Public, null, new[] { typeof(XmlElement), typeof(string), typeof(XmlTestStoreParameters), typeof(Type) }, null), typeof(XmlElement), typeof(string), typeof(XmlTestStoreParameters), typeof(Type));

                //    InitCallLambda(out XP_SaveSimpleField, xpersType.GetMethod("SaveSimpleField", BindingFlags.Instance | BindingFlags.Public, null, new[] { typeof(XmlElement), typeof(string), typeof(object), typeof(object) }, null), typeof(XmlElement), typeof(string), typeof(object), typeof(object));
                //    InitCallLambda(out XP_SaveObject, xpersType.GetMethod("SaveObject", BindingFlags.Instance | BindingFlags.Public, null, new[] { typeof(object), typeof(XmlElement), typeof(string), typeof(XmlTestStoreParameters) }, null), typeof(object), typeof(XmlElement), typeof(string), typeof(XmlTestStoreParameters));
                //}
            }
            catch (Exception ex)
            {
                Guard.Fail(ex, "Error while creating nonpublic bridges.");
                throw;
            }
        }

        public static NewExpression GetNewExpression(Type resultType, params Expression[] parameters)
        {
            Type[] types = Array.ConvertAll(parameters, p => p.Type);
            var ctor = resultType.GetConstructor(types);
            return Expression.New(ctor, parameters);
        }
        //public static MethodCallExpression GetSCallExpression(MethodInfo method, params Expression[] parameters)
        //{
        //    return Expression.Call(method, parameters);
        //}
        //public static MethodCallExpression GetCallExpression(Expression instance, MethodInfo method, params Expression[] parameters)
        //{
        //    return Expression.Call(instance, method, parameters);
        //}

        public static void InitNewLambda<TDelegate>(out TDelegate dmethod, Type resultType, params Type[] types)
        {
            var parameters = Array.ConvertAll(types, t => Expression.Parameter(t));
            var ctor = resultType.GetConstructor(types);
            CreateMethod<TDelegate>(Expression.New(ctor, parameters), out dmethod, parameters);
        }
        //public static void InitSCallLambda<TDelegate>(out TDelegate dmethod, MethodInfo method, params Type[] types)
        //{
        //    var parameters = Array.ConvertAll(types, t => Expression.Parameter(t));
        //    CreateMethod<TDelegate>(Expression.Call(method, parameters), out dmethod, parameters);
        //}
        public static void InitCallLambda<TDelegate>(out TDelegate dmethod, MethodInfo method, params Type[] types)
        {
            var subpars = Array.ConvertAll(types, t => Expression.Parameter(t));
            ParameterExpression[] arr = new ParameterExpression[subpars.Length + 1];
            arr[0] = Expression.Parameter(typeof(object));
            Array.Copy(subpars, 0, arr, 1, subpars.Length);
            CreateMethod<TDelegate>(Expression.Call(Expression.Convert(arr[0], method.ReflectedType), method, subpars), out dmethod, arr);
        }

        public static void CreateMethod<TDelegate>(Expression body, out TDelegate method, params ParameterExpression[] parameters)
        {
            var lambda = Expression.Lambda<TDelegate>(body, parameters);
            method = lambda.Compile();
        }

        // namespace Microsoft.VisualStudio.TestTools.Common
        public static class EqtHash
        {
            private static HashAlgorithm s_provider = (HashAlgorithm)new SHA1CryptoServiceProvider();
            private static HashAlgorithm Provider { get { return EqtHash.s_provider; } }

            public static Guid GuidFromString(string data)
            {
                byte[] hash = EqtHash.Provider.ComputeHash(Encoding.Unicode.GetBytes(data));
                byte[] b = new byte[16];
                Array.Copy((Array)hash, (Array)b, 16);
                return new Guid(b);
            }
        }
    }
}
