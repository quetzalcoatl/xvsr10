using System;
using System.Diagnostics;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;

namespace Xunit.Runner.VisualStudio.VS2010
{
    // FYI: the internal classes like Microsoft.VisualStudio.TestTools.TestTypes.Agent.ProcessStrategy
    // cannot be referenced directly, hence, a set of aliases is defined and their nearest BASE CLASSES
    // are used instead.
    using MSVST2A_AgentProcessManager = IDisposable; // surrogate for Microsoft.VisualStudio.TestTools.Agent.AgentProcessManager
    using MSVST2A_AgentProcessProxyManager = Object; // surrogate for Microsoft.VisualStudio.TestTools.Agent.AgentProcessProxyManager
    using MSVST2A_AgentProxy = Object; // surrogate for Microsoft.VisualStudio.TestTools.Agent.AgentProxy
    using MSVST2A_IProcessStrategy = Object; // surrogate for Microsoft.VisualStudio.TestTools.Agent.IProcessStrategy
    using MSVST2A_OutOfProcessStrategy = MarshalByRefObject; // surrogate for Microsoft.VisualStudio.TestTools.Agent.OutOfProcessStrategy
    using MSVST2A_OutOfProcessTestAgentStrategy = MarshalByRefObject; // surrogate for Microsoft.VisualStudio.TestTools.Agent.OutOfProcessTestAgentStrategy
    using MSVST2A_ProcessStrategy = Enum; // surrogate for Microsoft.VisualStudio.TestTools.Agent.ProcessStrategy
    using MSVST2A_TestAgentProxy = Object; // surrogate for Microsoft.VisualStudio.TestTools.Agent.TestAgentProxy
    using MSVST2C_AssemblyClrVersion = Enum; // surrogate for Microsoft.VisualStudio.TestTools.Common.AssemblyClrVersion
    using MSVST2E_HostProcessPlatformHelper = Object; // surrogate for Microsoft.VisualStudio.TestTools.Execution.HostProcessPlatformHelper

    /// <summary>
    /// This class provides access to all the needed functionality of hidden Microsoft.VisualStudio.TestTools.Agent namespace.
    /// </summary>
    public static class MSVST2A_Access
    {
        public static string VisualStudioIdePath
        {
            get
            {
                var path = MSVST2A_Tunnels.CH_ComputeApplicationPath(false);
                if (path == null) throw new InvalidOperationException("Could not obtain path to devenv.exe");
                if (!string.Equals(Path.GetFileName(path), "devenv.exe", StringComparison.InvariantCultureIgnoreCase)) throw new InvalidOperationException("Some path was found, but it does not match the devenv.exe");
                return Path.GetDirectoryName(path);
            }
        }

        public static string QTAgentExecutableFilename
        {
            get
            {
                // from note 003.1:
                //// To create the Strategy, a TestAgentProxy|DataCollectionAgentProxy is needed.
                //// To create it, an AgentProcessProxyManager is needed. The two classes above does not check it, but the Strategy's base class does some nullchecks.
                ////
                //// new AgentProcessProxyManager(null, ProcessorArchitecture=?, AssemblyClrVersion=?, ProcessStrategy=OutOfProc)
                //// new TestAgentProxy(proxyManager, int any, ProcessStrategy=OutOfProc)
                //// agentProxy.StrategyImpl or new OutOfProcessTestAgentStrategy(agentProxy, proxyManager, int any)
                //// then
                //// Microsoft.VisualStudio.TestTools.Agent.internal abstract class OutOfProcessStrategy.GetAgentProcessExeName(this.AgentProcessProxyManager.CurrentPlatform, this.AgentProcessProxyManager.CurrentClrVersion);
                ////
                //// platform data from: HostProcessPlatformHelper -> .CurrentPlatform .CurrentClrVersion

                var ph = MSVST2A_Tunnels.CreatePlatformHelper();
                var pa = MSVST2A_Tunnels.PH_CurrentPlatform(ph);
                var acv = MSVST2A_Tunnels.PH_CurrentClrVersion(ph);

                var ps = MSVST2A_Tunnels.strategyKind_OutOfProc;

                var appm = MSVST2A_Tunnels.CreateProxyManager(null, pa, acv, ps);

                var tap = MSVST2A_Tunnels.CreateTestAgentProxy(appm, 1, ps);
                var si = MSVST2A_Tunnels.TP_StrategyImpl(tap);

                var si2 = si as MSVST2A_OutOfProcessStrategy; // scam for the compiler
                if (!MSVST2A_Tunnels.strategyImplType.IsInstanceOfType(si)) // real type test
                    si2 = MSVST2A_Tunnels.CreateOutofprocStrategyImpl(tap, appm, 1) as MSVST2A_OutOfProcessStrategy;

                return MSVST2A_Tunnels.OO_GetAgentProcessExeName(si2, pa, acv);
            }
        }
    }

    /// <summary>
    /// This class provides all the ugly internals of accessing the hidden classes and methods
    /// </summary>
    public static class MSVST2A_Tunnels
    {
        public static readonly Type clrVersionType;

        public static readonly Type configHelperType;
        public static readonly Func<bool, string> CH_ComputeApplicationPath;

        public static readonly Type platformHelperType;
        public static readonly Func<MSVST2E_HostProcessPlatformHelper> CreatePlatformHelper;
        public static readonly Func<MSVST2E_HostProcessPlatformHelper, ProcessorArchitecture> PH_CurrentPlatform;
        public static readonly Func<MSVST2E_HostProcessPlatformHelper, MSVST2C_AssemblyClrVersion> PH_CurrentClrVersion;

        public static readonly Type strategyKindType;
        public static readonly MSVST2A_ProcessStrategy strategyKind_InProc;
        public static readonly MSVST2A_ProcessStrategy strategyKind_OutOfProc;

        public static readonly Type managerType;

        public static readonly Type proxyManagerType;
        public static readonly Func<MSVST2A_AgentProcessManager, ProcessorArchitecture, MSVST2C_AssemblyClrVersion, MSVST2A_ProcessStrategy, MSVST2A_AgentProcessProxyManager> CreateProxyManager;

        public static readonly Type agentProxyType;

        public static readonly Type testAgentProxyType;
        public static readonly Func<MSVST2A_AgentProcessProxyManager, int, MSVST2A_ProcessStrategy, MSVST2A_TestAgentProxy> CreateTestAgentProxy;
        public static readonly Func<MSVST2A_TestAgentProxy, MSVST2A_IProcessStrategy> TP_StrategyImpl;

        public static readonly Type baseStrategyType;
        public static readonly Type strategyImplType;
        public static readonly Func<MSVST2A_TestAgentProxy, MSVST2A_AgentProcessProxyManager, int, MSVST2A_OutOfProcessTestAgentStrategy> CreateOutofprocStrategyImpl;
        public static readonly Func<MSVST2A_OutOfProcessStrategy, ProcessorArchitecture, MSVST2C_AssemblyClrVersion, string> OO_GetAgentProcessExeName;

        static MSVST2A_Tunnels()
        {
            try
            {
                const string CommonAssembly = "Microsoft.VisualStudio.QualityTools.Common, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
                const string ClrVersionTypeName = "Microsoft.VisualStudio.TestTools.Common.AssemblyClrVersion";

                const string HelpersAssembly = "Microsoft.VisualStudio.QualityTools.ExecutionCommon, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
                const string ConfigHelperTypeName = "Microsoft.VisualStudio.TestTools.Execution.ConfigurationHelper";
                const string PlatformHelperTypeName = "Microsoft.VisualStudio.TestTools.Execution.HostProcessPlatformHelper";

                const string ProxyManagerAssembly = "Microsoft.VisualStudio.QualityTools.AgentProcessManager, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
                const string ProcessStrategyTypeName = "Microsoft.VisualStudio.TestTools.Agent.ProcessStrategy";
                const string ManagerTypeName = "Microsoft.VisualStudio.TestTools.Agent.AgentProcessManager";
                const string ProxyManagerTypeName = "Microsoft.VisualStudio.TestTools.Agent.AgentProcessProxyManager";
                const string agentProxyTypeName = "Microsoft.VisualStudio.TestTools.Agent.AgentProxy";
                const string TestAgentProxyTypeName = "Microsoft.VisualStudio.TestTools.Agent.TestAgentProxy";
                const string BaseStrategyTypeName = "Microsoft.VisualStudio.TestTools.Agent.OutOfProcessStrategy";
                const string StrategyImplTypeName = "Microsoft.VisualStudio.TestTools.Agent.OutOfProcessTestAgentStrategy";

                clrVersionType = Type.GetType(ClrVersionTypeName + ", " + CommonAssembly, throwOnError: true, ignoreCase: true);
                configHelperType = Type.GetType(ConfigHelperTypeName + ", " + HelpersAssembly, throwOnError: true, ignoreCase: true);
                platformHelperType = Type.GetType(PlatformHelperTypeName + ", " + HelpersAssembly, throwOnError: true, ignoreCase: true);
                strategyKindType = Type.GetType(ProcessStrategyTypeName + ", " + ProxyManagerAssembly, throwOnError: true, ignoreCase: true);
                managerType = Type.GetType(ManagerTypeName + ", " + ProxyManagerAssembly, throwOnError: true, ignoreCase: true);
                proxyManagerType = Type.GetType(ProxyManagerTypeName + ", " + ProxyManagerAssembly, throwOnError: true, ignoreCase: true);
                agentProxyType = Type.GetType(agentProxyTypeName + ", " + ProxyManagerAssembly, throwOnError: true, ignoreCase: true);
                testAgentProxyType = Type.GetType(TestAgentProxyTypeName + ", " + ProxyManagerAssembly, throwOnError: true, ignoreCase: true);
                baseStrategyType = Type.GetType(BaseStrategyTypeName + ", " + ProxyManagerAssembly, throwOnError: true, ignoreCase: true);
                strategyImplType = Type.GetType(StrategyImplTypeName + ", " + ProxyManagerAssembly, throwOnError: true, ignoreCase: true);

                IAHelpers.InitSCallLambda(out CH_ComputeApplicationPath, configHelperType.GetMethod("ComputeApplicationPath", BindingFlags.Static | BindingFlags.Public), typeof(bool));

                IAHelpers.InitNewLambda(out CreatePlatformHelper, platformHelperType);
                IAHelpers.InitCallLambda(out PH_CurrentPlatform, platformHelperType.GetProperty("CurrentPlatform").GetGetMethod());
                IAHelpers.InitCallLambda(out PH_CurrentClrVersion, platformHelperType.GetProperty("CurrentClrVersion").GetGetMethod());

                strategyKind_InProc = (MSVST2A_ProcessStrategy)Enum.Parse(strategyKindType, "InProc");
                strategyKind_OutOfProc = (MSVST2A_ProcessStrategy)Enum.Parse(strategyKindType, "OutOfProc");

                var _mgr = Expression.Parameter(typeof(MSVST2A_AgentProcessManager), "mgr");
                var _pmgr = Expression.Parameter(typeof(MSVST2A_AgentProcessProxyManager), "pmgr");
                var _ap = Expression.Parameter(typeof(MSVST2A_AgentProxy), "ap");
                var arch = Expression.Parameter(typeof(ProcessorArchitecture), "arch");
                var _clrv = Expression.Parameter(typeof(MSVST2C_AssemblyClrVersion), "clrv");
                var _kind = Expression.Parameter(typeof(MSVST2A_ProcessStrategy), "kind");
                var _str = Expression.Parameter(typeof(MSVST2A_OutOfProcessStrategy), "str");
                var id = Expression.Parameter(typeof(int), "id");

                var mgr = Expression.Convert(_mgr, managerType);
                var pmgr = Expression.Convert(_pmgr, proxyManagerType);
                var ap = Expression.Convert(_ap, agentProxyType);
                var clrv = Expression.Convert(_clrv, clrVersionType);
                var kind = Expression.Convert(_kind, strategyKindType);
                var str = Expression.Convert(_str, baseStrategyType);

                var ctorProxyManager = IAHelpers.GetNewExpression(proxyManagerType, mgr, arch, clrv, kind);
                IAHelpers.CreateMethod(ctorProxyManager, out CreateProxyManager, _mgr, arch, _clrv, _kind);

                var ctorAgentProxyManager = IAHelpers.GetNewExpression(testAgentProxyType, pmgr, id, kind);
                IAHelpers.CreateMethod(ctorAgentProxyManager, out CreateTestAgentProxy, _pmgr, id, _kind);
                
                var getStrImpl = IAHelpers.GetCallExpression(ap, testAgentProxyType.GetProperty("StrategyImpl", BindingFlags.Instance | BindingFlags.NonPublic).GetGetMethod(true));
                IAHelpers.CreateMethod(getStrImpl, out TP_StrategyImpl, _ap);

                var getExeName = IAHelpers.GetCallExpression(str, baseStrategyType.GetMethod("GetAgentProcessExeName", BindingFlags.Instance | BindingFlags.NonPublic), arch, clrv);
                IAHelpers.CreateMethod(getExeName, out OO_GetAgentProcessExeName, _str, arch, _clrv);

                var ctorStrImpl = IAHelpers.GetNewExpression(strategyImplType, ap, pmgr, id);
                IAHelpers.CreateMethod(ctorStrImpl, out CreateOutofprocStrategyImpl, _ap, _pmgr, id);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error while creating nonpublic bridges: " + ex.Message);
                throw;
            }
        }
    }
}
