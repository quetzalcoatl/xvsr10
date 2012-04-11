using System;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.Common;

namespace Xunit.Runner.VisualStudio.VS2010
{
    // FYI: the internal classes like Microsoft.VisualStudio.TestTools.TestTypes.Agent.ProcessStrategy
    // cannot be referenced directly, hence, a set of aliases is defined and their nearest BASE CLASSES
    // are used instead.
    using MSVST2V_ISolutionIntegrationManagerTmiHelper = IDisposable; // surrogate for Microsoft.VisualStudio.TestTools.Vsip.ISolutionIntegrationManagerTmiHelper
    using MSVST3M_ControllerProxy = IDisposable; // surrogate for Microsoft.VisualStudio.TestTools.TestManagement.ControllerProxy
    using MSVST3M_Tmi = ITmi; // surrogate for Microsoft.VisualStudio.TestTools.TestManagement.Tmi

    /// <summary>
    /// This class provides access to all the needed functionality of hidden Microsoft.VisualStudio.TestTools.TestManagement namespace.
    /// </summary>
    public static class MSVST3M_Access
    {
        public static ITmi GetTmi(IServiceProvider isp)
        {
            var hlp = MSVST3M_Tunnels.GetTmiHelper(isp);
            return MSVST3M_Tunnels.TH_TmiInstance(hlp);
        }

        public static void ShutdownLocalAgent(ITmi tmi)
        {
            // special URI is taken from internal static class Microsoft.VisualStudio.TestTools.Common.ControllerDefaults.public static field LocalControllerUri
            var cpx = MSVST3M_Tunnels.TM_GetControllerProxy(tmi, new Uri("local://"));
            if (MSVST3M_Tunnels.localCtrlType.IsAssignableFrom(cpx.GetType()))
                MSVST3M_Tunnels.LC_StopProcess(cpx);
        }
    }

    /// <summary>
    /// This class provides all the ugly internals of accessing the hidden classes and methods
    /// </summary>
    public static class MSVST3M_Tunnels
    {
        public static readonly Type sintegMgrType;
        public static readonly Type tmiHelperType;
        public static readonly Func<IServiceProvider, MSVST2V_ISolutionIntegrationManagerTmiHelper> GetTmiHelper;
        public static readonly Func<MSVST2V_ISolutionIntegrationManagerTmiHelper, MSVST3M_Tmi> TH_TmiInstance;

        public static readonly Type tmiType;
        public static readonly Func<MSVST3M_Tmi, Uri, MSVST3M_ControllerProxy> TM_GetControllerProxy;

        public static readonly Type localCtrlType;
        public static readonly Action<MSVST3M_ControllerProxy> LC_StopProcess;
        
        static MSVST3M_Tunnels()
        {
            try
            {
                const string BaseTuipAssembly = "Microsoft.VisualStudio.QualityTools.Vsip, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
                const string SolIntMgrTypeName = "Microsoft.VisualStudio.TestTools.Vsip.SSolutionIntegrationManager";
                const string TmiHelperTypeName = "Microsoft.VisualStudio.TestTools.Vsip.ISolutionIntegrationManagerTmiHelper";

                const string TMIAssembly = "Microsoft.VisualStudio.QualityTools.TMI, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
                const string TmiTypeName = "Microsoft.VisualStudio.TestTools.TestManagement.Tmi";
                const string LocalControllerTypeName = "Microsoft.VisualStudio.TestTools.TestManagement.LocalControllerProxy";

                sintegMgrType = Type.GetType(SolIntMgrTypeName + ", " + BaseTuipAssembly, throwOnError: true, ignoreCase: true);
                tmiHelperType = Type.GetType(TmiHelperTypeName + ", " + BaseTuipAssembly, throwOnError: true, ignoreCase: true);
                tmiType = Type.GetType(TmiTypeName + ", " + TMIAssembly, throwOnError: true, ignoreCase: true);
                localCtrlType = Type.GetType(LocalControllerTypeName + ", " + TMIAssembly, throwOnError: true, ignoreCase: true);

                GetTmiHelper = isp => (MSVST2V_ISolutionIntegrationManagerTmiHelper)isp.GetService(sintegMgrType);

                IAHelpers.InitCallLambda(out TH_TmiInstance, tmiHelperType.GetProperty("TmiInstance").GetGetMethod());

                var uri = Expression.Parameter(typeof(Uri), "uri");
                IAHelpers.InitCallLambda(out TM_GetControllerProxy, tmiType.GetMethod("GetControllerProxy", new[] { typeof(Uri) }), typeof(Uri));

                IAHelpers.InitCallLambda(out LC_StopProcess, localCtrlType.GetMethod("StopProcess", BindingFlags.Instance | BindingFlags.NonPublic, null, Type.EmptyTypes, null));
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error while creating nonpublic bridges: " + ex.Message);
                throw;
            }
        }
    }
}
