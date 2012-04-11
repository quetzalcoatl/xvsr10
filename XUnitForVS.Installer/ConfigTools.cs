using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;

namespace xunit.runner.visualstudio.vs2010.installer
{
    public static class ExeConfigPatcher
    {
        public static IDictionary<Assembly, KeyValuePair<bool, bool>> CheckQTConfigState(string devenvroot, string agentExecutable, string subpath, Assembly[] asms)
        {
            var agentConfigFile = Path.Combine(devenvroot, agentExecutable + ".config");
            string ns = "urn:schemas-microsoft-com:asm.v1";

            var x = new XmlDocument();
            x.Load(agentConfigFile);
            var nsm = new XmlNamespaceManager(x.NameTable);
            var prf = nsm.LookupPrefix(ns);
            if (string.IsNullOrEmpty(prf)) nsm.AddNamespace(prf = "asm1", ns);

            var result = new Dictionary<Assembly, KeyValuePair<bool, bool>>();
            foreach (var asm in asms)
            {
                var filepath = Path.Combine(subpath, Path.GetFileName(asm.Location));

                bool needCleanup = junkRemover(x, asm.GetName(), ns, prf, nsm);
                bool entryExists = exactFinder(x, filepath, asm.GetName(), ns, prf, nsm);

                result.Add(asm, new KeyValuePair<bool, bool>(needCleanup, entryExists));
            }
            return result;
        }
        
        public static void PerformQTConfigPatches(string devenvroot, string agentExecutable, string subpath, IDictionary<Assembly, KeyValuePair<bool, bool?>> actions)
        {
            var agentConfigFile = Path.Combine(devenvroot, agentExecutable + ".config");
            string ns = "urn:schemas-microsoft-com:asm.v1";

            var x = new XmlDocument();
            x.Load(agentConfigFile);
            var nsm = new XmlNamespaceManager(x.NameTable);
            var prf = nsm.LookupPrefix(ns);
            if (string.IsNullOrEmpty(prf)) nsm.AddNamespace(prf = "asm1", ns);

            bool changed = false;
            foreach (var pair in actions)
            {
                var asm = pair.Key;
                var cleanup = pair.Value.Key;
                var newstate = pair.Value.Value;
                var filepath = Path.Combine(subpath, Path.GetFileName(asm.Location));

                if (cleanup) changed |= junkRemover(x, asm.GetName(), ns, prf, nsm);

                if (newstate == true) changed |= exactWriter(x, filepath, asm.GetName(), ns, prf, nsm);
                else if (newstate == false) changed |= exactRemover(x, filepath, asm.GetName(), ns, prf, nsm);
            }

            if (changed)
                x.Save(agentConfigFile);
        }
        
        private static string tokenOf(string assemblyName)
        {
            return assemblyName.Substring(assemblyName.IndexOf("Token=") + 6);
        }
        
        private static string cultureOf(string assemblyName)
        {
            var a = assemblyName.IndexOf("Culture=") + 8;
            var b = assemblyName.IndexOf(',', a);
            return assemblyName.Substring(a, b - a);
        }

        private static bool exactFinder(XmlDocument doc, string filepath, AssemblyName asn, string ns, string prf, XmlNamespaceManager nsm)
        {
            var root = doc["configuration"];
            var runt = root == null ? null : root["runtime"];
            var assb = runt == null ? null : runt["assemblyBinding", ns];
            if (assb == null) return false;
            var token = tokenOf(asn.FullName);
            var cuname = cultureOf(asn.FullName);
            var arch = asn.ProcessorArchitecture.ToString().ToLower();
            // important: attributes don't have prefixes - no namespace
            var deass = assb.ChildNodes.Cast<XmlElement>().Where(node => node.Name == "dependentAssembly" && node.NamespaceURI == ns).Select(node => new { dea = node, asi = (XmlElement)node.SelectSingleNode(prf + ":assemblyIdentity", nsm), cbs = node.SelectNodes(prf + ":codeBase", nsm).Cast<XmlElement>().ToArray() }).ToArray();
            var partials = deass.Where(d =>
                d.asi.GetAttribute("name") == asn.Name
                && (!d.asi.GetAttributeNode("culture").Specified || d.asi.GetAttribute("culture") == cuname)
                && (!d.asi.GetAttributeNode("publicKeyToken").Specified || d.asi.GetAttribute("publicKeyToken") == token)
                && (!d.asi.GetAttributeNode("processorArchitecture").Specified || d.asi.GetAttribute("processorArchitecture") == arch))
                .ToArray();
            return partials.Any(d => d.cbs.Any(c => c.GetAttribute("version") == asn.Version.ToString() && c.GetAttribute("href") == filepath));
        }
        
        private static bool junkRemover(XmlDocument doc, AssemblyName asn, string ns, string prf, XmlNamespaceManager nsm)
        {
            var root = doc["configuration"];
            var runt = root == null ? null : root["runtime"];
            var assb = runt == null ? null : runt["assemblyBinding", ns];
            if (assb == null) return false;
            var token = tokenOf(asn.FullName);
            var cuname = cultureOf(asn.FullName);
            var arch = asn.ProcessorArchitecture.ToString().ToLower();
            // important: attributes don't have prefixes - no namespace
            var deass = assb.ChildNodes.Cast<XmlElement>().Where(node => node.Name == "dependentAssembly" && node.NamespaceURI == ns).Select(node => new { dea = node, asi = (XmlElement)node.SelectSingleNode(prf + ":assemblyIdentity", nsm), cbs = node.SelectNodes(prf + ":codeBase", nsm).Cast<XmlElement>().ToArray() }).ToArray();
            var toremove1 = deass.Where(d => d.asi.GetAttribute("name") == asn.Name && (d.asi.GetAttribute("culture") != cuname || d.asi.GetAttribute("publicKeyToken") != token || d.asi.GetAttribute("processorArchitecture") != arch)).ToArray();
            var toremove2 = deass.Except(toremove1).Where(d => d.asi.GetAttribute("name") == asn.Name).Select(d => new { d.dea, cbs = d.cbs.Where(c => c.GetAttribute("version") != asn.Version.ToString()).ToArray() }).ToArray();
            bool anyr = false;
            foreach (var d in toremove1) { anyr = true; assb.RemoveChild(d.dea); }
            foreach (var d in toremove2) foreach (var c in d.cbs) { anyr = true; d.dea.RemoveChild(c); }
            return anyr;
        }
        
        private static bool exactWriter(XmlDocument doc, string filepath, AssemblyName asn, string ns, string prf, XmlNamespaceManager nsm)
        {
            bool chg = false;
            var root = doc["configuration"]; if (root == null) { chg = true; root = (XmlElement)doc.AppendChild(doc.CreateElement("configuration")); }
            var runt = root["runtime"]; if (runt == null) { chg = true; runt = (XmlElement)root.AppendChild(doc.CreateElement("runtime")); }
            var assb = runt["assemblyBinding", ns]; if (assb == null) { chg = true; assb = (XmlElement)runt.AppendChild(doc.CreateElement("assemblyBinding", ns)); }
            var ver = asn.Version.ToString();
            var token = tokenOf(asn.FullName);
            var cuname = cultureOf(asn.FullName);
            var arch = asn.ProcessorArchitecture.ToString().ToLower();
            var deas = (XmlElement)assb.SelectSingleNode(prf + ":dependentAssembly[" + prf + ":assemblyIdentity/@name='" + asn.Name + "'][" + prf + ":assemblyIdentity/@culture='" + cuname + "'][" + prf + ":assemblyIdentity/@publicKeyToken='" + token + "'][" + prf + ":assemblyIdentity/@processorArchitecture='" + arch + "']", nsm);
            if (deas == null)
            {
                chg = true;
                deas = (XmlElement)assb.AppendChild(doc.CreateElement("dependentAssembly", ns));
                var asid = (XmlElement)deas.AppendChild(doc.CreateElement("assemblyIdentity", ns));
                asid.SetAttribute("name", asn.Name);
                asid.SetAttribute("publicKeyToken", token);
                asid.SetAttribute("culture", cuname);
                asid.SetAttribute("processorArchitecture", arch);
            }
            var coba = (XmlElement)deas.SelectSingleNode(prf + ":codeBase[@version='" + ver + "']", nsm);
            if (coba == null)
            {
                chg = true;
                coba = (XmlElement)deas.AppendChild(doc.CreateElement("codeBase", ns));
                coba.SetAttribute("version", ver);
            }
            if (coba.GetAttribute("href") != filepath)
            {
                chg = true;
                coba.SetAttribute("href", filepath);
            }
            return chg;
        }

        private static bool exactRemover(XmlDocument doc, string filepath, AssemblyName asn, string ns, string prf, XmlNamespaceManager nsm)
        {
            var root = doc["configuration"];
            var runt = root == null ? null : root["runtime"];
            var assb = runt == null ? null : runt["assemblyBinding", ns];
            if (assb == null) return false;
            var token = tokenOf(asn.FullName);
            var cuname = cultureOf(asn.FullName);
            var arch = asn.ProcessorArchitecture.ToString().ToLower();
            // important: attributes don't have prefixes - no namespace
            var deass = assb.ChildNodes.Cast<XmlElement>().Where(node => node.Name == "dependentAssembly" && node.NamespaceURI == ns).Select(node => new { dea = node, asi = (XmlElement)node.SelectSingleNode(prf + ":assemblyIdentity", nsm), cbs = node.SelectNodes(prf + ":codeBase", nsm).Cast<XmlElement>().ToArray() }).ToArray();
            var partials = deass.Where(d =>
                d.asi.GetAttribute("name") == asn.Name
                && (!d.asi.GetAttributeNode("culture").Specified || d.asi.GetAttribute("culture") == cuname)
                && (!d.asi.GetAttributeNode("publicKeyToken").Specified || d.asi.GetAttribute("publicKeyToken") == token)
                && (!d.asi.GetAttributeNode("processorArchitecture").Specified || d.asi.GetAttribute("processorArchitecture") == arch))
                .ToArray();
            bool anything = false;
            foreach (var part in partials)
            {
                var toremove = part.cbs.Where(c => c.GetAttribute("version") == asn.Version.ToString() && c.GetAttribute("href") == filepath).ToArray();
                foreach (var c in toremove) { anything = true; part.dea.RemoveChild(c); }
                if (part.dea.ChildNodes.Count == 1) { anything = true; part.dea.ParentNode.RemoveChild(part.dea); } // deass has assemblyidentity only?
            }
            return anything;
        }
    }
}
