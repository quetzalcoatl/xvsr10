using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;

namespace xunit.runner.visualstudio.vs2010.installer
{
    public static class ModuleInstallerWrapper
    {
        public static void RunInteractive(bool wait, string localRoot, string devenvdir, string agentExecutable, Assembly refs, params Assembly[] asms)
        {
            // look for the utility under the user's extensions directory and tries to find the newest updater utility
            var toolPath = FindNewestVersion("xunit.runner.visualstudio.vs2010.autoinstaller.exe", localRoot + @"\Extensions");
            if (toolPath == null)
            {
                MessageBox.Show("Could find file installer utility.");
                return;
            }

            var args = new List<string>();
            args.Add(devenvdir);
            args.Add(agentExecutable);
            args.Add(refs.Location);
            foreach (var asm in asms) args.Add(asm.Location);

            // pretty GAC attempt #3:
            Process pr = new Process();
            pr.StartInfo.FileName = toolPath;
            pr.StartInfo.Arguments = ArgvToCommandLine(args);
            pr.StartInfo.Verb = "runas";
            try { pr.Start(); }
            catch (Win32Exception) { return; }

            if (wait) pr.WaitForExit();
        }

        public static string FindNewestVersion(string filename, string toolRoot)
        {
            string best = null;
            AssemblyName bestInfo = null;
            foreach (var f in Directory.EnumerateFiles(toolRoot, filename, SearchOption.AllDirectories))
            {
                var asn2 = AssemblyName.GetAssemblyName(f);
                if (bestInfo == null || bestInfo.Version < asn2.Version)
                {
                    best = f;
                    bestInfo = asn2;
                }
            }
            return best;
        }

        // http://stackoverflow.com/questions/5510343/escape-command-line-arguments-in-c-sharp
        // + http://msdn.microsoft.com/en-us/library/system.diagnostics.processstartinfo.arguments(v=vs.90).aspx
        public static string ArgvToCommandLine(IEnumerable<string> args)
        {
            StringBuilder sb = new StringBuilder();
            foreach (string s in args)
            {
                sb.Append('"');
                // Escape double quotes (") and backslashes (\).
                int searchIndex = 0;
                while (true)
                {
                    // Put this test first to support zero length strings.
                    if (searchIndex >= s.Length)
                    {
                        break;
                    }
                    int quoteIndex = s.IndexOf('"', searchIndex);
                    if (quoteIndex < 0)
                    {
                        break;
                    }
                    sb.Append(s, searchIndex, quoteIndex - searchIndex);
                    EscapeBackslashes(sb, s, quoteIndex - 1);
                    sb.Append('\\');
                    sb.Append('"');
                    searchIndex = quoteIndex + 1;
                }
                sb.Append(s, searchIndex, s.Length - searchIndex);
                EscapeBackslashes(sb, s, s.Length - 1);
                sb.Append(@""" ");
            }
            return sb.ToString(0, Math.Max(0, sb.Length - 1));
        }
        public static void EscapeBackslashes(StringBuilder sb, string s, int lastSearchIndex)
        {
            // Backslashes must be escaped if and only if they precede a double quote.
            for (int i = lastSearchIndex; i >= 0; i--)
            {
                if (s[i] != '\\')
                {
                    break;
                }
                sb.Append('\\');
            }
        }
    }
}
