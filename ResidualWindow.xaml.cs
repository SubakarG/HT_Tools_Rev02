using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using MathNet.Numerics.LinearAlgebra;

namespace HelmertLSA
{
    public partial class ResidualWindow : Window
    {
        public bool Accepted { get; private set; } = false;

        // Declare fields as nullable where appropriate or initialize them
        private List<ResidualEntry>? residualEntries;
        private List<string> pointIds;
        private List<Vector<float>> sourcePoints;
        private List<Vector<float>> targetPoints;
        private HelmertSolver solver;
        private HelmertSolver.Result? transformationResult; // Marked as nullable
        private Stack<List<ResidualEntry>> undoStack = new(); // Initialized
        private Stack<List<ResidualEntry>> redoStack = new(); // Initialized

        public ResidualWindow(List<string> ids, List<Vector<float>> source, List<Vector<float>> target, HelmertSolver solverInstance)
        {
            InitializeComponent();

            pointIds = ids ?? throw new ArgumentNullException(nameof(ids)); // Null check for constructor arguments
            sourcePoints = source ?? throw new ArgumentNullException(nameof(source));
            targetPoints = target ?? throw new ArgumentNullException(nameof(target));
            solver = solverInstance ?? throw new ArgumentNullException(nameof(solverInstance));

            ComputeTransformation(); // Ensure transformationResult and residualEntries are initialized
        }

        private void ComputeTransformation()
        {
            // Save current state for undo
            if (residualEntries != null) // Null check before pushing to undoStack
            {
                undoStack.Push(residualEntries.Select(entry => entry.Clone()).ToList());
            }

            // Filter out selected points
            var filteredSource = new List<Vector<float>>();
            var filteredTarget = new List<Vector<float>>();
            var filteredIds = new List<string>();

            for (int i = 0; i < pointIds.Count; i++)
            {
                if (residualEntries == null || !residualEntries[i].IsSelected)
                {
                    filteredSource.Add(sourcePoints[i]);
                    filteredTarget.Add(targetPoints[i]);
                    filteredIds.Add(pointIds[i]);
                }
            }

            if (filteredSource.Count < 3)
            {
                MessageBox.Show("At least 3 points are required for transformation.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Perform transformation
            transformationResult = solver.Solve(filteredSource, filteredTarget);
            var residuals = solver.ComputeResiduals(filteredSource, filteredTarget, transformationResult);

            // Update residual entries
            residualEntries = new List<ResidualEntry>();
            for (int i = 0; i < filteredIds.Count; i++)
            {
                residualEntries.Add(new ResidualEntry
                {
                    PointID = filteredIds[i],
                    dX = (float)residuals[i][0],
                    dY = (float)residuals[i][1],
                    dZ = (float)residuals[i][2],
                    VectorLength = (float)residuals[i].L2Norm(),
                    IsSelected = false
                });
            }

            ResidualsGrid.ItemsSource = residualEntries;
            ResidualsGrid.Items.Refresh();

            // Update transformation details
            TransformationDetails.Text = $"Scale: {(float)transformationResult.Scale:F6}\n" +
                                         $"Translation: Tx = {(float)transformationResult.Translation[0]:F4}, " +
                                         $"Ty = {(float)transformationResult.Translation[1]:F4}, " +
                                         $"Tz = {(float)transformationResult.Translation[2]:F4}";

            // Update statistics
            var magnitudes = residualEntries.Select(r => r.VectorLength).ToList();
            StatisticsDetails.Text = $"Residual Statistics:\n" +
                                      $"  - Mean: {magnitudes.Average():F4}\n" +
                                      $"  - Std. Dev: {Math.Sqrt(magnitudes.Select(m => Math.Pow(m - magnitudes.Average(), 2)).Average()):F4}\n" +
                                      $"  - Max Residual: {magnitudes.Max():F4}";
        }

        private void Recalculate_Click(object sender, RoutedEventArgs e)
        {
            redoStack.Clear(); // Clear redo stack when recalculating
            ComputeTransformation();
        }

        private void Undo_Click(object sender, RoutedEventArgs e)
        {
            if (undoStack.Count > 0)
            {
                redoStack.Push(residualEntries?.Select(entry => entry.Clone()).ToList() ?? new List<ResidualEntry>());
                residualEntries = undoStack.Pop();
                ResidualsGrid.ItemsSource = residualEntries;
                ResidualsGrid.Items.Refresh();
            }
        }

        private void Redo_Click(object sender, RoutedEventArgs e)
        {
            if (redoStack.Count > 0)
            {
                undoStack.Push(residualEntries?.Select(entry => entry.Clone()).ToList() ?? new List<ResidualEntry>());
                residualEntries = redoStack.Pop();
                ResidualsGrid.ItemsSource = residualEntries;
                ResidualsGrid.Items.Refresh();
            }
        }

        private void Accept_Click(object sender, RoutedEventArgs e)
        {
            Accepted = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Accepted = false;
            Close();
        }
    }

    public class ResidualEntry
    {
        public string PointID { get; set; } = string.Empty; // Initialize to avoid null
        public float dX { get; set; }
        public float dY { get; set; }
        public float dZ { get; set; }
        public float VectorLength { get; set; }
        public bool IsSelected { get; set; }

        public ResidualEntry Clone()
        {
            return (ResidualEntry)this.MemberwiseClone();
        }
    }
}