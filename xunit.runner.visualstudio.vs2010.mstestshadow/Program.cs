using System;
using System.Reflection;
using System.Runtime.InteropServices;
namespace xunit.runner.visualstudio.vs2010.mstestshadow
{
    public class Program
    {
        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern IntPtr LoadLibrary(string lpFileName);

        public static void Main(string[] args)
        {
            //var mstestrunner = Type.GetType("Microsoft.VisualStudio.TestTools.RunnerCommandline, MSTest, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
            //// .Main(args)

            //var testhelper = Type.GetType("Microsoft.VisualStudio.TestTools.Common.TestConfigHelper, Microsoft.VisualStudio.QualityTools.Common, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
            //var defroot = testhelper.GetProperty("DefaultRegistryRoot", BindingFlags.Static | BindingFlags.NonPublic);

            //var k0 = Microsoft.Win32.RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.LocalMachine, Microsoft.Win32.RegistryView.Default);
            //var l0 = k0.OpenSubKey("Software\\Microsoft\\VisualStudio\\10.0\\EnterpriseTools\\QualityTools\\TestTypes");
            //var m0 = l0.SubKeyCount;

            // var assdev = Assembly.LoadFile(@"C:\Program Files\Microsoft Visual Studio 10.0\Common7\IDE\devenv.exe");
            //var x = LoadLibrary(@"C:\Program Files\Microsoft Visual Studio 10.0\Common7\IDE\devenv.exe");

            //var assmss = Assembly.Load(@"Microsoft.VisualStudio.Settings, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
            var extmgr = Microsoft.VisualStudio.Settings.ExternalSettingsManager.CreateForApplication(@"C:\Program Files\Microsoft Visual Studio 10.0\Common7\IDE\devenv.exe");

            var k1 = Microsoft.Win32.RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.LocalMachine, Microsoft.Win32.RegistryView.Default);
            var l1 = k1.OpenSubKey("Software\\Microsoft\\VisualStudio\\10.0\\EnterpriseTools\\QualityTools\\TestTypes");
            var m1 = l1.SubKeyCount;

            var cnt = extmgr.GetReadOnlySettingsStore(Microsoft.VisualStudio.Settings.SettingsScope.Configuration).GetSubCollectionCount("EnterpriseTools\\QualityTools\\TestTypes");
        }
    }
}
