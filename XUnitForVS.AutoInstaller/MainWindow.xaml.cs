using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Navigation;
using xunit.runner.visualstudio.vs2010.installer;

namespace xunit.runner.visualstudio.vs2010.autoinstaller
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Loaded += MainWindow_Loaded;
        }

        public ImageSource[] imgs;

        private string devenvroot;
        private string[] agentexenames;
        private string[] assPaths;

        private bool vsPathCorrect;
        private AssemblyOptions referrer;
        private IEnumerable<Assembly> asms;
        private IEnumerable<IInstallOptionsRow<object, object>> fileItems;
        private IEnumerable<IInstallOptionsRow<object, object>> configItems;

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            imgs = new ImageSource[5];
            imgs[0] = Resources["biOk"] as ImageSource;
            imgs[1] = Resources["biWr"] as ImageSource;
            imgs[2] = Resources["biEr"] as ImageSource;
            imgs[3] = Resources["biFx"] as ImageSource;
            imgs[4] = Resources["biWt"] as ImageSource;

            var args = Environment.GetCommandLineArgs();

            devenvroot = args[1];
            agentexenames = args[2].Split(';');
            assPaths = args.Skip(3).ToArray();

            vsPathCorrect = File.Exists(Path.Combine(devenvroot, "devenv.exe"))
                && agentexenames.All(agexfile =>
                    File.Exists(Path.Combine(devenvroot, agexfile)) && File.Exists(Path.Combine(devenvroot, agexfile + ".config")));

            var allasms = assPaths.Select(asmpath =>
            {
                AssemblyOptions item = new AssemblyOptions();
                item.main = this;

                if (!File.Exists(asmpath))
                    item.CurrentState = AssemblyInstallState.NotFound;
                else
                    try { item.assembly = Assembly.ReflectionOnlyLoadFrom(asmpath); }
                    catch
                    {
                        item.CurrentState = AssemblyInstallState.Error;
                        item.NewState = AssemblyInstallOption.PrivateAssemblies;
                    }

                if (item.assembly != null)
                {
                    item.AssemblyShortName = item.assembly.GetName().Name;
                    item.AssemblyVersion = item.assembly.GetName().Version.ToString();
                    item.CurrentState = currentStateOf(item.assembly, devenvroot);
                    item.NewState = newStateIf(item.CurrentState);
                    item.NextStates = nextStatesIf(item.CurrentState);
                }

                return item;
            }).ToArray();

            referrer = allasms.First();
            fileItems = allasms.Skip(1).Cast<IInstallOptionsRow<object, object>>();

            asms = fileItems.Cast<AssemblyOptions>().Select(it => it.assembly).ToArray();

            configItems = agentexenames.Select(agexefile =>
            {
                ConfigOptions item = new ConfigOptions();
                item.main = this;
                item.agentExeFile = agexefile;
                item.configState = ExeConfigPatcher.CheckQTConfigState(devenvroot, agexefile, AssemblyInstaller.VSPrivateSubDir, asms);

                item.AssemblyShortName = agexefile + ".config";
                item.CurrentState = currentStateOf(item, fileItems.Cast<AssemblyOptions>());
                item.NewState = newStateIf(item.CurrentState);
                item.NextStates = nextStatesIf(item.CurrentState);

                return item;
            }).ToArray();

            imgVS.Source = imgs[vsPathCorrect ? 0 : 2];

            hpVSClicky.NavigateUri = new Uri(devenvroot, UriKind.Absolute);
            txVSClicky.Text = devenvroot;

            lbItems.ItemsSource = fileItems.Concat(configItems).ToArray();

            btnUpdate.IsEnabled = vsPathCorrect;
        }

        private static AssemblyInstallState currentStateOf(Assembly ass, string vsroot)
        {
            if (AssemblyInstaller.IsInstalledInGac(ass)) return AssemblyInstallState.GAC;
            if (AssemblyInstaller.IsInstalledInGacDiff(ass)) return AssemblyInstallState.GACDiff;
            if (AssemblyInstaller.IsInstalledAsVSPrivate(ass, vsroot)) return AssemblyInstallState.PrivateAssemblies;
            if (AssemblyInstaller.IsInstalledAsVSPrivateDiff(ass, vsroot)) return AssemblyInstallState.PrivateAssembliesDiff;
            return AssemblyInstallState.Uninstalled;
        }
        private static ConfigInstallState currentStateOf(ConfigOptions item, IEnumerable<AssemblyOptions> asmopts)
        {
            var cfgtrash = asmopts.Any(opt => item.configState[opt.assembly].Key);
            var cfgany = asmopts.Any(opt => item.configState[opt.assembly].Value);
            if (!cfgtrash && !cfgany) return ConfigInstallState.Original;

            var cfgmismatch = asmopts.Any(opt =>
            {
                var state = item.configState[opt.assembly];
                bool shouldbe = shouldConfigEntryBePresentFor(opt);
                return state.Key || state.Value != shouldbe;
            });

            if (cfgmismatch) return ConfigInstallState.PatchedDiff;

            return ConfigInstallState.Patched;
        }

        public static ImageSource CurrentStateImg(AssemblyInstallState state, ImageSource[] imgs)
        {
            switch (state)
            {
                case AssemblyInstallState.Uninstalled: return imgs[1];
                case AssemblyInstallState.GAC: return imgs[0];
                case AssemblyInstallState.PrivateAssemblies: return imgs[0];
                case AssemblyInstallState.NotFound: return imgs[2];
                case AssemblyInstallState.Error: return imgs[2];
                default: return imgs[1];
            }
        }
        public static ImageSource CurrentStateImg(ConfigInstallState state, ImageSource[] imgs)
        {
            switch (state)
            {
                case ConfigInstallState.Original: return imgs[1];
                case ConfigInstallState.Patched: return imgs[0];
                case ConfigInstallState.NotFound: return imgs[2];
                case ConfigInstallState.Error: return imgs[2];
                default: return imgs[1];
            }
        }

        private static AssemblyInstallOption newStateIf(AssemblyInstallState state)
        {
            switch (state)
            {
                case AssemblyInstallState.Uninstalled: return AssemblyInstallOption.PrivateAssemblies;
                case AssemblyInstallState.GAC: return AssemblyInstallOption.NoAction;
                case AssemblyInstallState.GACDiff: return AssemblyInstallOption.PrivateAssemblies;
                case AssemblyInstallState.PrivateAssemblies: return AssemblyInstallOption.NoAction;
                case AssemblyInstallState.PrivateAssembliesDiff: return AssemblyInstallOption.PrivateAssemblies;
                case AssemblyInstallState.NotFound: return AssemblyInstallOption.NoAction;
                case AssemblyInstallState.Error: return AssemblyInstallOption.NoAction;
                default: return AssemblyInstallOption.NoAction;
            }
        }
        private static ConfigInstallOption newStateIf(ConfigInstallState state)
        {
            switch (state)
            {
                case ConfigInstallState.Original: return ConfigInstallOption.Patch;
                case ConfigInstallState.Patched: return ConfigInstallOption.NoAction;
                case ConfigInstallState.PatchedDiff: return ConfigInstallOption.Patch;
                case ConfigInstallState.NotFound: return ConfigInstallOption.NoAction;
                case ConfigInstallState.Error: return ConfigInstallOption.NoAction;
                default: return ConfigInstallOption.NoAction;
            }
        }

        private static readonly AssemblyInstallOption[] allAsmStates = new[] { AssemblyInstallOption.Uninstalled, /*InstallOption.GAC,*/ AssemblyInstallOption.PrivateAssemblies, AssemblyInstallOption.NoAction };
        private static readonly AssemblyInstallOption[] noAsmStates = new[] { AssemblyInstallOption.NoAction };
        private static IEnumerable<AssemblyInstallOption> nextStatesIf(AssemblyInstallState state)
        {
            switch (state)
            {
                case AssemblyInstallState.Uninstalled: return allAsmStates.Where(s => s != AssemblyInstallOption.Uninstalled);
                //case InstallState.GAC: return allStates.Where(s => s != InstallOption.GAC);
                case AssemblyInstallState.GACDiff: return allAsmStates;
                case AssemblyInstallState.PrivateAssemblies: return allAsmStates.Where(s => s != AssemblyInstallOption.PrivateAssemblies);
                case AssemblyInstallState.PrivateAssembliesDiff: return allAsmStates;
                case AssemblyInstallState.NotFound: return noAsmStates;
                case AssemblyInstallState.Error: return noAsmStates;
                default: return noAsmStates;
            }
        }

        private static readonly ConfigInstallOption[] allCfgStates = new[] { ConfigInstallOption.Restore, /*InstallOption.PrivateAssemblies,*/ ConfigInstallOption.Patch, ConfigInstallOption.NoAction };
        private static readonly ConfigInstallOption[] noCfgStates = new[] { ConfigInstallOption.NoAction };
        private static IEnumerable<ConfigInstallOption> nextStatesIf(ConfigInstallState state)
        {
            switch (state)
            {
                case ConfigInstallState.Original: return allCfgStates.Where(s => s != ConfigInstallOption.Restore);
                case ConfigInstallState.Patched: return allCfgStates.Where(s => s != ConfigInstallOption.Patch);
                case ConfigInstallState.PatchedDiff: return allCfgStates;
                case ConfigInstallState.NotFound: return noCfgStates;
                case ConfigInstallState.Error: return noCfgStates;
                default: return noCfgStates;
            }
        }

        public void UpdateConfigActionsAfterAssemblyActionChange()
        {
            if (configItems != null)
                foreach (var item in configItems.Cast<ConfigOptions>())
                {
                    item.configState = ExeConfigPatcher.CheckQTConfigState(devenvroot, item.agentExeFile, AssemblyInstaller.VSPrivateSubDir, asms);

                    item.CurrentState = currentStateOf(item, fileItems.Cast<AssemblyOptions>());
                    item.NewState = newStateIf(item.CurrentState);
                    item.NextStates = nextStatesIf(item.CurrentState);
                }
        }

        private static bool shouldConfigEntryBePresentFor(AssemblyOptions opt)
        {
            return opt.NewState == AssemblyInstallOption.PrivateAssemblies || opt.NewState == AssemblyInstallOption.NoAction && opt.CurrentState == AssemblyInstallState.PrivateAssemblies;
        }

        private void hpClicky_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private void btnMarkAllUninstall_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in fileItems.Cast<AssemblyOptions>())
                if (item.CurrentState == AssemblyInstallState.GAC
                    || item.CurrentState == AssemblyInstallState.GACDiff
                    || item.CurrentState == AssemblyInstallState.PrivateAssemblies
                    || item.CurrentState == AssemblyInstallState.PrivateAssembliesDiff)
                    item.NewState = AssemblyInstallOption.Uninstalled;
                else
                    item.NewState = AssemblyInstallOption.NoAction;

            foreach (var item in configItems.Cast<ConfigOptions>())
                if (item.CurrentState == ConfigInstallState.Patched
                    || item.CurrentState == ConfigInstallState.PatchedDiff)
                    item.NewState = ConfigInstallOption.Restore;
        }

        private void btnMarkAllDefault_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in fileItems.Cast<AssemblyOptions>())
                if (item.CurrentState != AssemblyInstallState.PrivateAssemblies)
                    item.NewState = AssemblyInstallOption.PrivateAssemblies;
                else
                    item.NewState = AssemblyInstallOption.NoAction;

            foreach (var item in configItems.Cast<ConfigOptions>())
                if (item.CurrentState != ConfigInstallState.Patched)
                    item.NewState = ConfigInstallOption.Patch;
        }

        private void btnUpdate_Click(object sender, RoutedEventArgs e)
        {
            bool anything = false;

            foreach (var item in fileItems.Concat(configItems).Where(it =>
                !object.Equals(it.NewState, AssemblyInstallOption.NoAction)
                && !object.Equals(it.NewState, ConfigInstallOption.NoAction)))
            {
                item.CurrentStateMark = imgs[4];
                anything = true;
            }

            if (!anything)
            {
                MessageBox.Show("No changes were selected", "Updater", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            btnUpdate.IsEnabled = false;
            btnClose.IsEnabled = false;

            ThreadPool.QueueUserWorkItem(_ =>
            {
                //// must precalc them because the loop below changes the items' states
                //IDictionary<Assembly, KeyValuePair<bool, bool?>> cfgChanges = null;
                //Dispatcher.Invoke(new Action(() =>
                //    cfgChanges = items.ToDictionary(it => it.assembly, it => new KeyValuePair<bool, bool?>(
                //        true, it.NewState == InstallOption.NoAction ? (bool?)null : it.NewState == InstallOption.PrivateAssemblies))));

                foreach (var item in fileItems.Cast<AssemblyOptions>())
                {
                    AssemblyInstallState curr = AssemblyInstallState.Unknown; AssemblyInstallOption next = AssemblyInstallOption.NoAction; Assembly ass = null;
                    Dispatcher.Invoke(new Action(() =>
                    {
                        curr = item.CurrentState;
                        next = item.NewState;
                        ass = item.assembly;
                    }));
                    if (next == AssemblyInstallOption.NoAction)
                        continue;

                    try
                    {
                        performUpdate(curr, next, ass);
                        Dispatcher.Invoke(new Action(() =>
                        {
                            item.CurrentStateMark = imgs[0];
                            item.CurrentState = currentStateOf(item.assembly, devenvroot);
                            item.NextStates = allAsmStates;
                            item.NewState = AssemblyInstallOption.NoAction;
                        }));
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.Invoke(new Action(() =>
                        {
                            item.CurrentState = AssemblyInstallState.Error;
                            item.CurrentStateMark = imgs[2];
                            item.CurrentState = currentStateOf(item.assembly, devenvroot);
                            item.NextStates = allAsmStates;
                            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }));
                    }
                }

                Dispatcher.Invoke(new Action(() =>
                {
                    var assstates = fileItems.Cast<AssemblyOptions>()
                        .ToDictionary(it => it.assembly, it => new KeyValuePair<bool, bool?>(
                            true, it.CurrentState == AssemblyInstallState.PrivateAssemblies));

                    foreach (var item in configItems.Cast<ConfigOptions>())
                        try
                        {
                            ExeConfigPatcher.PerformQTConfigPatches(devenvroot, item.agentExeFile, AssemblyInstaller.VSPrivateSubDir, assstates);

                            item.configState = ExeConfigPatcher.CheckQTConfigState(devenvroot, item.agentExeFile, AssemblyInstaller.VSPrivateSubDir, asms);

                            item.CurrentStateMark = imgs[0];
                            item.CurrentState = currentStateOf(item, fileItems.Cast<AssemblyOptions>());
                            item.NextStates = allCfgStates;
                            item.NewState = ConfigInstallOption.NoAction;
                        }
                        catch (Exception ex)
                        {
                            item.CurrentState = ConfigInstallState.Error;
                            item.CurrentStateMark = imgs[2];
                            item.CurrentState = currentStateOf(item, fileItems.Cast<AssemblyOptions>());
                            item.NextStates = allCfgStates;
                            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    btnUpdate.IsEnabled = true;
                    btnClose.IsEnabled = true;
                }));
            });
        }

        private void performUpdate(AssemblyInstallState curr, AssemblyInstallOption next, Assembly ass)
        {
            AssemblyInstaller.UninstallFromVSPrivate(ass, devenvroot);
            AssemblyInstaller.UninstallFromGac(ass, referrer.assembly);

            if (next == AssemblyInstallOption.PrivateAssemblies) AssemblyInstaller.InstallAsVSPrivate(ass, devenvroot);
            //if (next == InstallOption.GAC) QTARegistration.InstallInGac(ass, referrer.assembly);
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (btnClose.IsEnabled == false) { e.Cancel = true; return; }

            e.Cancel = !ensureCloseUnstable();
        }

        private bool ensureCloseUnstable()
        {
            var anythingLeft = fileItems == null // null trick: if closed too fast, before init finishes, simply delay the user a bit
                || configItems.Cast<ConfigOptions>().Any(it =>
                    it.CurrentState != ConfigInstallState.Patched) // patcheddiff means that cfg does not match the file setup - danger condition
                || fileItems.Cast<AssemblyOptions>().Any(it =>
                    it.CurrentState == AssemblyInstallState.Uninstalled // *diff means that in that location a version is found but it is not THE version - warn condition
                    || it.CurrentState == AssemblyInstallState.NotFound
                    || it.CurrentState == AssemblyInstallState.Error);

            if (!anythingLeft)
                return true;

            return MessageBoxResult.Yes == MessageBox.Show(
                "Some of the xUnit Runner's modules seem to be still not installed." + Environment.NewLine + Environment.NewLine +
                "Do you want to leave xUnit Runner probably partially unusable?", "Warning", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning, MessageBoxResult.No);
        }
    }

    public interface IInstallOptionsRow<out T, out U>
    {
        string AssemblyShortName { get; set; }
        string AssemblyVersion { get; set; }
        T CurrentState { get; }
        ImageSource CurrentStateMark { get; set; }
        U NewState { get; }
        IEnumerable<U> NextStates { get; }
    }

    public interface IInstallOptionsRow2<in T, in U>
    {
        string AssemblyShortName { get; set; }
        string AssemblyVersion { get; set; }
        T CurrentState { set; }
        ImageSource CurrentStateMark { get; set; }
        U NewState { set; }
        IEnumerable<U> NextStates { set; }
    }
}
