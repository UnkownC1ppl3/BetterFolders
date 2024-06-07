using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.ComponentModel;
using System.Windows.Interop;

namespace CustomFolderApp
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private string folderPath = @"C:\Users\kian\Documents\DesktopFolders\Tools";
        private const int ItemsPerPage = 10;
        private List<string> folderContents;
        private int currentPage = 0;
        private Stack<string> folderHistory = new Stack<string>();

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private Visibility _backButtonVisibility = Visibility.Collapsed;
        public Visibility BackButtonVisibility
        {
            get { return _backButtonVisibility; }
            set
            {
                _backButtonVisibility = value;
                OnPropertyChanged(nameof(BackButtonVisibility));
            }
        }

        // Import the SHGetFileInfo function from Shell32.dll
        [DllImport("Shell32.dll")]
        private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);

        // Define the SHFILEINFO structure
        [StructLayout(LayoutKind.Sequential)]
        private struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }

        // Define the flags for the SHGetFileInfo function
        private const uint SHGFI_ICON = 0x000000100;     // Get icon
        private const uint SHGFI_LARGEICON = 0x000000000;     // Get large icon
        private const uint SHGFI_SMALLICON = 0x000000001;     // Get small icon

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            LoadFolderContents();
        }

        private void LoadFolderContents()
        {
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            folderContents = Directory.GetFileSystemEntries(folderPath).ToList();
            DisplayCurrentPage();
            BackButtonVisibility = folderHistory.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void DisplayCurrentPage()
        {
            ContentGrid.Children.Clear();
            PaginationPanel.Children.Clear();

            var itemsToShow = folderContents.Skip(currentPage * ItemsPerPage).Take(ItemsPerPage);

            foreach (var item in itemsToShow)
            {
                var fileName = Path.GetFileName(item);

                Image image = null;
                try
                {
                    if (File.Exists(item))
                    {
                        var icon = System.Drawing.Icon.ExtractAssociatedIcon(item);
                        image = new Image
                        {
                            Source = Imaging.CreateBitmapSourceFromHIcon(
                                        icon.Handle,
                                        Int32Rect.Empty,
                                        BitmapSizeOptions.FromEmptyOptions()),
                            Width = 48, // Smaller width
                            Height = 48, // Smaller height
                            Margin = new Thickness(5)
                        };
                    }
                    else if (Directory.Exists(item))
                    {
                        // Get the folder icon using SHGetFileInfo function
                        SHFILEINFO shfi = new SHFILEINFO();
                        IntPtr hImg = SHGetFileInfo(item, 0, ref shfi, (uint)Marshal.SizeOf(shfi), SHGFI_ICON | SHGFI_LARGEICON);

                        if (hImg != IntPtr.Zero)
                        {
                            var icon = System.Drawing.Icon.FromHandle(shfi.hIcon);
                            image = new Image
                            {
                                Source = Imaging.CreateBitmapSourceFromHIcon(
                                            icon.Handle,
                                            Int32Rect.Empty,
                                            BitmapSizeOptions.FromEmptyOptions()),
                                Width = 48, // Smaller width
                                Height = 48, // Smaller height
                                Margin = new Thickness(5)
                            };
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading icon for {fileName}: {ex.Message}");
                    continue;
                }

                var textBlock = new TextBlock
                {
                    Text = fileName,
                    TextAlignment = TextAlignment.Center,
                    Foreground = Brushes.White,
                    FontSize = 10, // Smaller font size
                    Margin = new Thickness(5)
                };

                var stackPanel = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(10)
                };

                if (image != null)
                {
                    stackPanel.Children.Add(image);
                }
                stackPanel.Children.Add(textBlock);

                var border = new Border
                {
                    Child = stackPanel,
                    Margin = new Thickness(5)
                };

                border.MouseLeftButtonDown += (s, e) =>
                {
                    if (e.ClickCount == 2)
                    {
                        OpenItem(item);
                    }
                };

                ContentGrid.Children.Add(border);
            }

            int totalPages = (int)Math.Ceiling((double)folderContents.Count / ItemsPerPage);
            if (totalPages > 1)
            {
                for (int i = 0; i < totalPages; i++)
                {
                    var dot = new TextBlock
                    {
                        Text = "●",
                        FontSize = 20,
                        Margin = new Thickness(2),
                        Foreground = i == currentPage ? Brushes.White : Brushes.Gray,
                        Opacity = i == currentPage ? 1.0 : 0.5,
                        Cursor = Cursors.Hand
                    };

                    int pageIndex = i;
                    dot.MouseLeftButtonDown += (s, e) => ChangePage(pageIndex);

                    PaginationPanel.Children.Add(dot);
                }
            }
        }

        private void OpenItem(string path)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    folderHistory.Push(folderPath);
                    folderPath = path;
                    currentPage = 0;
                    LoadFolderContents();
                }
                else
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = path,
                        UseShellExecute = true
                    });
                    Application.Current.Shutdown();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening item: {ex.Message}");
            }
        }

        private void ChangePage(int pageIndex)
        {
            currentPage = pageIndex;
            DisplayCurrentPage();
        }

        private void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            int totalPages = (int)Math.Ceiling((double)folderContents.Count / ItemsPerPage);
            if (e.Delta > 0)
            {
                // Scroll up
                if (currentPage > 0)
                {
                    currentPage--;
                    DisplayCurrentPage();
                }
            }
            else
            {
                // Scroll down
                if (currentPage < totalPages - 1)
                {
                    currentPage++;
                    DisplayCurrentPage();
                }
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (folderHistory.Count > 0)
            {
                folderPath = folderHistory.Pop();
                currentPage = 0;
                LoadFolderContents();
            }
        }

        private void Grid_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                foreach (var file in files)
                {
                    var fileName = Path.GetFileName(file);
                    var destinationPath = Path.Combine(folderPath, fileName);

                    try
                    {
                        if (File.Exists(file))
                        {
                            File.Copy(file, destinationPath, true);
                        }
                        else if (Directory.Exists(file))
                        {
                            CopyDirectory(file, destinationPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error copying file: {ex.Message}");
                    }
                }

                LoadFolderContents();
            }
        }

        private void CopyDirectory(string sourceDir, string destinationDir)
        {   
            var dir = new DirectoryInfo(sourceDir);
            var dirs = dir.GetDirectories();

            Directory.CreateDirectory(destinationDir);

            foreach (var file in dir.GetFiles())
            {
                string targetFilePath = Path.Combine(destinationDir, file.Name);
                file.CopyTo(targetFilePath, true);
            }

            foreach (var subDir in dirs)
            {
                string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                CopyDirectory(subDir.FullName, newDestinationDir);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
