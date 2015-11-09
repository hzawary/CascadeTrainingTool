using Microsoft.Win32;
using System.Windows;

namespace WpfApp_PositiveBuilder_Demo
{
    public sealed class FolderBrowserDialog
    {
        private readonly OpenFileDialog _folderDialog;

        public FolderBrowserDialog() : this(null) { }
        public FolderBrowserDialog(string selectedPath)
        {
            _folderDialog = new OpenFileDialog
            {
                InitialDirectory = selectedPath,
                // Set validate names to false otherwise windows will not let you select "Folder Selection."
                ValidateNames = false,
                CheckFileExists = false,
                CheckPathExists = true,
                ReadOnlyChecked = true,
                DereferenceLinks = false,
                Title = "Open Folder",
                FileName = "Folder Selection.",
                Filter = "Folders (*.)|*.Nothing"
            };
        }

        public bool? ShowDialog()
        {
            return ShowDialog(null);
        }

        public bool? ShowDialog(Window owner)
        {
            return owner == null ? _folderDialog.ShowDialog() : _folderDialog.ShowDialog(owner);
        }

        /// <summary>
        /// Helper property. Parses FilePath into folder path.
        /// </summary>
        public string SelectedPath
        {
            get { return System.IO.Path.GetDirectoryName(_folderDialog.FileName); }
            set
            {
                if (!string.IsNullOrEmpty(value))
                    _folderDialog.FileName = value;
            }
        }

        public void Reset()
        {
            _folderDialog.Reset();
        }

    }
}
