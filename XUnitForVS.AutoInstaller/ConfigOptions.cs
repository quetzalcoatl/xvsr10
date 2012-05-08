using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Media;

namespace xunit.runner.visualstudio.vs2010.autoinstaller
{
    public enum ConfigInstallState
    {
        Unknown = 0,

        Original,
        Patched,
        PatchedDiff,
        NotFound,
        Error,
    }

    public enum ConfigInstallOption
    {
        Restore,
        Patch,
        NoAction,
    }

    public class ConfigOptions : DependencyObject,
        IInstallOptionsRow<ConfigInstallState, ConfigInstallOption>, IInstallOptionsRow2<ConfigInstallState, ConfigInstallOption>,
        IInstallOptionsRow<object, object>
    {
        public string AssemblyShortName { get { return (string)GetValue(AssemblyShortNameProperty); } set { SetValue(AssemblyShortNameProperty, value); } }
        public static readonly DependencyProperty AssemblyShortNameProperty = DependencyProperty.Register("AssemblyShortName", typeof(string), typeof(ConfigOptions), new UIPropertyMetadata(""));

        public string AssemblyVersion { get { return (string)GetValue(AssemblyVersionProperty); } set { SetValue(AssemblyVersionProperty, value); } }
        public static readonly DependencyProperty AssemblyVersionProperty = DependencyProperty.Register("AssemblyVersion", typeof(string), typeof(ConfigOptions), new UIPropertyMetadata(""));

        public ConfigInstallState CurrentState { get { return (ConfigInstallState)GetValue(CurrentStateProperty); } set { SetValue(CurrentStateProperty, value); } }
        public static readonly DependencyProperty CurrentStateProperty = DependencyProperty.Register("CurrentState", typeof(ConfigInstallState), typeof(ConfigOptions), new UIPropertyMetadata(ConfigInstallState.Unknown, oldStateChanged));

        public ImageSource CurrentStateMark { get { return (ImageSource)GetValue(CurrentStateMarkProperty); } set { SetValue(CurrentStateMarkProperty, value); } }
        public static readonly DependencyProperty CurrentStateMarkProperty = DependencyProperty.Register("CurrentStateMark", typeof(ImageSource), typeof(ConfigOptions), new UIPropertyMetadata(null));

        public IEnumerable<ConfigInstallOption> NextStates { get { return (IEnumerable<ConfigInstallOption>)GetValue(NextStatesProperty); } set { SetValue(NextStatesProperty, value); } }
        public static readonly DependencyProperty NextStatesProperty = DependencyProperty.Register("NextStates", typeof(IEnumerable<ConfigInstallOption>), typeof(ConfigOptions), new UIPropertyMetadata(new ConfigInstallOption[0]));

        public ConfigInstallOption NewState { get { return (ConfigInstallOption)GetValue(NewStateProperty); } set { SetValue(NewStateProperty, value); } }
        public static readonly DependencyProperty NewStateProperty = DependencyProperty.Register("NewState", typeof(ConfigInstallOption), typeof(ConfigOptions), new UIPropertyMetadata(ConfigInstallOption.NoAction, newStateChanged));

        public MainWindow main;
        public string agentExeFile;
        public IDictionary<Assembly, KeyValuePair<bool, bool>> configState;

        private static void oldStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = d as ConfigOptions;
            if (self == null) return;

            var newState = (ConfigInstallState)e.NewValue;
            if (self.NewState == ConfigInstallOption.NoAction)
                self.CurrentStateMark = MainWindow.CurrentStateImg(self.CurrentState, self.main.imgs);
        }

        private static void newStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = d as ConfigOptions;
            if (self == null) return;

            var newState = (ConfigInstallOption)e.NewValue;
            if (newState == ConfigInstallOption.NoAction)
                self.CurrentStateMark = MainWindow.CurrentStateImg(self.CurrentState, self.main.imgs);
            else
                self.CurrentStateMark = self.main.imgs[3];
        }

        #region IInstallOptionsRow<object,object> Members

        object IInstallOptionsRow<object, object>.CurrentState { get { return this.CurrentState; } }
        object IInstallOptionsRow<object, object>.NewState { get { return this.NewState; } }
        IEnumerable<object> IInstallOptionsRow<object, object>.NextStates { get { return this.NextStates.Cast<object>(); } }
        
        #endregion
    }
}
