using System;
using System.IO;
using System.Windows.Media.Imaging;

namespace WpfApp_PositiveBuilder_Demo.Help
{
    /// <summary>
    /// Interaction logic for HelpBox.xaml
    /// </summary>
    public partial class HelpBox
    {
        public HelpBox(string fileImagePath)
        {
            InitializeComponent();

            if (File.Exists(fileImagePath))
                WpfAnimatedGif.ImageBehavior.SetAnimatedSource(
                    HelpImage, new BitmapImage(new Uri(fileImagePath)));
            else
                Close();
        }
    }
}
