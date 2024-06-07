using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml.Linq;
using Forms = System.Windows.Forms;

namespace BuilderApp
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void BrowseFolder_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new Forms.FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == Forms.DialogResult.OK)
                {
                    FolderPathTextBox.Text = dialog.SelectedPath;
                }
            }
        }

        private void BrowseIcon_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Image Files (*.png;*.jpg;*.ico)|*.png;*.jpg;*.ico"
            };
            if (dialog.ShowDialog() == true)
            {
                IconPathTextBox.Text = dialog.FileName;
            }
        }

        private void GenerateExe_Click(object sender, RoutedEventArgs e)
        {
            var folderPath = FolderPathTextBox.Text;
            var softwareName = SoftwareNameTextBox.Text;
            var iconPath = IconPathTextBox.Text;

            if (string.IsNullOrEmpty(folderPath) || string.IsNullOrEmpty(softwareName) || string.IsNullOrEmpty(iconPath))
            {
                Forms.MessageBox.Show("Please fill all fields.", "Error", Forms.MessageBoxButtons.OK, Forms.MessageBoxIcon.Error);
                return;
            }

            var projectPath = @"..\..\..\..\Folders ++\Folders ++.csproj"; // Adjust path as needed

            // Update project file with new values
            UpdateProjectFile(projectPath, folderPath, softwareName, iconPath);

            // Build the project
            BuildProject(projectPath);

            Forms.MessageBox.Show("Executable generated successfully.", "Success", Forms.MessageBoxButtons.OK, Forms.MessageBoxIcon.Information);
        }

        private void UpdateProjectFile(string projectPath, string folderPath, string softwareName, string iconPath)
        {
            var doc = XDocument.Load(projectPath);
            var ns = doc.Root.Name.Namespace;

            // Update assembly name
            var assemblyNameElement = doc.Root.Element(ns + "PropertyGroup").Element(ns + "AssemblyName");
            if (assemblyNameElement != null)
            {
                assemblyNameElement.Value = softwareName;
            }

            // Update folder path and icon path in code file
            var mainWindowCsPath = Path.Combine(Path.GetDirectoryName(projectPath), "MainWindow.xaml.cs");
            var mainWindowCsContent = File.ReadAllText(mainWindowCsPath);
            mainWindowCsContent = mainWindowCsContent.Replace(@"C:\Users\kian\OneDrive\Desktop\Test__Folder", folderPath);
            mainWindowCsContent = mainWindowCsContent.Replace("Resources/folder.png", iconPath);
            File.WriteAllText(mainWindowCsPath, mainWindowCsContent);

            doc.Save(projectPath);
        }

        private void BuildProject(string projectPath)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"build \"{projectPath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            process.WaitForExit();

            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();

            if (process.ExitCode != 0)
            {
                throw new Exception($"Build failed: {error}");
            }
        }
    }
}
