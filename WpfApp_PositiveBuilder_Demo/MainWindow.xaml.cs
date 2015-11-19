using System.Drawing.Imaging;
using System.Globalization;
using System.Windows.Controls;
using AForge.Imaging;
using AForge.Imaging.Filters;
using NHotkey;
using NHotkey.Wpf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using WpfApp_PositiveBuilder_Demo.Properties;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Drawing.Color;
using Drawing = AForge.Imaging.Drawing;
using Image = System.Drawing.Image;

namespace WpfApp_PositiveBuilder_Demo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        #region Variables

        List<ImageManager.ImageDescriptor> _imageInfoList;

        string _imageFolderPath;

        int _currentImageIndex;

        Bitmap _originalBitmap;

        const int DefaultResizeWidth = 720;

        readonly Grayscale _grayscale = Grayscale.CommonAlgorithms.BT709;
        readonly ResizeBilinear _resizer = new ResizeBilinear(0, 0);
        readonly Crop _cropper = new Crop(Rectangle.Empty);
        readonly Median _noiseRemoval = new Median(3);
        readonly Mean _meanFilter = new Mean();


        const string BackgroundFolderName = "Negatives";
        string _backgroundFilePath = string.Concat(BackgroundFolderName, "/collection_file_of_negatives");

        const string SampleFolderName = "Positives";
        string _sampleFilePath = string.Concat(SampleFolderName, "/description_file_of_samples");

        const string SamplesFileCreator = "opencv_createsamples.exe";

        const string VectorFileName = "samples.vec";

        const string TrainCascadeCreator = "opencv_traincascade.exe";

        string _dateTimeNowToString;

        bool _isAutoExtractionBg = true;

        readonly string _currentlyExecutingPath =
            Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

        #endregion

        public MainWindow()
        {
            InitializeComponent();
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            LoadSetting();

            RefreshPicsCommandBinding_Executed(null, null);
        }
        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);

            AddHotkeys();
        }
        protected override void OnDeactivated(EventArgs e)
        {
            RemoveHotkeys();

            base.OnDeactivated(e);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            SaveSetting();

            base.OnClosing(e);
        }
        private void CloseCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Close();
        }
        private void HelpCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var tutorialPath = Path.Combine(_currentlyExecutingPath, "help/tutorial.mp4");

            if (File.Exists(tutorialPath))
                Process.Start(tutorialPath);

        }
        private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            new AboutBox { Owner = this }.ShowDialog();
        }

        void SetCurrentStatusMessage(string message, Brush foreground/*, Dispatcher dispatcher*/)
        {
            CurrentStatusTextBlock.Foreground = foreground;
            CurrentStatusTextBlock.Text = message;
        }

        private void RegionDeterminerUserControl_OnSelectedRegionCompleted(object sender, Int32Rect selectedRect)
        {
            if (RegionDeterminerUserControl.BmpSource != null)
                CroppedImage.Source = new CroppedBitmap(RegionDeterminerUserControl.BmpSource, selectedRect);

            if (selectedRect.Width < 10 || selectedRect.Height < 10)
                RegionDeterminerUserControl.ResetSelection();
        }
        private void RegionDeterminerUserControl_OnResetSelectionOccurred(object sender, Int32Rect selectedrect)
        {
            CroppedImage.Source = null;
        }


        #region Setting

        void SaveSetting()
        {
            Settings.Default.ImageFolderPath = _imageFolderPath;

            Settings.Default.Save();
        }
        void LoadSetting()
        {
            _imageFolderPath = Settings.Default.ImageFolderPath;

            _currentImageIndex = -1;

            #region Create necessary files and folders

            var dateTimeNow = DateTime.Now;

            _dateTimeNowToString = string.Format("{0:0000}{1:00}{2:00}{3:00}{4:00}{5:00}",
                dateTimeNow.Year, dateTimeNow.Day, dateTimeNow.Month,
                dateTimeNow.Hour, dateTimeNow.Minute, dateTimeNow.Second);

            if (!Directory.Exists(BackgroundFolderName))
                Directory.CreateDirectory(BackgroundFolderName);

            _backgroundFilePath = string.Format("{0}-{1}.txt", _backgroundFilePath, _dateTimeNowToString);

            if (!Directory.Exists(SampleFolderName))
                Directory.CreateDirectory(SampleFolderName);

            _sampleFilePath = string.Format("{0}-{1}.txt", _sampleFilePath, _dateTimeNowToString);

            #endregion
        }

        #endregion


        #region Hotkeys

        const string ConfirmNegative = "ConfirmNegative";
        const string ConfirmPositive = "ConfirmPositive";
        const string NextImage = "NextImage";
        const string PrevImage = "PrevImage";

        void AddHotkeys()
        {
            HotkeyManager.Current.AddOrReplace(ConfirmNegative, Key.Down, ModifierKeys.Control, OnHotkeyOccurred);
            HotkeyManager.Current.AddOrReplace(ConfirmPositive, Key.Up, ModifierKeys.Control, OnHotkeyOccurred);

            HotkeyManager.Current.AddOrReplace(PrevImage, Key.Left, ModifierKeys.Control, OnHotkeyOccurred);
            HotkeyManager.Current.AddOrReplace(NextImage, Key.Right, ModifierKeys.Control, OnHotkeyOccurred);
        }
        void OnHotkeyOccurred(object sender, HotkeyEventArgs e)
        {
            switch (e.Name)
            {
                case ConfirmNegative:
                    SaveBg_OnClick(null, null);
                    break;
                case ConfirmPositive:
                    SaveSample_OnClick(null, null);
                    break;
                case PrevImage:
                    PredecessorImage(true);
                    break;
                case NextImage:
                    SuccessorImage(true);
                    break;
            }

            e.Handled = true;
        }
        static void RemoveHotkeys()
        {
            try
            {
                HotkeyManager.Current.Remove(ConfirmNegative);
                HotkeyManager.Current.Remove(ConfirmPositive);

                HotkeyManager.Current.Remove(PrevImage);
                HotkeyManager.Current.Remove(NextImage);
            }
            // ReSharper disable EmptyGeneralCatchClause
            catch { }
            // ReSharper restore EmptyGeneralCatchClause
        }

        #endregion


        #region Feeder

        private void OpenCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var dialog = new FolderBrowserDialog(_imageFolderPath);

            if (dialog.ShowDialog() != true) return;

            _imageFolderPath = dialog.SelectedPath;

            RefreshPicsCommandBinding_Executed(null, null);
        }

        void SuccessorImage(bool isShowOnImgScene)
        {
            if (_imageInfoList == null || _imageInfoList.Count == 0) return;

            if (++_currentImageIndex >= _imageInfoList.Count)
                _currentImageIndex = 0;

            LoadImageFromCurrentIndex(isShowOnImgScene);
        }
        void PredecessorImage(bool isShowOnImgScene)
        {
            if (_imageInfoList == null || _imageInfoList.Count == 0) return;

            if (--_currentImageIndex < 0)
                _currentImageIndex = _imageInfoList.Count - 1;

            LoadImageFromCurrentIndex(isShowOnImgScene);
        }

        private void LoadImageFromCurrentIndex(bool isShowOnImgScene)
        {
            if (_imageInfoList[_currentImageIndex] == null) return;

            Dispatcher.Invoke(() =>
            {
                // Thumbnail of current image
                ThumbnailImage.Source = _imageInfoList[_currentImageIndex].BitmapSource;

                // Load original image from current image
                if (_originalBitmap != null) _originalBitmap.Dispose();

                _originalBitmap = Util.Get24BppRgb(_imageInfoList[_currentImageIndex].ImagePath);

                // Change original image
                ApplyFilters(ref _originalBitmap);

                if (!RegionDeterminerUserControl.SelectedRegion.IsEmpty)
                    RegionDeterminerUserControl.ResetSelection();

                if (isShowOnImgScene)
                    // Show changed image
                    RegionDeterminerUserControl.BmpSource = Util.BitmapToBitmapImage(
                        ref _originalBitmap, BitmapCacheOption.OnLoad, BitmapCreateOptions.DelayCreation);

                if (IsResized.IsChecked == null || !IsResized.IsChecked.Value)
                    NewWidthTextBox.Text = $"{_originalBitmap.Width}";
                SizeStatusTextBlock.Text = $"{_originalBitmap.Height}";

                ImageInfoTextBlock.Text = string.Format("[{0}/{1}] {2}",
                    _currentImageIndex + 1, _imageInfoList.Count, _imageInfoList[_currentImageIndex].ImagePath);
            });
        }

        private void ApplyFilters(ref Bitmap bitmap)
        {
            if (IsLuminance.IsChecked != null && IsLuminance.IsChecked.Value)
                bitmap = _grayscale.Apply(bitmap);

            if (IsRemovedNoises.IsChecked != null && IsRemovedNoises.IsChecked.Value)
                _noiseRemoval.ApplyInPlace(bitmap);

            if (IsResized.IsChecked == null || !IsResized.IsChecked.Value) return;
            try
            {
                _resizer.NewWidth = int.Parse(NewWidthTextBox.Text);
            }
            catch
            {
                _resizer.NewWidth = DefaultResizeWidth;
            }
            _resizer.NewHeight = (int)(Convert.ToSingle(bitmap.Height) / bitmap.Width * _resizer.NewWidth);
            bitmap = _resizer.Apply(bitmap);
        }

        private void NextImage_OnClick(object sender, RoutedEventArgs e)
        {
            SuccessorImage(true);
        }

        private void PrevImage_OnClick(object sender, RoutedEventArgs e)
        {
            PredecessorImage(true);
        }

        #endregion


        #region Slider windows

        private void RefreshPicsCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (_imageInfoList != null)
                _imageInfoList.Clear();

            ThumbnailImage.Source =
                CroppedImage.Source =
                RegionDeterminerUserControl.BmpSource = null;

            ImageInfoTextBlock.Text = "Loading images...";

            var flag = IsTopDirectorySearch.IsChecked != null && IsTopDirectorySearch.IsChecked.Value;
            var worker = new BackgroundWorker();
            worker.DoWork += delegate
            {
                _imageInfoList = ImageManager.LoadImages(
                    new Uri(_imageFolderPath, UriKind.RelativeOrAbsolute),
                    flag ? SearchOption.TopDirectoryOnly : SearchOption.AllDirectories);
            };
            worker.RunWorkerCompleted += delegate
            {
                ImageInfoTextBlock.Text = _imageInfoList.Count != 0 ?
                    "Number of images: " + _imageInfoList.Count :
                    "Not exist images on " + _imageFolderPath;

                SuccessorImage(true);

                worker.Dispose();
            };
            worker.RunWorkerAsync();
        }
        private void openResources_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_imageFolderPath))
                Process.Start(_imageFolderPath);
        }

        #endregion


        #region Save images

        private void SaveBg_OnClick(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;

            if (button != null) button.IsEnabled = false;

            CreateCurrentImageInfo(false);
            SetCurrentStatusMessage("Previous negative is saved.", Brushes.Aqua);

            if (button != null) button.IsEnabled = true;
        }
        private void SaveSample_OnClick(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;

            if (button != null) button.IsEnabled = false;

            CreateCurrentImageInfo(true);
            SetCurrentStatusMessage("Previous positive is saved.", Brushes.Lime);

            if (button != null) button.IsEnabled = true;
        }
        private void SaveAllBg_OnClick(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;

            if (button != null) button.IsEnabled = false;

            var worker = new BackgroundWorker();
            worker.DoWork += delegate
            {
                CreateCurrentImageInfo(false, _imageInfoList.Count - _currentImageIndex);
            };
            worker.RunWorkerCompleted += delegate
            {
                SetCurrentStatusMessage("All negatives is saved.", Brushes.Aqua);

                if (button != null) button.IsEnabled = true;

                worker.Dispose();
            };
            worker.RunWorkerAsync();
        }
        private void SaveAllSamples_OnClick(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;

            if (button != null) button.IsEnabled = false;

            var worker = new BackgroundWorker();
            worker.DoWork += delegate
            {
                CreateCurrentImageInfo(true, _imageInfoList.Count - _currentImageIndex);
            };
            worker.RunWorkerCompleted += delegate
            {
                SetCurrentStatusMessage("All positives is saved.", Brushes.Lime);

                if (button != null) button.IsEnabled = true;

                worker.Dispose();
            };
            worker.RunWorkerAsync();
        }
        void CreateCurrentImageInfo(bool isPositiveImage, int length = 1)
        {
            if (length < 1) return;

            if (_originalBitmap == null)
            {
                SetCurrentStatusMessage("Image is empty.", Brushes.Tomato);
                return;
            }

            var rect = RegionDeterminerUserControl.SelectedRegion;

            var fileInfo = new FileInfo(_imageInfoList[_currentImageIndex].ImagePath);
            var imageFileName = string.Concat(fileInfo.Name.Replace(' ', '_'), ".jpg");

            var text = Path.Combine(BackgroundFolderName, imageFileName);

            if (!rect.IsEmpty)
            {
                if (isPositiveImage)
                {
                    if (_isAutoExtractionBg)
                    {
                        Color fillColor;
                        using (var image = _meanFilter.Apply(_originalBitmap))
                        {
                            fillColor = image.GetPixel(0, 0);
                        }

                        using (var image = UnmanagedImage.FromManagedImage(_originalBitmap))
                        {
                            Drawing.FillRectangle(image, new Rectangle(rect.X, rect.Y, rect.Width, rect.Height), fillColor);

                            SaveImageWithDescription(
                                image.ToManagedImage(false),
                                BackgroundFolderName,
                                imageFileName,
                                _backgroundFilePath,
                                text);
                        }
                    }
                }
                else
                {
                    _cropper.Rectangle = new Rectangle(rect.X, rect.Y, rect.Width, rect.Height);
                    _originalBitmap = _cropper.Apply(_originalBitmap);
                }
            }
            else
            {
                rect.Width = _originalBitmap.Width;
                rect.Height = _originalBitmap.Height;
            }

            if (isPositiveImage)
                text = string.Format("{0} 1 {1} {2} {3} {4}", imageFileName, rect.X, rect.Y, rect.Width, rect.Height);

            SaveImageWithDescription(
                _originalBitmap,
                isPositiveImage ? SampleFolderName : BackgroundFolderName,
                imageFileName,
                isPositiveImage ? _sampleFilePath : _backgroundFilePath,
                text);

            SuccessorImage(length < 2);

            CreateCurrentImageInfo(isPositiveImage, --length);
        }

        static void SaveImageWithDescription(
            Image image, string folderName, string fileName, string infoFilePath, string text)
        {
            image.Save(Path.Combine(folderName, fileName), ImageFormat.Jpeg);

            using (var textWriter = File.AppendText(infoFilePath))
            {
                textWriter.WriteLine(text);
            }
        }

        static int GetNumOfSamples(string filePath, float percentage)
        {
            var totalRows = 0;

            if (!File.Exists(filePath)) return totalRows;

            using (var textFile = File.OpenText(filePath))
            {
                while (!textFile.EndOfStream)
                {
                    textFile.ReadLine();
                    totalRows++;
                }
            }

            return Convert.ToInt32(totalRows * percentage / 100);
        }

        #endregion


        private void IsAutoExtractionBg_OnClick(object sender, RoutedEventArgs e)
        {
            if (IsAutoExtractionBg.IsChecked != null)
                _isAutoExtractionBg = IsAutoExtractionBg.IsChecked.Value;
        }

        async void CreateVec_OnClick(object sender, RoutedEventArgs e)
        {
            File.Delete(VectorFileName);

            var percentage = Math.Max(50, Math.Min(100, Convert.ToSingle(PercentSamples.Text)));
            PercentSamples.Text = percentage.ToString(CultureInfo.InvariantCulture);

            var w = Convert.ToInt32(SampleWidth.Text);
            var h = Convert.ToInt32(SampleHeight.Text);

            WindowState = WindowState.Minimized;
            ShowInTaskbar = false;

            await Util.StartAndWaitProcess(SamplesFileCreator,
                string.Format("-info {0} -vec {1} -num {2} -w {3} -h {4}",
                _sampleFilePath, VectorFileName, GetNumOfSamples(_sampleFilePath, percentage), w, h));

            ShowInTaskbar = true;
            WindowState = WindowState.Maximized;
            SetCurrentStatusMessage(string.Format("Vector file {0} created.",
                !File.Exists(VectorFileName) ? "isn't" : "is"), Brushes.Orange);
        }
        async void ShowVec_OnClick(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(VectorFileName)) return;

            WindowState = WindowState.Minimized;
            ShowInTaskbar = false;

            var w = Convert.ToInt32(SampleWidth.Text);
            var h = Convert.ToInt32(SampleHeight.Text);

            await Util.StartAndWaitProcess(SamplesFileCreator,
                $"-vec {VectorFileName} -w {w} -h {h}");

            ShowInTaskbar = true;
            WindowState = WindowState.Maximized;
        }

        async void GenerateTrain_OnClick(object sender, RoutedEventArgs e)
        {
            var xmlFolderName = CascadeDirName.Text;
            if (Directory.Exists(xmlFolderName))
                Directory.Delete(xmlFolderName, true);
            Directory.CreateDirectory(xmlFolderName);

            var w = Convert.ToInt32(SampleWidth.Text);
            var h = Convert.ToInt32(SampleHeight.Text);
            var numPos = GetNumOfSamples(_sampleFilePath, Convert.ToSingle(PercentSamples.Text));
            var numNeg = GetNumOfSamples(_backgroundFilePath, 100);

            WindowState = WindowState.Minimized;
            ShowInTaskbar = false;

            await Util.StartAndWaitProcess(TrainCascadeCreator,
                string.Format("-data {0} -vec {1} -bg {2} -baseFormatSave -w {3} -h {4} -mode ALL -numPos {5} -numNeg {6} -numStages {7}",
                xmlFolderName, VectorFileName, _backgroundFilePath, w, h,
                numPos, numNeg, Convert.ToInt32(NumofStagesTextBox.Text)));

            ShowInTaskbar = true;
            WindowState = WindowState.Maximized;
            SetCurrentStatusMessage(string.Format("XML file {0} created.",
                !File.Exists(Path.Combine(xmlFolderName, "cascade.xml")) ? "isn't" : "is"), Brushes.Gold);
        }
    }
}
