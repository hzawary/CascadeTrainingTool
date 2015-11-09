using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Point = System.Windows.Point;

namespace WpfApp_PositiveBuilder_Demo
{
    /// <summary>
    /// Interaction logic for RegionDeterminer.xaml
    /// </summary>
    public partial class RegionDeterminer
    {
        #region local variables

        bool _isMouseLeftButtonDown;

        Point _firstPoint;

        #endregion

        public delegate void SelectedRegionEventHandler(object sender, Int32Rect selectedRect);
        public event SelectedRegionEventHandler SelectedRegionCompleted;
        public event SelectedRegionEventHandler ResetSelectionOccured;

        public BitmapSource BmpSource
        {
            get { return ImageSource.Source as BitmapSource; }
            set { ImageSource.Source = value; }
        }

        public Int32Rect SelectedRegion { get; private set; }

        public RegionDeterminer()
        {
            InitializeComponent();
        }

        private void MainCanvas_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.RightButton == MouseButtonState.Pressed) ResetSelection();

            if (e.LeftButton != MouseButtonState.Pressed) return;

            _firstPoint = e.GetPosition(MainCanvas);

            if (RegionRectangle.Visibility == Visibility.Collapsed)
            {
                RegionRectangle.Visibility = Visibility.Visible;

                HorizontalLine.Visibility = VerticalLine.Visibility = Visibility.Collapsed;

                MainCanvas.CaptureMouse();

                _isMouseLeftButtonDown = true;
            }
            else
            {
                _isMouseLeftButtonDown = false;

                MainCanvas.ReleaseMouseCapture();

                var shape = RegionRectangle as Shape;
                Util.TryBoundShapeToCanvas(ref shape, MainCanvas);

                MainCanvas.Cursor = Cursors.Arrow;

                if (SelectedRegionCompleted == null) return;

                var x = Convert.ToInt32(Canvas.GetLeft(RegionRectangle));
                var y = Convert.ToInt32(Canvas.GetTop(RegionRectangle));
                var width = Convert.ToInt32(RegionRectangle.Width);
                var height = Convert.ToInt32(RegionRectangle.Height);

                SelectedRegion = new Int32Rect(x, y, width, height);

                SelectedRegionCompleted(this, SelectedRegion);
            }
        }

        private void MainCanvas_OnMouseMove(object sender, MouseEventArgs e)
        {
            var currentPoint = e.GetPosition(MainCanvas);

            MoveCursor(currentPoint);

            if (!_isMouseLeftButtonDown) return;

            var finalPoint = e.GetPosition(MainCanvas);

            MoveRegion(_firstPoint, finalPoint);
        }

        void MoveCursor(Point currentPoint)
        {
            HorizontalLine.Y1 = HorizontalLine.Y2 = currentPoint.Y;

            VerticalLine.X1 = VerticalLine.X2 = currentPoint.X;
        }
        void MoveRegion(Point firstPosition, Point finalPosition)
        {
            Canvas.SetLeft(RegionRectangle, Math.Min(firstPosition.X, finalPosition.X));
            Canvas.SetTop(RegionRectangle, Math.Min(firstPosition.Y, finalPosition.Y));

            RegionRectangle.Width = Math.Abs(finalPosition.X - firstPosition.X);
            RegionRectangle.Height = Math.Abs(finalPosition.Y - firstPosition.Y);
        }
        public void ResetSelection()
        {
            MainCanvas.Cursor = Cursors.None;

            RegionRectangle.Visibility = Visibility.Collapsed;

            HorizontalLine.Visibility = VerticalLine.Visibility = Visibility.Visible;

            SelectedRegion = Int32Rect.Empty;

            Canvas.SetLeft(RegionRectangle, SelectedRegion.X);
            Canvas.SetTop(RegionRectangle, SelectedRegion.Y);

            RegionRectangle.Width = SelectedRegion.Width;
            RegionRectangle.Height = SelectedRegion.Height;

            if (ResetSelectionOccured != null)
                ResetSelectionOccured(this, SelectedRegion);
        }
    }
}
