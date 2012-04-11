using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace xunit.runner.visualstudio.vs2010.installer
{
    /// <summary>
    /// Interaction logic for Bounce.xaml
    /// </summary>
    public partial class Bounce : Window
    {
        public Bounce()
        {
            Shield = SystemIcons.Shield.ToImageSource();
            Warning = SystemIcons.Warning.ToImageSource();
            InitializeComponent();
        }

        public ImageSource Shield { get; set; }
        public ImageSource Warning { get; set; }

        public Visibility ShieldVisible
        {
            get
            {
                WindowsIdentity id = WindowsIdentity.GetCurrent();
                WindowsPrincipal p = new WindowsPrincipal(id);
                return p.IsInRole(WindowsBuiltInRole.Administrator) ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        private void btnNo_Click(object sender, RoutedEventArgs e) { this.DialogResult = false; }
        private void btnYes_Click(object sender, RoutedEventArgs e) { this.DialogResult = true; }
    }

    internal static class IconUtilities
    {
        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern bool DeleteObject(IntPtr hObject);

        public static ImageSource ToImageSource(this Icon icon)
        {
            Bitmap bitmap = icon.ToBitmap();
            IntPtr hBitmap = bitmap.GetHbitmap();

            ImageSource wpfBitmap = Imaging.CreateBitmapSourceFromHBitmap(
                hBitmap,
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());

            if (!DeleteObject(hBitmap))
                throw new Win32Exception();

            return wpfBitmap;
        }
    }
}
