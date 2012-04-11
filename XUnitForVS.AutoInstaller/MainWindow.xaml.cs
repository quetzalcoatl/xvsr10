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
        private string agentexename;
        private string[] assPaths;

        private bool vsPathCorrect;
        private AssemblyOptions referrer;
        private IEnumerable<AssemblyOptions> items;
        private IDictionary<Assembly, KeyValuePair<bool, bool>> configState;

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
            agentexename = args[2];
            assPaths = args.Skip(3).ToArray();

            vsPathCorrect = File.Exists(Path.Combine(devenvroot, "devenv.exe"))
                && File.Exists(Path.Combine(devenvroot, agentexename))
                && File.Exists(Path.Combine(devenvroot, agentexename + ".config"));

            var allasms = assPaths.Select(asmpath =>
            {
                AssemblyOptions item = new AssemblyOptions();
                item.main = this;

                if (!File.Exists(asmpath))
                    item.CurrentState = InstallState.NotFound;
                else
                    try { item.assembly = Assembly.ReflectionOnlyLoadFrom(asmpath); }
                    catch
                    {
                        item.CurrentState = InstallState.Error;
                        item.NewState = InstallOption.PrivateAssemblies;
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
            items = allasms.Skip(1);

            configState = ExeConfigPatcher.CheckQTConfigState(devenvroot, agentexename, AssemblyInstaller.VSPrivateSubDir, items.Select(it => it.assembly).ToArray());

            imgVS.Source = imgs[vsPathCorrect ? 0 : 2];
            txAgent.Text = agentexename + ".config";
            UpdateConfigMark(3, 0);
            hpVSClicky.NavigateUri = new Uri(devenvroot, UriKind.Absolute);
            txVSClicky.Text = devenvroot;

            lbItems.ItemsSource = items;

            btnUpdate.IsEnabled = vsPathCorrect;
        }

        private static InstallState currentStateOf(Assembly ass, string vsroot)
        {
            if (AssemblyInstaller.IsInstalledInGac(ass)) return InstallState.GAC;
            if (AssemblyInstaller.IsInstalledInGacDiff(ass)) return InstallState.GACDiff;
            if (AssemblyInstaller.IsInstalledAsVSPrivate(ass, vsroot)) return InstallState.PrivateAssemblies;
            if (AssemblyInstaller.IsInstalledAsVSPrivateDiff(ass, vsroot)) return InstallState.PrivateAssembliesDiff;
            return InstallState.Uninstalled;
        }

        public static ImageSource CurrentStateImg(InstallState state, ImageSource[] imgs)
        {
            switch (state)
            {
                case InstallState.Uninstalled: return imgs[1];
                case InstallState.GAC: return imgs[0];
                case InstallState.PrivateAssemblies: return imgs[0];
                case InstallState.NotFound: return imgs[2];
                case InstallState.Error: return imgs[2];
                default: return imgs[1];
            }
        }

        private static InstallOption newStateIf(InstallState state)
        {
            switch (state)
            {
                case InstallState.Uninstalled: return InstallOption.PrivateAssemblies;
                case InstallState.GAC: return InstallOption.NoAction;
                case InstallState.GACDiff: return InstallOption.PrivateAssemblies;
                case InstallState.PrivateAssemblies: return InstallOption.NoAction;
                case InstallState.PrivateAssembliesDiff: return InstallOption.PrivateAssemblies;
                case InstallState.NotFound: return InstallOption.NoAction;
                case InstallState.Error: return InstallOption.NoAction;
                default: return InstallOption.NoAction;
            }
        }

        private static readonly InstallOption[] allStates = new[] { InstallOption.Uninstalled, /*InstallOption.GAC,*/ InstallOption.PrivateAssemblies, InstallOption.NoAction };
        private static readonly InstallOption[] noStates = new[] { InstallOption.NoAction };
        private static IEnumerable<InstallOption> nextStatesIf(InstallState state)
        {
            switch (state)
            {
                case InstallState.Uninstalled: return allStates.Where(s => s != InstallOption.Uninstalled);
                //case InstallState.GAC: return allStates.Where(s => s != InstallOption.GAC);
                case InstallState.GACDiff: return allStates;
                case InstallState.PrivateAssemblies: return allStates.Where(s => s != InstallOption.PrivateAssemblies);
                case InstallState.PrivateAssembliesDiff: return allStates;
                case InstallState.NotFound: return noStates;
                case InstallState.Error: return noStates;
                default: return noStates;
            }
        }

        public void UpdateConfigMark(int needs, int not)
        {
            if (items != null)
                imgAgent.Source = imgs[checkAgentCfgNeedsFixing() ? needs : not];
        }

        private bool checkAgentCfgNeedsFixing()
        {
            foreach (var ass in items)
            {
                var state = configState[ass.assembly];
                bool shouldbe = ass.NewState == InstallOption.PrivateAssemblies || ass.NewState == InstallOption.NoAction && ass.CurrentState == InstallState.PrivateAssemblies;
                if (state.Key || state.Value != shouldbe) return true;
            }
            return false;
        }

        private void hpClicky_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private void btnMarkAllUninstall_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in items)
                if (item.CurrentState == InstallState.GAC
                    || item.CurrentState == InstallState.GACDiff
                    || item.CurrentState == InstallState.PrivateAssemblies
                    || item.CurrentState == InstallState.PrivateAssembliesDiff)
                    item.NewState = InstallOption.Uninstalled;
                else
                    item.NewState = InstallOption.NoAction;

            UpdateConfigMark(3, 0);
        }

        private void btnMarkAllDefault_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in items)
                if (item.CurrentState != InstallState.PrivateAssemblies)
                    item.NewState = InstallOption.PrivateAssemblies;
                else
                    item.NewState = InstallOption.NoAction;

            UpdateConfigMark(3, 0);
        }

        private void btnUpdate_Click(object sender, RoutedEventArgs e)
        {
            bool anything = checkAgentCfgNeedsFixing();

            foreach (var item in items.Where(it => it.NewState != InstallOption.NoAction))
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

                foreach (var item in items)
                {
                    InstallState curr = InstallState.Unknown; InstallOption next = InstallOption.NoAction; Assembly ass = null;
                    Dispatcher.Invoke(new Action(() =>
                    {
                        curr = item.CurrentState;
                        next = item.NewState;
                        ass = item.assembly;
                    }));
                    if (next == InstallOption.NoAction)
                        continue;

                    try
                    {
                        performUpdate(curr, next, ass);
                        Dispatcher.Invoke(new Action(() =>
                        {
                            item.CurrentStateMark = imgs[0];
                            item.CurrentState = currentStateOf(item.assembly, devenvroot);
                            item.NextStates = allStates;
                            item.NewState = InstallOption.NoAction;
                        }));
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.Invoke(new Action(() =>
                        {
                            item.CurrentState = InstallState.Error;
                            item.CurrentStateMark = imgs[2];
                            item.CurrentState = currentStateOf(item.assembly, devenvroot);
                            item.NextStates = allStates;
                            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }));
                    }
                }

                Dispatcher.Invoke(new Action(() =>
                {
                    try
                    {
                        ExeConfigPatcher.PerformQTConfigPatches(devenvroot, agentexename, AssemblyInstaller.VSPrivateSubDir,
                            items.ToDictionary(it => it.assembly, it => new KeyValuePair<bool, bool?>(
                                true,
                                it.CurrentState == InstallState.PrivateAssemblies
                        )));

                        configState = ExeConfigPatcher.CheckQTConfigState(devenvroot, agentexename, AssemblyInstaller.VSPrivateSubDir, items.Select(it => it.assembly).ToArray());

                        UpdateConfigMark(1, 0);
                    }
                    catch (Exception ex)
                    {
                        imgAgent.Source = imgs[2];
                        MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    btnUpdate.IsEnabled = true;
                    btnClose.IsEnabled = true;
                }));
            });
        }

        private void performUpdate(InstallState curr, InstallOption next, Assembly ass)
        {
            AssemblyInstaller.UninstallFromVSPrivate(ass, devenvroot);
            AssemblyInstaller.UninstallFromGac(ass, referrer.assembly);

            if (next == InstallOption.PrivateAssemblies) AssemblyInstaller.InstallAsVSPrivate(ass, devenvroot);
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
            var anythingLeft = items == null // null trick: if closed too fast, before init finishes, simply delay the user a bit
                || checkAgentCfgNeedsFixing()
                || items.Any(it =>
                    it.CurrentState == InstallState.Uninstalled
                    || it.CurrentState == InstallState.NotFound
                    || it.CurrentState == InstallState.Error);

            if (!anythingLeft)
                return true;

            return MessageBoxResult.Yes == MessageBox.Show(
                "Some of the xUnit Runner's modules seem to be still not installed." + Environment.NewLine + Environment.NewLine +
                "Do you want to leave xUnit Runner probably partially unusable?", "Warning", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning, MessageBoxResult.No);
        }
    }
}
