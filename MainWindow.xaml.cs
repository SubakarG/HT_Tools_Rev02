using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;

namespace HelmertLSA
{
    public partial class MainWindow : Window
    {
        private HelmertSolver _solver = new HelmertSolver();
        private bool _isDarkMode = false; // Used in ToggleTheme_Click

        private TextBox? _masterFileBox;
        private TextBox? _adjustFileBox;

        public MainWindow()
        {
            InitializeComponent();

            // Initialize fields with controls
            _masterFileBox = MasterFileBox;
            _adjustFileBox = AdjustFileBox;
        }

        private void BrowseMaster_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog { Filter = "CSV/TXT Files (*.csv;*.txt)|*.csv;*.txt" };
            if (dlg.ShowDialog() == true && _masterFileBox != null)
                _masterFileBox.Text = dlg.FileName;
        }

        private void BrowseAdjustable_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog { Filter = "CSV/TXT Files (*.csv;*.txt)|*.csv;*.txt" };
            if (dlg.ShowDialog() == true && _adjustFileBox != null)
                _adjustFileBox.Text = dlg.FileName;
        }

        private async void RunAdjustment_Click(object sender, RoutedEventArgs e)
        {
            if (_masterFileBox == null || _adjustFileBox == null)
            {
                MessageBox.Show("Master or Adjustable TextBox is not initialized.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrEmpty(_masterFileBox.Text) || string.IsNullOrEmpty(_adjustFileBox.Text))
            {
                MessageBox.Show("Please select both Master and Adjustable files.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string masterFile = _masterFileBox.Text;
            string adjustFile = _adjustFileBox.Text;

            await Task.Run(() =>
            {
                var masterPoints = LoadPoints(masterFile);
                var adjPoints = LoadPoints(adjustFile);

                if (masterPoints == null || adjPoints == null)
                {
                    Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show("Error reading files. Please check file format.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                    return;
                }

                var common = masterPoints.Join(adjPoints, m => m.Id, a => a.Id, (m, a) => (m, a)).ToList();

                if (common.Count < 3)
                {
                    Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show("At least 3 common points are required.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                    return;
                }

                var X_master = common.Select(p => p.m.Vector).ToList();
                var X_adj = common.Select(p => p.a.Vector).ToList();

                var result = _solver.Solve(X_adj, X_master);

                Dispatcher.Invoke(() =>
                {
                    var residuals = _solver.ComputeResiduals(X_adj, X_master, result);
                    var popup = new ResidualWindow(common.Select(p => p.a.Id).ToList(), X_adj, X_master, _solver);
                    popup.ShowDialog();
                });
            });
        }

        private void ToggleTheme_Click(object sender, RoutedEventArgs e)
        {
            // Toggle between light and dark mode
            _isDarkMode = !_isDarkMode;

            // Update the background and text colors
            var backgroundColor = _isDarkMode ? Brushes.Black : Brushes.White;
            var textColor = _isDarkMode ? Brushes.White : Brushes.Black;

            this.Background = backgroundColor;

            // Update all TextBlock elements
            foreach (var child in LogicalTreeHelper.GetChildren(this))
            {
                if (child is TextBlock textBlock)
                {
                    textBlock.Foreground = textColor;
                }
            }
        }

        private void VisualizeData_Click(object sender, RoutedEventArgs e)
        {
            // Placeholder for 3D visualization feature
            MessageBox.Show("3D Visualization feature is not implemented yet.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private List<Point3D>? LoadPoints(string path)
        {
            try
            {
                var lines = File.ReadAllLines(path);
                var points = new List<Point3D>();
                foreach (var line in lines.Skip(1))
                {
                    var parts = Regex.Split(line.Trim(), "[,\t]+");
                    if (parts.Length >= 4 && float.TryParse(parts[1], out float x) &&
                        float.TryParse(parts[2], out float y) && float.TryParse(parts[3], out float z))
                    {
                        points.Add(new Point3D(parts[0], x, y, z));
                    }
                }
                return points;
            }
            catch
            {
                return null; // Explicitly return null on failure
            }
        }
    }
}