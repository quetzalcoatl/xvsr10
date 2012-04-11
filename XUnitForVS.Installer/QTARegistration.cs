using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.GAC;
using System.IO;
using System.Linq;
using System.Reflection;

namespace xunit.runner.visualstudio.vs2010.installer
{
    public static class AssemblyInstaller
    {
        [ThreadStatic]
        private static IAssemblyCache _gac = null;
        private static IAssemblyCache Gac { get { return _gac ?? (_gac = System.GAC.AssemblyCache.CreateAssemblyCache()); } }

        // public static string MakeLocalPath(string uriString) { return new Uri(uriString).LocalPath; }
        public const string VSPrivateSubDir = @"PrivateAssemblies\xunit.runner.visualstudio.vs2010"; // installroot points to devenv.exe and already contains Common7\IDE\

        public static bool IsInstalledAsVSPrivate(Assembly asm, string installRoot)
        {
            var head = Path.Combine(installRoot, VSPrivateSubDir);
            var path = Path.Combine(head, Path.GetFileName(asm.Location));
            if (!File.Exists(path)) return false;
            try
            {
                var asn1 = asm.GetName();
                var asn2 = AssemblyName.GetAssemblyName(path);
                return asn2.FullName == asn1.FullName
                    && asn2.HashAlgorithm == asn1.HashAlgorithm
                    && asn2.ProcessorArchitecture == asn1.ProcessorArchitecture;
            }
            catch { return false; }
        }
        public static bool IsInstalledAsVSPrivateDiff(Assembly asm, string installRoot)
        {
            var head = Path.Combine(installRoot, VSPrivateSubDir);
            var path = Path.Combine(head, Path.GetFileName(asm.Location));
            if (!File.Exists(path)) return false;
            try
            {
                var asn1 = asm.GetName();
                var asn2 = AssemblyName.GetAssemblyName(path);
                return asn2.FullName != asn1.FullName
                    || asn2.HashAlgorithm != asn1.HashAlgorithm
                    || asn2.ProcessorArchitecture != asn1.ProcessorArchitecture;
            }
            catch { return false; }
        }

        public static bool IsInstalledInGac(Assembly asm)
        {
            var ai = new ASSEMBLY_INFO
            {
                cbAssemblyInfo = (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(ASSEMBLY_INFO)),
                dwAssemblyFlags = 0,
                uliAssemblySizeInKB = 0,
                pszCurrentAssemblyPathBuf = new string('\0', 8192),
                cchBuf = 8192,
            };
            var hresult = (uint)Gac.QueryAssemblyInfo((uint)QUERYASMINFO_FLAG.QUERYASMINFO_FLAG_VALIDATE, asm.FullName, ref ai);
            return (ai.dwAssemblyFlags & 1) == 1 && (hresult == 0 || hresult == 0x8007007a); // &ASSEMBLYINFO_FLAG__INSTALLED(==1) != 0; //0x80070002 - no file, 0x8007007a - buffer too small, 0x80131047 is a FUSION_E_INVALID_NAME 
        }
        public static bool IsInstalledInGacDiff(Assembly asm)
        {
            var ai = new ASSEMBLY_INFO
            {
                cbAssemblyInfo = (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(ASSEMBLY_INFO)),
                dwAssemblyFlags = 0,
                uliAssemblySizeInKB = 0,
                pszCurrentAssemblyPathBuf = new string('\0', 8192),
                cchBuf = 8192,
            };
            var hresult = (uint)Gac.QueryAssemblyInfo((uint)QUERYASMINFO_FLAG.QUERYASMINFO_FLAG_VALIDATE, asm.GetName().Name, ref ai);
            if (hresult != 0 && hresult != 0x8007007a) return false; // &ASSEMBLYINFO_FLAG__INSTALLED(==1) != 0; //0x80070002 - no file, 0x8007007a - buffer too small, 0x80131047 is a FUSION_E_INVALID_NAME
            return !IsInstalledInGac(asm);
        }

        public static void InstallInGac(Assembly asm, Assembly referrer)
        {
            //http://support.microsoft.com/default.aspx?scid=kb;en-us;317540
            //http://www.codeproject.com/Articles/4318/Undocumented-Fusion
            //http://blogs.msdn.com/b/junfeng/archive/2004/09/14/229649.aspx
            // & http://www.codeproject.com/Articles/17968/Making-Your-Application-UAC-Aware
            // pretty GAC attempt #2: - usually fail with access denied, need to be run under elevation
            var refpath = referrer.Location;
            var location = asm.Location;
            var refs = new[]{
                new System.GAC.FUSION_INSTALL_REFERENCE{
                    cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(System.GAC.FUSION_INSTALL_REFERENCE)),
                    dwFlags = 0,
                    guidScheme = System.GAC.AssemblyCache.FUSION_REFCOUNT_FILEPATH_GUID,
                    szIdentifier = refpath,
                    szNonCannonicalData = "Auto-registered for QTAgent",
                }
            };

            var hresult = Gac.InstallAssembly((uint)System.GAC.IASSEMBLYCACHE_INSTALL_FLAG.IASSEMBLYCACHE_INSTALL_FLAG_REFRESH, location, refs);
            if (hresult != 0) new Win32Exception(hresult);
        }

        // removes only THIS EXACT version, leaves all other
        public static void UninstallFromGac(Assembly asm, Assembly referrer)
        {
            //http://support.microsoft.com/default.aspx?scid=kb;en-us;317540
            //http://www.codeproject.com/Articles/4318/Undocumented-Fusion
            //http://blogs.msdn.com/b/junfeng/archive/2004/09/14/229649.aspx
            // & http://www.codeproject.com/Articles/17968/Making-Your-Application-UAC-Aware
            // pretty GAC attempt #2: - usually fail with access denied, need to be run under elevation
            var refpath = referrer.Location;
            var assname = asm.FullName;
            var refs = new[]{
                new System.GAC.FUSION_INSTALL_REFERENCE{
                    cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(System.GAC.FUSION_INSTALL_REFERENCE)),
                    dwFlags = 0,
                    guidScheme = System.GAC.AssemblyCache.FUSION_REFCOUNT_FILEPATH_GUID,
                    szIdentifier = refpath,
                    szNonCannonicalData = "Auto-registered for QTAgent",
                }
            };
            uint dispo_ = 0;
            var hresult = (uint)Gac.UninstallAssembly(0, assname, refs, out dispo_);
            if (hresult == 0x80070005) throw new UnauthorizedAccessException();
            if (hresult != 0) new Win32Exception((int)hresult);
            var dispo = (IASSEMBLYCACHE_UNINSTALL_DISPOSITION)dispo_;
            if (dispo != 0
                && dispo != IASSEMBLYCACHE_UNINSTALL_DISPOSITION.IASSEMBLYCACHE_UNINSTALL_DISPOSITION_UNINSTALLED
                && dispo != IASSEMBLYCACHE_UNINSTALL_DISPOSITION.IASSEMBLYCACHE_UNINSTALL_DISPOSITION_ALREADY_UNINSTALLED
                && dispo != IASSEMBLYCACHE_UNINSTALL_DISPOSITION.IASSEMBLYCACHE_UNINSTALL_DISPOSITION_DELETE_PENDING
                && dispo != IASSEMBLYCACHE_UNINSTALL_DISPOSITION.IASSEMBLYCACHE_UNINSTALL_DISPOSITION_REFERENCE_NOT_FOUND)
                throw new InvalidOperationException("Failed to remote the assembly. Operation returned: " + dispo);
        }

        public static void InstallAsVSPrivate(Assembly asm, string installRoot)
        {
            var path = asm.Location;
            var head = Path.Combine(installRoot, VSPrivateSubDir);
            var dest = Path.Combine(head, Path.GetFileName(path));

            if (!Directory.Exists(head)) Directory.CreateDirectory(head);
            File.Copy(path, dest, true);
        }
        public static bool InstallAsVSPrivate2(Assembly asm, string installRoot)
        {
            try { InstallAsVSPrivate(asm, installRoot); return true; }
            catch { return false; }
        }

        // removes any matching filename
        public static void UninstallFromVSPrivate(Assembly asm, string installRoot)
        {
            var path = asm.Location;
            var head = Path.Combine(installRoot, VSPrivateSubDir);
            var dest = Path.Combine(head, Path.GetFileName(path));

            if (!Directory.Exists(head)) return;
            File.Delete(dest); // maybe bin-compare the source and the deleted file -- no! new version will not be able to overwrite!
        }

        public static IDictionary<Assembly, string> CheckInstalledAssemblies(string devenvdir, Assembly[] asms)
        {
            return asms.ToDictionary(asm => asm, asm =>
                AssemblyInstaller.IsInstalledInGac(asm) ? "GAC" :
                AssemblyInstaller.IsInstalledAsVSPrivate(asm, devenvdir) ? "Priv" :
                AssemblyInstaller.InstallAsVSPrivate2(asm, devenvdir) ? "Priv" : "missing");
        }
    }
}
