using System;
using System.IO;
using System.Windows;
using System.Windows.Media;

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
            {
                //var player = new MediaPlayer();
                //player.Open(new Uri(fileImagePath));
                //var drawing = new VideoDrawing
                //{
                //    Rect = new Rect(0, 0, 300, 200),
                //    Player = player
                //};
                //player.Play();
                //var brush = new DrawingBrush(drawing);
                //Background = brush;
                Player.Source = new Uri(fileImagePath);
                Player.Play();
            }
            else
                Close();
        }
    }
}
