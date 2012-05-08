using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Media;

namespace xunit.runner.visualstudio.vs2010.autoinstaller
{
    public enum InstallState
    {
        Unknown = 0,

        Uninstalled,
        GAC,
        GACDiff,
        PrivateAssemblies,
        PrivateAssembliesDiff,
        NotFound,
        Error,
    }

    public enum InstallOption
    {
        Uninstalled,
        // GAC, - disabled. seems to cause more problems than gains
        PrivateAssemblies,
        NoAction,
    }

    public class AssemblyOptions : DependencyObject
    {
        public string AssemblyShortName { get { return (string)GetValue(AssemblyShortNameProperty); } set { SetValue(AssemblyShortNameProperty, value); } }
        public static readonly DependencyProperty AssemblyShortNameProperty = DependencyProperty.Register("AssemblyShortName", typeof(string), typeof(AssemblyOptions), new UIPropertyMetadata(""));

        public string AssemblyVersion { get { return (string)GetValue(AssemblyVersionProperty); } set { SetValue(AssemblyVersionProperty, value); } }
        public static readonly DependencyProperty AssemblyVersionProperty = DependencyProperty.Register("AssemblyVersion", typeof(string), typeof(AssemblyOptions), new UIPropertyMetadata(""));

        public InstallState CurrentState { get { return (InstallState)GetValue(CurrentStateProperty); } set { SetValue(CurrentStateProperty, value); } }
        public static readonly DependencyProperty CurrentStateProperty = DependencyProperty.Register("CurrentState", typeof(InstallState), typeof(AssemblyOptions), new UIPropertyMetadata(InstallState.Unknown, oldStateChanged));

        public ImageSource CurrentStateMark { get { return (ImageSource)GetValue(CurrentStateMarkProperty); } set { SetValue(CurrentStateMarkProperty, value); } }
        public static readonly DependencyProperty CurrentStateMarkProperty = DependencyProperty.Register("CurrentStateMark", typeof(ImageSource), typeof(AssemblyOptions), new UIPropertyMetadata(null));

        public IEnumerable<InstallOption> NextStates { get { return (IEnumerable<InstallOption>)GetValue(NextStatesProperty); } set { SetValue(NextStatesProperty, value); } }
        public static readonly DependencyProperty NextStatesProperty = DependencyProperty.Register("NextStates", typeof(IEnumerable<InstallOption>), typeof(AssemblyOptions), new UIPropertyMetadata(new InstallOption[0]));

        public InstallOption NewState { get { return (InstallOption)GetValue(NewStateProperty); } set { SetValue(NewStateProperty, value); } }
        public static readonly DependencyProperty NewStateProperty = DependencyProperty.Register("NewState", typeof(InstallOption), typeof(AssemblyOptions), new UIPropertyMetadata(InstallOption.NoAction, newStateChanged));

        public MainWindow main;
        public Assembly assembly;

        private static void oldStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = d as AssemblyOptions;
            if (self == null) return;

            var newState = (InstallState)e.NewValue;
            if (self.NewState == InstallOption.NoAction)
                self.CurrentStateMark = MainWindow.CurrentStateImg(self.CurrentState, self.main.imgs);

            self.main.UpdateConfig1Mark(3, 0);
            self.main.UpdateConfig2Mark(3, 0);
        }

        private static void newStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = d as AssemblyOptions;
            if (self == null) return;

            var newState = (InstallOption)e.NewValue;
            if (newState == InstallOption.NoAction)
                self.CurrentStateMark = MainWindow.CurrentStateImg(self.CurrentState, self.main.imgs);
            else
                self.CurrentStateMark = self.main.imgs[3];

            self.main.UpdateConfig1Mark(3, 0);
            self.main.UpdateConfig2Mark(3, 0);
        }
    }
}
