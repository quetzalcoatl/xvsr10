using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Media;

namespace xunit.runner.visualstudio.vs2010.autoinstaller
{
    public enum AssemblyInstallState
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

    public enum AssemblyInstallOption
    {
        Uninstalled,
        // GAC, - disabled. seems to cause more problems than gains
        PrivateAssemblies,
        NoAction,
    }

    public class AssemblyOptions : DependencyObject, 
        IInstallOptionsRow<AssemblyInstallState, AssemblyInstallOption>, IInstallOptionsRow2<AssemblyInstallState, AssemblyInstallOption>,
        IInstallOptionsRow<object, object>
    {
        public string AssemblyShortName { get { return (string)GetValue(AssemblyShortNameProperty); } set { SetValue(AssemblyShortNameProperty, value); } }
        public static readonly DependencyProperty AssemblyShortNameProperty = DependencyProperty.Register("AssemblyShortName", typeof(string), typeof(AssemblyOptions), new UIPropertyMetadata(""));

        public string AssemblyVersion { get { return (string)GetValue(AssemblyVersionProperty); } set { SetValue(AssemblyVersionProperty, value); } }
        public static readonly DependencyProperty AssemblyVersionProperty = DependencyProperty.Register("AssemblyVersion", typeof(string), typeof(AssemblyOptions), new UIPropertyMetadata(""));

        public AssemblyInstallState CurrentState { get { return (AssemblyInstallState)GetValue(CurrentStateProperty); } set { SetValue(CurrentStateProperty, value); } }
        public static readonly DependencyProperty CurrentStateProperty = DependencyProperty.Register("CurrentState", typeof(AssemblyInstallState), typeof(AssemblyOptions), new UIPropertyMetadata(AssemblyInstallState.Unknown, oldStateChanged));

        public ImageSource CurrentStateMark { get { return (ImageSource)GetValue(CurrentStateMarkProperty); } set { SetValue(CurrentStateMarkProperty, value); } }
        public static readonly DependencyProperty CurrentStateMarkProperty = DependencyProperty.Register("CurrentStateMark", typeof(ImageSource), typeof(AssemblyOptions), new UIPropertyMetadata(null));

        public IEnumerable<AssemblyInstallOption> NextStates { get { return (IEnumerable<AssemblyInstallOption>)GetValue(NextStatesProperty); } set { SetValue(NextStatesProperty, value); } }
        public static readonly DependencyProperty NextStatesProperty = DependencyProperty.Register("NextStates", typeof(IEnumerable<AssemblyInstallOption>), typeof(AssemblyOptions), new UIPropertyMetadata(new AssemblyInstallOption[0]));

        public AssemblyInstallOption NewState { get { return (AssemblyInstallOption)GetValue(NewStateProperty); } set { SetValue(NewStateProperty, value); } }
        public static readonly DependencyProperty NewStateProperty = DependencyProperty.Register("NewState", typeof(AssemblyInstallOption), typeof(AssemblyOptions), new UIPropertyMetadata(AssemblyInstallOption.NoAction, newStateChanged));

        public MainWindow main;
        public Assembly assembly;

        private static void oldStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = d as AssemblyOptions;
            if (self == null) return;

            var newState = (AssemblyInstallState)e.NewValue;
            if (self.NewState == AssemblyInstallOption.NoAction)
                self.CurrentStateMark = MainWindow.CurrentStateImg(self.CurrentState, self.main.imgs);

            self.main.UpdateConfigActionsAfterAssemblyActionChange();
        }

        private static void newStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = d as AssemblyOptions;
            if (self == null) return;

            var newState = (AssemblyInstallOption)e.NewValue;
            if (newState == AssemblyInstallOption.NoAction)
                self.CurrentStateMark = MainWindow.CurrentStateImg(self.CurrentState, self.main.imgs);
            else
                self.CurrentStateMark = self.main.imgs[3];

            self.main.UpdateConfigActionsAfterAssemblyActionChange();
        }

        #region IInstallOptionsRow<object,object> Members

        object IInstallOptionsRow<object, object>.CurrentState { get { return this.CurrentState; } }
        object IInstallOptionsRow<object, object>.NewState { get { return this.NewState; } }
        IEnumerable<object> IInstallOptionsRow<object, object>.NextStates { get { return this.NextStates.Cast<object>(); } }

        #endregion
    }
}
