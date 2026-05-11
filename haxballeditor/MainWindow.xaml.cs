using Microsoft.Win32;
using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace haxballeditor
{
    public partial class MainWindow : Window
    {
        private const string AppVersion = "1.0.0";
        private const string UpdateManifestUrl = "https://raw.githubusercontent.com/xYigit58/HaxStudio-Updates/main/latest.json";
        private static readonly HttpClient UpdateHttpClient = new() { Timeout = TimeSpan.FromSeconds(10) };

        private string currentTool = "Select";
        private StadiumData stadium = new StadiumData();

        private int? selectedVertexIndex = null;
        private int? selectedSegmentIndex = null;
        private int? selectedDiscIndex = null;
        private int? selectedGoalIndex = null;
        private int? selectedPlaneIndex = null;
        private int? selectedJointIndex = null;
        private int? selectedRedSpawnIndex = null;
        private int? selectedBlueSpawnIndex = null;

        private readonly Dictionary<Ellipse, int> vertexShapeIndexes = new();
        private readonly Dictionary<Shape, int> segmentShapeIndexes = new();
        private readonly Dictionary<Ellipse, int> discShapeIndexes = new();
        private readonly Dictionary<Shape, int> goalShapeIndexes = new();
        private readonly Dictionary<Shape, int> planeShapeIndexes = new();
        private readonly Dictionary<Shape, int> jointShapeIndexes = new();
        private readonly Dictionary<Ellipse, int> redSpawnShapeIndexes = new();
        private readonly Dictionary<Ellipse, int> blueSpawnShapeIndexes = new();

        private bool isDraggingSegment = false;
        private Point segmentDragStartPoint;
        private int? segmentDragStartVertexIndex = null;
        private Line? segmentPreviewLine = null;

        private bool isDraggingGoal = false;
        private Point goalDragStartPoint;
        private Line? goalPreviewLine = null;

        private bool isDraggingPlane = false;
        private Point planeDragStartPoint;
        private Line? planePreviewLine = null;

        private bool isDraggingGoalEndpoint = false;
        private int? draggingGoalEndpointGoalIndex = null;
        private int draggingGoalEndpointNumber = -1;

        private bool isDraggingVertex = false;
        private int? draggingVertexIndex = null;
        private Point vertexDragOffset;

        private bool isDraggingDisc = false;
        private int? draggingDiscIndex = null;
        private Point discDragOffset;

        private bool isDraggingRedSpawn = false;
        private int? draggingRedSpawnIndex = null;
        private Point redSpawnDragOffset;

        private bool isDraggingBlueSpawn = false;
        private int? draggingBlueSpawnIndex = null;
        private Point blueSpawnDragOffset;

        private bool isDraggingCurveHandle = false;
        private int? draggingCurveSegmentIndex = null;
        private DateTime lastCurveDragRenderTime = DateTime.MinValue;
        private const int CurveDragRenderIntervalMs = 34;

        private string? currentFilePath = null;
        private bool isUpdatingUiFromData = false;
        private bool isUpdatingJsonPreviewFromCode = false;
        private bool jsonPreviewUserEdited = false;

        private double viewportZoom = 1.0;
        private double viewportPanX = 0;
        private double viewportPanY = 0;

        private bool isPanningViewport = false;
        private Point viewportPanStartMouse;
        private double viewportPanStartX;
        private double viewportPanStartY;

        private bool isDraggingSelectionRectangle = false;
        private Point selectionRectangleStartScreen;
        private Rectangle? selectionRectangleShape = null;
        private bool selectionRectangleTouchMode = false;

        private readonly List<SelectedItem> selectedItems = new();

        private bool isDraggingSelectedItems = false;
        private Point selectedItemsDragStartData;
        private readonly Dictionary<int, Point> selectedVertexDragStartPositions = new();
        private readonly Dictionary<int, Point> selectedDiscDragStartPositions = new();
        private readonly Dictionary<int, (double X0, double Y0, double X1, double Y1)> selectedGoalDragStartPositions = new();
        private readonly Dictionary<int, double> selectedPlaneDragStartDists = new();
        private readonly Dictionary<int, Point> selectedRedSpawnDragStartPositions = new();
        private readonly Dictionary<int, Point> selectedBlueSpawnDragStartPositions = new();

        private const double VertexHitRadius = 14;
        private const double MinimumSegmentLength = 5;
        private const double DefaultDiscRadius = 10;
        private const string DefaultColor = "FFFFFF";
        private const double CurveVisualScale = 0.8;

        private bool showViewportGrid = false;
        private bool showViewportVertexes = true;
        private bool showViewportSegments = true;
        private bool showViewportDiscs = true;
        private bool showViewportPlanes = true;
        private bool showViewportGrassStripes = true;
        private bool showViewportInvisibleObjects = true;
        private bool autoMirrorPlacement = false;
        private string viewportVertexSize = "Medium";
        private bool validationWarningBeforeSaveEnabled = true;
        private bool validationPanelAutoRefreshEnabled = false;
        private double savedLeftPanelWidth = 360;
        private double savedRightPanelWidth = 420;
        private double savedBottomPanelHeight = 280;
        private double savedWindowWidth = 1280;
        private double savedWindowHeight = 820;
        private bool autoSaveEnabled = true;
        private int autoSaveIntervalSeconds = 120;
        private string? customAutoSaveFolderPath = null;
        private bool hasUnsavedChangesForAutoSave = false;
        private DateTime? lastAutoSaveTime = null;
        private readonly DispatcherTimer autoSaveTimer = new();

        private string layersSearchText = "";
        private string layersTypeFilter = "All";
        private bool isUpdatingObjectsList = false;

        private readonly Stack<string> undoStack = new();
        private readonly Stack<string> redoStack = new();
        private readonly Stack<string> undoDescriptionStack = new();
        private readonly Stack<string> redoDescriptionStack = new();
        private bool isRestoringHistory = false;
        private bool suppressUndoPush = false;

        private readonly List<ClipboardItem> clipboardItems = new();

        private bool snapToGrid = false;
        private double snapGridSize = 25;
        private bool isUpdatingSnapUi = false;

        private readonly HashSet<string> hiddenObjectKeys = new();
        private readonly HashSet<string> lockedObjectKeys = new();

        private readonly Dictionary<string, Window> detachedPanelWindows = new();
        private readonly Dictionary<string, object> detachedPanelContents = new();
        private readonly Dictionary<string, string> panelDockStates = new()
        {
            ["Inspector"] = "Hidden",
            ["Layers"] = "Hidden",
            ["Validator"] = "Hidden",
            ["JSON"] = "Hidden",
            ["History"] = "Hidden"
        };

        private Point detachedPanelDragStartPoint;
        private string? detachedPanelDragStartName = null;
        private bool isApplyingPreferences = false;

        public MainWindow()
        {
            InitializeComponent();
            LoadEditorPreferences();
            InitializeAutoSaveTimer();
            UpdateToolSelectionUi();

            Loaded += MainWindow_Loaded;
            PreviewKeyDown += MainWindow_PreviewKeyDown;
            PreviewKeyUp += MainWindow_PreviewKeyUp;
            StateChanged += (_, _) => UpdateMaximizeRestoreButtonText();
            Closing += (_, _) => { CaptureLayoutPreferences(); SaveEditorPreferences(); };

            CreateNewStadiumData();
            UpdateBackgroundUiFromData();
            UpdateJsonPreview();
            UpdateObjectCount();
            UpdateObjectsList();
            UpdateHistoryPanel();
            UpdateViewportMiniToolbarUi();
            UpdateInspectorForSelection("None");
            UpdateStatus("Editor ready.");
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ApplySavedWindowMetrics();
            InitializePanelDockLayout();
            UpdateViewportMiniToolbarUi();
            RenderStadium();
            UpdateJsonPreview();
            UpdateMaximizeRestoreButtonText();
            UpdateAutoSaveTimerState();
            await HideSplashOverlayAsync();
        }

        private async Task HideSplashOverlayAsync()
        {
            await RunSplashStepAsync("Starting HaxStudio...", "Initializing application shell", 8, 900);
            await RunSplashStepAsync("Loading editor core...", "Preparing stadium data model and file services", 20, 1100);
            await RunSplashStepAsync("Preparing viewport...", "Starting renderer, camera controls, grid and snap systems", 34, 1300);
            await RunSplashStepAsync("Loading object tools...", "Registering vertex, segment, disc, goal, plane, spawn and joint tools", 48, 1400);
            await RunSplashStepAsync("Building workspace...", "Loading Inspector, Layers, Validator and JSON editor panels", 64, 1500);
            await RunSplashStepAsync("Loading preferences...", "Applying theme, hotkeys, viewport display options and update settings", 78, 1300);
            await RunSplashStepAsync("Checking interface...", "Finalizing custom title bar, status bar and editor layout", 90, 1200);
            await RunSplashStepAsync("Ready.", "Opening HaxStudio workspace", 100, 650);
            await PlaySplashFinalBrandAnimationAsync();

            if (SplashOverlay != null)
            {
                SplashOverlay.Visibility = Visibility.Collapsed;
            }
        }



        private async Task PlaySplashFinalBrandAnimationAsync()
        {
            if (SplashContentCard == null || SplashFinalBrandOverlay == null || SplashFinalBrandScale == null)
            {
                await Task.Delay(500);
                return;
            }

            SplashFinalBrandOverlay.Opacity = 0;
            SplashFinalBrandScale.ScaleX = 0.92;
            SplashFinalBrandScale.ScaleY = 0.92;

            DoubleAnimation cardFadeOut = new()
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(320),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            DoubleAnimation brandFadeIn = new()
            {
                From = 0,
                To = 1,
                BeginTime = TimeSpan.FromMilliseconds(120),
                Duration = TimeSpan.FromMilliseconds(620),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            DoubleAnimation brandScaleIn = new()
            {
                From = 0.92,
                To = 1,
                BeginTime = TimeSpan.FromMilliseconds(120),
                Duration = TimeSpan.FromMilliseconds(720),
                EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.25 }
            };

            SplashContentCard.BeginAnimation(OpacityProperty, cardFadeOut);
            SplashFinalBrandOverlay.BeginAnimation(OpacityProperty, brandFadeIn);
            SplashFinalBrandScale.BeginAnimation(ScaleTransform.ScaleXProperty, brandScaleIn);
            SplashFinalBrandScale.BeginAnimation(ScaleTransform.ScaleYProperty, brandScaleIn);

            await Task.Delay(1450);

            DoubleAnimation overlayFadeOut = new()
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(520),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };

            SplashOverlay.BeginAnimation(OpacityProperty, overlayFadeOut);
            await Task.Delay(560);

            SplashOverlay.BeginAnimation(OpacityProperty, null);
            SplashOverlay.Opacity = 0;
        }

        private async Task RunSplashStepAsync(string status, string detail, double progress, int delayMilliseconds)
        {
            if (SplashStatusText != null)
            {
                SplashStatusText.Text = status;
            }

            if (SplashDetailText != null)
            {
                SplashDetailText.Text = detail;
            }

            if (SplashProgressBar != null)
            {
                SplashProgressBar.Value = progress;
            }

            if (SplashPercentText != null)
            {
                SplashPercentText.Text = $"{progress:0}%";
            }

            await Task.Delay(delayMilliseconds);
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                ToggleMaximizeRestore();
                e.Handled = true;
                return;
            }

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                try
                {
                    DragMove();
                }
                catch (InvalidOperationException)
                {
                    // DragMove can throw if the mouse state changes during the drag. Safe to ignore.
                }
            }
        }

        private void MinimizeWindowButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeRestoreWindowButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleMaximizeRestore();
        }

        private void CloseWindowButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ToggleMaximizeRestore()
        {
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;

            UpdateMaximizeRestoreButtonText();
        }

        private void UpdateMaximizeRestoreButtonText()
        {
            if (MaximizeRestoreWindowButton == null)
            {
                return;
            }

            MaximizeRestoreWindowButton.ToolTip = WindowState == WindowState.Maximized ? "Restore" : "Maximize";
        }

        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            bool ctrlPressed = IsCtrlPressed();
            bool textBoxFocused = Keyboard.FocusedElement is TextBox;

            if (ctrlPressed && !textBoxFocused && HandleToolShortcut(e.Key))
            {
                e.Handled = true;
                return;
            }

            if (ctrlPressed && e.Key == Key.Z && !textBoxFocused)
            {
                UndoLastAction();
                e.Handled = true;
                return;
            }

            if (ctrlPressed && e.Key == Key.Y && !textBoxFocused)
            {
                RedoLastAction();
                e.Handled = true;
                return;
            }

            if (ctrlPressed && e.Key == Key.C && !textBoxFocused)
            {
                CopySelectedObjectsToClipboard();
                e.Handled = true;
                return;
            }

            if (ctrlPressed && e.Key == Key.V && !textBoxFocused)
            {
                PasteClipboardObjects();
                e.Handled = true;
                return;
            }

            if (ctrlPressed && e.Key == Key.D && !textBoxFocused)
            {
                DuplicateSelectedObjects();
                e.Handled = true;
                return;
            }

            if (ctrlPressed && Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) && e.Key == Key.H && !textBoxFocused)
            {
                MirrorSelectedObjects(true);
                e.Handled = true;
                return;
            }

            if (ctrlPressed && Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) && e.Key == Key.V && !textBoxFocused)
            {
                MirrorSelectedObjects(false);
                e.Handled = true;
                return;
            }

            if (textBoxFocused)
            {
                return;
            }

            if (e.Key == Key.Home || e.Key == Key.F)
            {
                ResetViewport();
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Escape)
            {
                ClearSelection();
                RenderStadium();
                UpdateStatus("Selection cleared.");
                e.Handled = true;
                return;
            }

            if (e.Key != Key.Delete && e.Key != Key.Back)
            {
                return;
            }

            if (Keyboard.FocusedElement is TextBox)
            {
                return;
            }

            if (currentTool != "Select")
            {
                return;
            }

            if (selectedItems.Count > 1 || (selectedItems.Count == 1 && !HasSingleSelection()))
            {
                DeleteSelectedItems();
                e.Handled = true;
                return;
            }

            if (selectedBlueSpawnIndex != null)
            {
                DeleteSelectedBlueSpawn();
                e.Handled = true;
                return;
            }

            if (selectedRedSpawnIndex != null)
            {
                DeleteSelectedRedSpawn();
                e.Handled = true;
                return;
            }

            if (selectedJointIndex != null)
            {
                DeleteSelectedJoint();
                e.Handled = true;
                return;
            }

            if (selectedPlaneIndex != null)
            {
                DeleteSelectedPlane();
                e.Handled = true;
                return;
            }

            if (selectedGoalIndex != null)
            {
                DeleteSelectedGoal();
                e.Handled = true;
                return;
            }

            if (selectedDiscIndex != null)
            {
                DeleteSelectedDisc();
                e.Handled = true;
                return;
            }

            if (selectedSegmentIndex != null)
            {
                DeleteSelectedSegment();
                e.Handled = true;
                return;
            }

            if (selectedVertexIndex != null)
            {
                DeleteSelectedVertex();
                e.Handled = true;
                return;
            }

            UpdateStatus("No object selected to delete.");
        }

        private void MapCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.PreviousSize.Width > 0 &&
                e.PreviousSize.Height > 0 &&
                e.NewSize.Width > 0 &&
                e.NewSize.Height > 0)
            {
                double oldCenterX = e.PreviousSize.Width / 2.0;
                double oldCenterY = e.PreviousSize.Height / 2.0;
                double newCenterX = e.NewSize.Width / 2.0;
                double newCenterY = e.NewSize.Height / 2.0;

                double centerDeltaX = newCenterX - oldCenterX;
                double centerDeltaY = newCenterY - oldCenterY;

                // The editor stores point-like stadium objects in canvas-relative data space
                // and exports them by subtracting the current canvas center. When a splitter
                // resize changes MapCanvas.ActualWidth/ActualHeight, that center changes too.
                // Move the stored canvas-space objects by the same center delta so their
                // exported HaxBall coordinates stay unchanged and the viewport does not drift.
                TranslateCanvasAnchoredStadiumObjects(centerDeltaX, centerDeltaY);
                UpdateViewportInfo();
            }

            RenderStadium();
        }

        private void NewButton_Click(object sender, RoutedEventArgs e)
        {
            PushUndoState("New Stadium");
            currentFilePath = null;
            ClearHistoryStacks();
            hiddenObjectKeys.Clear();
            lockedObjectKeys.Clear();
            CreateNewStadiumData();
            ClearSelection();
            CancelAllDrags();

            currentTool = "Select";
            CurrentToolText.Text = "Select";
            SegmentFirstVertexText.Text = "None";
            UpdateToolSelectionUi();

            UpdateBackgroundUiFromData();
            RenderStadium();
            UpdateJsonPreview();
            UpdateObjectCount();
            UpdateObjectsList();
            hasUnsavedChangesForAutoSave = false;
            UpdateStatus("New stadium created.");
        }

        private bool HandleToolShortcut(Key key)
        {
            switch (key)
            {
                case Key.L:
                    SelectTool_Click(this, new RoutedEventArgs());
                    return true;
                case Key.E:
                    AddVertexButton_Click(this, new RoutedEventArgs());
                    return true;
                case Key.T:
                    AddSegmentButton_Click(this, new RoutedEventArgs());
                    return true;
                case Key.I:
                    AddDiscButton_Click(this, new RoutedEventArgs());
                    return true;
                case Key.G:
                    AddGoalButton_Click(this, new RoutedEventArgs());
                    return true;
                case Key.P:
                    AddPlaneButton_Click(this, new RoutedEventArgs());
                    return true;
                case Key.R:
                    AddRedSpawnButton_Click(this, new RoutedEventArgs());
                    return true;
                case Key.B:
                    AddBlueSpawnButton_Click(this, new RoutedEventArgs());
                    return true;
                default:
                    return false;
            }
        }

        private void SelectTool_Click(object sender, RoutedEventArgs e)
        {
            currentTool = "Select";
            CancelSegmentDrag();
            CancelGoalDrag();
            CancelPlaneDrag();

            CurrentToolText.Text = "Select";
            SegmentFirstVertexText.Text = "None";
            UpdateToolSelectionUi();

            UpdateStatus("Select tool activated.");
        }

        private void AddVertexButton_Click(object sender, RoutedEventArgs e)
        {
            SetTool("AddVertex", "Add Vertex", "Add Vertex tool activated. Click on the viewport.");
        }

        private void AddSegmentButton_Click(object sender, RoutedEventArgs e)
        {
            SetTool("AddSegment", "Add Segment", "Drag Segment tool activated. Hold mouse, drag, and release.");
        }

        private void AddDiscButton_Click(object sender, RoutedEventArgs e)
        {
            SetTool("AddDisc", "Add Disc", "Add Disc tool activated. Click on the viewport.");
        }

        private void AddGoalButton_Click(object sender, RoutedEventArgs e)
        {
            SetTool("AddGoal", "Add Goal", "Add Goal tool activated. Drag on the viewport.");
        }

        private void AddPlaneButton_Click(object sender, RoutedEventArgs e)
        {
            SetTool("AddPlane", "Add Plane", "Add Plane tool activated. Drag a line; editor converts it to normal/dist.");
        }

        private void AddRedSpawnButton_Click(object sender, RoutedEventArgs e)
        {
            SetTool("AddRedSpawn", "Add Red Spawn", "Add Red Spawn tool activated. Click on the viewport.");
        }

        private void AddBlueSpawnButton_Click(object sender, RoutedEventArgs e)
        {
            SetTool("AddBlueSpawn", "Add Blue Spawn", "Add Blue Spawn tool activated. Click on the viewport.");
        }

        private void SetTool(string toolName, string displayName, string status)
        {
            currentTool = toolName;
            CancelAllDrags();

            CurrentToolText.Text = displayName;
            SegmentFirstVertexText.Text = "None";
            UpdateToolSelectionUi();

            UpdateStatus(status);
        }


        private void UpdateToolSelectionUi()
        {
            SetToolButtonSelected(SelectToolButton, currentTool == "Select");
            SetToolButtonSelected(AddVertexToolButton, currentTool == "AddVertex");
            SetToolButtonSelected(AddSegmentToolButton, currentTool == "AddSegment");
            SetToolButtonSelected(AddDiscToolButton, currentTool == "AddDisc");
            SetToolButtonSelected(AddGoalToolButton, currentTool == "AddGoal");
            SetToolButtonSelected(AddPlaneToolButton, currentTool == "AddPlane");
            SetToolButtonSelected(AddRedSpawnToolButton, currentTool == "AddRedSpawn");
            SetToolButtonSelected(AddBlueSpawnToolButton, currentTool == "AddBlueSpawn");

            SetToolMenuItemChecked(SelectToolMenuItem, currentTool == "Select");
            SetToolMenuItemChecked(AddVertexToolMenuItem, currentTool == "AddVertex");
            SetToolMenuItemChecked(AddSegmentToolMenuItem, currentTool == "AddSegment");
            SetToolMenuItemChecked(AddDiscToolMenuItem, currentTool == "AddDisc");
            SetToolMenuItemChecked(AddGoalToolMenuItem, currentTool == "AddGoal");
            SetToolMenuItemChecked(AddPlaneToolMenuItem, currentTool == "AddPlane");
            SetToolMenuItemChecked(AddRedSpawnToolMenuItem, currentTool == "AddRedSpawn");
            SetToolMenuItemChecked(AddBlueSpawnToolMenuItem, currentTool == "AddBlueSpawn");
        }

        private void SetToolButtonSelected(Button button, bool isSelected)
        {
            button.Style = (Style)FindResource(isSelected ? "PrimaryTopButton" : "TopButton");
        }

        private void SetToolMenuItemChecked(MenuItem menuItem, bool isChecked)
        {
            menuItem.IsChecked = isChecked;
        }

        private void CancelAllDrags()
        {
            CancelSegmentDrag();
            CancelGoalDrag();
            CancelPlaneDrag();
            CancelGoalEndpointDrag();
            CancelVertexDrag();
            CancelDiscDrag();
            CancelRedSpawnDrag();
            CancelBlueSpawnDrag();
            CancelCurveHandleDrag();
            CancelSelectedItemsDrag();
            CancelSelectionRectangle();
        }

        private void CancelSelectionRectangle()
        {
            isDraggingSelectionRectangle = false;
            selectionRectangleTouchMode = false;
            if (selectionRectangleShape != null)
            {
                MapCanvas.Children.Remove(selectionRectangleShape);
                selectionRectangleShape = null;
            }
            ReleaseCanvasMouseIfSafe();
        }

        private void GoalTeamComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isUpdatingUiFromData)
            {
                return;
            }

            if (selectedGoalIndex != null)
            {
                SetSelectedGoalTeam(GetSelectedGoalTeam());
            }
        }

        private void GoalTeamRedButton_Click(object sender, RoutedEventArgs e)
        {
            SetGoalTeamComboBox("red");
            SetSelectedGoalTeam("red");
        }

        private void GoalTeamBlueButton_Click(object sender, RoutedEventArgs e)
        {
            SetGoalTeamComboBox("blue");
            SetSelectedGoalTeam("blue");
        }

        private void SetSelectedGoalTeam(string team)
        {
            if (selectedGoalIndex == null)
            {
                UpdateStatus("No goal selected. Select a goal first.");
                return;
            }

            int index = selectedGoalIndex.Value;

            if (index < 0 || index >= stadium.Goals.Count)
            {
                ClearSelection();
                return;
            }

            stadium.Goals[index].Team = NormalizeGoalTeam(team);

            SelectGoal(index);
            RenderStadium();
            UpdateObjectsList();
            UpdateJsonPreview();

            UpdateStatus($"Goal #{index} team changed to {stadium.Goals[index].Team}.");
        }

        private string GetSelectedGoalTeam()
        {
            if (GoalTeamComboBox.SelectedItem is ComboBoxItem item && item.Content != null)
            {
                return NormalizeGoalTeam(item.Content.ToString());
            }

            return "red";
        }

        private void SetGoalTeamComboBox(string team)
        {
            isUpdatingUiFromData = true;

            try
            {
                GoalTeamComboBox.SelectedIndex = NormalizeGoalTeam(team) == "blue" ? 1 : 0;
            }
            finally
            {
                isUpdatingUiFromData = false;
            }
        }

        private string NormalizeGoalTeam(string? team)
        {
            string normalized = team?.Trim().ToLowerInvariant() ?? "red";
            return normalized == "blue" ? "blue" : "red";
        }

        private Brush GetGoalBrush(string? team)
        {
            return NormalizeGoalTeam(team) == "blue" ? Brushes.DodgerBlue : Brushes.Red;
        }

        private void AddJointButton_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(JointD0TextBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int d0))
            {
                UpdateStatus("Invalid joint d0 index.");
                return;
            }

            if (!int.TryParse(JointD1TextBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int d1))
            {
                UpdateStatus("Invalid joint d1 index.");
                return;
            }

            if (d0 < 0 || d0 >= stadium.Discs.Count || d1 < 0 || d1 >= stadium.Discs.Count)
            {
                UpdateStatus("Joint disc index is out of range.");
                return;
            }

            DiscData disc0 = stadium.Discs[d0];
            DiscData disc1 = stadium.Discs[d1];

            double length = GetDistance(new Point(disc0.X, disc0.Y), new Point(disc1.X, disc1.Y));

            PushUndoState("Add Joint");

            JointData joint = new JointData
            {
                D0 = d0,
                D1 = d1,
                Strength = "rigid",
                Length = Math.Round(length, 2),
                Color = null
            };

            stadium.Joints.Add(joint);

            int index = stadium.Joints.Count - 1;
            SelectJoint(index);
            RenderStadium();
            UpdateObjectCount();
            UpdateObjectsList();
            UpdateJsonPreview();

            UpdateStatus($"Joint #{index} added between Disc #{d0} and Disc #{d1}.");
        }


        private void ViewInspectorMenuItem_Click(object sender, RoutedEventArgs e) => DockEditorPanel("Inspector", "Right");
        private void ViewLayersMenuItem_Click(object sender, RoutedEventArgs e) => DockEditorPanel("Layers", "Right");
        private void ViewValidatorMenuItem_Click(object sender, RoutedEventArgs e) => DockEditorPanel("Validator", "Right");
        private void ViewJsonMenuItem_Click(object sender, RoutedEventArgs e) => DockEditorPanel("JSON", "Right");

        private void ViewInspectorDockLeftMenuItem_Click(object sender, RoutedEventArgs e) => DockEditorPanel("Inspector", "Left");
        private void ViewInspectorDockRightMenuItem_Click(object sender, RoutedEventArgs e) => DockEditorPanel("Inspector", "Right");
        private void ViewInspectorDockBottomMenuItem_Click(object sender, RoutedEventArgs e) => DockEditorPanel("Inspector", "Bottom");
        private void ViewInspectorFloatMenuItem_Click(object sender, RoutedEventArgs e) => OpenDetachedPanel("Inspector");
        private void ViewInspectorHideMenuItem_Click(object sender, RoutedEventArgs e) => HideEditorPanel("Inspector");

        private void ViewLayersDockLeftMenuItem_Click(object sender, RoutedEventArgs e) => DockEditorPanel("Layers", "Left");
        private void ViewLayersDockRightMenuItem_Click(object sender, RoutedEventArgs e) => DockEditorPanel("Layers", "Right");
        private void ViewLayersDockBottomMenuItem_Click(object sender, RoutedEventArgs e) => DockEditorPanel("Layers", "Bottom");
        private void ViewLayersFloatMenuItem_Click(object sender, RoutedEventArgs e) => OpenDetachedPanel("Layers");
        private void ViewLayersHideMenuItem_Click(object sender, RoutedEventArgs e) => HideEditorPanel("Layers");

        private void ViewValidatorDockLeftMenuItem_Click(object sender, RoutedEventArgs e) => DockEditorPanel("Validator", "Left");
        private void ViewValidatorDockRightMenuItem_Click(object sender, RoutedEventArgs e) => DockEditorPanel("Validator", "Right");
        private void ViewValidatorDockBottomMenuItem_Click(object sender, RoutedEventArgs e) => DockEditorPanel("Validator", "Bottom");
        private void ViewValidatorFloatMenuItem_Click(object sender, RoutedEventArgs e) => OpenDetachedPanel("Validator");
        private void ViewValidatorHideMenuItem_Click(object sender, RoutedEventArgs e) => HideEditorPanel("Validator");

        private void ViewJsonDockLeftMenuItem_Click(object sender, RoutedEventArgs e) => DockEditorPanel("JSON", "Left");
        private void ViewJsonDockRightMenuItem_Click(object sender, RoutedEventArgs e) => DockEditorPanel("JSON", "Right");
        private void ViewJsonDockBottomMenuItem_Click(object sender, RoutedEventArgs e) => DockEditorPanel("JSON", "Bottom");
        private void ViewJsonFloatMenuItem_Click(object sender, RoutedEventArgs e) => OpenDetachedPanel("JSON");
        private void ViewJsonHideMenuItem_Click(object sender, RoutedEventArgs e) => HideEditorPanel("JSON");

        private void ViewHistoryDockLeftMenuItem_Click(object sender, RoutedEventArgs e) => DockEditorPanel("History", "Left");
        private void ViewHistoryDockRightMenuItem_Click(object sender, RoutedEventArgs e) => DockEditorPanel("History", "Right");
        private void ViewHistoryDockBottomMenuItem_Click(object sender, RoutedEventArgs e) => DockEditorPanel("History", "Bottom");
        private void ViewHistoryFloatMenuItem_Click(object sender, RoutedEventArgs e) => OpenDetachedPanel("History");
        private void ViewHistoryHideMenuItem_Click(object sender, RoutedEventArgs e) => HideEditorPanel("History");

        private void ViewHideAllPanelsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            HideEditorPanel("Inspector");
            HideEditorPanel("Layers");
            HideEditorPanel("Validator");
            HideEditorPanel("JSON");
            HideEditorPanel("History");
            UpdateStatus("All panels hidden.");
        }

        private void InitializePanelDockLayout()
        {
            isApplyingPreferences = true;
            try
            {
                ApplySavedPanelDockState("Inspector");
                ApplySavedPanelDockState("Layers");
                ApplySavedPanelDockState("Validator");
                ApplySavedPanelDockState("JSON");
                ApplySavedPanelDockState("History");
            }
            finally
            {
                isApplyingPreferences = false;
            }

            RefreshDockHostVisibility();
            UpdateStatus("Editor ready. Panel layout restored from settings.");
        }

        private void ApplySavedPanelDockState(string panelName)
        {
            string state = panelDockStates.TryGetValue(panelName, out string? savedState)
                ? savedState
                : "Hidden";

            if (state == "Floating")
            {
                // Floating windows are restored as right-docked panels to avoid opening
                // unexpected external windows during startup.
                DockEditorPanel(panelName, "Right");
                return;
            }

            if (state == "Left" || state == "Right" || state == "Bottom")
            {
                DockEditorPanel(panelName, state);
                return;
            }

            HideEditorPanel(panelName, false);
        }

        private void PanelHeader_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is string panelName)
            {
                OpenDetachedPanel(panelName);
                e.Handled = true;
            }
        }

        private void PanelHeader_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is string panelName)
            {
                if (e.ClickCount >= 2)
                {
                    detachedPanelDragStartName = null;
                    OpenDetachedPanel(panelName);
                    e.Handled = true;
                    return;
                }

                detachedPanelDragStartPoint = e.GetPosition(this);
                detachedPanelDragStartName = panelName;
            }
        }

        private void PanelHeader_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed || detachedPanelDragStartName == null)
            {
                return;
            }

            Point currentPoint = e.GetPosition(this);
            double dragX = Math.Abs(currentPoint.X - detachedPanelDragStartPoint.X);
            double dragY = Math.Abs(currentPoint.Y - detachedPanelDragStartPoint.Y);

            if (dragX < SystemParameters.MinimumHorizontalDragDistance && dragY < SystemParameters.MinimumVerticalDragDistance)
            {
                return;
            }

            string panelName = detachedPanelDragStartName;
            detachedPanelDragStartName = null;
            OpenDetachedPanel(panelName);
            e.Handled = true;
        }

        private void OpenDetachedPanel(string panelName)
        {
            if (detachedPanelWindows.TryGetValue(panelName, out Window? existingWindow))
            {
                existingWindow.Activate();
                return;
            }

            TabItem? tabItem = GetPanelTabItem(panelName);
            if (tabItem == null || tabItem.Content == null)
            {
                return;
            }

            object panelContent = tabItem.Content;
            detachedPanelContents[panelName] = panelContent;
            tabItem.Content = CreateDetachedPanelPlaceholder(panelName);
            tabItem.IsSelected = true;

            Window panelWindow = new()
            {
                Title = $"HaxStudio - {GetPanelDisplayName(panelName)}",
                Owner = this,
                Width = GetDetachedPanelWidth(panelName),
                Height = GetDetachedPanelHeight(panelName),
                MinWidth = 360,
                MinHeight = 420,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                WindowStyle = WindowStyle.None,
                ResizeMode = ResizeMode.CanResize,
                Background = new SolidColorBrush(Color.FromRgb(17, 19, 23)),
                Content = CreateDetachedPanelWindowContent(panelName, panelContent)
            };

            panelWindow.Closed += (_, _) => RestoreDetachedPanel(panelName, panelWindow);
            detachedPanelWindows[panelName] = panelWindow;
            panelDockStates[panelName] = "Floating";
            SaveEditorPreferences();

            panelWindow.Show();
            panelWindow.Activate();

            RefreshDockHostVisibility();
            UpdateStatus($"{GetPanelDisplayName(panelName)} detached into a separate window.");
        }

        private void ShowDockedPanel(string panelName)
        {
            DockEditorPanel(panelName, "Right");
        }

        private void DockEditorPanel(string panelName, string dockSide)
        {
            if (detachedPanelWindows.TryGetValue(panelName, out Window? panelWindow))
            {
                panelWindow.Close();
            }

            TabItem? tabItem = GetPanelTabItem(panelName);
            TabControl? targetTabControl = GetDockTabControl(dockSide);

            if (tabItem == null || targetTabControl == null)
            {
                return;
            }

            MoveTabItemToTabControl(tabItem, targetTabControl);

            tabItem.Visibility = Visibility.Visible;
            tabItem.IsSelected = true;
            targetTabControl.SelectedItem = tabItem;

            panelDockStates[panelName] = dockSide;
            if (!isApplyingPreferences)
            {
                SaveEditorPreferences();
            }

            RefreshDockHostVisibility();
            UpdateStatus($"{GetPanelDisplayName(panelName)} docked to {dockSide.ToLowerInvariant()}.");
        }

        private void HideEditorPanel(string panelName, bool updateStatus = true)
        {
            if (detachedPanelWindows.TryGetValue(panelName, out Window? panelWindow))
            {
                panelWindow.Close();
            }

            TabItem? tabItem = GetPanelTabItem(panelName);
            if (tabItem == null)
            {
                return;
            }

            MoveTabItemToTabControl(tabItem, HiddenPanelTabControl);
            panelDockStates[panelName] = "Hidden";
            if (!isApplyingPreferences)
            {
                SaveEditorPreferences();
            }

            RefreshDockHostVisibility();

            if (updateStatus)
            {
                UpdateStatus($"{GetPanelDisplayName(panelName)} panel hidden.");
            }
        }

        private void MoveTabItemToTabControl(TabItem tabItem, TabControl targetTabControl)
        {
            if (tabItem.Parent is TabControl currentTabControl)
            {
                if (ReferenceEquals(currentTabControl, targetTabControl))
                {
                    return;
                }

                currentTabControl.Items.Remove(tabItem);
            }

            if (!targetTabControl.Items.Contains(tabItem))
            {
                targetTabControl.Items.Add(tabItem);
            }
        }

        private TabControl? GetDockTabControl(string dockSide)
        {
            return dockSide switch
            {
                "Left" => LeftPanelTabControl,
                "Right" => RightPanelTabControl,
                "Bottom" => BottomPanelTabControl,
                _ => null
            };
        }

        private void RefreshDockHostVisibility()
        {
            bool hasLeft = LeftPanelTabControl.Items.Count > 0;
            bool hasRight = RightPanelTabControl.Items.Count > 0;
            bool hasBottom = BottomPanelTabControl.Items.Count > 0;

            LeftPanelHost.Visibility = hasLeft ? Visibility.Visible : Visibility.Collapsed;
            LeftPanelGridSplitter.Visibility = hasLeft ? Visibility.Visible : Visibility.Collapsed;
            LeftPanelColumn.Width = hasLeft ? new GridLength(Math.Max(320, LeftPanelColumn.ActualWidth > 0 ? LeftPanelColumn.ActualWidth : savedLeftPanelWidth)) : new GridLength(0);
            LeftPanelSplitterColumn.Width = hasLeft ? new GridLength(6) : new GridLength(0);

            RightPanelHost.Visibility = hasRight ? Visibility.Visible : Visibility.Collapsed;
            RightPanelGridSplitter.Visibility = hasRight ? Visibility.Visible : Visibility.Collapsed;
            RightPanelColumn.Width = hasRight ? new GridLength(Math.Max(320, RightPanelColumn.ActualWidth > 0 ? RightPanelColumn.ActualWidth : savedRightPanelWidth)) : new GridLength(0);
            RightPanelSplitterColumn.Width = hasRight ? new GridLength(6) : new GridLength(0);

            BottomPanelHost.Visibility = hasBottom ? Visibility.Visible : Visibility.Collapsed;
            BottomPanelGridSplitter.Visibility = hasBottom ? Visibility.Visible : Visibility.Collapsed;
            BottomPanelRow.Height = hasBottom ? new GridLength(Math.Max(220, BottomPanelRow.ActualHeight > 0 ? BottomPanelRow.ActualHeight : savedBottomPanelHeight)) : new GridLength(0);
            BottomPanelSplitterRow.Height = hasBottom ? new GridLength(6) : new GridLength(0);

            RenderStadium();
        }

        private Border CreateDetachedPanelWindowContent(string panelName, object panelContent)
        {
            Grid root = new()
            {
                Background = new SolidColorBrush(Color.FromRgb(17, 19, 23))
            };

            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(38) });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            Border titleBar = new()
            {
                Background = new SolidColorBrush(Color.FromRgb(24, 27, 32)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(52, 58, 69)),
                BorderThickness = new Thickness(0, 0, 0, 1)
            };
            titleBar.MouseLeftButtonDown += (_, e) =>
            {
                if (e.ClickCount == 2)
                {
                    return;
                }

                Window? ownerWindow = Window.GetWindow(root);
                if (ownerWindow == null)
                {
                    return;
                }

                try
                {
                    ownerWindow.DragMove();
                }
                catch (InvalidOperationException)
                {
                    // DragMove can throw if the mouse state changes during drag. Safe to ignore.
                }
            };
            Grid.SetRow(titleBar, 0);
            root.Children.Add(titleBar);

            DockPanel titleContent = new()
            {
                LastChildFill = true,
                Margin = new Thickness(12, 0, 8, 0)
            };
            titleBar.Child = titleContent;

            StackPanel titleLeft = new()
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };
            DockPanel.SetDock(titleLeft, Dock.Left);
            titleContent.Children.Add(titleLeft);

            Border icon = new()
            {
                Width = 18,
                Height = 18,
                CornerRadius = new CornerRadius(5),
                Background = new SolidColorBrush(Color.FromRgb(47, 183, 232)),
                Margin = new Thickness(0, 0, 8, 0)
            };
            titleLeft.Children.Add(icon);

            titleLeft.Children.Add(new TextBlock
            {
                Text = GetPanelDisplayName(panelName),
                Foreground = new SolidColorBrush(Color.FromRgb(232, 237, 244)),
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            });

            StackPanel titleButtons = new()
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };
            DockPanel.SetDock(titleButtons, Dock.Right);
            titleContent.Children.Add(titleButtons);

            Button dockRightButton = CreateDetachedTitleButton("Dock Right");
            dockRightButton.Margin = new Thickness(0, 0, 6, 0);
            dockRightButton.Click += (_, _) => DockEditorPanel(panelName, "Right");
            titleButtons.Children.Add(dockRightButton);

            Button closeButton = CreateDetachedTitleButton("X");
            closeButton.Width = 32;
            closeButton.Click += (_, _) => Window.GetWindow(root)?.Close();
            titleButtons.Children.Add(closeButton);

            Border contentHost = new()
            {
                Background = new SolidColorBrush(Color.FromRgb(28, 31, 36)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(52, 58, 69)),
                BorderThickness = new Thickness(1, 0, 1, 1),
                Padding = new Thickness(0),
                Child = panelContent as UIElement
            };
            Grid.SetRow(contentHost, 1);
            root.Children.Add(contentHost);

            return new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(17, 19, 23)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(67, 75, 88)),
                BorderThickness = new Thickness(1),
                Child = root
            };
        }

        private static Button CreateDetachedTitleButton(string text)
        {
            Button button = new()
            {
                Content = text,
                Height = 24,
                MinWidth = 46,
                Padding = new Thickness(10, 0, 10, 0),
                Foreground = new SolidColorBrush(Color.FromRgb(232, 237, 244)),
                Background = new SolidColorBrush(Color.FromRgb(43, 48, 57)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(75, 85, 100)),
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand
            };

            return button;
        }

        private static void DetachElementFromCurrentParent(UIElement element)
        {
            DependencyObject? parent = VisualTreeHelper.GetParent(element);

            if (parent == null)
            {
                return;
            }

            if (parent is Border border && ReferenceEquals(border.Child, element))
            {
                border.Child = null;
                return;
            }

            if (parent is ContentControl contentControl && ReferenceEquals(contentControl.Content, element))
            {
                contentControl.Content = null;
                return;
            }

            if (parent is Panel panel)
            {
                panel.Children.Remove(element);
                return;
            }

            if (parent is Decorator decorator && ReferenceEquals(decorator.Child, element))
            {
                decorator.Child = null;
            }
        }

        private void RestoreDetachedPanel(string panelName, Window panelWindow)
        {
            if (!detachedPanelWindows.TryGetValue(panelName, out Window? trackedWindow) || !ReferenceEquals(trackedWindow, panelWindow))
            {
                return;
            }

            detachedPanelWindows.Remove(panelName);

            if (detachedPanelContents.TryGetValue(panelName, out object? panelContent))
            {
                if (panelContent is UIElement panelElement)
                {
                    DetachElementFromCurrentParent(panelElement);
                }

                panelWindow.Content = null;

                if (panelContent is UIElement panelElementAfterWindowDetach)
                {
                    DetachElementFromCurrentParent(panelElementAfterWindowDetach);
                }

                TabItem? tabItem = GetPanelTabItem(panelName);
                if (tabItem != null)
                {
                    tabItem.Content = panelContent;

                    if (tabItem.Parent == null)
                    {
                        MoveTabItemToTabControl(tabItem, HiddenPanelTabControl);
                    }
                }

                detachedPanelContents.Remove(panelName);
            }
            else
            {
                panelWindow.Content = null;
            }

            RefreshDockHostVisibility();
            UpdateStatus($"{GetPanelDisplayName(panelName)} floating window closed.");
        }

        private TabItem? GetPanelTabItem(string panelName)
        {
            return panelName switch
            {
                "Inspector" => InspectorTabItem,
                "Layers" => LayersTabItem,
                "Validator" => ValidatorTabItem,
                "JSON" => JsonTabItem,
                "History" => HistoryTabItem,
                _ => null
            };
        }

        private static string GetPanelDisplayName(string panelName)
        {
            return panelName == "JSON" ? "JSON" : panelName;
        }

        private static double GetDetachedPanelWidth(string panelName)
        {
            return panelName switch
            {
                "JSON" => 760,
                "History" => 560,
                "Layers" => 520,
                "Validator" => 560,
                _ => 520
            };
        }

        private static double GetDetachedPanelHeight(string panelName)
        {
            return panelName switch
            {
                "JSON" => 640,
                "History" => 560,
                _ => 680
            };
        }

        private Border CreateDetachedPanelPlaceholder(string panelName)
        {
            StackPanel content = new()
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(20)
            };

            content.Children.Add(new TextBlock
            {
                Text = $"{GetPanelDisplayName(panelName)} is open in a separate window.",
                Foreground = new SolidColorBrush(Color.FromRgb(235, 238, 242)),
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 10)
            });

            content.Children.Add(new TextBlock
            {
                Text = "Close the floating window or use View > Panels to dock it to the left, right, or bottom.",
                Foreground = new SolidColorBrush(Color.FromRgb(145, 152, 162)),
                FontSize = 12,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 16)
            });

            Button restoreButton = new()
            {
                Content = "Dock Right",
                MinWidth = 110,
                Height = 32,
                Padding = new Thickness(12, 0, 12, 0),
                Foreground = new SolidColorBrush(Color.FromRgb(235, 238, 242)),
                Background = new SolidColorBrush(Color.FromRgb(43, 47, 54)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(69, 76, 86)),
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand
            };
            restoreButton.Click += (_, _) => DockEditorPanel(panelName, "Right");
            content.Children.Add(restoreButton);

            return new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(24, 25, 28)),
                Child = content
            };
        }

        private void SettingsMenu_Click(object sender, RoutedEventArgs e)
        {
            ShowSettingsWindow();
        }

        private void ShowSettingsWindow()
        {
            Window settingsWindow = new()
            {
                Title = "Settings",
                Owner = this,
                Width = 760,
                Height = 520,
                MinWidth = 700,
                MinHeight = 480,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                WindowStyle = WindowStyle.None,
                ShowInTaskbar = false,
                ResizeMode = ResizeMode.NoResize,
                Background = new SolidColorBrush(Color.FromRgb(24, 25, 28))
            };

            Grid root = new()
            {
                Margin = new Thickness(18)
            };

            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            TextBlock title = new()
            {
                Text = "Settings",
                Foreground = new SolidColorBrush(Color.FromRgb(235, 238, 242)),
                FontSize = 22,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 14)
            };
            Grid.SetRow(title, 0);
            root.Children.Add(title);

            Grid body = new();
            body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(210) });
            body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });
            body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            Grid.SetRow(body, 1);
            root.Children.Add(body);

            Border categoryCard = new()
            {
                Background = new SolidColorBrush(Color.FromRgb(31, 33, 38)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(58, 62, 68)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(8)
            };
            Grid.SetColumn(categoryCard, 0);
            body.Children.Add(categoryCard);

            ListBox categoryList = new()
            {
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Foreground = new SolidColorBrush(Color.FromRgb(235, 238, 242)),
                FontSize = 13
            };

            categoryList.Items.Add(CreateSettingsCategoryItem("Preferences", "Validator and viewport behavior"));
            categoryList.Items.Add(CreateSettingsCategoryItem("Hotkeys", "Keyboard and mouse shortcuts"));
            categoryList.Items.Add(CreateSettingsCategoryItem("Themes", "Appearance and colors"));
            categoryList.Items.Add(CreateSettingsCategoryItem("Language", "Interface language"));
            categoryList.Items.Add(CreateSettingsCategoryItem("Check for Updates", "Update status"));
            categoryList.Items.Add(CreateSettingsCategoryItem("About HaxStudio", "Version and project info"));
            categoryCard.Child = categoryList;

            Border contentCard = new()
            {
                Background = new SolidColorBrush(Color.FromRgb(31, 33, 38)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(58, 62, 68)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(16)
            };
            Grid.SetColumn(contentCard, 2);
            body.Children.Add(contentCard);

            ScrollViewer scrollViewer = new()
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            StackPanel content = new();
            scrollViewer.Content = content;
            contentCard.Child = scrollViewer;

            void ShowCategory(string category)
            {
                content.Children.Clear();

                if (category == "Preferences")
                {
                    FillPreferencesSettings(content);
                    return;
                }

                if (category == "Hotkeys")
                {
                    FillHotkeysSettings(content);
                    return;
                }

                if (category == "Themes")
                {
                    FillThemeSettings(content);
                    return;
                }

                if (category == "Language")
                {
                    FillLanguageSettings(content);
                    return;
                }

                if (category == "Check for Updates")
                {
                    FillUpdateSettings(content);
                    return;
                }

                if (category == "About HaxStudio")
                {
                    FillAboutSettings(content);
                }
            }

            categoryList.SelectionChanged += (_, _) =>
            {
                if (categoryList.SelectedItem is ListBoxItem item && item.Tag is string categoryName)
                {
                    ShowCategory(categoryName);
                }
            };

            categoryList.SelectedIndex = 0;

            StackPanel footer = new()
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 14, 0, 0)
            };

            Button resetViewButton = CreateSettingsButton("Reset View");
            resetViewButton.Margin = new Thickness(0, 0, 8, 0);
            resetViewButton.Click += (_, _) => ResetViewport();
            footer.Children.Add(resetViewButton);

            Button closeButton = CreateSettingsButton("Close");
            closeButton.Click += (_, _) => settingsWindow.Close();
            footer.Children.Add(closeButton);

            Grid.SetRow(footer, 2);
            root.Children.Add(footer);

            settingsWindow.Content = root;
            settingsWindow.ShowDialog();
        }

        private ListBoxItem CreateSettingsCategoryItem(string title, string subtitle)
        {
            StackPanel panel = new()
            {
                Margin = new Thickness(4, 7, 4, 7)
            };

            panel.Children.Add(new TextBlock
            {
                Text = title,
                Foreground = new SolidColorBrush(Color.FromRgb(235, 238, 242)),
                FontSize = 13,
                FontWeight = FontWeights.SemiBold
            });

            panel.Children.Add(new TextBlock
            {
                Text = subtitle,
                Foreground = new SolidColorBrush(Color.FromRgb(145, 152, 162)),
                FontSize = 11,
                Margin = new Thickness(0, 2, 0, 0)
            });

            return new ListBoxItem
            {
                Content = panel,
                Tag = title,
                Padding = new Thickness(4),
                Margin = new Thickness(0, 0, 0, 4),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };
        }

        private void FillHotkeysSettings(StackPanel content)
        {
            AddSettingsPageTitle(content, "Hotkeys", "Keyboard and mouse shortcuts for editing faster.");

            AddSettingsSectionTitle(content, "Tools");
            AddSettingsInfoRow(content, "Ctrl + L", "Select tool");
            AddSettingsInfoRow(content, "Ctrl + E", "Add Vertex tool");
            AddSettingsInfoRow(content, "Ctrl + T", "Add Segment tool");
            AddSettingsInfoRow(content, "Ctrl + I", "Add Disc tool");
            AddSettingsInfoRow(content, "Ctrl + G", "Add Goal tool");
            AddSettingsInfoRow(content, "Ctrl + P", "Add Plane tool");
            AddSettingsInfoRow(content, "Ctrl + R", "Add Red Spawn tool");
            AddSettingsInfoRow(content, "Ctrl + B", "Add Blue Spawn tool");

            AddSettingsSectionTitle(content, "Edit Workflow");
            AddSettingsInfoRow(content, "Ctrl + Z", "Undo last action");
            AddSettingsInfoRow(content, "Ctrl + Y", "Redo last undone action");
            AddSettingsInfoRow(content, "Ctrl + C", "Copy selected objects");
            AddSettingsInfoRow(content, "Ctrl + V", "Paste copied objects");
            AddSettingsInfoRow(content, "Ctrl + D", "Duplicate selected objects");
            AddSettingsInfoRow(content, "Delete / Backspace", "Delete selected objects");
            AddSettingsInfoRow(content, "Escape", "Clear selection / cancel current selection state");

            AddSettingsSectionTitle(content, "Viewport");
            AddSettingsInfoRow(content, "Mouse Wheel", "Zoom in / out");
            AddSettingsInfoRow(content, "Middle Mouse Drag", "Pan viewport");
            AddSettingsInfoRow(content, "Space + Left Drag", "Pan viewport while using the select tool");
            AddSettingsInfoRow(content, "F / Home", "Reset viewport");

            AddSettingsSectionTitle(content, "Selection");
            AddSettingsInfoRow(content, "Left Drag", "Select objects fully inside rectangle");
            AddSettingsInfoRow(content, "Right Drag", "Select objects touched by rectangle");
            AddSettingsInfoRow(content, "Ctrl + Click", "Add/remove object from selection");
            AddSettingsInfoRow(content, "Shift + Rectangle Select", "Add rectangle results to current selection");

            AddSettingsNote(content, "Tool and edit shortcuts are disabled while typing inside text boxes, so they will not break normal text input.");
        }

        private void FillLanguageSettings(StackPanel content)
        {
            AddSettingsPageTitle(content, "Language", "Choose the interface language.");

            AddSettingsSectionTitle(content, "Display Language");
            ComboBox languageComboBox = CreateSettingsComboBox(220);
            languageComboBox.Items.Add(CreateSettingsComboBoxItem("English"));
            languageComboBox.Items.Add(CreateSettingsComboBoxItem("Turkce"));
            languageComboBox.SelectedIndex = 0;
            content.Children.Add(languageComboBox);

            AddSettingsNote(content, "Language switching is prepared in the UI. Actual translation tables will be added later.");
        }

        private void FillPreferencesSettings(StackPanel content)
        {
            AddSettingsPageTitle(content, "Preferences", "Editor behavior, validator warnings and viewport display options.");

            AddSettingsSectionTitle(content, "Validator");

            CheckBox saveValidationCheckBox = CreateSettingsCheckBox("Warn Before Save When Critical Errors Exist", validationWarningBeforeSaveEnabled);
            saveValidationCheckBox.Checked += (_, _) => { validationWarningBeforeSaveEnabled = true; SaveEditorPreferences(); UpdateStatus("Save validation warnings enabled."); };
            saveValidationCheckBox.Unchecked += (_, _) => { validationWarningBeforeSaveEnabled = false; SaveEditorPreferences(); UpdateStatus("Save validation warnings disabled."); };
            content.Children.Add(saveValidationCheckBox);

            CheckBox autoRefreshValidationCheckBox = CreateSettingsCheckBox("Auto Refresh Validator Panel After Validate", validationPanelAutoRefreshEnabled);
            autoRefreshValidationCheckBox.Checked += (_, _) => { validationPanelAutoRefreshEnabled = true; SaveEditorPreferences(); RefreshValidationPanel(false); UpdateStatus("Validator auto refresh enabled."); };
            autoRefreshValidationCheckBox.Unchecked += (_, _) => { validationPanelAutoRefreshEnabled = false; SaveEditorPreferences(); UpdateStatus("Validator auto refresh disabled."); };
            content.Children.Add(autoRefreshValidationCheckBox);

            AddSettingsNote(content, "When save validation is enabled, Save / Save As will warn you before writing a stadium that contains critical errors such as broken segment indexes or invalid disc references.");

            AddSettingsSectionTitle(content, "AutoSave");

            CheckBox autoSaveCheckBox = CreateSettingsCheckBox("Enable AutoSave Backup", autoSaveEnabled);
            autoSaveCheckBox.Checked += (_, _) =>
            {
                autoSaveEnabled = true;
                SaveEditorPreferences();
                UpdateAutoSaveTimerState();
                UpdateStatus("AutoSave enabled.");
            };
            autoSaveCheckBox.Unchecked += (_, _) =>
            {
                autoSaveEnabled = false;
                SaveEditorPreferences();
                UpdateAutoSaveTimerState();
                UpdateStatus("AutoSave disabled.");
            };
            content.Children.Add(autoSaveCheckBox);

            TextBlock autoSaveFolderLabel = new()
            {
                Text = "AutoSave Folder",
                Foreground = new SolidColorBrush(Color.FromRgb(235, 238, 242)),
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 10, 0, 6)
            };
            content.Children.Add(autoSaveFolderLabel);

            Grid autoSaveFolderGrid = new()
            {
                Margin = new Thickness(0, 0, 0, 8)
            };
            autoSaveFolderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            autoSaveFolderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            autoSaveFolderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            TextBox autoSaveFolderTextBox = CreateSettingsTextBox(0);
            autoSaveFolderTextBox.Text = GetAutoSaveFolderDisplayText();
            autoSaveFolderTextBox.IsReadOnly = true;
            autoSaveFolderTextBox.ToolTip = "The folder where AutoSave backup files are written.";
            Grid.SetColumn(autoSaveFolderTextBox, 0);
            autoSaveFolderGrid.Children.Add(autoSaveFolderTextBox);

            Button browseAutoSaveFolderButton = CreateSettingsActionButton("Browse...");
            browseAutoSaveFolderButton.Margin = new Thickness(8, 0, 0, 0);
            browseAutoSaveFolderButton.Click += (_, _) =>
            {
                BrowseAutoSaveFolder(autoSaveFolderTextBox);
            };
            Grid.SetColumn(browseAutoSaveFolderButton, 1);
            autoSaveFolderGrid.Children.Add(browseAutoSaveFolderButton);

            Button resetAutoSaveFolderButton = CreateSettingsActionButton("Reset Default");
            resetAutoSaveFolderButton.Margin = new Thickness(8, 0, 0, 0);
            resetAutoSaveFolderButton.Click += (_, _) =>
            {
                customAutoSaveFolderPath = null;
                SaveEditorPreferences();
                autoSaveFolderTextBox.Text = GetAutoSaveFolderDisplayText();
                UpdateStatus("AutoSave folder reset to default.");
            };
            Grid.SetColumn(resetAutoSaveFolderButton, 2);
            autoSaveFolderGrid.Children.Add(resetAutoSaveFolderButton);

            content.Children.Add(autoSaveFolderGrid);

            AddSettingsNote(content, "AutoSave writes a separate .autosave.hbs backup every 2 minutes when there are unsaved changes. If you choose a custom folder, all AutoSave backups will be written there. If you reset to default, saved stadiums use their own folder and unsaved stadiums use AppData/Local/HaxStudio/AutoSave.");

            AddSettingsSectionTitle(content, "Viewport Display");

            CheckBox showGridCheckBox = CreateSettingsCheckBox("Show Grid", showViewportGrid);
            showGridCheckBox.Checked += (_, _) => { showViewportGrid = true; RenderStadium(); UpdateViewportMiniToolbarUi(); SaveEditorPreferences(); UpdateStatus("Viewport grid enabled."); };
            showGridCheckBox.Unchecked += (_, _) => { showViewportGrid = false; RenderStadium(); UpdateViewportMiniToolbarUi(); SaveEditorPreferences(); UpdateStatus("Viewport grid disabled."); };
            content.Children.Add(showGridCheckBox);

            CheckBox showVertexesCheckBox = CreateSettingsCheckBox("Show Vertexes", showViewportVertexes);
            showVertexesCheckBox.Checked += (_, _) => { showViewportVertexes = true; RenderStadium(); UpdateViewportMiniToolbarUi(); SaveEditorPreferences(); UpdateStatus("Vertex display enabled."); };
            showVertexesCheckBox.Unchecked += (_, _) => { showViewportVertexes = false; RenderStadium(); UpdateViewportMiniToolbarUi(); SaveEditorPreferences(); UpdateStatus("Vertex display disabled."); };
            content.Children.Add(showVertexesCheckBox);

            CheckBox showSegmentsCheckBox = CreateSettingsCheckBox("Show Segments", showViewportSegments);
            showSegmentsCheckBox.Checked += (_, _) => { showViewportSegments = true; RenderStadium(); UpdateViewportMiniToolbarUi(); SaveEditorPreferences(); UpdateStatus("Segment display enabled."); };
            showSegmentsCheckBox.Unchecked += (_, _) => { showViewportSegments = false; RenderStadium(); UpdateViewportMiniToolbarUi(); SaveEditorPreferences(); UpdateStatus("Segment display disabled."); };
            content.Children.Add(showSegmentsCheckBox);

            CheckBox showDiscsCheckBox = CreateSettingsCheckBox("Show Discs", showViewportDiscs);
            showDiscsCheckBox.Checked += (_, _) => { showViewportDiscs = true; RenderStadium(); UpdateViewportMiniToolbarUi(); SaveEditorPreferences(); UpdateStatus("Disc display enabled."); };
            showDiscsCheckBox.Unchecked += (_, _) => { showViewportDiscs = false; RenderStadium(); UpdateViewportMiniToolbarUi(); SaveEditorPreferences(); UpdateStatus("Disc display disabled."); };
            content.Children.Add(showDiscsCheckBox);

            CheckBox showPlanesCheckBox = CreateSettingsCheckBox("Show Planes", showViewportPlanes);
            showPlanesCheckBox.Checked += (_, _) => { showViewportPlanes = true; RenderStadium(); UpdateViewportMiniToolbarUi(); SaveEditorPreferences(); UpdateStatus("Plane display enabled."); };
            showPlanesCheckBox.Unchecked += (_, _) => { showViewportPlanes = false; RenderStadium(); UpdateViewportMiniToolbarUi(); SaveEditorPreferences(); UpdateStatus("Plane display disabled."); };
            content.Children.Add(showPlanesCheckBox);

            CheckBox showStripesCheckBox = CreateSettingsCheckBox("Show Background Stripes", showViewportGrassStripes);
            showStripesCheckBox.Checked += (_, _) => { showViewportGrassStripes = true; RenderStadium(); SaveEditorPreferences(); UpdateStatus("Background stripes enabled."); };
            showStripesCheckBox.Unchecked += (_, _) => { showViewportGrassStripes = false; RenderStadium(); SaveEditorPreferences(); UpdateStatus("Background stripes disabled."); };
            content.Children.Add(showStripesCheckBox);

            CheckBox showInvisibleCheckBox = CreateSettingsCheckBox("Show Invisible Objects in Editor", showViewportInvisibleObjects);
            showInvisibleCheckBox.Checked += (_, _) => { showViewportInvisibleObjects = true; RenderStadium(); UpdateViewportMiniToolbarUi(); SaveEditorPreferences(); UpdateStatus("Invisible object preview enabled."); };
            showInvisibleCheckBox.Unchecked += (_, _) => { showViewportInvisibleObjects = false; RenderStadium(); UpdateViewportMiniToolbarUi(); SaveEditorPreferences(); UpdateStatus("Invisible object preview disabled."); };
            content.Children.Add(showInvisibleCheckBox);

            AddSettingsSectionTitle(content, "Vertex Size");
            ComboBox vertexSizeComboBox = CreateSettingsComboBox(180);
            vertexSizeComboBox.Items.Add(CreateSettingsComboBoxItem("Small"));
            vertexSizeComboBox.Items.Add(CreateSettingsComboBoxItem("Medium"));
            vertexSizeComboBox.Items.Add(CreateSettingsComboBoxItem("Large"));
            vertexSizeComboBox.SelectedIndex = viewportVertexSize == "Small" ? 0 : viewportVertexSize == "Large" ? 2 : 1;
            vertexSizeComboBox.SelectionChanged += (_, _) =>
            {
                if (vertexSizeComboBox.SelectedItem is ComboBoxItem item && item.Content != null)
                {
                    viewportVertexSize = item.Content.ToString() ?? "Medium";
                    RenderStadium();
                    UpdateStatus($"Vertex size changed to {viewportVertexSize}.");
                }
            };
            content.Children.Add(vertexSizeComboBox);
        }

        private void FillThemeSettings(StackPanel content)
        {
            AddSettingsPageTitle(content, "Themes", "Appearance information and future theme presets.");

            AddSettingsSectionTitle(content, "Current Theme");
            AddSettingsInfoRow(content, "Current Theme", "Dark Compact");
            AddSettingsInfoRow(content, "Viewport Style", "HaxPuck-like grass background");
            AddSettingsInfoRow(content, "Accent Color", "Blue");

            AddSettingsNote(content, "Viewport display switches and vertex size were moved to Preferences. Later this page can contain selectable full editor themes.");
        }

        private CheckBox CreateSettingsCheckBox(string text, bool isChecked)
        {
            CheckBox checkBox = new()
            {
                Content = text,
                IsChecked = isChecked,
                Foreground = new SolidColorBrush(Color.FromRgb(235, 238, 242)),
                FontSize = 13,
                Margin = new Thickness(0, 6, 0, 6),
                Cursor = Cursors.Hand
            };

            return checkBox;
        }

        private ComboBox CreateSettingsComboBox(double width)
        {
            ComboBox comboBox = new()
            {
                Height = 30,
                Width = width,
                HorizontalAlignment = HorizontalAlignment.Left,
                Background = new SolidColorBrush(Color.FromRgb(18, 21, 26)),
                Foreground = new SolidColorBrush(Color.FromRgb(232, 237, 245)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(69, 76, 86)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(8, 3, 8, 3),
                Margin = new Thickness(0, 4, 0, 8)
            };

            if (TryFindResource("DarkComboBoxStyle") is Style darkComboBoxStyle)
            {
                comboBox.Style = darkComboBoxStyle;
            }

            comboBox.Resources.Add(SystemColors.WindowBrushKey, new SolidColorBrush(Color.FromRgb(18, 21, 26)));
            comboBox.Resources.Add(SystemColors.ControlBrushKey, new SolidColorBrush(Color.FromRgb(18, 21, 26)));
            comboBox.Resources.Add(SystemColors.HighlightBrushKey, new SolidColorBrush(Color.FromRgb(14, 99, 156)));
            comboBox.Resources.Add(SystemColors.HighlightTextBrushKey, Brushes.White);

            return comboBox;
        }
        private TextBox CreateSettingsTextBox(double width)
        {
            return new TextBox
            {
                Width = width > 0 ? width : double.NaN,
                Height = 32,
                Padding = new Thickness(9, 5, 9, 5),
                Foreground = new SolidColorBrush(Color.FromRgb(235, 238, 242)),
                Background = new SolidColorBrush(Color.FromRgb(24, 26, 30)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(68, 74, 84)),
                BorderThickness = new Thickness(1),
                FontSize = 12,
                VerticalContentAlignment = VerticalAlignment.Center
            };
        }

        private Button CreateSettingsActionButton(string text)
        {
            return new Button
            {
                Content = text,
                Height = 32,
                MinWidth = 96,
                Padding = new Thickness(12, 0, 12, 0),
                Foreground = new SolidColorBrush(Color.FromRgb(235, 238, 242)),
                Background = new SolidColorBrush(Color.FromRgb(43, 47, 54)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(69, 76, 86)),
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand
            };
        }


        private ComboBoxItem CreateSettingsComboBoxItem(string text)
        {
            return new ComboBoxItem
            {
                Content = text,
                Background = new SolidColorBrush(Color.FromRgb(18, 21, 26)),
                Foreground = new SolidColorBrush(Color.FromRgb(232, 237, 245)),
                Padding = new Thickness(8, 5, 8, 5)
            };
        }

        private void FillAboutSettings(StackPanel content)
        {
            AddSettingsPageTitle(content, "About HaxStudio", "Project information.");

            AddSettingsInfoRow(content, "Application", "HaxStudio");
            AddSettingsInfoRow(content, "Version", "1.0.0");
            AddSettingsInfoRow(content, "Description", "Modern Windows editor for HaxBall .hbs stadium files.");
            AddSettingsInfoRow(content, "Project Type", "WPF / C# desktop application");
            AddSettingsInfoRow(content, "Viewport", "HaxPuck-inspired renderer");
            AddSettingsInfoRow(content, "Current Focus", "Stable v1.0 release preparation");

            AddSettingsNote(content, "HaxStudio is a custom Windows tool for creating, editing, validating, and exporting HaxBall stadium files.");
        }

        private void FillUpdateSettings(StackPanel content)
        {
            AddSettingsPageTitle(content, "Check for Updates", "Check the public HaxStudio update manifest and download new builds from inside the app.");

            AddSettingsInfoRow(content, "Current Version", AppVersion);
            AddSettingsInfoRow(content, "Update Channel", "Stable");
            AddSettingsInfoRow(content, "Manifest", UpdateManifestUrl);

            TextBlock resultText = new()
            {
                Text = "Press Check for Updates to compare your local version with the latest public manifest.",
                Foreground = new SolidColorBrush(Color.FromRgb(168, 174, 184)),
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 12, 0, 0)
            };

            Button checkButton = CreateSettingsButton("Check for Updates");
            checkButton.HorizontalAlignment = HorizontalAlignment.Left;
            checkButton.Margin = new Thickness(0, 14, 0, 0);
            checkButton.Click += async (_, _) =>
            {
                checkButton.IsEnabled = false;
                string oldText = checkButton.Content?.ToString() ?? "Check for Updates";
                checkButton.Content = "Checking...";
                resultText.Text = "Checking for updates...";

                try
                {
                    UpdateCheckResult result = await CheckForUpdatesAsync();

                    if (result.IsNewerVersionAvailable)
                    {
                        resultText.Text = $"New version available: {result.LatestVersion}\n\nCurrent version: {AppVersion}\n\nRelease notes:\n{result.ReleaseNotesText}";

                        MessageBoxResult downloadResult = MessageBox.Show(
                            $"A new HaxStudio update is available.\n\nCurrent version: {AppVersion}\nLatest version: {result.LatestVersion}\n\nDownload the update inside HaxStudio now?",
                            "HaxStudio Update Available",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Information);

                        if (downloadResult == MessageBoxResult.Yes)
                        {
                            if (!CanDownloadUpdatePackage(result.DownloadUrl))
                            {
                                resultText.Text += "\n\nThis update manifest does not point to a direct .zip/.exe/.msi package yet. Opening the release page instead.";

                                if (!string.IsNullOrWhiteSpace(result.ReleasePageUrl))
                                {
                                    OpenExternalUrl(result.ReleasePageUrl);
                                }
                                else if (!string.IsNullOrWhiteSpace(result.DownloadUrl))
                                {
                                    OpenExternalUrl(result.DownloadUrl);
                                }

                                return;
                            }

                            checkButton.Content = "Downloading...";
                            resultText.Text = $"Downloading HaxStudio {result.LatestVersion}...";

                            string downloadedFilePath = await DownloadUpdatePackageAsync(result, progressText =>
                            {
                                Dispatcher.Invoke(() => resultText.Text = progressText);
                            });

                            resultText.Text = $"Update downloaded successfully.\n\nFile:\n{downloadedFilePath}\n\nClose HaxStudio, extract/run the downloaded package, then start the new version.";

                            MessageBoxResult openFolderResult = MessageBox.Show(
                                $"Update downloaded successfully.\n\n{downloadedFilePath}\n\nOpen the download folder?",
                                "Update Downloaded",
                                MessageBoxButton.YesNo,
                                MessageBoxImage.Information);

                            if (openFolderResult == MessageBoxResult.Yes)
                            {
                                OpenFileLocation(downloadedFilePath);
                            }
                        }
                    }
                    else
                    {
                        resultText.Text = $"You are up to date.\n\nCurrent version: {AppVersion}\nLatest version: {result.LatestVersion}";

                        MessageBox.Show(
                            $"HaxStudio is up to date.\n\nCurrent version: {AppVersion}",
                            "Check for Updates",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    resultText.Text = $"Update check failed.\n\n{ex.Message}";

                    MessageBox.Show(
                        $"Could not check for updates.\n\n{ex.Message}",
                        "Check for Updates",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
                finally
                {
                    checkButton.Content = oldText;
                    checkButton.IsEnabled = true;
                }
            };
            content.Children.Add(checkButton);
            content.Children.Add(resultText);

            AddSettingsNote(content, "Update metadata is read from the public HaxStudio-Updates repository. New builds can be downloaded inside the app when the manifest points to a direct release package. The source code repository can stay private.");
        }

        private async Task<UpdateCheckResult> CheckForUpdatesAsync()
        {
            string json = await UpdateHttpClient.GetStringAsync(UpdateManifestUrl);

            UpdateManifest? manifest = JsonSerializer.Deserialize<UpdateManifest>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (manifest == null || string.IsNullOrWhiteSpace(manifest.Version))
            {
                throw new InvalidOperationException("The update manifest is empty or invalid.");
            }

            string releaseNotesText = manifest.ReleaseNotes == null || manifest.ReleaseNotes.Count == 0
                ? "No release notes provided."
                : "- " + string.Join("\n- ", manifest.ReleaseNotes);

            return new UpdateCheckResult
            {
                LatestVersion = manifest.Version.Trim(),
                DownloadUrl = manifest.DownloadUrl ?? "",
                ReleasePageUrl = manifest.ReleasePageUrl ?? manifest.DownloadUrl ?? "",
                FileName = string.IsNullOrWhiteSpace(manifest.FileName) ? $"HaxStudio-v{manifest.Version.Trim()}.zip" : manifest.FileName.Trim(),
                ReleaseNotesText = releaseNotesText,
                IsNewerVersionAvailable = CompareVersions(manifest.Version, AppVersion) > 0
            };
        }

        private static bool CanDownloadUpdatePackage(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return false;
            }

            string lowerUrl = url.Trim().ToLowerInvariant();
            return lowerUrl.EndsWith(".zip") || lowerUrl.EndsWith(".exe") || lowerUrl.EndsWith(".msi");
        }

        private static async Task<string> DownloadUpdatePackageAsync(UpdateCheckResult result, Action<string>? progressCallback = null)
        {
            if (string.IsNullOrWhiteSpace(result.DownloadUrl))
            {
                throw new InvalidOperationException("The update manifest does not contain a download URL.");
            }

            string downloadsFolder = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads",
                "HaxStudio Updates");

            Directory.CreateDirectory(downloadsFolder);

            string safeFileName = MakeSafeFileName(result.FileName);
            string targetPath = System.IO.Path.Combine(downloadsFolder, safeFileName);

            using HttpResponseMessage response = await UpdateHttpClient.GetAsync(result.DownloadUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            long? totalBytes = response.Content.Headers.ContentLength;
            await using Stream inputStream = await response.Content.ReadAsStreamAsync();
            await using FileStream outputStream = new(targetPath, FileMode.Create, FileAccess.Write, FileShare.None);

            byte[] buffer = new byte[81920];
            long totalRead = 0;

            while (true)
            {
                int read = await inputStream.ReadAsync(buffer, 0, buffer.Length);
                if (read == 0)
                {
                    break;
                }

                await outputStream.WriteAsync(buffer, 0, read);
                totalRead += read;

                if (totalBytes.HasValue && totalBytes.Value > 0)
                {
                    double percent = totalRead * 100.0 / totalBytes.Value;
                    progressCallback?.Invoke($"Downloading HaxStudio {result.LatestVersion}... {percent:0}%");
                }
                else
                {
                    progressCallback?.Invoke($"Downloading HaxStudio {result.LatestVersion}... {totalRead / 1024.0 / 1024.0:0.0} MB");
                }
            }

            return targetPath;
        }

        private static string MakeSafeFileName(string fileName)
        {
            string safe = string.IsNullOrWhiteSpace(fileName) ? "HaxStudio-Update.zip" : fileName.Trim();

            foreach (char invalidChar in System.IO.Path.GetInvalidFileNameChars())
            {
                safe = safe.Replace(invalidChar, '_');
            }

            return safe;
        }

        private static int CompareVersions(string latestVersion, string currentVersion)
        {
            Version latest = ParseVersion(latestVersion);
            Version current = ParseVersion(currentVersion);
            return latest.CompareTo(current);
        }

        private static Version ParseVersion(string versionText)
        {
            string clean = (versionText ?? "0.0.0").Trim();

            if (clean.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            {
                clean = clean.Substring(1);
            }

            int dashIndex = clean.IndexOf('-');
            if (dashIndex >= 0)
            {
                clean = clean.Substring(0, dashIndex);
            }

            return Version.TryParse(clean, out Version? version) ? version : new Version(0, 0, 0);
        }

        private static void OpenExternalUrl(string url)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }

        private static void OpenFileLocation(string filePath)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select,\"{filePath}\"",
                UseShellExecute = true
            });
        }

        private void AddSettingsPageTitle(StackPanel parent, string titleText, string subtitleText)
        {
            TextBlock title = new()
            {
                Text = titleText,
                Foreground = new SolidColorBrush(Color.FromRgb(235, 238, 242)),
                FontSize = 20,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 4)
            };
            parent.Children.Add(title);

            TextBlock subtitle = new()
            {
                Text = subtitleText,
                Foreground = new SolidColorBrush(Color.FromRgb(145, 152, 162)),
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 14)
            };
            parent.Children.Add(subtitle);
        }

        private void AddSettingsNote(StackPanel parent, string text)
        {
            Border noteBorder = new()
            {
                Background = new SolidColorBrush(Color.FromRgb(37, 41, 48)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(62, 70, 82)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 14, 0, 0)
            };

            noteBorder.Child = new TextBlock
            {
                Text = text,
                Foreground = new SolidColorBrush(Color.FromRgb(172, 180, 191)),
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap
            };

            parent.Children.Add(noteBorder);
        }

        private void AddSettingsSectionTitle(StackPanel parent, string text)
        {
            TextBlock title = new()
            {
                Text = text,
                Foreground = new SolidColorBrush(Color.FromRgb(109, 184, 245)),
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, parent.Children.Count == 0 ? 0 : 16, 0, 8)
            };

            parent.Children.Add(title);
        }

        private void AddSettingsInfoRow(StackPanel parent, string label, string value)
        {
            Grid row = new()
            {
                Margin = new Thickness(0, 0, 0, 7)
            };

            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            TextBlock labelText = new()
            {
                Text = label,
                Foreground = new SolidColorBrush(Color.FromRgb(226, 230, 235)),
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(labelText, 0);
            row.Children.Add(labelText);

            TextBlock valueText = new()
            {
                Text = value,
                Foreground = new SolidColorBrush(Color.FromRgb(168, 174, 184)),
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetColumn(valueText, 1);
            row.Children.Add(valueText);

            parent.Children.Add(row);
        }

        private Button CreateSettingsButton(string text)
        {
            return new Button
            {
                Content = text,
                MinWidth = 92,
                Height = 32,
                Padding = new Thickness(12, 0, 12, 0),
                Foreground = new SolidColorBrush(Color.FromRgb(235, 238, 242)),
                Background = new SolidColorBrush(Color.FromRgb(43, 47, 54)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(69, 76, 86)),
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand
            };
        }

        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            OpenStadium();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!PrepareEditorDataBeforeSave())
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(currentFilePath))
            {
                SaveAsStadium();
                return;
            }

            SaveStadiumToFile(currentFilePath);
        }

        private void SaveAsButton_Click(object sender, RoutedEventArgs e)
        {
            if (!PrepareEditorDataBeforeSave())
            {
                return;
            }

            SaveAsStadium();
        }

        private void AutoSaveNowButton_Click(object sender, RoutedEventArgs e)
        {
            RunAutoSaveBackup(true);
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }


        private void ApplyStadiumPhysicsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PushUndoState("Edit Stadium Physics");
                ApplyStadiumPhysicsFromUi();
                UpdateJsonPreview();
                RefreshValidationPanel(false);
                UpdateStatus("Stadium properties and physics updated.");
            }
            catch (FormatException ex)
            {
                MessageBox.Show(ex.Message, "Invalid Stadium Physics", MessageBoxButton.OK, MessageBoxImage.Warning);
                UpdateStatus("Stadium physics update failed.");
            }
        }

        private void ResetStadiumPhysicsButton_Click(object sender, RoutedEventArgs e)
        {
            SetDefaultStadiumPhysicsUiValues();
            UpdateStatus("Default stadium physics values loaded into the inspector. Click Apply Physics to save them.");
        }

        private void SetDefaultStadiumPhysicsUiValues()
        {
            CanBeStoredCheckBox.IsChecked = true;

            PlayerGravityXTextBox.Text = "0";
            PlayerGravityYTextBox.Text = "0";
            PlayerRadiusTextBox.Text = "15";
            PlayerBCoefTextBox.Text = "0.5";
            PlayerInvMassTextBox.Text = "0.5";
            PlayerDampingTextBox.Text = "0.96";
            PlayerCGroupTextBox.Text = "red, blue";
            PlayerAccelerationTextBox.Text = "0.1";
            PlayerKickingAccelerationTextBox.Text = "0.07";
            PlayerKickingDampingTextBox.Text = "0.96";
            PlayerKickStrengthTextBox.Text = "5";
            PlayerKickbackTextBox.Text = "0";

            BallGravityXTextBox.Text = "0";
            BallGravityYTextBox.Text = "0";
            BallRadiusTextBox.Text = "10";
            BallBCoefTextBox.Text = "0.5";
            BallInvMassTextBox.Text = "1";
            BallDampingTextBox.Text = "0.99";
            BallColorTextBox.Text = "#FFFFFF";
            BallCMaskTextBox.Text = "all";
            BallCGroupTextBox.Text = "ball";
        }

        private void UpdateStadiumPhysicsUiFromData()
        {
            if (isUpdatingUiFromData)
            {
                return;
            }

            isUpdatingUiFromData = true;

            try
            {
                Dictionary<string, JsonElement>? playerPhysics = GetTopLevelObjectExtension("playerPhysics");
                Dictionary<string, JsonElement>? ballPhysics = GetTopLevelObjectExtension("ballPhysics");

                CanBeStoredCheckBox.IsChecked = GetTopLevelBoolExtension("canBeStored", true);

                SetVector2TextBoxes(playerPhysics, "gravity", PlayerGravityXTextBox, PlayerGravityYTextBox, "0", "0");
                PlayerRadiusTextBox.Text = GetObjectNumberString(playerPhysics, "radius", "15");
                PlayerBCoefTextBox.Text = GetObjectNumberString(playerPhysics, "bCoef", "0.5");
                PlayerInvMassTextBox.Text = GetObjectNumberString(playerPhysics, "invMass", "0.5");
                PlayerDampingTextBox.Text = GetObjectNumberString(playerPhysics, "damping", "0.96");
                PlayerCGroupTextBox.Text = GetObjectStringOrArrayString(playerPhysics, "cGroup", "red, blue");
                PlayerAccelerationTextBox.Text = GetObjectNumberString(playerPhysics, "acceleration", "0.1");
                PlayerKickingAccelerationTextBox.Text = GetObjectNumberString(playerPhysics, "kickingAcceleration", "0.07");
                PlayerKickingDampingTextBox.Text = GetObjectNumberString(playerPhysics, "kickingDamping", "0.96");
                PlayerKickStrengthTextBox.Text = GetObjectNumberString(playerPhysics, "kickStrength", "5");
                PlayerKickbackTextBox.Text = GetObjectNumberString(playerPhysics, "kickback", "0");

                SetVector2TextBoxes(ballPhysics, "gravity", BallGravityXTextBox, BallGravityYTextBox, "0", "0");
                BallRadiusTextBox.Text = GetObjectNumberString(ballPhysics, "radius", "10");
                BallBCoefTextBox.Text = GetObjectNumberString(ballPhysics, "bCoef", "0.5");
                BallInvMassTextBox.Text = GetObjectNumberString(ballPhysics, "invMass", "1");
                BallDampingTextBox.Text = GetObjectNumberString(ballPhysics, "damping", "0.99");
                BallColorTextBox.Text = FormatColorForUi(GetObjectString(ballPhysics, "color", "FFFFFF"));
                BallCMaskTextBox.Text = GetObjectStringOrArrayString(ballPhysics, "cMask", "all");
                BallCGroupTextBox.Text = GetObjectStringOrArrayString(ballPhysics, "cGroup", "ball");
            }
            finally
            {
                isUpdatingUiFromData = false;
            }
        }

        private void ApplyStadiumPhysicsFromUi()
        {
            Dictionary<string, object?> playerPhysics = new()
            {
                ["gravity"] = ReadVector2ForPhysics(PlayerGravityXTextBox.Text, PlayerGravityYTextBox.Text, "playerPhysics.gravity"),
                ["radius"] = ReadPhysicsNumber(PlayerRadiusTextBox.Text, "playerPhysics.radius"),
                ["bCoef"] = ReadPhysicsNumber(PlayerBCoefTextBox.Text, "playerPhysics.bCoef"),
                ["invMass"] = ReadPhysicsNumber(PlayerInvMassTextBox.Text, "playerPhysics.invMass"),
                ["damping"] = ReadPhysicsNumber(PlayerDampingTextBox.Text, "playerPhysics.damping"),
                ["cGroup"] = ReadCollisionListForPhysics(PlayerCGroupTextBox.Text, "playerPhysics.cGroup"),
                ["acceleration"] = ReadPhysicsNumber(PlayerAccelerationTextBox.Text, "playerPhysics.acceleration"),
                ["kickingAcceleration"] = ReadPhysicsNumber(PlayerKickingAccelerationTextBox.Text, "playerPhysics.kickingAcceleration"),
                ["kickingDamping"] = ReadPhysicsNumber(PlayerKickingDampingTextBox.Text, "playerPhysics.kickingDamping"),
                ["kickStrength"] = ReadPhysicsNumber(PlayerKickStrengthTextBox.Text, "playerPhysics.kickStrength"),
                ["kickback"] = ReadPhysicsNumber(PlayerKickbackTextBox.Text, "playerPhysics.kickback")
            };

            Dictionary<string, object?> ballPhysics = new()
            {
                ["gravity"] = ReadVector2ForPhysics(BallGravityXTextBox.Text, BallGravityYTextBox.Text, "ballPhysics.gravity"),
                ["radius"] = ReadPhysicsNumber(BallRadiusTextBox.Text, "ballPhysics.radius"),
                ["bCoef"] = ReadPhysicsNumber(BallBCoefTextBox.Text, "ballPhysics.bCoef"),
                ["invMass"] = ReadPhysicsNumber(BallInvMassTextBox.Text, "ballPhysics.invMass"),
                ["damping"] = ReadPhysicsNumber(BallDampingTextBox.Text, "ballPhysics.damping"),
                ["color"] = NormalizeColorForExport(BallColorTextBox.Text),
                ["cMask"] = ReadCollisionListForPhysics(BallCMaskTextBox.Text, "ballPhysics.cMask"),
                ["cGroup"] = ReadCollisionListForPhysics(BallCGroupTextBox.Text, "ballPhysics.cGroup")
            };

            stadium.ExtensionData ??= new Dictionary<string, JsonElement>();
            stadium.ExtensionData["canBeStored"] = JsonSerializer.SerializeToElement(CanBeStoredCheckBox.IsChecked == true);
            stadium.ExtensionData["playerPhysics"] = JsonSerializer.SerializeToElement(playerPhysics);
            stadium.ExtensionData["ballPhysics"] = JsonSerializer.SerializeToElement(ballPhysics);
        }

        private double ReadPhysicsNumber(string text, string fieldName)
        {
            if (!TryReadDouble(text, out double value))
            {
                throw new FormatException($"Invalid {fieldName} value.");
            }

            return value;
        }

        private List<double> ReadVector2ForPhysics(string xText, string yText, string fieldName)
        {
            if (!TryReadDouble(xText, out double x) || !TryReadDouble(yText, out double y))
            {
                throw new FormatException($"Invalid {fieldName} value. Use two numbers like 0 and 0.");
            }

            return new List<double> { x, y };
        }

        private List<string> ReadCollisionListForPhysics(string text, string fieldName)
        {
            List<string>? values = ParseCollisionText(text);
            if (values == null || values.Count == 0)
            {
                throw new FormatException($"Invalid {fieldName} value. Use one or more values separated by commas.");
            }

            return values;
        }

        private bool GetTopLevelBoolExtension(string key, bool defaultValue)
        {
            if (stadium.ExtensionData == null || !stadium.ExtensionData.TryGetValue(key, out JsonElement value))
            {
                return defaultValue;
            }

            if (value.ValueKind == JsonValueKind.True)
            {
                return true;
            }

            if (value.ValueKind == JsonValueKind.False)
            {
                return false;
            }

            if (value.ValueKind == JsonValueKind.String && bool.TryParse(value.GetString(), out bool parsed))
            {
                return parsed;
            }

            return defaultValue;
        }

        private Dictionary<string, JsonElement>? GetTopLevelObjectExtension(string key)
        {
            if (stadium.ExtensionData == null || !stadium.ExtensionData.TryGetValue(key, out JsonElement value) || value.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(value.GetRawText());
            }
            catch
            {
                return null;
            }
        }

        private string GetObjectNumberString(Dictionary<string, JsonElement>? data, string key, string defaultValue)
        {
            if (data == null || !data.TryGetValue(key, out JsonElement value))
            {
                return defaultValue;
            }

            if (value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out double number))
            {
                return number.ToString("0.##", CultureInfo.InvariantCulture);
            }

            return value.ToString();
        }

        private string GetObjectString(Dictionary<string, JsonElement>? data, string key, string defaultValue)
        {
            if (data == null || !data.TryGetValue(key, out JsonElement value))
            {
                return defaultValue;
            }

            if (value.ValueKind == JsonValueKind.String)
            {
                return value.GetString() ?? defaultValue;
            }

            return value.ToString();
        }

        private string GetObjectStringOrArrayString(Dictionary<string, JsonElement>? data, string key, string defaultValue)
        {
            if (data == null || !data.TryGetValue(key, out JsonElement value))
            {
                return defaultValue;
            }

            if (value.ValueKind == JsonValueKind.String)
            {
                return value.GetString() ?? defaultValue;
            }

            if (value.ValueKind == JsonValueKind.Array)
            {
                List<string> parts = new();
                foreach (JsonElement item in value.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String)
                    {
                        string? text = item.GetString();
                        if (!string.IsNullOrWhiteSpace(text)) parts.Add(text);
                    }
                    else
                    {
                        parts.Add(item.ToString());
                    }
                }

                return parts.Count > 0 ? string.Join(", ", parts) : defaultValue;
            }

            return value.ToString();
        }

        private void SetVector2TextBoxes(Dictionary<string, JsonElement>? data, string key, TextBox xTextBox, TextBox yTextBox, string defaultX, string defaultY)
        {
            xTextBox.Text = defaultX;
            yTextBox.Text = defaultY;

            if (data == null || !data.TryGetValue(key, out JsonElement value))
            {
                return;
            }

            if (value.ValueKind == JsonValueKind.Array && value.GetArrayLength() >= 2)
            {
                JsonElement x = value[0];
                JsonElement y = value[1];
                xTextBox.Text = x.ValueKind == JsonValueKind.Number && x.TryGetDouble(out double xNumber)
                    ? xNumber.ToString("0.##", CultureInfo.InvariantCulture)
                    : x.ToString();
                yTextBox.Text = y.ValueKind == JsonValueKind.Number && y.TryGetDouble(out double yNumber)
                    ? yNumber.ToString("0.##", CultureInfo.InvariantCulture)
                    : y.ToString();
            }
        }

        private void ApplyJsonButton_Click(object sender, RoutedEventArgs e)
        {
            ApplyJsonToEditor();
        }

        private void RefreshJsonButton_Click(object sender, RoutedEventArgs e)
        {
            ApplyBackgroundSettingsFromUi();
            UpdateJsonPreview();
            UpdateStatus("JSON refreshed from editor data.");
        }

        private void JsonPreviewTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (isUpdatingJsonPreviewFromCode)
            {
                return;
            }

            jsonPreviewUserEdited = true;
        }

        private bool PrepareEditorDataBeforeSave()
        {
            if (jsonPreviewUserEdited)
            {
                return TryApplyJsonPreviewToEditor("Save");
            }

            ApplyBackgroundSettingsFromUi();
            return true;
        }

        private void BgTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isUpdatingUiFromData)
            {
                return;
            }

            ApplyBackgroundSettingsFromUi();
            RenderStadium();
            UpdateJsonPreview();
            UpdateStatus("Background type updated.");
        }

        private void BackgroundSettingTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (isUpdatingUiFromData)
            {
                return;
            }

            ApplyBackgroundSettingsFromUi();
            RenderStadium();
            UpdateJsonPreview();
            UpdateStatus("Background settings updated.");
        }

        private void BackgroundSettingTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
            {
                return;
            }

            ApplyBackgroundSettingsFromUi();
            RenderStadium();
            UpdateJsonPreview();
            UpdateStatus("Background settings updated.");

            Keyboard.ClearFocus();
            e.Handled = true;
        }

        private void ObjectPropertyTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            ApplySelectedObjectProperties();
        }

        private void ObjectPropertyTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
            {
                return;
            }

            ApplySelectedObjectProperties();
            Keyboard.ClearFocus();
            e.Handled = true;
        }

        private void CollisionCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (isUpdatingUiFromData)
            {
                return;
            }

            ApplySelectedObjectProperties();
        }

        private void VisComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isUpdatingUiFromData)
            {
                return;
            }

            ApplySelectedObjectProperties();
        }

        private void ApplySelectedObjectProperties()
        {
            if (HasSingleSelection())
            {
                PushUndoState("Edit Object Properties");
            }

            if (selectedVertexIndex != null)
            {
                ApplyVertexProperties(selectedVertexIndex.Value);
                return;
            }

            if (selectedRedSpawnIndex != null)
            {
                ApplyRedSpawnProperties(selectedRedSpawnIndex.Value);
                return;
            }

            if (selectedBlueSpawnIndex != null)
            {
                ApplyBlueSpawnProperties(selectedBlueSpawnIndex.Value);
                return;
            }

            if (selectedDiscIndex != null)
            {
                ApplyDiscProperties(selectedDiscIndex.Value);
                return;
            }

            if (selectedSegmentIndex != null)
            {
                ApplySegmentProperties(selectedSegmentIndex.Value);
                return;
            }

            if (selectedPlaneIndex != null)
            {
                ApplyPlaneProperties(selectedPlaneIndex.Value);
                return;
            }
        }

        private bool TryReadObjectCoordinates(out double x, out double y)
        {
            x = 0;
            y = 0;

            string xText = !string.IsNullOrWhiteSpace(ObjectXTextBox.Text) ? ObjectXTextBox.Text : PositionXTextBox.Text;
            string yText = !string.IsNullOrWhiteSpace(ObjectYTextBox.Text) ? ObjectYTextBox.Text : PositionYTextBox.Text;

            if (!TryReadDouble(xText, out x))
            {
                UpdateStatus("Invalid X value.");
                return false;
            }

            if (!TryReadDouble(yText, out y))
            {
                UpdateStatus("Invalid Y value.");
                return false;
            }

            return true;
        }

        private void ApplyVertexProperties(int vertexIndex)
        {
            if (vertexIndex < 0 || vertexIndex >= stadium.Vertexes.Count)
            {
                return;
            }

            if (!TryReadObjectCoordinates(out double x, out double y))
            {
                return;
            }

            VertexData vertex = stadium.Vertexes[vertexIndex];
            vertex.X = GetCanvasCenterX() + x;
            vertex.Y = GetCanvasCenterY() + y;

            SelectVertex(vertexIndex);
            RenderStadium();
            UpdateObjectsList();
            UpdateJsonPreview();
            UpdateStatus($"Vertex #{vertexIndex} position updated.");
        }

        private void ApplyRedSpawnProperties(int spawnIndex)
        {
            if (spawnIndex < 0 || spawnIndex >= stadium.RedSpawnPoints.Count)
            {
                return;
            }

            if (!TryReadObjectCoordinates(out double x, out double y))
            {
                return;
            }

            SpawnPointData spawn = stadium.RedSpawnPoints[spawnIndex];
            spawn.X = GetCanvasCenterX() + x;
            spawn.Y = GetCanvasCenterY() + y;

            SelectRedSpawn(spawnIndex);
            RenderStadium();
            UpdateObjectsList();
            UpdateJsonPreview();
            UpdateStatus($"Red Spawn #{spawnIndex} position updated.");
        }

        private void ApplyBlueSpawnProperties(int spawnIndex)
        {
            if (spawnIndex < 0 || spawnIndex >= stadium.BlueSpawnPoints.Count)
            {
                return;
            }

            if (!TryReadObjectCoordinates(out double x, out double y))
            {
                return;
            }

            SpawnPointData spawn = stadium.BlueSpawnPoints[spawnIndex];
            spawn.X = GetCanvasCenterX() + x;
            spawn.Y = GetCanvasCenterY() + y;

            SelectBlueSpawn(spawnIndex);
            RenderStadium();
            UpdateObjectsList();
            UpdateJsonPreview();
            UpdateStatus($"Blue Spawn #{spawnIndex} position updated.");
        }

        private void ApplyDiscProperties(int discIndex)
        {
            if (discIndex < 0 || discIndex >= stadium.Discs.Count)
            {
                return;
            }

            if (!TryReadPositiveDouble(RadiusTextBox.Text, out double radius))
            {
                UpdateStatus("Invalid disc radius.");
                return;
            }

            DiscData disc = stadium.Discs[discIndex];

            if (TryReadObjectCoordinates(out double x, out double y))
            {
                disc.X = GetCanvasCenterX() + x;
                disc.Y = GetCanvasCenterY() + y;
            }
            else
            {
                return;
            }

            disc.Radius = radius;

            disc.Color = string.IsNullOrWhiteSpace(ObjectColorTextBox.Text)
                ? null
                : NormalizeColorForExport(ObjectColorTextBox.Text);

            if (string.IsNullOrWhiteSpace(BCoefTextBox.Text))
            {
                disc.BCoef = null;
            }
            else if (TryReadDouble(BCoefTextBox.Text, out double bCoef))
            {
                disc.BCoef = bCoef;
            }
            else
            {
                UpdateStatus("Invalid disc bCoef.");
                return;
            }

            if (string.IsNullOrWhiteSpace(InvMassTextBox.Text))
            {
                disc.InvMass = null;
            }
            else if (TryReadDouble(InvMassTextBox.Text, out double invMass))
            {
                disc.InvMass = invMass;
            }
            else
            {
                UpdateStatus("Invalid disc invMass.");
                return;
            }

            disc.CGroup = ReadCollisionGroupFromUi();
            disc.CMask = ReadCollisionMaskFromUi();

            try
            {
                disc.ExtensionData = SetExtensionString(disc.ExtensionData, "trait", TraitTextBox.Text);
                disc.ExtensionData = SetExtensionDouble(disc.ExtensionData, "bias", BiasTextBox.Text);
                disc.ExtensionData = SetExtensionBool(disc.ExtensionData, "vis", ReadVisFromUi());
                disc.ExtensionData = SetExtensionDouble(disc.ExtensionData, "damping", DampingTextBox.Text);
                disc.ExtensionData = SetExtensionVector2(disc.ExtensionData, "speed", SpeedXTextBox.Text, SpeedYTextBox.Text);
                disc.ExtensionData = SetExtensionVector2(disc.ExtensionData, "gravity", GravityXTextBox.Text, GravityYTextBox.Text);
            }
            catch (FormatException ex)
            {
                UpdateStatus(ex.Message);
                return;
            }

            SelectDisc(discIndex);
            RenderStadium();
            UpdateObjectsList();
            UpdateJsonPreview();

            UpdateStatus($"Disc #{discIndex} properties updated.");
        }

        private void ApplySegmentProperties(int segmentIndex)
        {
            if (segmentIndex < 0 || segmentIndex >= stadium.Segments.Count)
            {
                return;
            }

            SegmentData segment = stadium.Segments[segmentIndex];

            segment.Color = string.IsNullOrWhiteSpace(ObjectColorTextBox.Text)
                ? null
                : NormalizeColorForExport(ObjectColorTextBox.Text);

            if (string.IsNullOrWhiteSpace(CurveTextBox.Text))
            {
                segment.Curve = null;
            }
            else if (TryReadDouble(CurveTextBox.Text, out double curve))
            {
                segment.Curve = Math.Abs(curve) < 0.0001 ? null : curve;
            }
            else
            {
                UpdateStatus("Invalid segment curve.");
                return;
            }

            if (string.IsNullOrWhiteSpace(BCoefTextBox.Text))
            {
                segment.BCoef = null;
            }
            else if (TryReadDouble(BCoefTextBox.Text, out double bCoef))
            {
                segment.BCoef = bCoef;
            }
            else
            {
                UpdateStatus("Invalid segment bCoef.");
                return;
            }

            segment.CGroup = ReadCollisionGroupFromUi();
            segment.CMask = ReadCollisionMaskFromUi();

            try
            {
                segment.ExtensionData = SetExtensionString(segment.ExtensionData, "trait", TraitTextBox.Text);
                segment.ExtensionData = SetExtensionDouble(segment.ExtensionData, "bias", BiasTextBox.Text);
                segment.ExtensionData = SetExtensionBool(segment.ExtensionData, "vis", ReadVisFromUi());
            }
            catch (FormatException ex)
            {
                UpdateStatus(ex.Message);
                return;
            }

            SelectSegment(segmentIndex);
            RenderStadium();
            UpdateObjectsList();
            UpdateJsonPreview();

            UpdateStatus($"Segment #{segmentIndex} properties updated.");
        }

        private void ApplyPlaneProperties(int planeIndex)
        {
            if (planeIndex < 0 || planeIndex >= stadium.Planes.Count)
            {
                return;
            }

            PlaneData plane = stadium.Planes[planeIndex];

            if (string.IsNullOrWhiteSpace(BCoefTextBox.Text))
            {
                plane.BCoef = null;
            }
            else if (TryReadDouble(BCoefTextBox.Text, out double bCoef))
            {
                plane.BCoef = bCoef;
            }
            else
            {
                UpdateStatus("Invalid plane bCoef.");
                return;
            }

            plane.CGroup = ReadCollisionGroupFromUi();
            plane.CMask = ReadCollisionMaskFromUi();

            try
            {
                plane.ExtensionData = SetExtensionString(plane.ExtensionData, "trait", TraitTextBox.Text);
                plane.ExtensionData = SetExtensionDouble(plane.ExtensionData, "bias", BiasTextBox.Text);
                plane.ExtensionData = SetExtensionBool(plane.ExtensionData, "vis", ReadVisFromUi());
            }
            catch (FormatException ex)
            {
                UpdateStatus(ex.Message);
                return;
            }

            SelectPlane(planeIndex);
            RenderStadium();
            UpdateObjectsList();
            UpdateJsonPreview();

            UpdateStatus($"Plane #{planeIndex} properties updated.");
        }

        private void CreateNewStadiumData()
        {
            stadium = new StadiumData
            {
                Name = "New Stadium",
                Width = 420,
                Height = 200,
                SpawnDistance = 170,
                Bg = new BgData
                {
                    Type = null,
                    Width = 420,
                    Height = 200,
                    KickOffRadius = 75,
                    CornerRadius = 0,
                    Color = null
                },
                Vertexes = new List<VertexData>(),
                Segments = new List<SegmentData>(),
                Goals = new List<GoalData>(),
                Discs = new List<DiscData>(),
                Planes = new List<PlaneData>(),
                Joints = new List<JointData>(),
                RedSpawnPoints = new List<SpawnPointData>(),
                BlueSpawnPoints = new List<SpawnPointData>(),
                Traits = CreateDefaultTraits()
            };
        }

        private void ApplyBackgroundSettingsFromUi()
        {
            if (stadium.Bg == null)
            {
                stadium.Bg = new BgData();
            }

            string selectedType = GetSelectedBackgroundType();
            stadium.Bg.Type = selectedType == "none" ? null : selectedType;

            stadium.Bg.Color = string.IsNullOrWhiteSpace(BgColorTextBox.Text)
                ? null
                : NormalizeColorForExport(BgColorTextBox.Text);

            if (TryReadPositiveDouble(BgWidthTextBox.Text, out double bgWidth))
            {
                stadium.Bg.Width = bgWidth;
                stadium.Width = (int)Math.Round(bgWidth);
            }

            if (TryReadPositiveDouble(BgHeightTextBox.Text, out double bgHeight))
            {
                stadium.Bg.Height = bgHeight;
                stadium.Height = (int)Math.Round(bgHeight);
            }

            if (TryReadPositiveDouble(BgKickOffRadiusTextBox.Text, out double kickOffRadius))
            {
                stadium.Bg.KickOffRadius = kickOffRadius;
            }

            if (TryReadPositiveDouble(BgCornerRadiusTextBox.Text, out double cornerRadius))
            {
                stadium.Bg.CornerRadius = cornerRadius;
            }
        }

        private string GetSelectedBackgroundType()
        {
            if (BgTypeComboBox.SelectedItem is ComboBoxItem item && item.Content != null)
            {
                return item.Content.ToString()!.Trim().ToLowerInvariant();
            }

            return "none";
        }

        private void UpdateBackgroundUiFromData()
        {
            isUpdatingUiFromData = true;

            try
            {
                EnsureDefaultBackground();

                string type = stadium.Bg?.Type?.Trim().ToLowerInvariant() ?? "none";
                BgTypeComboBox.SelectedIndex = type == "grass" ? 1 : type == "hockey" ? 2 : 0;

                BgColorTextBox.Text = FormatColorForUi(stadium.Bg?.Color);
                BgWidthTextBox.Text = (stadium.Bg?.Width ?? stadium.Width).ToString("0.##", CultureInfo.InvariantCulture);
                BgHeightTextBox.Text = (stadium.Bg?.Height ?? stadium.Height).ToString("0.##", CultureInfo.InvariantCulture);
                BgKickOffRadiusTextBox.Text = (stadium.Bg?.KickOffRadius ?? 75).ToString("0.##", CultureInfo.InvariantCulture);
                BgCornerRadiusTextBox.Text = (stadium.Bg?.CornerRadius ?? 0).ToString("0.##", CultureInfo.InvariantCulture);
            }
            finally
            {
                isUpdatingUiFromData = false;
            }
        }

        private void EnsureDefaultBackground()
        {
            stadium.Bg ??= new BgData();

            if (stadium.Bg.Width == null || stadium.Bg.Width <= 0)
            {
                stadium.Bg.Width = stadium.Width > 0 ? stadium.Width : 420;
            }

            if (stadium.Bg.Height == null || stadium.Bg.Height <= 0)
            {
                stadium.Bg.Height = stadium.Height > 0 ? stadium.Height : 200;
            }

            if (stadium.Bg.KickOffRadius == null || stadium.Bg.KickOffRadius < 0)
            {
                stadium.Bg.KickOffRadius = 75;
            }

            if (stadium.Bg.CornerRadius == null || stadium.Bg.CornerRadius < 0)
            {
                stadium.Bg.CornerRadius = 0;
            }
        }

        private string FormatColorForUi(string? color)
        {
            return string.IsNullOrWhiteSpace(color) ? "" : "#" + NormalizeColorForExport(color);
        }

        private string NormalizeColorForExport(string? color)
        {
            if (string.IsNullOrWhiteSpace(color))
            {
                return DefaultColor;
            }

            string normalized = color.Trim();

            if (normalized.StartsWith("#"))
            {
                normalized = normalized.Substring(1);
            }

            normalized = normalized.ToUpperInvariant();

            if (normalized.Length == 3)
            {
                normalized = $"{normalized[0]}{normalized[0]}{normalized[1]}{normalized[1]}{normalized[2]}{normalized[2]}";
            }

            if (normalized.Length != 6)
            {
                return DefaultColor;
            }

            foreach (char c in normalized)
            {
                bool isHex =
                    (c >= '0' && c <= '9') ||
                    (c >= 'A' && c <= 'F');

                if (!isHex)
                {
                    return DefaultColor;
                }
            }

            return normalized;
        }

        private bool TryReadPositiveDouble(string text, out double value)
        {
            bool ok = double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);

            if (!ok || value <= 0)
            {
                value = 0;
                return false;
            }

            return true;
        }

        private bool TryReadDouble(string text, out double value)
        {
            return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        private List<string>? ReadCollisionGroupFromUi()
        {
            List<string>? textValues = ParseCollisionText(CGroupTextBox.Text);
            if (textValues != null)
            {
                return textValues;
            }

            List<string> values = new();

            if (CGroupBallCheckBox.IsChecked == true) values.Add("ball");
            if (CGroupRedCheckBox.IsChecked == true) values.Add("red");
            if (CGroupBlueCheckBox.IsChecked == true) values.Add("blue");
            if (CGroupWallCheckBox.IsChecked == true) values.Add("wall");

            return values.Count > 0 ? values : null;
        }

        private List<string>? ReadCollisionMaskFromUi()
        {
            List<string>? textValues = ParseCollisionText(CMaskTextBox.Text);
            if (textValues != null)
            {
                return textValues;
            }

            List<string> values = new();

            if (CMaskBallCheckBox.IsChecked == true) values.Add("ball");
            if (CMaskRedCheckBox.IsChecked == true) values.Add("red");
            if (CMaskBlueCheckBox.IsChecked == true) values.Add("blue");
            if (CMaskWallCheckBox.IsChecked == true) values.Add("wall");

            return values.Count > 0 ? values : null;
        }

        private List<string>? ParseCollisionText(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            List<string> values = new();
            string[] parts = text.Split(new[] { ',', ' ', ';', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string part in parts)
            {
                string value = part.Trim();
                if (value.Length > 0 && !values.Contains(value))
                {
                    values.Add(value);
                }
            }

            return values.Count > 0 ? values : null;
        }

        private string FormatCollisionText(List<string>? values)
        {
            return values == null || values.Count == 0 ? "" : string.Join(", ", values);
        }

        private void SetCollisionUiFromData(List<string>? cGroup, List<string>? cMask)
        {
            isUpdatingUiFromData = true;

            try
            {
                CGroupBallCheckBox.IsChecked = cGroup != null && cGroup.Contains("ball");
                CGroupRedCheckBox.IsChecked = cGroup != null && cGroup.Contains("red");
                CGroupBlueCheckBox.IsChecked = cGroup != null && cGroup.Contains("blue");
                CGroupWallCheckBox.IsChecked = cGroup != null && cGroup.Contains("wall");

                CMaskBallCheckBox.IsChecked = cMask != null && cMask.Contains("ball");
                CMaskRedCheckBox.IsChecked = cMask != null && cMask.Contains("red");
                CMaskBlueCheckBox.IsChecked = cMask != null && cMask.Contains("blue");
                CMaskWallCheckBox.IsChecked = cMask != null && cMask.Contains("wall");

                CGroupTextBox.Text = FormatCollisionText(cGroup);
                CMaskTextBox.Text = FormatCollisionText(cMask);
            }
            finally
            {
                isUpdatingUiFromData = false;
            }
        }

        private void ApplyJsonToEditor()
        {
            TryApplyJsonPreviewToEditor("Apply JSON");
        }

        private bool TryApplyJsonPreviewToEditor(string actionName)
        {
            try
            {
                JsonSerializerOptions options = new()
                {
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true,
                    PropertyNameCaseInsensitive = true
                };

                ExportStadiumData? jsonData = JsonSerializer.Deserialize<ExportStadiumData>(JsonPreviewTextBox.Text, options);

                if (jsonData == null)
                {
                    MessageBox.Show("JSON data could not be parsed.", "JSON Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    UpdateStatus($"{actionName} failed.");
                    return false;
                }

                PushUndoState(actionName);
                LoadExportDataIntoEditor(jsonData);

                UpdateBackgroundUiFromData();
                UpdateJsonPreview();
                UpdateObjectCount();
                UpdateObjectsList();
                RenderStadium();

                jsonPreviewUserEdited = false;
                UpdateStatus(actionName == "Save" ? "JSON changes applied before save." : "JSON applied to editor.");
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "JSON Error", MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateStatus($"{actionName} failed.");
                return false;
            }
        }

        private void OpenStadium()
        {
            OpenFileDialog openFileDialog = new()
            {
                Title = "Open HaxBall Stadium",
                Filter = "HaxBall Stadium (*.hbs)|*.hbs|JSON File (*.json)|*.json|All Files (*.*)|*.*",
                DefaultExt = ".hbs"
            };

            bool? result = openFileDialog.ShowDialog();

            if (result != true)
            {
                UpdateStatus("Open cancelled.");
                return;
            }

            try
            {
                string json = File.ReadAllText(openFileDialog.FileName);

                JsonSerializerOptions options = new()
                {
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true,
                    PropertyNameCaseInsensitive = true
                };

                ExportStadiumData? loadedData = JsonSerializer.Deserialize<ExportStadiumData>(json, options);

                if (loadedData == null)
                {
                    MessageBox.Show("The selected file could not be loaded.", "Open Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    UpdateStatus("Open failed.");
                    return;
                }

                LoadExportDataIntoEditor(loadedData);

                currentFilePath = openFileDialog.FileName;
                undoStack.Clear();
                redoStack.Clear();
                hiddenObjectKeys.Clear();
                lockedObjectKeys.Clear();

                UpdateBackgroundUiFromData();
                UpdateJsonPreview();
                UpdateObjectCount();
                UpdateObjectsList();

                UpdateStatus($"Opened: {System.IO.Path.GetFileName(currentFilePath)}");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Open Error", MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateStatus("Open failed.");
            }
        }

        private void LoadExportDataIntoEditor(ExportStadiumData loadedData)
        {
            CancelAllDrags();

            stadium = new StadiumData
            {
                Name = loadedData.Name,
                Width = loadedData.Width,
                Height = loadedData.Height,
                SpawnDistance = loadedData.SpawnDistance,
                Bg = loadedData.Bg ?? new BgData(),
                Vertexes = new List<VertexData>(),
                Segments = loadedData.Segments ?? new List<SegmentData>(),
                Goals = new List<GoalData>(),
                Discs = new List<DiscData>(),
                Planes = loadedData.Planes ?? new List<PlaneData>(),
                Joints = loadedData.Joints ?? new List<JointData>(),
                RedSpawnPoints = new List<SpawnPointData>(),
                BlueSpawnPoints = new List<SpawnPointData>(),
                Traits = loadedData.Traits ?? CreateDefaultTraits(),
                ExtensionData = loadedData.ExtensionData
            };

            EnsureDefaultBackground();

            double centerX = GetCanvasCenterX();
            double centerY = GetCanvasCenterY();

            if (loadedData.Vertexes != null)
            {
                foreach (ExportVertexData exportVertex in loadedData.Vertexes)
                {
                    stadium.Vertexes.Add(new VertexData
                    {
                        X = exportVertex.X + centerX,
                        Y = exportVertex.Y + centerY,
                        ExtensionData = CloneExtensionData(exportVertex.ExtensionData)
                    });
                }
            }

            if (loadedData.Goals != null)
            {
                foreach (ExportGoalData exportGoal in loadedData.Goals)
                {
                    if (exportGoal.P0 == null || exportGoal.P0.Count < 2) continue;
                    if (exportGoal.P1 == null || exportGoal.P1.Count < 2) continue;

                    stadium.Goals.Add(new GoalData
                    {
                        X0 = exportGoal.P0[0] + centerX,
                        Y0 = exportGoal.P0[1] + centerY,
                        X1 = exportGoal.P1[0] + centerX,
                        Y1 = exportGoal.P1[1] + centerY,
                        Team = NormalizeGoalTeam(exportGoal.Team),
                        ExtensionData = CloneExtensionData(exportGoal.ExtensionData)
                    });
                }
            }

            if (loadedData.Discs != null)
            {
                foreach (ExportDiscData exportDisc in loadedData.Discs)
                {
                    double haxX = 0;
                    double haxY = 0;

                    if (exportDisc.Pos != null && exportDisc.Pos.Count >= 2)
                    {
                        haxX = exportDisc.Pos[0];
                        haxY = exportDisc.Pos[1];
                    }

                    stadium.Discs.Add(new DiscData
                    {
                        X = haxX + centerX,
                        Y = haxY + centerY,
                        Radius = exportDisc.Radius,
                        Color = exportDisc.Color,
                        BCoef = exportDisc.BCoef,
                        InvMass = exportDisc.InvMass,
                        CGroup = exportDisc.CGroup,
                        CMask = exportDisc.CMask,
                        ExtensionData = CloneExtensionData(exportDisc.ExtensionData)
                    });
                }
            }

            if (loadedData.RedSpawnPoints != null)
            {
                foreach (List<double> spawn in loadedData.RedSpawnPoints)
                {
                    if (spawn.Count >= 2)
                    {
                        stadium.RedSpawnPoints.Add(new SpawnPointData
                        {
                            X = spawn[0] + centerX,
                            Y = spawn[1] + centerY
                        });
                    }
                }
            }

            if (loadedData.BlueSpawnPoints != null)
            {
                foreach (List<double> spawn in loadedData.BlueSpawnPoints)
                {
                    if (spawn.Count >= 2)
                    {
                        stadium.BlueSpawnPoints.Add(new SpawnPointData
                        {
                            X = spawn[0] + centerX,
                            Y = spawn[1] + centerY
                        });
                    }
                }
            }

            ClearSelection();
            RenderStadium();
        }

        private Dictionary<string, JsonElement>? CloneExtensionData(Dictionary<string, JsonElement>? source)
        {
            if (source == null)
            {
                return null;
            }

            Dictionary<string, JsonElement> clone = new();

            foreach (KeyValuePair<string, JsonElement> item in source)
            {
                clone[item.Key] = item.Value.Clone();
            }

            return clone;
        }

        private string GetExtensionString(Dictionary<string, JsonElement>? extensionData, string key)
        {
            if (extensionData == null || !extensionData.TryGetValue(key, out JsonElement value))
            {
                return "";
            }

            if (value.ValueKind == JsonValueKind.String)
            {
                return value.GetString() ?? "";
            }

            return value.ToString();
        }

        private string GetExtensionDoubleString(Dictionary<string, JsonElement>? extensionData, string key)
        {
            if (extensionData == null || !extensionData.TryGetValue(key, out JsonElement value))
            {
                return "";
            }

            if (value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out double number))
            {
                return number.ToString("0.##", CultureInfo.InvariantCulture);
            }

            return value.ToString();
        }

        private bool? GetExtensionBool(Dictionary<string, JsonElement>? extensionData, string key)
        {
            if (extensionData == null || !extensionData.TryGetValue(key, out JsonElement value))
            {
                return null;
            }

            if (value.ValueKind == JsonValueKind.True) return true;
            if (value.ValueKind == JsonValueKind.False) return false;

            if (value.ValueKind == JsonValueKind.String && bool.TryParse(value.GetString(), out bool parsed))
            {
                return parsed;
            }

            return null;
        }

        private Dictionary<string, JsonElement>? SetExtensionString(Dictionary<string, JsonElement>? extensionData, string key, string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                if (extensionData != null) extensionData.Remove(key);
                return extensionData != null && extensionData.Count > 0 ? extensionData : null;
            }

            extensionData ??= new Dictionary<string, JsonElement>();
            extensionData[key] = JsonSerializer.SerializeToElement(value.Trim());
            return extensionData;
        }

        private Dictionary<string, JsonElement>? SetExtensionDouble(Dictionary<string, JsonElement>? extensionData, string key, string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                if (extensionData != null) extensionData.Remove(key);
                return extensionData != null && extensionData.Count > 0 ? extensionData : null;
            }

            if (!TryReadDouble(value, out double number))
            {
                throw new FormatException($"Invalid {key} value.");
            }

            extensionData ??= new Dictionary<string, JsonElement>();
            extensionData[key] = JsonSerializer.SerializeToElement(number);
            return extensionData;
        }

        private string GetExtensionVectorComponentString(Dictionary<string, JsonElement>? extensionData, string key, int index)
        {
            if (extensionData == null || !extensionData.TryGetValue(key, out JsonElement value))
            {
                return "";
            }

            if (value.ValueKind == JsonValueKind.Array && value.GetArrayLength() > index)
            {
                JsonElement item = value[index];
                if (item.ValueKind == JsonValueKind.Number && item.TryGetDouble(out double number))
                {
                    return number.ToString("0.##", CultureInfo.InvariantCulture);
                }

                return item.ToString();
            }

            return "";
        }

        private Dictionary<string, JsonElement>? SetExtensionVector2(Dictionary<string, JsonElement>? extensionData, string key, string? xText, string? yText)
        {
            bool xEmpty = string.IsNullOrWhiteSpace(xText);
            bool yEmpty = string.IsNullOrWhiteSpace(yText);

            if (xEmpty && yEmpty)
            {
                if (extensionData != null) extensionData.Remove(key);
                return extensionData != null && extensionData.Count > 0 ? extensionData : null;
            }

            if (xEmpty || yEmpty || !TryReadDouble(xText ?? "", out double x) || !TryReadDouble(yText ?? "", out double y))
            {
                throw new FormatException($"Invalid {key} value. Use both X and Y numbers, or leave both empty.");
            }

            extensionData ??= new Dictionary<string, JsonElement>();
            extensionData[key] = JsonSerializer.SerializeToElement(new List<double> { x, y });
            return extensionData;
        }

        private Dictionary<string, JsonElement>? SetExtensionBool(Dictionary<string, JsonElement>? extensionData, string key, bool? value)
        {
            if (value == null)
            {
                if (extensionData != null) extensionData.Remove(key);
                return extensionData != null && extensionData.Count > 0 ? extensionData : null;
            }

            extensionData ??= new Dictionary<string, JsonElement>();
            extensionData[key] = JsonSerializer.SerializeToElement(value.Value);
            return extensionData;
        }

        private bool? ReadVisFromUi()
        {
            if (VisComboBox.SelectedItem is ComboBoxItem item && item.Content != null)
            {
                string text = item.Content.ToString()!.Trim().ToLowerInvariant();
                if (text == "true") return true;
                if (text == "false") return false;
            }

            return null;
        }

        private void SetVisComboBoxFromData(bool? value)
        {
            isUpdatingUiFromData = true;
            try
            {
                VisComboBox.SelectedIndex = value == null ? 0 : value.Value ? 1 : 2;
            }
            finally
            {
                isUpdatingUiFromData = false;
            }
        }

        private void InitializeAutoSaveTimer()
        {
            autoSaveTimer.Interval = TimeSpan.FromSeconds(autoSaveIntervalSeconds);
            autoSaveTimer.Tick += AutoSaveTimer_Tick;
        }

        private void UpdateAutoSaveTimerState()
        {
            autoSaveTimer.Interval = TimeSpan.FromSeconds(autoSaveIntervalSeconds);

            if (autoSaveEnabled)
            {
                if (!autoSaveTimer.IsEnabled)
                {
                    autoSaveTimer.Start();
                }
            }
            else
            {
                autoSaveTimer.Stop();
            }
        }

        private void AutoSaveTimer_Tick(object? sender, EventArgs e)
        {
            RunAutoSaveBackup(false);
        }

        private void RunAutoSaveBackup(bool force)
        {
            if (!autoSaveEnabled && !force)
            {
                return;
            }

            if (!force && !hasUnsavedChangesForAutoSave)
            {
                return;
            }

            if (isRestoringHistory || isDraggingSelectedItems || isDraggingSegment || isDraggingGoal || isDraggingPlane ||
                isDraggingVertex || isDraggingDisc || isDraggingRedSpawn || isDraggingBlueSpawn || isDraggingCurveHandle ||
                isDraggingGoalEndpoint || isDraggingSelectionRectangle)
            {
                return;
            }

            try
            {
                if (jsonPreviewUserEdited)
                {
                    TryApplyJsonPreviewToEditor("AutoSave");
                }
                else
                {
                    ApplyBackgroundSettingsFromUi();
                }

                string autoSavePath = GetAutoSaveFilePath();
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(autoSavePath)!);
                File.WriteAllText(autoSavePath, BuildStadiumJson());

                lastAutoSaveTime = DateTime.Now;
                hasUnsavedChangesForAutoSave = false;
                UpdateStatus($"AutoSaved backup: {System.IO.Path.GetFileName(autoSavePath)}");
            }
            catch (Exception ex)
            {
                UpdateStatus($"AutoSave failed: {ex.Message}");
            }
        }

        private string GetAutoSaveFilePath()
        {
            string autoSaveDirectory = GetAutoSaveDirectory();
            string fileName = !string.IsNullOrWhiteSpace(currentFilePath)
                ? System.IO.Path.GetFileNameWithoutExtension(currentFilePath)
                : MakeSafeFileName(string.IsNullOrWhiteSpace(stadium.Name) ? "New Stadium" : stadium.Name);

            return System.IO.Path.Combine(autoSaveDirectory, $"{fileName}.autosave.hbs");
        }

        private string GetAutoSaveDirectory()
        {
            if (!string.IsNullOrWhiteSpace(customAutoSaveFolderPath))
            {
                return customAutoSaveFolderPath;
            }

            if (!string.IsNullOrWhiteSpace(currentFilePath))
            {
                return System.IO.Path.GetDirectoryName(currentFilePath) ?? GetDefaultUnsavedAutoSaveDirectory();
            }

            return GetDefaultUnsavedAutoSaveDirectory();
        }

        private string GetDefaultUnsavedAutoSaveDirectory()
        {
            return System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "HaxStudio",
                "AutoSave");
        }

        private string GetAutoSaveFolderDisplayText()
        {
            if (!string.IsNullOrWhiteSpace(customAutoSaveFolderPath))
            {
                return customAutoSaveFolderPath;
            }

            return $"Default: {GetAutoSaveDirectory()}";
        }

        private void BrowseAutoSaveFolder(TextBox targetTextBox)
        {
            try
            {
                OpenFolderDialog dialog = new()
                {
                    Title = "Choose AutoSave Folder",
                    InitialDirectory = Directory.Exists(GetAutoSaveDirectory())
                        ? GetAutoSaveDirectory()
                        : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                };

                bool? result = dialog.ShowDialog(this);

                if (result != true || string.IsNullOrWhiteSpace(dialog.FolderName))
                {
                    UpdateStatus("AutoSave folder selection cancelled.");
                    return;
                }

                customAutoSaveFolderPath = dialog.FolderName;
                SaveEditorPreferences();
                targetTextBox.Text = GetAutoSaveFolderDisplayText();
                UpdateStatus($"AutoSave folder set: {customAutoSaveFolderPath}");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "AutoSave Folder Error", MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateStatus("AutoSave folder selection failed.");
            }
        }

        private void LoadEditorPreferences()
        {
            try
            {
                string path = GetEditorPreferencesFilePath();
                if (!File.Exists(path))
                {
                    return;
                }

                string json = File.ReadAllText(path);
                EditorPreferences? preferences = JsonSerializer.Deserialize<EditorPreferences>(json);

                if (preferences == null)
                {
                    return;
                }

                autoSaveEnabled = preferences.AutoSaveEnabled ?? autoSaveEnabled;
                validationWarningBeforeSaveEnabled = preferences.ValidationWarningBeforeSaveEnabled ?? validationWarningBeforeSaveEnabled;
                validationPanelAutoRefreshEnabled = preferences.ValidationPanelAutoRefreshEnabled ?? validationPanelAutoRefreshEnabled;

                showViewportGrid = preferences.ShowViewportGrid ?? showViewportGrid;
                showViewportVertexes = preferences.ShowViewportVertexes ?? showViewportVertexes;
                showViewportSegments = preferences.ShowViewportSegments ?? showViewportSegments;
                showViewportDiscs = preferences.ShowViewportDiscs ?? showViewportDiscs;
                showViewportPlanes = preferences.ShowViewportPlanes ?? showViewportPlanes;
                showViewportGrassStripes = preferences.ShowViewportGrassStripes ?? showViewportGrassStripes;
                showViewportInvisibleObjects = preferences.ShowViewportInvisibleObjects ?? showViewportInvisibleObjects;
                autoMirrorPlacement = preferences.AutoMirrorPlacement ?? autoMirrorPlacement;

                snapToGrid = preferences.SnapToGrid ?? snapToGrid;
                snapGridSize = preferences.SnapGridSize ?? snapGridSize;
                viewportVertexSize = string.IsNullOrWhiteSpace(preferences.ViewportVertexSize) ? viewportVertexSize : preferences.ViewportVertexSize;

                savedLeftPanelWidth = preferences.LeftPanelWidth ?? savedLeftPanelWidth;
                savedRightPanelWidth = preferences.RightPanelWidth ?? savedRightPanelWidth;
                savedBottomPanelHeight = preferences.BottomPanelHeight ?? savedBottomPanelHeight;
                savedWindowWidth = preferences.WindowWidth ?? savedWindowWidth;
                savedWindowHeight = preferences.WindowHeight ?? savedWindowHeight;

                if (!string.IsNullOrWhiteSpace(preferences.CustomAutoSaveFolderPath))
                {
                    customAutoSaveFolderPath = preferences.CustomAutoSaveFolderPath;
                }

                if (preferences.PanelDockStates != null)
                {
                    foreach (KeyValuePair<string, string> item in preferences.PanelDockStates)
                    {
                        if (panelDockStates.ContainsKey(item.Key))
                        {
                            panelDockStates[item.Key] = NormalizePanelDockState(item.Value);
                        }
                    }
                }
            }
            catch
            {
                // Preferences are non-critical. If loading fails, keep defaults.
            }
        }

        private void SaveEditorPreferences()
        {
            if (isApplyingPreferences)
            {
                return;
            }

            try
            {
                CaptureLayoutPreferences();

                string path = GetEditorPreferencesFilePath();
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);

                EditorPreferences preferences = new()
                {
                    AutoSaveEnabled = autoSaveEnabled,
                    CustomAutoSaveFolderPath = customAutoSaveFolderPath,
                    ValidationWarningBeforeSaveEnabled = validationWarningBeforeSaveEnabled,
                    ValidationPanelAutoRefreshEnabled = validationPanelAutoRefreshEnabled,

                    ShowViewportGrid = showViewportGrid,
                    ShowViewportVertexes = showViewportVertexes,
                    ShowViewportSegments = showViewportSegments,
                    ShowViewportDiscs = showViewportDiscs,
                    ShowViewportPlanes = showViewportPlanes,
                    ShowViewportGrassStripes = showViewportGrassStripes,
                    ShowViewportInvisibleObjects = showViewportInvisibleObjects,
                    AutoMirrorPlacement = autoMirrorPlacement,

                    SnapToGrid = snapToGrid,
                    SnapGridSize = snapGridSize,
                    ViewportVertexSize = viewportVertexSize,

                    LeftPanelWidth = savedLeftPanelWidth,
                    RightPanelWidth = savedRightPanelWidth,
                    BottomPanelHeight = savedBottomPanelHeight,
                    WindowWidth = savedWindowWidth,
                    WindowHeight = savedWindowHeight,
                    PanelDockStates = new Dictionary<string, string>(panelDockStates)
                };

                JsonSerializerOptions options = new()
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };

                File.WriteAllText(path, JsonSerializer.Serialize(preferences, options));
            }
            catch
            {
                UpdateStatus("Preferences could not be saved.");
            }
        }

        private void CaptureLayoutPreferences()
        {
            if (LeftPanelColumn.ActualWidth > 0)
            {
                savedLeftPanelWidth = LeftPanelColumn.ActualWidth;
            }

            if (RightPanelColumn.ActualWidth > 0)
            {
                savedRightPanelWidth = RightPanelColumn.ActualWidth;
            }

            if (BottomPanelRow.ActualHeight > 0)
            {
                savedBottomPanelHeight = BottomPanelRow.ActualHeight;
            }

            if (Width > 0)
            {
                savedWindowWidth = Width;
            }

            if (Height > 0)
            {
                savedWindowHeight = Height;
            }
        }

        private void ApplySavedWindowMetrics()
        {
            if (savedWindowWidth >= MinWidth)
            {
                Width = savedWindowWidth;
            }

            if (savedWindowHeight >= MinHeight)
            {
                Height = savedWindowHeight;
            }

            if (savedLeftPanelWidth > 0)
            {
                LeftPanelColumn.Width = new GridLength(savedLeftPanelWidth);
            }

            if (savedRightPanelWidth > 0)
            {
                RightPanelColumn.Width = new GridLength(savedRightPanelWidth);
            }

            if (savedBottomPanelHeight > 0)
            {
                BottomPanelRow.Height = new GridLength(savedBottomPanelHeight);
            }
        }

        private static string NormalizePanelDockState(string? state)
        {
            return state switch
            {
                "Left" => "Left",
                "Right" => "Right",
                "Bottom" => "Bottom",
                "Floating" => "Floating",
                _ => "Hidden"
            };
        }

        private string GetEditorPreferencesFilePath()
        {
            return System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "HaxStudio",
                "settings.json");
        }

        private void SaveAsStadium()
        {
            SaveFileDialog saveFileDialog = new()
            {
                Title = "Save HaxBall Stadium",
                Filter = "HaxBall Stadium (*.hbs)|*.hbs|JSON File (*.json)|*.json|All Files (*.*)|*.*",
                DefaultExt = ".hbs",
                AddExtension = true,
                FileName = "new_stadium.hbs"
            };

            bool? result = saveFileDialog.ShowDialog();

            if (result != true)
            {
                UpdateStatus("Save cancelled.");
                return;
            }

            currentFilePath = saveFileDialog.FileName;
            SaveStadiumToFile(currentFilePath);
        }

        private void SaveStadiumToFile(string filePath)
        {
            if (ShouldCancelSaveDueToValidation())
            {
                UpdateStatus("Save cancelled by validator.");
                return;
            }

            try
            {
                string json = BuildStadiumJson();
                File.WriteAllText(filePath, json);

                hasUnsavedChangesForAutoSave = false;

                if (validationPanelAutoRefreshEnabled)
                {
                    RefreshValidationPanel(false);
                }

                UpdateStatus($"Saved: {System.IO.Path.GetFileName(filePath)}");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateStatus("Save failed.");
            }
        }

        private string BuildStadiumJson()
        {
            ExportStadiumData exportData = CreateExportStadiumData();

            JsonSerializerOptions options = new()
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            return JsonSerializer.Serialize(exportData, options);
        }

        private ExportStadiumData CreateExportStadiumData()
        {
            EnsureDefaultBackground();

            ExportStadiumData exportData = new()
            {
                Name = stadium.Name,
                Width = stadium.Width,
                Height = stadium.Height,
                SpawnDistance = stadium.SpawnDistance,
                Bg = stadium.Bg ?? new BgData(),
                Vertexes = new List<ExportVertexData>(),
                Segments = new List<SegmentData>(),
                Goals = new List<ExportGoalData>(),
                Discs = new List<ExportDiscData>(),
                Planes = stadium.Planes ?? new List<PlaneData>(),
                Joints = stadium.Joints ?? new List<JointData>(),
                RedSpawnPoints = new List<List<double>>(),
                BlueSpawnPoints = new List<List<double>>(),
                Traits = stadium.Traits ?? CreateDefaultTraits(),
                ExtensionData = CloneExtensionData(stadium.ExtensionData)
            };

            double centerX = GetCanvasCenterX();
            double centerY = GetCanvasCenterY();

            foreach (VertexData vertex in stadium.Vertexes)
            {
                exportData.Vertexes.Add(new ExportVertexData
                {
                    X = Math.Round(vertex.X - centerX, 2),
                    Y = Math.Round(vertex.Y - centerY, 2),
                    ExtensionData = CloneExtensionData(vertex.ExtensionData)
                });
            }

            foreach (SegmentData segment in stadium.Segments)
            {
                exportData.Segments.Add(new SegmentData
                {
                    V0 = segment.V0,
                    V1 = segment.V1,
                    Color = segment.Color,
                    Curve = segment.Curve,
                    BCoef = segment.BCoef,
                    CGroup = segment.CGroup,
                    CMask = segment.CMask,
                    ExtensionData = CloneExtensionData(segment.ExtensionData)
                });
            }

            foreach (GoalData goal in stadium.Goals)
            {
                exportData.Goals.Add(new ExportGoalData
                {
                    P0 = new List<double>
                    {
                        Math.Round(goal.X0 - centerX, 2),
                        Math.Round(goal.Y0 - centerY, 2)
                    },
                    P1 = new List<double>
                    {
                        Math.Round(goal.X1 - centerX, 2),
                        Math.Round(goal.Y1 - centerY, 2)
                    },
                    Team = NormalizeGoalTeam(goal.Team),
                    ExtensionData = CloneExtensionData(goal.ExtensionData)
                });
            }

            foreach (DiscData disc in stadium.Discs)
            {
                exportData.Discs.Add(new ExportDiscData
                {
                    Pos = new List<double>
                    {
                        Math.Round(disc.X - centerX, 2),
                        Math.Round(disc.Y - centerY, 2)
                    },
                    Radius = disc.Radius,
                    Color = disc.Color,
                    BCoef = disc.BCoef,
                    InvMass = disc.InvMass,
                    CGroup = disc.CGroup,
                    CMask = disc.CMask,
                    ExtensionData = CloneExtensionData(disc.ExtensionData)
                });
            }

            foreach (SpawnPointData spawn in stadium.RedSpawnPoints)
            {
                exportData.RedSpawnPoints.Add(new List<double>
                {
                    Math.Round(spawn.X - centerX, 2),
                    Math.Round(spawn.Y - centerY, 2)
                });
            }

            foreach (SpawnPointData spawn in stadium.BlueSpawnPoints)
            {
                exportData.BlueSpawnPoints.Add(new List<double>
                {
                    Math.Round(spawn.X - centerX, 2),
                    Math.Round(spawn.Y - centerY, 2)
                });
            }

            return exportData;
        }

        private Dictionary<string, TraitData> CreateDefaultTraits()
        {
            return new Dictionary<string, TraitData>
            {
                {
                    "ballArea",
                    new TraitData
                    {
                        Vis = false,
                        BCoef = 1,
                        CMask = new List<string> { "ball" }
                    }
                },
                {
                    "goalPost",
                    new TraitData
                    {
                        Radius = 8,
                        InvMass = 0,
                        BCoef = 0.5
                    }
                },
                {
                    "goalNet",
                    new TraitData
                    {
                        Vis = true,
                        BCoef = 0.1,
                        CMask = new List<string> { "ball" }
                    }
                },
                {
                    "kickOffBarrier",
                    new TraitData
                    {
                        Vis = false,
                        BCoef = 0.1,
                        CGroup = new List<string> { "redKO", "blueKO" },
                        CMask = new List<string> { "red", "blue" }
                    }
                }
            };
        }

        private void MapCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            MapCanvas.Focus();

            Point mousePos = e.GetPosition(MapCanvas);
            Point dataMousePos = ScreenToDataPoint(mousePos);

            if (currentTool == "Select" && !Keyboard.IsKeyDown(Key.Space))
            {
                // Start rectangle selection only when the click begins on empty viewport space.
                // Without this guard, clicking a segment/goal/plane first selects it, then the Canvas
                // also starts a tiny selection rectangle and clears the Inspector on mouse up.
                if (ReferenceEquals(e.OriginalSource, MapCanvas))
                {
                    BeginSelectionRectangle(mousePos, false);
                    e.Handled = true;
                }

                return;
            }

            if (currentTool == "AddVertex")
            {
                AddVertex(dataMousePos.X, dataMousePos.Y);
                return;
            }

            if (currentTool == "AddSegment")
            {
                BeginSegmentDrag(dataMousePos, null);
                return;
            }

            if (currentTool == "AddDisc")
            {
                AddDisc(dataMousePos.X, dataMousePos.Y, DefaultDiscRadius);
                return;
            }

            if (currentTool == "AddGoal")
            {
                BeginGoalDrag(dataMousePos);
                return;
            }

            if (currentTool == "AddPlane")
            {
                BeginPlaneDrag(dataMousePos);
                return;
            }

            if (currentTool == "AddRedSpawn")
            {
                AddRedSpawn(dataMousePos.X, dataMousePos.Y);
                return;
            }

            if (currentTool == "AddBlueSpawn")
            {
                AddBlueSpawn(dataMousePos.X, dataMousePos.Y);
                return;
            }
        }

        private void MapCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (isDraggingSelectionRectangle)
            {
                FinishSelectionRectangle(e.GetPosition(MapCanvas));
                return;
            }

            if (isDraggingSelectedItems)
            {
                FinishSelectedItemsDrag();
                return;
            }

            if (isDraggingGoalEndpoint)
            {
                FinishGoalEndpointDrag();
                return;
            }

            if (isDraggingCurveHandle)
            {
                FinishCurveHandleDrag();
                return;
            }

            if (isDraggingRedSpawn)
            {
                FinishRedSpawnDrag();
                return;
            }

            if (isDraggingBlueSpawn)
            {
                FinishBlueSpawnDrag();
                return;
            }

            if (isDraggingDisc)
            {
                FinishDiscDrag();
                return;
            }

            if (isDraggingVertex)
            {
                FinishVertexDrag();
                return;
            }

            if (isDraggingSegment)
            {
                FinishSegmentDrag(ScreenToDataPoint(e.GetPosition(MapCanvas)));
                return;
            }

            if (isDraggingGoal)
            {
                FinishGoalDrag(ScreenToDataPoint(e.GetPosition(MapCanvas)));
                return;
            }

            if (isDraggingPlane)
            {
                FinishPlaneDrag(ScreenToDataPoint(e.GetPosition(MapCanvas)));
                return;
            }
        }

        private void MapCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            Point mousePos = e.GetPosition(MapCanvas);

            if (isPanningViewport)
            {
                Vector delta = mousePos - viewportPanStartMouse;
                viewportPanX = viewportPanStartX + delta.X;
                viewportPanY = viewportPanStartY + delta.Y;
                UpdateViewportInfo();
                RenderStadium();
                return;
            }

            if (isDraggingSelectionRectangle)
            {
                UpdateSelectionRectangle(mousePos);
                return;
            }

            Point dataMousePos = ScreenToDataPoint(mousePos);

            if (isDraggingSelectedItems)
            {
                DragSelectedItems(dataMousePos);
                return;
            }

            double haxX = dataMousePos.X - GetCanvasCenterX();
            double haxY = dataMousePos.Y - GetCanvasCenterY();
            MousePositionText.Text = $"HaxBall X: {haxX:0}  Y: {haxY:0}";

            if (isDraggingGoalEndpoint && draggingGoalEndpointGoalIndex != null)
            {
                DragGoalEndpoint(dataMousePos);
                return;
            }

            if (isDraggingCurveHandle && draggingCurveSegmentIndex != null)
            {
                DragCurveHandle(dataMousePos);
                return;
            }

            if (isDraggingRedSpawn && draggingRedSpawnIndex != null)
            {
                DragSelectedRedSpawn(dataMousePos);
                return;
            }

            if (isDraggingBlueSpawn && draggingBlueSpawnIndex != null)
            {
                DragSelectedBlueSpawn(dataMousePos);
                return;
            }

            if (isDraggingDisc && draggingDiscIndex != null)
            {
                DragSelectedDisc(dataMousePos);
                return;
            }

            if (isDraggingVertex && draggingVertexIndex != null)
            {
                DragSelectedVertex(dataMousePos);
                return;
            }

            if (isDraggingSegment && segmentPreviewLine != null)
            {
                segmentPreviewLine.X2 = mousePos.X;
                segmentPreviewLine.Y2 = mousePos.Y;
                return;
            }

            if (isDraggingGoal && goalPreviewLine != null)
            {
                goalPreviewLine.X2 = mousePos.X;
                goalPreviewLine.Y2 = mousePos.Y;
                return;
            }

            if (isDraggingPlane && planePreviewLine != null)
            {
                planePreviewLine.X2 = mousePos.X;
                planePreviewLine.Y2 = mousePos.Y;
            }
        }

        private int AddVertex(double x, double y)
        {
            PushUndoState(autoMirrorPlacement ? "Add Mirrored Vertex" : "Add Vertex");
            Point snapped = SnapDataPoint(new Point(x, y));
            x = snapped.X;
            y = snapped.Y;

            stadium.Vertexes.Add(new VertexData { X = x, Y = y });

            int index = stadium.Vertexes.Count - 1;

            if (autoMirrorPlacement)
            {
                double mirrorX = MirrorCanvasX(x);
                if (Math.Abs(mirrorX - x) > 0.001)
                {
                    stadium.Vertexes.Add(new VertexData { X = mirrorX, Y = y });
                    int mirrorIndex = stadium.Vertexes.Count - 1;
                    SelectMirroredPair("Vertex", index, mirrorIndex);
                    UpdateStatus($"Vertex #{index} added with mirrored Vertex #{mirrorIndex}.");
                }
                else
                {
                    SelectVertex(index);
                    UpdateStatus($"Vertex #{index} added on mirror axis.");
                }
            }
            else
            {
                SelectVertex(index);
                UpdateStatus($"Vertex #{index} added.");
            }

            RenderStadium();
            UpdateObjectCount();
            UpdateObjectsList();
            UpdateJsonPreview();

            return index;
        }

        private double GetViewportVertexVisualSize()
        {
            return viewportVertexSize switch
            {
                "Small" => 4,
                "Large" => 7,
                _ => 5
            };
        }

        private Brush SelectionAccentBrush => new SolidColorBrush(Color.FromRgb(255, 230, 64));
        private Brush SelectionAccentSoftBrush => new SolidColorBrush(Color.FromArgb(95, 255, 230, 64));
        private Brush SelectionCyanBrush => new SolidColorBrush(Color.FromRgb(47, 183, 232));

        private void DrawSelectionCircle(Point center, double radius, int zIndex, Brush? stroke = null, double thickness = 2, DoubleCollection? dash = null)
        {
            Ellipse circle = new()
            {
                Width = radius * 2,
                Height = radius * 2,
                Fill = Brushes.Transparent,
                Stroke = stroke ?? SelectionAccentBrush,
                StrokeThickness = thickness,
                StrokeDashArray = dash,
                IsHitTestVisible = false
            };

            Canvas.SetLeft(circle, center.X - radius);
            Canvas.SetTop(circle, center.Y - radius);
            Panel.SetZIndex(circle, zIndex);
            MapCanvas.Children.Add(circle);
        }

        private void DrawSelectionFilledDot(Point center, double radius, int zIndex, Brush? fill = null, Brush? stroke = null)
        {
            Ellipse dot = new()
            {
                Width = radius * 2,
                Height = radius * 2,
                Fill = fill ?? SelectionAccentBrush,
                Stroke = stroke ?? Brushes.White,
                StrokeThickness = 1.5,
                IsHitTestVisible = false
            };

            Canvas.SetLeft(dot, center.X - radius);
            Canvas.SetTop(dot, center.Y - radius);
            Panel.SetZIndex(dot, zIndex);
            MapCanvas.Children.Add(dot);
        }

        private void DrawSelectionEndpointMarkers(Point p0, Point p1, int zIndex)
        {
            DrawSelectionFilledDot(p0, 4.5, zIndex, SelectionAccentBrush, Brushes.White);
            DrawSelectionFilledDot(p1, 4.5, zIndex, SelectionAccentBrush, Brushes.White);
        }

        private void DrawSelectionCenterCross(Point center, double size, int zIndex)
        {
            AddViewportLine(
                new Point(center.X - size, center.Y),
                new Point(center.X + size, center.Y),
                Brushes.White,
                1.4,
                zIndex,
                null);

            AddViewportLine(
                new Point(center.X, center.Y - size),
                new Point(center.X, center.Y + size),
                Brushes.White,
                1.4,
                zIndex,
                null);
        }

        private void DrawSelectionNormalArrow(Point start, Vector direction, double length, int zIndex)
        {
            if (direction.Length < 0.0001)
            {
                return;
            }

            direction.Normalize();

            Point end = new(start.X + direction.X * length, start.Y + direction.Y * length);
            AddViewportLine(start, end, SelectionAccentBrush, Math.Max(1.5, ScaleLength(2)), zIndex, null);

            Vector back = -direction;
            Vector side = new(-direction.Y, direction.X);

            Point arrowA = new(end.X + back.X * 10 + side.X * 5, end.Y + back.Y * 10 + side.Y * 5);
            Point arrowB = new(end.X + back.X * 10 - side.X * 5, end.Y + back.Y * 10 - side.Y * 5);

            AddViewportLine(end, arrowA, SelectionAccentBrush, 1.6, zIndex, null);
            AddViewportLine(end, arrowB, SelectionAccentBrush, 1.6, zIndex, null);
        }

        private void DrawVertex(double x, double y, int index)
        {
            if (IsObjectHidden("Vertex", index)) return;
            bool isSelected = selectedVertexIndex == index || IsObjectSelected("Vertex", index);
            Point screenPoint = DataToScreenPoint(x, y);

            // HaxPuck-style vertices: tiny visual dot, larger invisible hit area.
            // The visual size stays constant on screen so zooming does not create huge blue/magenta dots.
            double normalVertexSize = GetViewportVertexVisualSize();
            double selectedVertexSize = normalVertexSize + 3;
            double visualSize = isSelected ? selectedVertexSize : normalVertexSize;
            double visualRadius = visualSize / 2.0;
            double hitSize = Math.Max(14, visualSize + 8);
            double hitRadius = hitSize / 2.0;

            Ellipse hitShape = new()
            {
                Width = hitSize,
                Height = hitSize,
                Fill = Brushes.Transparent,
                Stroke = Brushes.Transparent,
                StrokeThickness = 0,
                Cursor = Cursors.Hand
            };

            Canvas.SetLeft(hitShape, screenPoint.X - hitRadius);
            Canvas.SetTop(hitShape, screenPoint.Y - hitRadius);
            Panel.SetZIndex(hitShape, isSelected ? 31 : 11);

            vertexShapeIndexes[hitShape] = index;
            hitShape.MouseLeftButtonDown += VertexShape_MouseLeftButtonDown;
            MapCanvas.Children.Add(hitShape);

            if (isSelected)
            {
                DrawSelectionCircle(screenPoint, Math.Max(8, visualSize + 5), 30, SelectionAccentSoftBrush, 3);
                DrawSelectionCircle(screenPoint, Math.Max(5, visualSize + 2), 31, SelectionAccentBrush, 1.5);
                DrawSelectionCenterCross(screenPoint, 7, 33);
            }

            Ellipse vertexDot = new()
            {
                Width = visualSize,
                Height = visualSize,
                Fill = isSelected
                    ? new SolidColorBrush(Color.FromRgb(255, 235, 0))
                    : new SolidColorBrush(Color.FromRgb(255, 0, 255)),
                Stroke = isSelected ? Brushes.White : Brushes.Transparent,
                StrokeThickness = isSelected ? 1.2 : 0,
                IsHitTestVisible = false
            };

            Canvas.SetLeft(vertexDot, screenPoint.X - visualRadius);
            Canvas.SetTop(vertexDot, screenPoint.Y - visualRadius);
            Panel.SetZIndex(vertexDot, isSelected ? 32 : 12);

            MapCanvas.Children.Add(vertexDot);
        }

        private void VertexShape_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Ellipse vertexShape) return;
            if (!vertexShapeIndexes.TryGetValue(vertexShape, out int vertexIndex)) return;

            if (currentTool == "Select")
            {
                e.Handled = true;
                Point dataPoint = ScreenToDataPoint(e.GetPosition(MapCanvas));
                if (HandleSelectObjectMouseDown("Vertex", vertexIndex, dataPoint)) return;
                SelectVertex(vertexIndex);
                BeginVertexDrag(vertexIndex, dataPoint);
                UpdateStatus($"Vertex #{vertexIndex} selected.");
                return;
            }

            if (currentTool == "AddSegment")
            {
                e.Handled = true;
                VertexData vertex = stadium.Vertexes[vertexIndex];
                BeginSegmentDrag(new Point(vertex.X, vertex.Y), vertexIndex);
            }
        }

        private void SegmentShape_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Shape segmentShape) return;
            if (!segmentShapeIndexes.TryGetValue(segmentShape, out int segmentIndex)) return;
            if (currentTool != "Select") return;

            e.Handled = true;
            Point dataPoint = ScreenToDataPoint(e.GetPosition(MapCanvas));

            int targetSegmentIndex = segmentIndex;

            if (!IsCtrlPressed() &&
                selectedSegmentIndex != null &&
                selectedSegmentIndex.Value >= 0 &&
                selectedSegmentIndex.Value < stadium.Segments.Count &&
                SegmentsShareSameVertexPair(stadium.Segments[selectedSegmentIndex.Value], stadium.Segments[segmentIndex]))
            {
                targetSegmentIndex = FindNextOverlappingSegmentIndex(selectedSegmentIndex.Value);
            }

            if (HandleSelectObjectMouseDown("Segment", targetSegmentIndex, dataPoint)) return;

            SelectSegment(targetSegmentIndex);
            UpdateInspectorForSelection("Segment");
            BeginSelectedItemsDrag(dataPoint);
            RenderStadium();

            if (targetSegmentIndex != segmentIndex)
            {
                UpdateStatus($"Overlapping segment switched: Segment #{targetSegmentIndex} selected.");
            }
            else
            {
                UpdateStatus($"Segment #{targetSegmentIndex} selected.");
            }
        }

        private int FindNextOverlappingSegmentIndex(int currentSegmentIndex)
        {
            if (currentSegmentIndex < 0 || currentSegmentIndex >= stadium.Segments.Count)
            {
                return currentSegmentIndex;
            }

            SegmentData currentSegment = stadium.Segments[currentSegmentIndex];
            List<int> overlappingSegments = new();

            for (int i = 0; i < stadium.Segments.Count; i++)
            {
                SegmentData otherSegment = stadium.Segments[i];

                if (SegmentsShareSameVertexPair(currentSegment, otherSegment))
                {
                    overlappingSegments.Add(i);
                }
            }

            if (overlappingSegments.Count <= 1)
            {
                return currentSegmentIndex;
            }

            overlappingSegments.Sort();

            int currentListIndex = overlappingSegments.IndexOf(currentSegmentIndex);

            if (currentListIndex < 0)
            {
                return overlappingSegments[0];
            }

            int nextListIndex = currentListIndex + 1;

            if (nextListIndex >= overlappingSegments.Count)
            {
                nextListIndex = 0;
            }

            return overlappingSegments[nextListIndex];
        }

        private bool SegmentsShareSameVertexPair(SegmentData a, SegmentData b)
        {
            bool sameDirection = a.V0 == b.V0 && a.V1 == b.V1;
            bool oppositeDirection = a.V0 == b.V1 && a.V1 == b.V0;

            return sameDirection || oppositeDirection;
        }

        private void DiscShape_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Ellipse discShape) return;
            if (!discShapeIndexes.TryGetValue(discShape, out int discIndex)) return;
            if (currentTool != "Select") return;

            e.Handled = true;
            Point dataPoint = ScreenToDataPoint(e.GetPosition(MapCanvas));
            if (HandleSelectObjectMouseDown("Disc", discIndex, dataPoint)) return;
            SelectDisc(discIndex);
            BeginDiscDrag(discIndex, dataPoint);
            UpdateStatus($"Disc #{discIndex} selected.");
        }

        private void GoalShape_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Shape goalShape) return;
            if (!goalShapeIndexes.TryGetValue(goalShape, out int goalIndex)) return;
            if (currentTool != "Select") return;

            e.Handled = true;
            Point dataPoint = ScreenToDataPoint(e.GetPosition(MapCanvas));
            if (HandleSelectObjectMouseDown("Goal", goalIndex, dataPoint)) return;
            SelectGoal(goalIndex);
            UpdateInspectorForSelection("Goal");
            BeginSelectedItemsDrag(dataPoint);
            RenderStadium();
            UpdateStatus($"Goal #{goalIndex} selected.");
        }

        private void PlaneShape_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Shape planeShape) return;
            if (!planeShapeIndexes.TryGetValue(planeShape, out int planeIndex)) return;
            if (currentTool != "Select") return;

            e.Handled = true;
            Point dataPoint = ScreenToDataPoint(e.GetPosition(MapCanvas));
            if (HandleSelectObjectMouseDown("Plane", planeIndex, dataPoint)) return;
            SelectPlane(planeIndex);
            UpdateInspectorForSelection("Plane");
            BeginSelectedItemsDrag(dataPoint);
            RenderStadium();
            UpdateStatus($"Plane #{planeIndex} selected.");
        }

        private void JointShape_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Shape jointShape) return;
            if (!jointShapeIndexes.TryGetValue(jointShape, out int jointIndex)) return;
            if (currentTool != "Select") return;

            e.Handled = true;
            Point dataPoint = ScreenToDataPoint(e.GetPosition(MapCanvas));
            if (HandleSelectObjectMouseDown("Joint", jointIndex, dataPoint)) return;
            SelectJoint(jointIndex);
            BeginSelectedItemsDrag(dataPoint);
            RenderStadium();
            UpdateStatus($"Joint #{jointIndex} selected.");
        }

        private void RedSpawnShape_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Ellipse shape) return;
            if (!redSpawnShapeIndexes.TryGetValue(shape, out int index)) return;
            if (currentTool != "Select") return;

            e.Handled = true;
            Point dataPoint = ScreenToDataPoint(e.GetPosition(MapCanvas));
            if (HandleSelectObjectMouseDown("RedSpawn", index, dataPoint)) return;
            SelectRedSpawn(index);
            BeginRedSpawnDrag(index, dataPoint);
            UpdateStatus($"Red Spawn #{index} selected.");
        }

        private void BlueSpawnShape_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Ellipse shape) return;
            if (!blueSpawnShapeIndexes.TryGetValue(shape, out int index)) return;
            if (currentTool != "Select") return;

            e.Handled = true;
            Point dataPoint = ScreenToDataPoint(e.GetPosition(MapCanvas));
            if (HandleSelectObjectMouseDown("BlueSpawn", index, dataPoint)) return;
            SelectBlueSpawn(index);
            BeginBlueSpawnDrag(index, dataPoint);
            UpdateStatus($"Blue Spawn #{index} selected.");
        }

        private void GoalEndpoint_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Ellipse endpointShape) return;
            if (currentTool != "Select") return;
            if (endpointShape.Tag is not string tag) return;

            string[] parts = tag.Split(':');

            if (parts.Length != 2) return;
            if (!int.TryParse(parts[0], out int goalIndex)) return;
            if (!int.TryParse(parts[1], out int endpointNumber)) return;
            if (goalIndex < 0 || goalIndex >= stadium.Goals.Count) return;
            if (endpointNumber != 0 && endpointNumber != 1) return;

            e.Handled = true;

            SelectGoal(goalIndex);
            BeginGoalEndpointDrag(goalIndex, endpointNumber);

            UpdateStatus($"Dragging Goal #{goalIndex} endpoint p{endpointNumber}.");
        }

        private void CurveHandle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (selectedSegmentIndex == null) return;
            if (currentTool != "Select") return;

            e.Handled = true;
            BeginCurveHandleDrag(selectedSegmentIndex.Value);
            UpdateStatus($"Curve handle dragging started for Segment #{selectedSegmentIndex.Value}.");
        }

        private int AddDisc(double x, double y, double radius)
        {
            PushUndoState(autoMirrorPlacement ? "Add Mirrored Disc" : "Add Disc");
            Point snapped = SnapDataPoint(new Point(x, y));
            x = snapped.X;
            y = snapped.Y;

            DiscData disc = new()
            {
                X = x,
                Y = y,
                Radius = radius,
                Color = null,
                BCoef = null,
                InvMass = null,
                CGroup = null,
                CMask = null
            };

            stadium.Discs.Add(disc);

            int index = stadium.Discs.Count - 1;

            if (autoMirrorPlacement)
            {
                DiscData mirroredDisc = CloneData(disc);
                mirroredDisc.X = MirrorCanvasX(mirroredDisc.X);

                if (Math.Abs(mirroredDisc.X - disc.X) > 0.001)
                {
                    stadium.Discs.Add(mirroredDisc);
                    int mirrorIndex = stadium.Discs.Count - 1;
                    SelectMirroredPair("Disc", index, mirrorIndex);
                    UpdateStatus($"Disc #{index} added with mirrored Disc #{mirrorIndex}.");
                }
                else
                {
                    SelectDisc(index);
                    UpdateStatus($"Disc #{index} added on mirror axis.");
                }
            }
            else
            {
                SelectDisc(index);
                UpdateStatus($"Disc #{index} added.");
            }

            RenderStadium();
            UpdateObjectCount();
            UpdateObjectsList();
            UpdateJsonPreview();

            return index;
        }

        private void DrawDisc(int index, DiscData disc)
        {
            if (IsObjectHidden("Disc", index)) return;
            bool isSelected = selectedDiscIndex == index || IsObjectSelected("Disc", index);
            if (!showViewportDiscs && !isSelected) return;
            bool isVisible = IsDiscVisible(disc);

            if (!isVisible && !showViewportInvisibleObjects && !isSelected)
            {
                return;
            }

            double radius = disc.Radius ?? DefaultDiscRadius;
            Point screenPoint = DataToScreenPoint(disc.X, disc.Y);
            double screenRadius = Math.Max(3, ScaleLength(radius));

            Brush fillBrush = Brushes.White;
            Brush strokeBrush = Brushes.Black;
            double strokeThickness = Math.Max(1, ScaleLength(2));
            bool transparent = string.Equals(disc.Color, "transparent", StringComparison.OrdinalIgnoreCase);

            if (!isVisible)
            {
                fillBrush = Brushes.Transparent;
                strokeBrush = new SolidColorBrush(Color.FromArgb(180, 255, 255, 255));
                strokeThickness = Math.Max(1, ScaleLength(1));
            }
            else if (transparent)
            {
                fillBrush = Brushes.Transparent;
            }
            else if (!string.IsNullOrWhiteSpace(disc.Color))
            {
                fillBrush = new SolidColorBrush(ColorFromHex(disc.Color));
            }

            if (isSelected)
            {
                DrawSelectionCircle(screenPoint, screenRadius + 5, 8, SelectionAccentSoftBrush, Math.Max(3, ScaleLength(5)));
                DrawSelectionCircle(screenPoint, screenRadius, 9, SelectionAccentBrush, Math.Max(1.5, ScaleLength(2)));
                DrawSelectionCircle(screenPoint, Math.Max(4, screenRadius * 0.35), 10, SelectionCyanBrush, 1.4, new DoubleCollection { 3, 3 });
                DrawSelectionCenterCross(screenPoint, Math.Min(10, Math.Max(5, screenRadius * 0.45)), 11);
            }

            Ellipse discShape = new()
            {
                Width = screenRadius * 2,
                Height = screenRadius * 2,
                Fill = fillBrush,
                Stroke = strokeBrush,
                StrokeThickness = strokeThickness,
                StrokeDashArray = isVisible ? null : new DoubleCollection { 4, 4 },
                Cursor = Cursors.Hand
            };

            Canvas.SetLeft(discShape, screenPoint.X - screenRadius);
            Canvas.SetTop(discShape, screenPoint.Y - screenRadius);
            Panel.SetZIndex(discShape, isSelected ? 9 : 3);

            discShapeIndexes[discShape] = index;
            discShape.MouseLeftButtonDown += DiscShape_MouseLeftButtonDown;

            MapCanvas.Children.Add(discShape);
        }

        private void BeginGoalDrag(Point startPoint)
        {
            isDraggingGoal = true;
            goalDragStartPoint = startPoint;

            Point screenStart = DataToScreenPoint(startPoint.X, startPoint.Y);

            goalPreviewLine = new Line
            {
                X1 = screenStart.X,
                Y1 = screenStart.Y,
                X2 = screenStart.X,
                Y2 = screenStart.Y,
                Stroke = GetGoalBrush(GetSelectedGoalTeam()),
                StrokeThickness = 4,
                StrokeDashArray = new DoubleCollection { 4, 4 }
            };

            Panel.SetZIndex(goalPreviewLine, 6);
            MapCanvas.Children.Add(goalPreviewLine);
            MapCanvas.CaptureMouse();

            UpdateStatus("Dragging goal.");
        }

        private void FinishGoalDrag(Point endPoint)
        {
            double distance = GetDistance(goalDragStartPoint, endPoint);

            if (distance < MinimumSegmentLength)
            {
                CancelGoalDrag();
                UpdateStatus("Goal cancelled. Too short.");
                return;
            }

            AddGoal(goalDragStartPoint, endPoint, GetSelectedGoalTeam());
            CancelGoalDrag();
        }

        private void CancelGoalDrag()
        {
            isDraggingGoal = false;

            if (goalPreviewLine != null)
            {
                MapCanvas.Children.Remove(goalPreviewLine);
                goalPreviewLine = null;
            }

            ReleaseCanvasMouseIfSafe();
        }

        private int AddGoal(Point p0, Point p1, string team)
        {
            PushUndoState(autoMirrorPlacement ? "Add Mirrored Goal" : "Add Goal");
            p0 = SnapDataPoint(p0);
            p1 = SnapDataPoint(p1);

            GoalData goal = new()
            {
                X0 = p0.X,
                Y0 = p0.Y,
                X1 = p1.X,
                Y1 = p1.Y,
                Team = NormalizeGoalTeam(team)
            };

            stadium.Goals.Add(goal);

            int index = stadium.Goals.Count - 1;

            if (autoMirrorPlacement)
            {
                GoalData mirroredGoal = CloneData(goal);
                mirroredGoal.X0 = MirrorCanvasX(mirroredGoal.X0);
                mirroredGoal.X1 = MirrorCanvasX(mirroredGoal.X1);

                if (Math.Abs(mirroredGoal.X0 - goal.X0) > 0.001 || Math.Abs(mirroredGoal.X1 - goal.X1) > 0.001)
                {
                    stadium.Goals.Add(mirroredGoal);
                    int mirrorIndex = stadium.Goals.Count - 1;
                    SelectMirroredPair("Goal", index, mirrorIndex);
                    UpdateStatus($"Goal #{index} added with mirrored Goal #{mirrorIndex}.");
                }
                else
                {
                    SelectGoal(index);
                    UpdateStatus($"Goal #{index} added on mirror axis.");
                }
            }
            else
            {
                SelectGoal(index);
                UpdateStatus($"Goal #{index} added.");
            }

            RenderStadium();
            UpdateObjectCount();
            UpdateObjectsList();
            UpdateJsonPreview();

            return index;
        }

        private void DrawGoal(int index, GoalData goal)
        {
            if (IsObjectHidden("Goal", index)) return;
            bool isSelected = selectedGoalIndex == index || IsObjectSelected("Goal", index);
            Point p0 = DataToScreenPoint(goal.X0, goal.Y0);
            Point p1 = DataToScreenPoint(goal.X1, goal.Y1);

            if (isSelected)
            {
                AddViewportLine(p0, p1, SelectionAccentSoftBrush, Math.Max(5, ScaleLength(8)), 8, null);
                AddViewportLine(p0, p1, SelectionAccentBrush, Math.Max(2, ScaleLength(4)), 9, null);
                DrawSelectionEndpointMarkers(p0, p1, 29);
            }

            bool blue = NormalizeGoalTeam(goal.Team) == "blue";
            Brush thickBrush = new SolidColorBrush(blue ? Color.FromRgb(127, 127, 255) : Color.FromRgb(255, 127, 127));
            Brush thinBrush = new SolidColorBrush(blue ? Color.FromArgb(210, 0, 0, 255) : Color.FromArgb(210, 255, 0, 0));

            Line goalLine = new()
            {
                X1 = p0.X,
                Y1 = p0.Y,
                X2 = p1.X,
                Y2 = p1.Y,
                Stroke = thickBrush,
                StrokeThickness = Math.Max(1, ScaleLength(2)),
                Cursor = Cursors.Hand
            };

            Panel.SetZIndex(goalLine, isSelected ? 9 : 4);
            goalShapeIndexes[goalLine] = index;
            goalLine.MouseLeftButtonDown += GoalShape_MouseLeftButtonDown;
            MapCanvas.Children.Add(goalLine);

            AddViewportLine(p0, p1, thinBrush, Math.Max(1, ScaleLength(1)), isSelected ? 10 : 5, null);

            if (isSelected)
            {
                DrawGoalEndpoint(index, 0, p0.X, p0.Y);
                DrawGoalEndpoint(index, 1, p1.X, p1.Y);
            }
        }

        private void DrawGoalEndpoint(int goalIndex, int endpointNumber, double x, double y)
        {
            Ellipse point = new()
            {
                Width = 18,
                Height = 18,
                Fill = SelectionAccentBrush,
                Stroke = Brushes.White,
                StrokeThickness = 2,
                Cursor = Cursors.SizeAll,
                Tag = $"{goalIndex}:{endpointNumber}"
            };

            Canvas.SetLeft(point, x - 8);
            Canvas.SetTop(point, y - 8);
            Panel.SetZIndex(point, 30);

            point.MouseLeftButtonDown += GoalEndpoint_MouseLeftButtonDown;

            MapCanvas.Children.Add(point);
        }

        private void BeginPlaneDrag(Point startPoint)
        {
            isDraggingPlane = true;
            planeDragStartPoint = startPoint;

            Point screenStart = DataToScreenPoint(startPoint.X, startPoint.Y);

            planePreviewLine = new Line
            {
                X1 = screenStart.X,
                Y1 = screenStart.Y,
                X2 = screenStart.X,
                Y2 = screenStart.Y,
                Stroke = Brushes.MediumPurple,
                StrokeThickness = 4,
                StrokeDashArray = new DoubleCollection { 8, 5 }
            };

            Panel.SetZIndex(planePreviewLine, 6);
            MapCanvas.Children.Add(planePreviewLine);
            MapCanvas.CaptureMouse();

            UpdateStatus("Dragging plane.");
        }

        private void FinishPlaneDrag(Point endPoint)
        {
            double distance = GetDistance(planeDragStartPoint, endPoint);

            if (distance < MinimumSegmentLength)
            {
                CancelPlaneDrag();
                UpdateStatus("Plane cancelled. Too short.");
                return;
            }

            AddPlaneFromLine(planeDragStartPoint, endPoint);
            CancelPlaneDrag();
        }

        private void CancelPlaneDrag()
        {
            isDraggingPlane = false;

            if (planePreviewLine != null)
            {
                MapCanvas.Children.Remove(planePreviewLine);
                planePreviewLine = null;
            }

            ReleaseCanvasMouseIfSafe();
        }

        private int AddPlaneFromLine(Point p0, Point p1)
        {
            PushUndoState(autoMirrorPlacement ? "Add Mirrored Plane" : "Add Plane");
            p0 = SnapDataPoint(p0);
            p1 = SnapDataPoint(p1);

            double centerX = GetCanvasCenterX();
            double centerY = GetCanvasCenterY();

            double x0 = p0.X - centerX;
            double y0 = p0.Y - centerY;
            double x1 = p1.X - centerX;
            double y1 = p1.Y - centerY;

            double dx = x1 - x0;
            double dy = y1 - y0;
            double length = Math.Sqrt(dx * dx + dy * dy);

            if (length <= 0.0001)
            {
                length = 1;
            }

            double normalX = -dy / length;
            double normalY = dx / length;

            double dist = normalX * x0 + normalY * y0;

            PlaneData plane = new()
            {
                Normal = new List<double>
                {
                    Math.Round(normalX, 4),
                    Math.Round(normalY, 4)
                },
                Dist = Math.Round(dist, 2),
                BCoef = null,
                CGroup = null,
                CMask = null
            };

            stadium.Planes.Add(plane);

            int index = stadium.Planes.Count - 1;

            if (autoMirrorPlacement)
            {
                PlaneData mirroredPlane = CloneData(plane);
                if (mirroredPlane.Normal != null && mirroredPlane.Normal.Count >= 2)
                {
                    mirroredPlane.Normal[0] = -mirroredPlane.Normal[0];
                    mirroredPlane.Dist = -mirroredPlane.Dist;
                    stadium.Planes.Add(mirroredPlane);
                    int mirrorIndex = stadium.Planes.Count - 1;
                    SelectMirroredPair("Plane", index, mirrorIndex);
                    UpdateStatus($"Plane #{index} added with mirrored Plane #{mirrorIndex}.");
                }
                else
                {
                    SelectPlane(index);
                    UpdateStatus($"Plane #{index} added.");
                }
            }
            else
            {
                SelectPlane(index);
                UpdateStatus($"Plane #{index} added.");
            }

            RenderStadium();
            UpdateObjectCount();
            UpdateObjectsList();
            UpdateJsonPreview();

            return index;
        }

        private void DrawPlane(int index, PlaneData plane)
        {
            if (IsObjectHidden("Plane", index)) return;
            if (plane.Normal == null || plane.Normal.Count < 2)
            {
                return;
            }

            bool isSelected = selectedPlaneIndex == index || IsObjectSelected("Plane", index);

            double nx = plane.Normal[0];
            double ny = plane.Normal[1];
            double dist = plane.Dist;

            double centerX = GetCanvasCenterX();
            double centerY = GetCanvasCenterY();

            double px = nx * dist;
            double py = ny * dist;

            double tx = -ny;
            double ty = nx;

            double lineLength = Math.Max(MapCanvas.ActualWidth, MapCanvas.ActualHeight) * 2.5 / Math.Max(0.0001, viewportZoom);

            Point dataA = new(centerX + px - tx * lineLength, centerY + py - ty * lineLength);
            Point dataB = new(centerX + px + tx * lineLength, centerY + py + ty * lineLength);
            Point a = DataToScreenPoint(dataA.X, dataA.Y);
            Point b = DataToScreenPoint(dataB.X, dataB.Y);

            if (isSelected)
            {
                AddViewportLine(a, b, SelectionAccentSoftBrush, Math.Max(4, ScaleLength(6)), 7, new DoubleCollection { 12, 6 });
                AddViewportLine(a, b, SelectionAccentBrush, Math.Max(2, ScaleLength(3)), 8, null);

                Point planePoint = DataToScreenPoint(centerX + px, centerY + py);
                DrawSelectionFilledDot(planePoint, 5, 29, SelectionAccentBrush, Brushes.White);
                DrawSelectionNormalArrow(planePoint, new Vector(nx, ny), 46, 30);
            }

            Line planeLine = new()
            {
                X1 = a.X,
                Y1 = a.Y,
                X2 = b.X,
                Y2 = b.Y,
                Stroke = new SolidColorBrush(Color.FromArgb(210, 0, 0, 0)),
                StrokeThickness = Math.Max(1, ScaleLength(2)),
                Cursor = Cursors.Hand
            };

            Panel.SetZIndex(planeLine, isSelected ? 8 : 1);
            planeShapeIndexes[planeLine] = index;
            planeLine.MouseLeftButtonDown += PlaneShape_MouseLeftButtonDown;
            MapCanvas.Children.Add(planeLine);

            AddViewportLine(a, b, new SolidColorBrush(Color.FromArgb(210, 255, 255, 255)), Math.Max(1, ScaleLength(1)), isSelected ? 9 : 2, null);
        }

        private int AddRedSpawn(double x, double y)
        {
            PushUndoState(autoMirrorPlacement ? "Add Mirrored Red Spawn" : "Add Red Spawn");
            Point snapped = SnapDataPoint(new Point(x, y));
            x = snapped.X;
            y = snapped.Y;

            stadium.RedSpawnPoints.Add(new SpawnPointData { X = x, Y = y });

            int index = stadium.RedSpawnPoints.Count - 1;

            if (autoMirrorPlacement)
            {
                double mirrorX = MirrorCanvasX(x);
                if (Math.Abs(mirrorX - x) > 0.001)
                {
                    stadium.RedSpawnPoints.Add(new SpawnPointData { X = mirrorX, Y = y });
                    int mirrorIndex = stadium.RedSpawnPoints.Count - 1;
                    SelectMirroredPair("RedSpawn", index, mirrorIndex);
                    UpdateStatus($"Red Spawn #{index} added with mirrored Red Spawn #{mirrorIndex}.");
                }
                else
                {
                    SelectRedSpawn(index);
                    UpdateStatus($"Red Spawn #{index} added on mirror axis.");
                }
            }
            else
            {
                SelectRedSpawn(index);
                UpdateStatus($"Red Spawn #{index} added.");
            }

            RenderStadium();
            UpdateObjectCount();
            UpdateObjectsList();
            UpdateJsonPreview();

            return index;
        }

        private int AddBlueSpawn(double x, double y)
        {
            PushUndoState(autoMirrorPlacement ? "Add Mirrored Blue Spawn" : "Add Blue Spawn");
            Point snapped = SnapDataPoint(new Point(x, y));
            x = snapped.X;
            y = snapped.Y;

            stadium.BlueSpawnPoints.Add(new SpawnPointData { X = x, Y = y });

            int index = stadium.BlueSpawnPoints.Count - 1;

            if (autoMirrorPlacement)
            {
                double mirrorX = MirrorCanvasX(x);
                if (Math.Abs(mirrorX - x) > 0.001)
                {
                    stadium.BlueSpawnPoints.Add(new SpawnPointData { X = mirrorX, Y = y });
                    int mirrorIndex = stadium.BlueSpawnPoints.Count - 1;
                    SelectMirroredPair("BlueSpawn", index, mirrorIndex);
                    UpdateStatus($"Blue Spawn #{index} added with mirrored Blue Spawn #{mirrorIndex}.");
                }
                else
                {
                    SelectBlueSpawn(index);
                    UpdateStatus($"Blue Spawn #{index} added on mirror axis.");
                }
            }
            else
            {
                SelectBlueSpawn(index);
                UpdateStatus($"Blue Spawn #{index} added.");
            }

            RenderStadium();
            UpdateObjectCount();
            UpdateObjectsList();
            UpdateJsonPreview();

            return index;
        }

        private void DrawSpawnPoint(int index, SpawnPointData spawn, string team)
        {
            bool isRed = team == "red";
            if (IsObjectHidden(isRed ? "RedSpawn" : "BlueSpawn", index)) return;
            bool isSelected = isRed
                ? selectedRedSpawnIndex == index || IsObjectSelected("RedSpawn", index)
                : selectedBlueSpawnIndex == index || IsObjectSelected("BlueSpawn", index);

            Point screenPoint = DataToScreenPoint(spawn.X, spawn.Y);

            if (isSelected)
            {
                DrawSelectionCircle(screenPoint, 12, 24, isRed ? new SolidColorBrush(Color.FromArgb(130, 248, 113, 113)) : new SolidColorBrush(Color.FromArgb(130, 96, 165, 250)), 4);
                DrawSelectionCircle(screenPoint, 8, 25, SelectionAccentBrush, 1.5);
                DrawSelectionCenterCross(screenPoint, 8, 27);
            }

            Ellipse spawnShape = new()
            {
                Width = isSelected ? 14 : 10,
                Height = isSelected ? 14 : 10,
                Fill = isRed ? Brushes.Red : Brushes.DodgerBlue,
                Stroke = isSelected ? Brushes.Gold : Brushes.White,
                StrokeThickness = isSelected ? 2 : 1,
                Cursor = Cursors.Hand
            };

            double radius = isSelected ? 7 : 5;

            Canvas.SetLeft(spawnShape, screenPoint.X - radius);
            Canvas.SetTop(spawnShape, screenPoint.Y - radius);
            Panel.SetZIndex(spawnShape, isSelected ? 25 : 12);

            if (isRed)
            {
                redSpawnShapeIndexes[spawnShape] = index;
                spawnShape.MouseLeftButtonDown += RedSpawnShape_MouseLeftButtonDown;
            }
            else
            {
                blueSpawnShapeIndexes[spawnShape] = index;
                spawnShape.MouseLeftButtonDown += BlueSpawnShape_MouseLeftButtonDown;
            }

            MapCanvas.Children.Add(spawnShape);
        }

        private void DrawJoint(int index, JointData joint)
        {
            if (IsObjectHidden("Joint", index)) return;
            if (joint.D0 < 0 || joint.D0 >= stadium.Discs.Count) return;
            if (joint.D1 < 0 || joint.D1 >= stadium.Discs.Count) return;

            DiscData d0 = stadium.Discs[joint.D0];
            DiscData d1 = stadium.Discs[joint.D1];

            bool isSelected = selectedJointIndex == index || IsObjectSelected("Joint", index);

            Brush brush = isSelected ? Brushes.Gold : Brushes.Orange;

            if (!isSelected && !string.IsNullOrWhiteSpace(joint.Color))
            {
                brush = new SolidColorBrush(ColorFromHex(joint.Color));
            }

            Point p0 = DataToScreenPoint(d0.X, d0.Y);
            Point p1 = DataToScreenPoint(d1.X, d1.Y);

            if (isSelected)
            {
                AddViewportLine(p0, p1, SelectionAccentSoftBrush, 8, 5, new DoubleCollection { 6, 4 });
                DrawSelectionEndpointMarkers(p0, p1, 28);
            }

            Line line = new()
            {
                X1 = p0.X,
                Y1 = p0.Y,
                X2 = p1.X,
                Y2 = p1.Y,
                Stroke = brush,
                StrokeThickness = isSelected ? 5 : 3,
                StrokeDashArray = new DoubleCollection { 4, 4 },
                Cursor = Cursors.Hand
            };

            Panel.SetZIndex(line, isSelected ? 6 : 2);

            jointShapeIndexes[line] = index;
            line.MouseLeftButtonDown += JointShape_MouseLeftButtonDown;

            MapCanvas.Children.Add(line);
        }

        private bool HandleSelectObjectMouseDown(string type, int index, Point dataMousePosition)
        {
            if (IsCtrlPressed())
            {
                ToggleObjectSelection(type, index);
                RenderStadium();
                UpdateStatus($"Ctrl selection: {selectedItems.Count} object(s).");
                return true;
            }

            if (IsObjectSelected(type, index) && selectedItems.Count > 1)
            {
                BeginSelectedItemsDrag(dataMousePosition);
                return true;
            }

            return false;
        }

        private bool IsCtrlPressed()
        {
            return Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
        }

        private void ToggleObjectSelection(string type, int index)
        {
            ClearSingleSelectionIndexes();

            int existingIndex = selectedItems.FindIndex(item => item.Type == type && item.Index == index);

            if (existingIndex >= 0)
            {
                selectedItems.RemoveAt(existingIndex);
            }
            else
            {
                selectedItems.Add(new SelectedItem(type, index));
            }

            UpdateMultiSelectionSummary();
        }

        private void BeginSelectedItemsDrag(Point dataMousePosition)
        {
            if (selectedItems.Count == 0)
            {
                return;
            }

            PushUndoState("Move Selected Objects");
            dataMousePosition = SnapDataPoint(dataMousePosition);

            selectedVertexDragStartPositions.Clear();
            selectedDiscDragStartPositions.Clear();
            selectedGoalDragStartPositions.Clear();
            selectedPlaneDragStartDists.Clear();
            selectedRedSpawnDragStartPositions.Clear();
            selectedBlueSpawnDragStartPositions.Clear();

            foreach (SelectedItem item in selectedItems)
            {
                AddDragSnapshotForSelectedItem(item);
            }

            if (selectedVertexDragStartPositions.Count == 0 &&
                selectedDiscDragStartPositions.Count == 0 &&
                selectedGoalDragStartPositions.Count == 0 &&
                selectedPlaneDragStartDists.Count == 0 &&
                selectedRedSpawnDragStartPositions.Count == 0 &&
                selectedBlueSpawnDragStartPositions.Count == 0)
            {
                return;
            }

            isDraggingSelectedItems = true;
            selectedItemsDragStartData = dataMousePosition;
            MapCanvas.CaptureMouse();
            UpdateStatus($"Moving {selectedItems.Count} selected object(s).");
        }

        private void AddDragSnapshotForSelectedItem(SelectedItem item)
        {
            if (IsObjectLocked(item.Type, item.Index))
            {
                return;
            }

            switch (item.Type)
            {
                case "Vertex":
                    AddVertexDragSnapshot(item.Index);
                    break;

                case "Segment":
                    if (item.Index >= 0 && item.Index < stadium.Segments.Count)
                    {
                        SegmentData segment = stadium.Segments[item.Index];
                        AddVertexDragSnapshot(segment.V0);
                        AddVertexDragSnapshot(segment.V1);
                    }
                    break;

                case "Disc":
                    AddDiscDragSnapshot(item.Index);
                    break;

                case "Goal":
                    if (item.Index >= 0 && item.Index < stadium.Goals.Count && !selectedGoalDragStartPositions.ContainsKey(item.Index))
                    {
                        GoalData goal = stadium.Goals[item.Index];
                        selectedGoalDragStartPositions[item.Index] = (goal.X0, goal.Y0, goal.X1, goal.Y1);
                    }
                    break;

                case "Plane":
                    if (item.Index >= 0 && item.Index < stadium.Planes.Count && !selectedPlaneDragStartDists.ContainsKey(item.Index))
                    {
                        selectedPlaneDragStartDists[item.Index] = stadium.Planes[item.Index].Dist;
                    }
                    break;

                case "Joint":
                    if (item.Index >= 0 && item.Index < stadium.Joints.Count)
                    {
                        JointData joint = stadium.Joints[item.Index];
                        AddDiscDragSnapshot(joint.D0);
                        AddDiscDragSnapshot(joint.D1);
                    }
                    break;

                case "RedSpawn":
                    if (item.Index >= 0 && item.Index < stadium.RedSpawnPoints.Count && !selectedRedSpawnDragStartPositions.ContainsKey(item.Index))
                    {
                        SpawnPointData spawn = stadium.RedSpawnPoints[item.Index];
                        selectedRedSpawnDragStartPositions[item.Index] = new Point(spawn.X, spawn.Y);
                    }
                    break;

                case "BlueSpawn":
                    if (item.Index >= 0 && item.Index < stadium.BlueSpawnPoints.Count && !selectedBlueSpawnDragStartPositions.ContainsKey(item.Index))
                    {
                        SpawnPointData spawn = stadium.BlueSpawnPoints[item.Index];
                        selectedBlueSpawnDragStartPositions[item.Index] = new Point(spawn.X, spawn.Y);
                    }
                    break;
            }
        }

        private void AddVertexDragSnapshot(int vertexIndex)
        {
            if (vertexIndex < 0 || vertexIndex >= stadium.Vertexes.Count || selectedVertexDragStartPositions.ContainsKey(vertexIndex))
            {
                return;
            }

            VertexData vertex = stadium.Vertexes[vertexIndex];
            selectedVertexDragStartPositions[vertexIndex] = new Point(vertex.X, vertex.Y);
        }

        private void AddDiscDragSnapshot(int discIndex)
        {
            if (discIndex < 0 || discIndex >= stadium.Discs.Count || selectedDiscDragStartPositions.ContainsKey(discIndex))
            {
                return;
            }

            DiscData disc = stadium.Discs[discIndex];
            selectedDiscDragStartPositions[discIndex] = new Point(disc.X, disc.Y);
        }

        private void DragSelectedItems(Point dataMousePosition)
        {
            dataMousePosition = SnapDataPoint(dataMousePosition);
            double dx = dataMousePosition.X - selectedItemsDragStartData.X;
            double dy = dataMousePosition.Y - selectedItemsDragStartData.Y;

            foreach (KeyValuePair<int, Point> item in selectedVertexDragStartPositions)
            {
                if (item.Key >= 0 && item.Key < stadium.Vertexes.Count)
                {
                    Point next = new Point(item.Value.X + dx, item.Value.Y + dy);
                    if (snapToGrid) next = SnapDataPoint(next);
                    stadium.Vertexes[item.Key].X = next.X;
                    stadium.Vertexes[item.Key].Y = next.Y;
                }
            }

            foreach (KeyValuePair<int, Point> item in selectedDiscDragStartPositions)
            {
                if (item.Key >= 0 && item.Key < stadium.Discs.Count)
                {
                    Point next = new Point(item.Value.X + dx, item.Value.Y + dy);
                    if (snapToGrid) next = SnapDataPoint(next);
                    stadium.Discs[item.Key].X = next.X;
                    stadium.Discs[item.Key].Y = next.Y;
                }
            }

            foreach (KeyValuePair<int, (double X0, double Y0, double X1, double Y1)> item in selectedGoalDragStartPositions)
            {
                if (item.Key >= 0 && item.Key < stadium.Goals.Count)
                {
                    GoalData goal = stadium.Goals[item.Key];
                    Point p0 = new Point(item.Value.X0 + dx, item.Value.Y0 + dy);
                    Point p1 = new Point(item.Value.X1 + dx, item.Value.Y1 + dy);
                    if (snapToGrid)
                    {
                        p0 = SnapDataPoint(p0);
                        p1 = SnapDataPoint(p1);
                    }
                    goal.X0 = p0.X;
                    goal.Y0 = p0.Y;
                    goal.X1 = p1.X;
                    goal.Y1 = p1.Y;
                }
            }

            foreach (KeyValuePair<int, double> item in selectedPlaneDragStartDists)
            {
                if (item.Key >= 0 && item.Key < stadium.Planes.Count)
                {
                    PlaneData plane = stadium.Planes[item.Key];

                    if (plane.Normal != null && plane.Normal.Count >= 2)
                    {
                        plane.Dist = item.Value + plane.Normal[0] * dx + plane.Normal[1] * dy;
                    }
                }
            }

            foreach (KeyValuePair<int, Point> item in selectedRedSpawnDragStartPositions)
            {
                if (item.Key >= 0 && item.Key < stadium.RedSpawnPoints.Count)
                {
                    Point next = new Point(item.Value.X + dx, item.Value.Y + dy);
                    if (snapToGrid) next = SnapDataPoint(next);
                    stadium.RedSpawnPoints[item.Key].X = next.X;
                    stadium.RedSpawnPoints[item.Key].Y = next.Y;
                }
            }

            foreach (KeyValuePair<int, Point> item in selectedBlueSpawnDragStartPositions)
            {
                if (item.Key >= 0 && item.Key < stadium.BlueSpawnPoints.Count)
                {
                    Point next = new Point(item.Value.X + dx, item.Value.Y + dy);
                    if (snapToGrid) next = SnapDataPoint(next);
                    stadium.BlueSpawnPoints[item.Key].X = next.X;
                    stadium.BlueSpawnPoints[item.Key].Y = next.Y;
                }
            }

            RenderStadium();
        }

        private void FinishSelectedItemsDrag()
        {
            isDraggingSelectedItems = false;
            selectedVertexDragStartPositions.Clear();
            selectedDiscDragStartPositions.Clear();
            selectedGoalDragStartPositions.Clear();
            selectedPlaneDragStartDists.Clear();
            selectedRedSpawnDragStartPositions.Clear();
            selectedBlueSpawnDragStartPositions.Clear();

            if (MapCanvas.IsMouseCaptured)
            {
                MapCanvas.ReleaseMouseCapture();
            }

            if (HasSingleSelection())
            {
                RefreshInspectorForCurrentSingleSelection();
            }
            else
            {
                UpdateMultiSelectionSummary();
            }

            UpdateObjectsList();
            UpdateJsonPreview();
            UpdateStatus($"Moved {selectedItems.Count} selected object(s).");
        }

        private void CancelSelectedItemsDrag()
        {
            isDraggingSelectedItems = false;
            selectedVertexDragStartPositions.Clear();
            selectedDiscDragStartPositions.Clear();
            selectedGoalDragStartPositions.Clear();
            selectedPlaneDragStartDists.Clear();
            selectedRedSpawnDragStartPositions.Clear();
            selectedBlueSpawnDragStartPositions.Clear();
            ReleaseCanvasMouseIfSafe();
        }

        private void BeginGoalEndpointDrag(int goalIndex, int endpointNumber)
        {
            if (IsObjectLocked("Goal", goalIndex))
            {
                UpdateStatus($"Goal #{goalIndex} is locked.");
                return;
            }
            PushUndoState("Move Goal Endpoint");
            isDraggingGoalEndpoint = true;
            draggingGoalEndpointGoalIndex = goalIndex;
            draggingGoalEndpointNumber = endpointNumber;
            MapCanvas.CaptureMouse();
        }

        private void DragGoalEndpoint(Point mousePosition)
        {
            mousePosition = SnapDataPoint(mousePosition);
            if (draggingGoalEndpointGoalIndex == null) return;

            int goalIndex = draggingGoalEndpointGoalIndex.Value;
            if (goalIndex < 0 || goalIndex >= stadium.Goals.Count) return;

            GoalData goal = stadium.Goals[goalIndex];

            if (draggingGoalEndpointNumber == 0)
            {
                goal.X0 = mousePosition.X;
                goal.Y0 = mousePosition.Y;
            }
            else if (draggingGoalEndpointNumber == 1)
            {
                goal.X1 = mousePosition.X;
                goal.Y1 = mousePosition.Y;
            }

            SelectGoal(goalIndex);
            RenderStadium();
            UpdateObjectsList();
            UpdateJsonPreview();
        }

        private void FinishGoalEndpointDrag()
        {
            if (draggingGoalEndpointGoalIndex != null)
            {
                UpdateStatus($"Goal #{draggingGoalEndpointGoalIndex.Value} endpoint moved.");
            }

            isDraggingGoalEndpoint = false;
            draggingGoalEndpointGoalIndex = null;
            draggingGoalEndpointNumber = -1;

            ReleaseCanvasMouseIfSafe();
            UpdateJsonPreview();
            UpdateObjectsList();
        }

        private void CancelGoalEndpointDrag()
        {
            isDraggingGoalEndpoint = false;
            draggingGoalEndpointGoalIndex = null;
            draggingGoalEndpointNumber = -1;
            ReleaseCanvasMouseIfSafe();
        }

        private void BeginVertexDrag(int vertexIndex, Point mousePosition)
        {
            if (vertexIndex < 0 || vertexIndex >= stadium.Vertexes.Count) return;
            if (IsObjectLocked("Vertex", vertexIndex))
            {
                UpdateStatus($"Vertex #{vertexIndex} is locked.");
                return;
            }
            PushUndoState("Move Vertex");
            mousePosition = SnapDataPoint(mousePosition);

            VertexData vertex = stadium.Vertexes[vertexIndex];

            isDraggingVertex = true;
            draggingVertexIndex = vertexIndex;
            vertexDragOffset = new Point(mousePosition.X - vertex.X, mousePosition.Y - vertex.Y);

            MapCanvas.CaptureMouse();
        }

        private void DragSelectedVertex(Point mousePosition)
        {
            if (draggingVertexIndex == null) return;

            int index = draggingVertexIndex.Value;
            if (index < 0 || index >= stadium.Vertexes.Count) return;

            VertexData vertex = stadium.Vertexes[index];

            if (snapToGrid)
            {
                Point snapped = SnapDataPoint(mousePosition);
                vertex.X = snapped.X;
                vertex.Y = snapped.Y;
            }
            else
            {
                vertex.X = mousePosition.X - vertexDragOffset.X;
                vertex.Y = mousePosition.Y - vertexDragOffset.Y;
            }

            SelectVertex(index);
            RenderStadium();
            UpdateObjectsList();
        }

        private void FinishVertexDrag()
        {
            isDraggingVertex = false;
            draggingVertexIndex = null;

            ReleaseCanvasMouseIfSafe();
            UpdateJsonPreview();
            UpdateObjectsList();
        }

        private void CancelVertexDrag()
        {
            isDraggingVertex = false;
            draggingVertexIndex = null;
            ReleaseCanvasMouseIfSafe();
        }

        private void BeginDiscDrag(int discIndex, Point mousePosition)
        {
            if (discIndex < 0 || discIndex >= stadium.Discs.Count) return;
            if (IsObjectLocked("Disc", discIndex))
            {
                UpdateStatus($"Disc #{discIndex} is locked.");
                return;
            }
            PushUndoState("Move Disc");
            mousePosition = SnapDataPoint(mousePosition);

            DiscData disc = stadium.Discs[discIndex];

            isDraggingDisc = true;
            draggingDiscIndex = discIndex;
            discDragOffset = new Point(mousePosition.X - disc.X, mousePosition.Y - disc.Y);

            MapCanvas.CaptureMouse();
        }

        private void DragSelectedDisc(Point mousePosition)
        {
            if (draggingDiscIndex == null) return;

            int index = draggingDiscIndex.Value;
            if (index < 0 || index >= stadium.Discs.Count) return;

            DiscData disc = stadium.Discs[index];

            if (snapToGrid)
            {
                Point snapped = SnapDataPoint(mousePosition);
                disc.X = snapped.X;
                disc.Y = snapped.Y;
            }
            else
            {
                disc.X = mousePosition.X - discDragOffset.X;
                disc.Y = mousePosition.Y - discDragOffset.Y;
            }

            SelectDisc(index);
            RenderStadium();
            UpdateObjectsList();
        }

        private void FinishDiscDrag()
        {
            isDraggingDisc = false;
            draggingDiscIndex = null;

            ReleaseCanvasMouseIfSafe();
            UpdateJsonPreview();
            UpdateObjectsList();
        }

        private void CancelDiscDrag()
        {
            isDraggingDisc = false;
            draggingDiscIndex = null;
            ReleaseCanvasMouseIfSafe();
        }

        private void BeginRedSpawnDrag(int index, Point mousePosition)
        {
            if (index < 0 || index >= stadium.RedSpawnPoints.Count) return;
            if (IsObjectLocked("RedSpawn", index))
            {
                UpdateStatus($"Red Spawn #{index} is locked.");
                return;
            }
            PushUndoState("Move Red Spawn");
            mousePosition = SnapDataPoint(mousePosition);

            SpawnPointData spawn = stadium.RedSpawnPoints[index];

            isDraggingRedSpawn = true;
            draggingRedSpawnIndex = index;
            redSpawnDragOffset = new Point(mousePosition.X - spawn.X, mousePosition.Y - spawn.Y);

            MapCanvas.CaptureMouse();
        }

        private void DragSelectedRedSpawn(Point mousePosition)
        {
            if (draggingRedSpawnIndex == null) return;

            int index = draggingRedSpawnIndex.Value;
            if (index < 0 || index >= stadium.RedSpawnPoints.Count) return;

            SpawnPointData spawn = stadium.RedSpawnPoints[index];

            if (snapToGrid)
            {
                Point snapped = SnapDataPoint(mousePosition);
                spawn.X = snapped.X;
                spawn.Y = snapped.Y;
            }
            else
            {
                spawn.X = mousePosition.X - redSpawnDragOffset.X;
                spawn.Y = mousePosition.Y - redSpawnDragOffset.Y;
            }

            SelectRedSpawn(index);
            RenderStadium();
            UpdateObjectsList();
            UpdateJsonPreview();
        }

        private void FinishRedSpawnDrag()
        {
            isDraggingRedSpawn = false;
            draggingRedSpawnIndex = null;

            ReleaseCanvasMouseIfSafe();
            UpdateJsonPreview();
            UpdateObjectsList();
        }

        private void CancelRedSpawnDrag()
        {
            isDraggingRedSpawn = false;
            draggingRedSpawnIndex = null;
            ReleaseCanvasMouseIfSafe();
        }

        private void BeginBlueSpawnDrag(int index, Point mousePosition)
        {
            if (index < 0 || index >= stadium.BlueSpawnPoints.Count) return;
            if (IsObjectLocked("BlueSpawn", index))
            {
                UpdateStatus($"Blue Spawn #{index} is locked.");
                return;
            }
            PushUndoState("Move Blue Spawn");
            mousePosition = SnapDataPoint(mousePosition);

            SpawnPointData spawn = stadium.BlueSpawnPoints[index];

            isDraggingBlueSpawn = true;
            draggingBlueSpawnIndex = index;
            blueSpawnDragOffset = new Point(mousePosition.X - spawn.X, mousePosition.Y - spawn.Y);

            MapCanvas.CaptureMouse();
        }

        private void DragSelectedBlueSpawn(Point mousePosition)
        {
            if (draggingBlueSpawnIndex == null) return;

            int index = draggingBlueSpawnIndex.Value;
            if (index < 0 || index >= stadium.BlueSpawnPoints.Count) return;

            SpawnPointData spawn = stadium.BlueSpawnPoints[index];

            if (snapToGrid)
            {
                Point snapped = SnapDataPoint(mousePosition);
                spawn.X = snapped.X;
                spawn.Y = snapped.Y;
            }
            else
            {
                spawn.X = mousePosition.X - blueSpawnDragOffset.X;
                spawn.Y = mousePosition.Y - blueSpawnDragOffset.Y;
            }

            SelectBlueSpawn(index);
            RenderStadium();
            UpdateObjectsList();
            UpdateJsonPreview();
        }

        private void FinishBlueSpawnDrag()
        {
            isDraggingBlueSpawn = false;
            draggingBlueSpawnIndex = null;

            ReleaseCanvasMouseIfSafe();
            UpdateJsonPreview();
            UpdateObjectsList();
        }

        private void CancelBlueSpawnDrag()
        {
            isDraggingBlueSpawn = false;
            draggingBlueSpawnIndex = null;
            ReleaseCanvasMouseIfSafe();
        }

        private void RequestCurveDragRender(bool force = false)
        {
            DateTime now = DateTime.UtcNow;
            if (!force && (now - lastCurveDragRenderTime).TotalMilliseconds < CurveDragRenderIntervalMs)
            {
                return;
            }

            lastCurveDragRenderTime = now;
            RenderStadium();
        }

        private void BeginCurveHandleDrag(int segmentIndex)
        {
            if (segmentIndex < 0 || segmentIndex >= stadium.Segments.Count) return;
            if (IsObjectLocked("Segment", segmentIndex))
            {
                UpdateStatus($"Segment #{segmentIndex} is locked.");
                return;
            }
            PushUndoState("Edit Segment Curve");

            isDraggingCurveHandle = true;
            draggingCurveSegmentIndex = segmentIndex;
            lastCurveDragRenderTime = DateTime.MinValue;

            MapCanvas.CaptureMouse();
        }

        private void DragCurveHandle(Point mousePosition)
        {
            mousePosition = SnapDataPoint(mousePosition);
            if (draggingCurveSegmentIndex == null) return;

            int segmentIndex = draggingCurveSegmentIndex.Value;
            if (segmentIndex < 0 || segmentIndex >= stadium.Segments.Count) return;

            SegmentData segment = stadium.Segments[segmentIndex];

            if (!TryGetSegmentPoints(segment, out Point p0, out Point p1)) return;

            double dx = p1.X - p0.X;
            double dy = p1.Y - p0.Y;
            double length = Math.Sqrt(dx * dx + dy * dy);

            if (length <= 0.0001) return;

            Point mid = new((p0.X + p1.X) / 2.0, (p0.Y + p1.Y) / 2.0);

            double normalX = -dy / length;
            double normalY = dx / length;

            double mouseDx = mousePosition.X - mid.X;
            double mouseDy = mousePosition.Y - mid.Y;

            double signedOffset = (mouseDx * normalX) + (mouseDy * normalY);
            double absOffset = Math.Abs(signedOffset);
            double halfChord = length / 2.0;

            double angleRad = 4.0 * Math.Atan(absOffset / halfChord);
            double angleDeg = angleRad * 180.0 / Math.PI;

            // Invert the sign here too, so dragging the handle toward the visible
            // HaxBall side writes the same curve sign that the game expects.
            double curve = Math.Round(-Math.Sign(signedOffset) * angleDeg, 2);

            segment.Curve = Math.Abs(curve) < 0.5 ? null : curve;

            if (selectedSegmentIndex != segmentIndex)
            {
                SelectSegment(segmentIndex);
            }
            else if (CurveTextBox != null)
            {
                CurveTextBox.Text = (segment.Curve ?? 0).ToString("0.##", CultureInfo.InvariantCulture);
            }

            RequestCurveDragRender();
        }

        private void FinishCurveHandleDrag()
        {
            int? finishedSegmentIndex = draggingCurveSegmentIndex;

            isDraggingCurveHandle = false;
            draggingCurveSegmentIndex = null;

            ReleaseCanvasMouseIfSafe();

            if (finishedSegmentIndex != null)
            {
                SelectSegment(finishedSegmentIndex.Value);
            }

            RenderStadium();
            UpdateJsonPreview();
            UpdateObjectsList();
            UpdateStatus("Segment curve updated.");
        }

        private void CancelCurveHandleDrag()
        {
            isDraggingCurveHandle = false;
            draggingCurveSegmentIndex = null;
            ReleaseCanvasMouseIfSafe();
            RenderStadium();
        }

        private void UpdateInspectorForSelection(string selectionType)
        {
            bool hasSelection = selectionType != "None";
            bool isMulti = selectionType == "Multi";
            bool isSegment = selectionType == "Segment";
            bool isDisc = selectionType == "Disc";
            bool isGoal = selectionType == "Goal";
            bool isPlane = selectionType == "Plane";
            bool isJoint = selectionType == "Joint";
            bool isVertex = selectionType == "Vertex";
            bool isSpawn = selectionType == "RedSpawn" || selectionType == "BlueSpawn";

            SetVisibility(MultiPropertiesExpander, isMulti);
            SetVisibility(ObjectPropertiesExpander, isSegment || isDisc || isPlane || isJoint || isVertex || isSpawn || isGoal);
            SetVisibility(GoalTeamExpander, isGoal);
            SetVisibility(CollisionExpander, false);

            // Utility panels stay available only when nothing specific is selected.
            SetVisibility(JointToolExpander, !hasSelection);
            SetVisibility(BackgroundExpander, !hasSelection);
            SetVisibility(StadiumPhysicsExpander, !hasSelection);

            if (!hasSelection)
            {
                UpdateStadiumPhysicsUiFromData();
            }

            SetVisibility(ObjectXFieldPanel, isVertex || isDisc || isSpawn);
            SetVisibility(ObjectYFieldPanel, isVertex || isDisc || isSpawn);
            SetVisibility(RadiusFieldPanel, isDisc);
            SetVisibility(CurveFieldPanel, isSegment);
            SetVisibility(InvMassFieldPanel, isDisc);
            SetVisibility(DampingFieldPanel, isDisc);
            SetVisibility(SpeedFieldPanel, isDisc);
            SetVisibility(GravityFieldPanel, isDisc);
            SetVisibility(ColorFieldPanel, isSegment || isDisc || isJoint);
            SetVisibility(BCoefFieldPanel, isSegment || isDisc || isPlane);
            SetVisibility(TraitFieldPanel, isSegment || isDisc || isPlane);
            SetVisibility(BiasFieldPanel, isSegment || isDisc || isPlane);
            SetVisibility(VisFieldPanel, isSegment || isDisc || isPlane);
            SetVisibility(CollisionTextFieldsPanel, isSegment || isDisc || isPlane);

            if (ObjectPropertiesExpander.Visibility == Visibility.Visible)
            {
                ObjectPropertiesExpander.IsExpanded = true;
            }

            if (GoalTeamExpander.Visibility == Visibility.Visible)
            {
                GoalTeamExpander.IsExpanded = true;
            }

            if (CollisionExpander.Visibility == Visibility.Visible)
            {
                CollisionExpander.IsExpanded = true;
            }

            if (!hasSelection)
            {
                JointToolExpander.IsExpanded = false;
                BackgroundExpander.IsExpanded = false;
                StadiumPhysicsExpander.IsExpanded = true;
            }

            if (isMulti)
            {
                MultiPropertiesExpander.IsExpanded = true;
                ObjectPropertiesExpander.IsExpanded = false;
            }
        }

        private void SetVisibility(UIElement element, bool visible)
        {
            element.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ClearSelection()
        {
            selectedVertexIndex = null;
            selectedSegmentIndex = null;
            selectedDiscIndex = null;
            selectedGoalIndex = null;
            selectedPlaneIndex = null;
            selectedJointIndex = null;
            selectedRedSpawnIndex = null;
            selectedBlueSpawnIndex = null;
            selectedItems.Clear();

            SelectedObjectText.Text = "None";
            SelectionInfoTextBlock.Text = "No selection";
            PositionXTextBox.Text = "0";
            PositionYTextBox.Text = "0";
            ObjectXTextBox.Text = "";
            ObjectYTextBox.Text = "";
            RadiusTextBox.Text = "0";
            ObjectColorTextBox.Text = "";
            CurveTextBox.Text = "0";
            BCoefTextBox.Text = "";
            InvMassTextBox.Text = "";
            TraitTextBox.Text = "";
            BiasTextBox.Text = "";
            DampingTextBox.Text = "";
            SpeedXTextBox.Text = "";
            SpeedYTextBox.Text = "";
            GravityXTextBox.Text = "";
            GravityYTextBox.Text = "";
            SetVisComboBoxFromData(null);
            ClearMultiPropertyInputs();

            SetCollisionUiFromData(null, null);
            UpdateInspectorForSelection("None");
        }

        private void ClearSelectionIndexesOnly()
        {
            selectedVertexIndex = null;
            selectedSegmentIndex = null;
            selectedDiscIndex = null;
            selectedGoalIndex = null;
            selectedPlaneIndex = null;
            selectedJointIndex = null;
            selectedRedSpawnIndex = null;
            selectedBlueSpawnIndex = null;
            selectedItems.Clear();
        }

        private void SelectVertex(int index)
        {
            ClearSelectionIndexesOnly();
            selectedVertexIndex = index;
            selectedItems.Add(new SelectedItem("Vertex", index));

            VertexData vertex = stadium.Vertexes[index];

            SelectedObjectText.Text = $"Vertex #{index}";
            SelectionInfoTextBlock.Text = BuildSelectionInfoText("Vertex", index);
            string xText = (vertex.X - GetCanvasCenterX()).ToString("0.##", CultureInfo.InvariantCulture);
            string yText = (vertex.Y - GetCanvasCenterY()).ToString("0.##", CultureInfo.InvariantCulture);
            PositionXTextBox.Text = xText;
            PositionYTextBox.Text = yText;
            ObjectXTextBox.Text = xText;
            ObjectYTextBox.Text = yText;
            RadiusTextBox.Text = "0";
            ObjectColorTextBox.Text = "";
            CurveTextBox.Text = "0";
            BCoefTextBox.Text = "";
            InvMassTextBox.Text = "";
            TraitTextBox.Text = "";
            BiasTextBox.Text = "";
            SetVisComboBoxFromData(null);

            SetCollisionUiFromData(null, null);
            UpdateInspectorForSelection("Vertex");
        }

        private void SelectSegment(int index)
        {
            if (index < 0 || index >= stadium.Segments.Count) return;

            ClearSelectionIndexesOnly();
            selectedSegmentIndex = index;
            selectedItems.Add(new SelectedItem("Segment", index));

            SegmentData segment = stadium.Segments[index];

            SelectedObjectText.Text = $"Segment #{index}";
            SelectionInfoTextBlock.Text = BuildSelectionInfoText("Segment", index);
            PositionXTextBox.Text = $"v0: {segment.V0}";
            PositionYTextBox.Text = $"v1: {segment.V1}";
            ObjectXTextBox.Text = "";
            ObjectYTextBox.Text = "";
            RadiusTextBox.Text = "0";
            ObjectColorTextBox.Text = FormatColorForUi(segment.Color);
            CurveTextBox.Text = (segment.Curve ?? 0).ToString("0.##", CultureInfo.InvariantCulture);
            BCoefTextBox.Text = segment.BCoef?.ToString("0.##", CultureInfo.InvariantCulture) ?? "";
            InvMassTextBox.Text = "";
            TraitTextBox.Text = GetExtensionString(segment.ExtensionData, "trait");
            BiasTextBox.Text = GetExtensionDoubleString(segment.ExtensionData, "bias");
            SetVisComboBoxFromData(GetExtensionBool(segment.ExtensionData, "vis"));

            SetCollisionUiFromData(segment.CGroup, segment.CMask);
            UpdateInspectorForSelection("Segment");
        }

        private void SelectDisc(int index)
        {
            if (index < 0 || index >= stadium.Discs.Count) return;

            ClearSelectionIndexesOnly();
            selectedDiscIndex = index;
            selectedItems.Add(new SelectedItem("Disc", index));

            DiscData disc = stadium.Discs[index];

            SelectedObjectText.Text = $"Disc #{index}";
            SelectionInfoTextBlock.Text = BuildSelectionInfoText("Disc", index);
            string xText = (disc.X - GetCanvasCenterX()).ToString("0.##", CultureInfo.InvariantCulture);
            string yText = (disc.Y - GetCanvasCenterY()).ToString("0.##", CultureInfo.InvariantCulture);
            PositionXTextBox.Text = xText;
            PositionYTextBox.Text = yText;
            ObjectXTextBox.Text = xText;
            ObjectYTextBox.Text = yText;
            RadiusTextBox.Text = (disc.Radius ?? DefaultDiscRadius).ToString("0.##", CultureInfo.InvariantCulture);
            ObjectColorTextBox.Text = FormatColorForUi(disc.Color);
            CurveTextBox.Text = "0";
            BCoefTextBox.Text = disc.BCoef?.ToString("0.##", CultureInfo.InvariantCulture) ?? "";
            InvMassTextBox.Text = disc.InvMass?.ToString("0.##", CultureInfo.InvariantCulture) ?? "";
            TraitTextBox.Text = GetExtensionString(disc.ExtensionData, "trait");
            BiasTextBox.Text = GetExtensionDoubleString(disc.ExtensionData, "bias");
            DampingTextBox.Text = GetExtensionDoubleString(disc.ExtensionData, "damping");
            SpeedXTextBox.Text = GetExtensionVectorComponentString(disc.ExtensionData, "speed", 0);
            SpeedYTextBox.Text = GetExtensionVectorComponentString(disc.ExtensionData, "speed", 1);
            GravityXTextBox.Text = GetExtensionVectorComponentString(disc.ExtensionData, "gravity", 0);
            GravityYTextBox.Text = GetExtensionVectorComponentString(disc.ExtensionData, "gravity", 1);
            SetVisComboBoxFromData(GetExtensionBool(disc.ExtensionData, "vis"));

            SetCollisionUiFromData(disc.CGroup, disc.CMask);
            UpdateInspectorForSelection("Disc");
        }

        private void SelectGoal(int index)
        {
            if (index < 0 || index >= stadium.Goals.Count) return;

            ClearSelectionIndexesOnly();
            selectedGoalIndex = index;
            selectedItems.Add(new SelectedItem("Goal", index));

            GoalData goal = stadium.Goals[index];

            SelectedObjectText.Text = $"Goal #{index}";
            SelectionInfoTextBlock.Text = BuildSelectionInfoText("Goal", index);
            PositionXTextBox.Text = $"p0: {goal.X0 - GetCanvasCenterX():0.##}, {goal.Y0 - GetCanvasCenterY():0.##}";
            PositionYTextBox.Text = $"p1: {goal.X1 - GetCanvasCenterX():0.##}, {goal.Y1 - GetCanvasCenterY():0.##}";
            ObjectXTextBox.Text = "";
            ObjectYTextBox.Text = "";
            RadiusTextBox.Text = "0";
            ObjectColorTextBox.Text = "";
            CurveTextBox.Text = "0";
            BCoefTextBox.Text = "";
            InvMassTextBox.Text = "";
            TraitTextBox.Text = "";
            BiasTextBox.Text = "";
            SetVisComboBoxFromData(null);

            SetCollisionUiFromData(null, null);
            SetGoalTeamComboBox(goal.Team);
            UpdateInspectorForSelection("Goal");
        }

        private void SelectPlane(int index)
        {
            if (index < 0 || index >= stadium.Planes.Count) return;

            ClearSelectionIndexesOnly();
            selectedPlaneIndex = index;
            selectedItems.Add(new SelectedItem("Plane", index));

            PlaneData plane = stadium.Planes[index];

            string normalText = plane.Normal != null && plane.Normal.Count >= 2
                ? $"{plane.Normal[0]:0.###}, {plane.Normal[1]:0.###}"
                : "invalid";

            SelectedObjectText.Text = $"Plane #{index}";
            SelectionInfoTextBlock.Text = BuildSelectionInfoText("Plane", index);
            PositionXTextBox.Text = $"normal: {normalText}";
            PositionYTextBox.Text = $"dist: {plane.Dist:0.##}";
            ObjectXTextBox.Text = "";
            ObjectYTextBox.Text = "";
            RadiusTextBox.Text = "0";
            ObjectColorTextBox.Text = "";
            CurveTextBox.Text = "0";
            BCoefTextBox.Text = plane.BCoef?.ToString("0.##", CultureInfo.InvariantCulture) ?? "";
            InvMassTextBox.Text = "";
            TraitTextBox.Text = GetExtensionString(plane.ExtensionData, "trait");
            BiasTextBox.Text = GetExtensionDoubleString(plane.ExtensionData, "bias");
            SetVisComboBoxFromData(GetExtensionBool(plane.ExtensionData, "vis"));

            SetCollisionUiFromData(plane.CGroup, plane.CMask);
            UpdateInspectorForSelection("Plane");
        }

        private void SelectJoint(int index)
        {
            if (index < 0 || index >= stadium.Joints.Count) return;

            ClearSelectionIndexesOnly();
            selectedJointIndex = index;
            selectedItems.Add(new SelectedItem("Joint", index));

            JointData joint = stadium.Joints[index];

            SelectedObjectText.Text = $"Joint #{index}";
            SelectionInfoTextBlock.Text = BuildSelectionInfoText("Joint", index);
            PositionXTextBox.Text = $"d0: {joint.D0}";
            PositionYTextBox.Text = $"d1: {joint.D1}";
            ObjectXTextBox.Text = "";
            ObjectYTextBox.Text = "";
            RadiusTextBox.Text = "0";
            ObjectColorTextBox.Text = FormatColorForUi(joint.Color);
            CurveTextBox.Text = "0";
            BCoefTextBox.Text = "";
            InvMassTextBox.Text = "";
            TraitTextBox.Text = "";
            BiasTextBox.Text = "";
            SetVisComboBoxFromData(null);

            SetCollisionUiFromData(null, null);
            UpdateInspectorForSelection("Joint");
        }

        private void SelectRedSpawn(int index)
        {
            if (index < 0 || index >= stadium.RedSpawnPoints.Count) return;

            ClearSelectionIndexesOnly();
            selectedRedSpawnIndex = index;
            selectedItems.Add(new SelectedItem("RedSpawn", index));

            SpawnPointData spawn = stadium.RedSpawnPoints[index];

            SelectedObjectText.Text = $"Red Spawn #{index}";
            SelectionInfoTextBlock.Text = BuildSelectionInfoText("RedSpawn", index);
            string xText = (spawn.X - GetCanvasCenterX()).ToString("0.##", CultureInfo.InvariantCulture);
            string yText = (spawn.Y - GetCanvasCenterY()).ToString("0.##", CultureInfo.InvariantCulture);
            PositionXTextBox.Text = xText;
            PositionYTextBox.Text = yText;
            ObjectXTextBox.Text = xText;
            ObjectYTextBox.Text = yText;
            RadiusTextBox.Text = "0";
            ObjectColorTextBox.Text = "";
            CurveTextBox.Text = "0";
            BCoefTextBox.Text = "";
            InvMassTextBox.Text = "";
            TraitTextBox.Text = "";
            BiasTextBox.Text = "";
            SetVisComboBoxFromData(null);

            SetCollisionUiFromData(null, null);
            UpdateInspectorForSelection("RedSpawn");
        }

        private void SelectBlueSpawn(int index)
        {
            if (index < 0 || index >= stadium.BlueSpawnPoints.Count) return;

            ClearSelectionIndexesOnly();
            selectedBlueSpawnIndex = index;
            selectedItems.Add(new SelectedItem("BlueSpawn", index));

            SpawnPointData spawn = stadium.BlueSpawnPoints[index];

            SelectedObjectText.Text = $"Blue Spawn #{index}";
            SelectionInfoTextBlock.Text = BuildSelectionInfoText("BlueSpawn", index);
            string xText = (spawn.X - GetCanvasCenterX()).ToString("0.##", CultureInfo.InvariantCulture);
            string yText = (spawn.Y - GetCanvasCenterY()).ToString("0.##", CultureInfo.InvariantCulture);
            PositionXTextBox.Text = xText;
            PositionYTextBox.Text = yText;
            ObjectXTextBox.Text = xText;
            ObjectYTextBox.Text = yText;
            RadiusTextBox.Text = "0";
            ObjectColorTextBox.Text = "";
            CurveTextBox.Text = "0";
            BCoefTextBox.Text = "";
            InvMassTextBox.Text = "";
            TraitTextBox.Text = "";
            BiasTextBox.Text = "";
            SetVisComboBoxFromData(null);

            SetCollisionUiFromData(null, null);
            UpdateInspectorForSelection("BlueSpawn");
        }

        private void DeleteSelectedVertex()
        {
            if (selectedVertexIndex == null) return;
            if (IsObjectLocked("Vertex", selectedVertexIndex.Value)) { UpdateStatus($"Vertex #{selectedVertexIndex.Value} is locked."); return; }
            PushUndoState("Delete Vertex");

            int deletedIndex = selectedVertexIndex.Value;

            if (deletedIndex < 0 || deletedIndex >= stadium.Vertexes.Count)
            {
                ClearSelection();
                return;
            }

            stadium.Vertexes.RemoveAt(deletedIndex);

            List<SegmentData> updatedSegments = new();

            foreach (SegmentData segment in stadium.Segments)
            {
                if (segment.V0 == deletedIndex || segment.V1 == deletedIndex)
                {
                    continue;
                }

                int newV0 = segment.V0 > deletedIndex ? segment.V0 - 1 : segment.V0;
                int newV1 = segment.V1 > deletedIndex ? segment.V1 - 1 : segment.V1;

                updatedSegments.Add(new SegmentData
                {
                    V0 = newV0,
                    V1 = newV1,
                    Color = segment.Color,
                    Curve = segment.Curve,
                    BCoef = segment.BCoef,
                    CGroup = segment.CGroup,
                    CMask = segment.CMask,
                    ExtensionData = CloneExtensionData(segment.ExtensionData)
                });
            }

            stadium.Segments = updatedSegments;

            ClearSelection();
            RenderStadium();
            UpdateObjectCount();
            UpdateObjectsList();
            UpdateJsonPreview();

            UpdateStatus($"Vertex #{deletedIndex} deleted. Connected segments removed.");
        }

        private void DeleteSelectedSegment()
        {
            if (selectedSegmentIndex == null) return;
            if (IsObjectLocked("Segment", selectedSegmentIndex.Value)) { UpdateStatus($"Segment #{selectedSegmentIndex.Value} is locked."); return; }
            PushUndoState("Delete Segment");

            int deletedIndex = selectedSegmentIndex.Value;

            if (deletedIndex < 0 || deletedIndex >= stadium.Segments.Count)
            {
                ClearSelection();
                return;
            }

            stadium.Segments.RemoveAt(deletedIndex);

            ClearSelection();
            RenderStadium();
            UpdateObjectCount();
            UpdateObjectsList();
            UpdateJsonPreview();

            UpdateStatus($"Segment #{deletedIndex} deleted.");
        }

        private void DeleteSelectedDisc()
        {
            if (selectedDiscIndex == null) return;
            if (IsObjectLocked("Disc", selectedDiscIndex.Value)) { UpdateStatus($"Disc #{selectedDiscIndex.Value} is locked."); return; }
            PushUndoState("Delete Disc");

            int deletedIndex = selectedDiscIndex.Value;

            if (deletedIndex < 0 || deletedIndex >= stadium.Discs.Count)
            {
                ClearSelection();
                return;
            }

            stadium.Discs.RemoveAt(deletedIndex);

            List<JointData> updatedJoints = new();

            foreach (JointData joint in stadium.Joints)
            {
                if (joint.D0 == deletedIndex || joint.D1 == deletedIndex)
                {
                    continue;
                }

                int newD0 = joint.D0 > deletedIndex ? joint.D0 - 1 : joint.D0;
                int newD1 = joint.D1 > deletedIndex ? joint.D1 - 1 : joint.D1;

                updatedJoints.Add(new JointData
                {
                    D0 = newD0,
                    D1 = newD1,
                    Strength = joint.Strength,
                    Length = joint.Length,
                    Color = joint.Color,
                    ExtensionData = CloneExtensionData(joint.ExtensionData)
                });
            }

            stadium.Joints = updatedJoints;

            ClearSelection();
            RenderStadium();
            UpdateObjectCount();
            UpdateObjectsList();
            UpdateJsonPreview();

            UpdateStatus($"Disc #{deletedIndex} deleted. Connected joints removed.");
        }

        private void DeleteSelectedGoal()
        {
            if (selectedGoalIndex == null) return;
            if (IsObjectLocked("Goal", selectedGoalIndex.Value)) { UpdateStatus($"Goal #{selectedGoalIndex.Value} is locked."); return; }
            PushUndoState("Delete Goal");

            int deletedIndex = selectedGoalIndex.Value;

            if (deletedIndex < 0 || deletedIndex >= stadium.Goals.Count)
            {
                ClearSelection();
                return;
            }

            stadium.Goals.RemoveAt(deletedIndex);

            ClearSelection();
            RenderStadium();
            UpdateObjectCount();
            UpdateObjectsList();
            UpdateJsonPreview();

            UpdateStatus($"Goal #{deletedIndex} deleted.");
        }

        private void DeleteSelectedPlane()
        {
            if (selectedPlaneIndex == null) return;
            if (IsObjectLocked("Plane", selectedPlaneIndex.Value)) { UpdateStatus($"Plane #{selectedPlaneIndex.Value} is locked."); return; }
            PushUndoState("Delete Plane");

            int deletedIndex = selectedPlaneIndex.Value;

            if (deletedIndex < 0 || deletedIndex >= stadium.Planes.Count)
            {
                ClearSelection();
                return;
            }

            stadium.Planes.RemoveAt(deletedIndex);

            ClearSelection();
            RenderStadium();
            UpdateObjectCount();
            UpdateObjectsList();
            UpdateJsonPreview();

            UpdateStatus($"Plane #{deletedIndex} deleted.");
        }

        private void DeleteSelectedJoint()
        {
            if (selectedJointIndex == null) return;
            if (IsObjectLocked("Joint", selectedJointIndex.Value)) { UpdateStatus($"Joint #{selectedJointIndex.Value} is locked."); return; }
            PushUndoState("Delete Joint");

            int deletedIndex = selectedJointIndex.Value;

            if (deletedIndex < 0 || deletedIndex >= stadium.Joints.Count)
            {
                ClearSelection();
                return;
            }

            stadium.Joints.RemoveAt(deletedIndex);

            ClearSelection();
            RenderStadium();
            UpdateObjectCount();
            UpdateObjectsList();
            UpdateJsonPreview();

            UpdateStatus($"Joint #{deletedIndex} deleted.");
        }

        private void DeleteSelectedRedSpawn()
        {
            if (selectedRedSpawnIndex == null) return;
            if (IsObjectLocked("RedSpawn", selectedRedSpawnIndex.Value)) { UpdateStatus($"Red Spawn #{selectedRedSpawnIndex.Value} is locked."); return; }
            PushUndoState("Delete Red Spawn");

            int deletedIndex = selectedRedSpawnIndex.Value;

            if (deletedIndex < 0 || deletedIndex >= stadium.RedSpawnPoints.Count)
            {
                ClearSelection();
                return;
            }

            stadium.RedSpawnPoints.RemoveAt(deletedIndex);

            ClearSelection();
            RenderStadium();
            UpdateObjectCount();
            UpdateObjectsList();
            UpdateJsonPreview();

            UpdateStatus($"Red Spawn #{deletedIndex} deleted.");
        }

        private void DeleteSelectedBlueSpawn()
        {
            if (selectedBlueSpawnIndex == null) return;
            if (IsObjectLocked("BlueSpawn", selectedBlueSpawnIndex.Value)) { UpdateStatus($"Blue Spawn #{selectedBlueSpawnIndex.Value} is locked."); return; }
            PushUndoState("Delete Blue Spawn");

            int deletedIndex = selectedBlueSpawnIndex.Value;

            if (deletedIndex < 0 || deletedIndex >= stadium.BlueSpawnPoints.Count)
            {
                ClearSelection();
                return;
            }

            stadium.BlueSpawnPoints.RemoveAt(deletedIndex);

            ClearSelection();
            RenderStadium();
            UpdateObjectCount();
            UpdateObjectsList();
            UpdateJsonPreview();

            UpdateStatus($"Blue Spawn #{deletedIndex} deleted.");
        }

        private void BeginSegmentDrag(Point startPoint, int? startVertexIndex)
        {
            isDraggingSegment = true;
            segmentDragStartPoint = startPoint;
            segmentDragStartVertexIndex = startVertexIndex;

            SegmentFirstVertexText.Text = startVertexIndex != null ? $"Vertex #{startVertexIndex.Value}" : "New Vertex";

            Point screenStart = DataToScreenPoint(startPoint.X, startPoint.Y);

            segmentPreviewLine = new Line
            {
                X1 = screenStart.X,
                Y1 = screenStart.Y,
                X2 = screenStart.X,
                Y2 = screenStart.Y,
                Stroke = Brushes.DeepSkyBlue,
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 4, 4 }
            };

            Panel.SetZIndex(segmentPreviewLine, 5);

            MapCanvas.Children.Add(segmentPreviewLine);
            MapCanvas.CaptureMouse();

            UpdateStatus("Dragging segment.");
        }

        private void FinishSegmentDrag(Point endPoint)
        {
            double distance = GetDistance(segmentDragStartPoint, endPoint);

            if (distance < MinimumSegmentLength)
            {
                CancelSegmentDrag();
                UpdateStatus("Segment cancelled. Too short.");
                return;
            }

            PushUndoState("Add Segment");
            segmentDragStartPoint = SnapDataPoint(segmentDragStartPoint);
            endPoint = SnapDataPoint(endPoint);

            suppressUndoPush = true;
            int startVertexIndex;
            int endVertexIndex;
            try
            {
                startVertexIndex = segmentDragStartVertexIndex ?? AddVertex(segmentDragStartPoint.X, segmentDragStartPoint.Y);
                int? existingEndVertexIndex = FindNearestVertexIndex(endPoint, startVertexIndex);
                endVertexIndex = existingEndVertexIndex ?? AddVertex(endPoint.X, endPoint.Y);
            }
            finally
            {
                suppressUndoPush = false;
            }

            if (startVertexIndex == endVertexIndex)
            {
                CancelSegmentDrag();
                UpdateStatus("Segment cancelled. Start and end vertex are the same.");
                return;
            }

            int segmentIndex = AddSegment(startVertexIndex, endVertexIndex);

            if (autoMirrorPlacement)
            {
                VertexData startVertex = stadium.Vertexes[startVertexIndex];
                VertexData endVertex = stadium.Vertexes[endVertexIndex];

                VertexData mirroredStart = CloneData(startVertex);
                VertexData mirroredEnd = CloneData(endVertex);
                mirroredStart.X = MirrorCanvasX(mirroredStart.X);
                mirroredEnd.X = MirrorCanvasX(mirroredEnd.X);

                if (Math.Abs(mirroredStart.X - startVertex.X) > 0.001 || Math.Abs(mirroredEnd.X - endVertex.X) > 0.001)
                {
                    stadium.Vertexes.Add(mirroredStart);
                    int mirroredStartIndex = stadium.Vertexes.Count - 1;
                    stadium.Vertexes.Add(mirroredEnd);
                    int mirroredEndIndex = stadium.Vertexes.Count - 1;

                    int mirroredSegmentIndex = AddSegment(mirroredStartIndex, mirroredEndIndex, true);
                    SelectMirroredPair("Segment", segmentIndex, mirroredSegmentIndex);
                    UpdateStatus($"Segment #{segmentIndex} added with mirrored Segment #{mirroredSegmentIndex}.");
                }
                else
                {
                    SelectSegment(segmentIndex);
                    UpdateStatus($"Segment #{segmentIndex} added on mirror axis.");
                }

                RenderStadium();
                UpdateObjectCount();
                UpdateObjectsList();
                UpdateJsonPreview();
            }
            else
            {
                UpdateStatus($"Segment added between Vertex #{startVertexIndex} and Vertex #{endVertexIndex}.");
            }

            CancelSegmentDrag();
        }

        private void CancelSegmentDrag()
        {
            isDraggingSegment = false;
            segmentDragStartVertexIndex = null;
            SegmentFirstVertexText.Text = "None";

            if (segmentPreviewLine != null)
            {
                MapCanvas.Children.Remove(segmentPreviewLine);
                segmentPreviewLine = null;
            }

            ReleaseCanvasMouseIfSafe();
        }

        private int? FindNearestVertexIndex(Point point, int? ignoreIndex = null)
        {
            for (int i = 0; i < stadium.Vertexes.Count; i++)
            {
                if (ignoreIndex != null && i == ignoreIndex.Value) continue;

                VertexData vertex = stadium.Vertexes[i];
                double distance = GetDistance(point, new Point(vertex.X, vertex.Y));

                if (distance <= VertexHitRadius)
                {
                    return i;
                }
            }

            return null;
        }

        private double GetDistance(Point a, Point b)
        {
            double dx = a.X - b.X;
            double dy = a.Y - b.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        private int AddSegment(int v0, int v1, bool deferRefresh = false)
        {
            stadium.Segments.Add(new SegmentData
            {
                V0 = v0,
                V1 = v1,
                Color = null,
                Curve = null,
                BCoef = null,
                CGroup = null,
                CMask = null
            });

            int index = stadium.Segments.Count - 1;

            if (!deferRefresh)
            {
                SelectSegment(index);
                RenderStadium();
                UpdateObjectCount();
                UpdateObjectsList();
                UpdateJsonPreview();
            }

            return index;
        }

        private bool TryGetSegmentPoints(SegmentData segment, out Point p0, out Point p1)
        {
            p0 = new Point();
            p1 = new Point();

            if (segment.V0 < 0 || segment.V0 >= stadium.Vertexes.Count) return false;
            if (segment.V1 < 0 || segment.V1 >= stadium.Vertexes.Count) return false;

            VertexData first = stadium.Vertexes[segment.V0];
            VertexData second = stadium.Vertexes[segment.V1];

            p0 = new Point(first.X, first.Y);
            p1 = new Point(second.X, second.Y);

            return true;
        }

        private void DrawSegment(int segmentIndex, int v0, int v1)
        {
            if (segmentIndex < 0 || segmentIndex >= stadium.Segments.Count) return;
            if (!showViewportSegments && selectedSegmentIndex != segmentIndex && !IsObjectSelected("Segment", segmentIndex)) return;
            if (IsObjectHidden("Segment", segmentIndex)) return;

            SegmentData segment = stadium.Segments[segmentIndex];

            if (!TryGetSegmentPoints(segment, out Point p0, out Point p1)) return;

            bool isSelected = selectedSegmentIndex == segmentIndex || IsObjectSelected("Segment", segmentIndex);

            Point screenP0 = DataToScreenPoint(p0.X, p0.Y);
            Point screenP1 = DataToScreenPoint(p1.X, p1.Y);
            bool isVisible = IsSegmentVisible(segment);

            if (!isVisible && !showViewportInvisibleObjects && !isSelected)
            {
                return;
            }

            Shape hitShape;

            if (isSelected)
            {
                bool lightweightCurveDrag = isDraggingCurveHandle && draggingCurveSegmentIndex == segmentIndex;

                Shape selectionShape = CreateSegmentShape(
                    screenP0,
                    screenP1,
                    segment.Curve,
                    lightweightCurveDrag ? SelectionAccentBrush : SelectionAccentSoftBrush,
                    lightweightCurveDrag ? Math.Max(2, ScaleLength(3)) : Math.Max(4, ScaleLength(isVisible ? 8 : 5)));

                selectionShape.IsHitTestVisible = false;
                Panel.SetZIndex(selectionShape, 5);
                MapCanvas.Children.Add(selectionShape);

                if (!lightweightCurveDrag)
                {
                    Shape selectionOutline = CreateSegmentShape(screenP0, screenP1, segment.Curve, SelectionAccentBrush, Math.Max(2, ScaleLength(isVisible ? 4 : 3)));
                    selectionOutline.IsHitTestVisible = false;
                    Panel.SetZIndex(selectionOutline, 6);
                    MapCanvas.Children.Add(selectionOutline);

                    DrawSelectionEndpointMarkers(screenP0, screenP1, 28);
                }
            }

            if (isVisible)
            {
                Brush segmentBrush = GetSegmentBrush(segment);
                hitShape = CreateSegmentShape(screenP0, screenP1, segment.Curve, segmentBrush, Math.Max(1, ScaleLength(3)));
                Panel.SetZIndex(hitShape, isSelected ? 6 : 3);
            }
            else
            {
                hitShape = CreateSegmentShape(screenP0, screenP1, segment.Curve, new SolidColorBrush(Color.FromArgb(220, 255, 255, 255)), Math.Max(1, ScaleLength(2)));
                Panel.SetZIndex(hitShape, isSelected ? 6 : 3);

                Shape thinInvisibleShape = CreateSegmentShape(screenP0, screenP1, segment.Curve, new SolidColorBrush(Color.FromArgb(220, 0, 0, 0)), Math.Max(1, ScaleLength(1)));
                thinInvisibleShape.IsHitTestVisible = false;
                Panel.SetZIndex(thinInvisibleShape, isSelected ? 7 : 4);
                MapCanvas.Children.Add(thinInvisibleShape);
            }

            hitShape.Cursor = Cursors.Hand;
            segmentShapeIndexes[hitShape] = segmentIndex;
            hitShape.MouseLeftButtonDown += SegmentShape_MouseLeftButtonDown;
            MapCanvas.Children.Add(hitShape);

            if (isSelected)
            {
                Point handlePoint = GetHaxBallArcHandlePoint(screenP0, screenP1, segment.Curve ?? 0);
                DrawCurveHandle(segmentIndex, screenP0, screenP1, handlePoint);

                if (!isDraggingCurveHandle || draggingCurveSegmentIndex != segmentIndex)
                {
                    DrawSelectionFilledDot(handlePoint, 3.5, 29, SelectionCyanBrush, Brushes.White);
                }
            }
        }


        private Shape CreateSegmentShape(Point p0, Point p1, double? curve, Brush stroke, double thickness)
        {
            if (curve != null && Math.Abs(curve.Value) > 0.0001)
            {
                return new System.Windows.Shapes.Path
                {
                    Data = CreateHaxBallArcGeometry(p0, p1, curve.Value),
                    Stroke = stroke,
                    StrokeThickness = thickness,
                    Fill = Brushes.Transparent
                };
            }

            return new Line
            {
                X1 = p0.X,
                Y1 = p0.Y,
                X2 = p1.X,
                Y2 = p1.Y,
                Stroke = stroke,
                StrokeThickness = thickness
            };
        }

        private Brush GetSegmentBrush(SegmentData segment)
        {
            if (!string.IsNullOrWhiteSpace(segment.Color) && !string.Equals(segment.Color, "transparent", StringComparison.OrdinalIgnoreCase))
            {
                return new SolidColorBrush(ColorFromHex(segment.Color));
            }

            return Brushes.Black;
        }

        private bool IsSegmentVisible(SegmentData segment)
        {
            bool? directVis = TryGetExtensionBool(segment.ExtensionData, "vis");
            if (directVis != null)
            {
                return directVis.Value;
            }

            string? traitName = TryGetExtensionString(segment.ExtensionData, "trait");
            if (!string.IsNullOrWhiteSpace(traitName) && stadium.Traits != null && stadium.Traits.TryGetValue(traitName, out TraitData? trait))
            {
                if (trait.Vis != null)
                {
                    return trait.Vis.Value;
                }
            }

            return true;
        }

        private bool IsDiscVisible(DiscData disc)
        {
            bool? directVis = TryGetExtensionBool(disc.ExtensionData, "vis");
            if (directVis != null)
            {
                return directVis.Value;
            }

            string? traitName = TryGetExtensionString(disc.ExtensionData, "trait");
            if (!string.IsNullOrWhiteSpace(traitName) && stadium.Traits != null && stadium.Traits.TryGetValue(traitName, out TraitData? trait))
            {
                if (trait.Vis != null)
                {
                    return trait.Vis.Value;
                }
            }

            return true;
        }

        private bool? TryGetExtensionBool(Dictionary<string, JsonElement>? extensionData, string key)
        {
            if (extensionData == null || !extensionData.TryGetValue(key, out JsonElement element))
            {
                return null;
            }

            if (element.ValueKind == JsonValueKind.True)
            {
                return true;
            }

            if (element.ValueKind == JsonValueKind.False)
            {
                return false;
            }

            return null;
        }

        private string? TryGetExtensionString(Dictionary<string, JsonElement>? extensionData, string key)
        {
            if (extensionData == null || !extensionData.TryGetValue(key, out JsonElement element))
            {
                return null;
            }

            if (element.ValueKind == JsonValueKind.String)
            {
                return element.GetString();
            }

            return null;
        }
        private PathGeometry CreateHaxBallArcGeometry(Point p0, Point p1, double curve)
        {
            double chordLength = GetDistance(p0, p1);

            if (chordLength <= 0.0001)
            {
                return new PathGeometry();
            }

            // HaxBall's curve sign is opposite of WPF ArcSegment's screen-space sweep
            // direction. Keep the exported curve value unchanged, but invert the sign
            // only while rendering the viewport preview.
            double viewportCurve = -curve;
            double absCurve = Math.Abs(viewportCurve);

            if (absCurve >= 359.9)
            {
                absCurve = 359.9;
            }

            double angleRad = absCurve * Math.PI / 180.0;
            double radius = chordLength / (2.0 * Math.Sin(angleRad / 2.0));

            bool isLargeArc = absCurve > 180.0;

            SweepDirection sweepDirection = viewportCurve > 0
                ? SweepDirection.Counterclockwise
                : SweepDirection.Clockwise;

            PathFigure figure = new()
            {
                StartPoint = p0,
                IsClosed = false
            };

            figure.Segments.Add(new ArcSegment
            {
                Point = p1,
                Size = new Size(radius, radius),
                RotationAngle = 0,
                IsLargeArc = isLargeArc,
                SweepDirection = sweepDirection,
                IsStroked = true
            });

            PathGeometry geometry = new();
            geometry.Figures.Add(figure);

            return geometry;
        }

        private Point GetHaxBallArcHandlePoint(Point p0, Point p1, double curve)
        {
            double dx = p1.X - p0.X;
            double dy = p1.Y - p0.Y;
            double chordLength = Math.Sqrt(dx * dx + dy * dy);

            if (chordLength <= 0.0001 || Math.Abs(curve) <= 0.0001)
            {
                return new Point((p0.X + p1.X) / 2.0, (p0.Y + p1.Y) / 2.0);
            }

            // Same sign correction as CreateHaxBallArcGeometry: the handle should
            // appear on the same side as HaxBall's actual curve preview.
            double viewportCurve = -curve;
            double absCurve = Math.Abs(viewportCurve);

            if (absCurve >= 359.9)
            {
                absCurve = 359.9;
            }

            double angleRad = absCurve * Math.PI / 180.0;
            double sagitta = (chordLength / 2.0) * Math.Tan(angleRad / 4.0);

            double normalX = -dy / chordLength;
            double normalY = dx / chordLength;
            double sign = viewportCurve >= 0 ? 1.0 : -1.0;

            Point mid = new Point((p0.X + p1.X) / 2.0, (p0.Y + p1.Y) / 2.0);

            return new Point(
                mid.X + normalX * sagitta * sign,
                mid.Y + normalY * sagitta * sign
            );
        }

        private Point GetSegmentControlPoint(Point p0, Point p1, double curve)
        {
            return GetHaxBallArcHandlePoint(p0, p1, curve);
        }

        private void DrawCurveHandle(int segmentIndex, Point p0, Point p1, Point handlePoint)
        {
            Ellipse handle = new()
            {
                Width = 14,
                Height = 14,
                Fill = Brushes.Gold,
                Stroke = Brushes.White,
                StrokeThickness = 2,
                Cursor = Cursors.SizeAll
            };

            Canvas.SetLeft(handle, handlePoint.X - 7);
            Canvas.SetTop(handle, handlePoint.Y - 7);
            Panel.SetZIndex(handle, 20);

            handle.MouseLeftButtonDown += CurveHandle_MouseLeftButtonDown;

            MapCanvas.Children.Add(handle);

            Line guideLine = new()
            {
                X1 = (p0.X + p1.X) / 2.0,
                Y1 = (p0.Y + p1.Y) / 2.0,
                X2 = handlePoint.X,
                Y2 = handlePoint.Y,
                Stroke = new SolidColorBrush(Color.FromArgb(120, 255, 215, 0)),
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection { 3, 3 },
                IsHitTestVisible = false
            };

            Panel.SetZIndex(guideLine, 19);
            MapCanvas.Children.Add(guideLine);
        }

        private void RenderStadium()
        {
            if (MapCanvas == null) return;

            MapCanvas.Children.Clear();
            vertexShapeIndexes.Clear();
            segmentShapeIndexes.Clear();
            discShapeIndexes.Clear();
            goalShapeIndexes.Clear();
            planeShapeIndexes.Clear();
            jointShapeIndexes.Clear();
            redSpawnShapeIndexes.Clear();
            blueSpawnShapeIndexes.Clear();

            DrawEditorBackground();

            if (showViewportPlanes)
            {
                for (int i = 0; i < stadium.Planes.Count; i++)
                {
                    DrawPlane(i, stadium.Planes[i]);
                }
            }

            for (int i = 0; i < stadium.Segments.Count; i++)
            {
                SegmentData segment = stadium.Segments[i];
                DrawSegment(i, segment.V0, segment.V1);
            }

            for (int i = 0; i < stadium.Goals.Count; i++)
            {
                DrawGoal(i, stadium.Goals[i]);
            }

            for (int i = 0; i < stadium.Joints.Count; i++)
            {
                DrawJoint(i, stadium.Joints[i]);
            }

            for (int i = 0; i < stadium.Discs.Count; i++)
            {
                DrawDisc(i, stadium.Discs[i]);
            }

            for (int i = 0; i < stadium.RedSpawnPoints.Count; i++)
            {
                DrawSpawnPoint(i, stadium.RedSpawnPoints[i], "red");
            }

            for (int i = 0; i < stadium.BlueSpawnPoints.Count; i++)
            {
                DrawSpawnPoint(i, stadium.BlueSpawnPoints[i], "blue");
            }

            if (showViewportVertexes)
            {
                for (int i = 0; i < stadium.Vertexes.Count; i++)
                {
                    DrawVertex(stadium.Vertexes[i].X, stadium.Vertexes[i].Y, i);
                }
            }
        }

        private void DrawViewportGrid(double canvasWidth, double canvasHeight)
        {
            if (canvasWidth <= 0 || canvasHeight <= 0)
            {
                return;
            }

            double centerX = GetCanvasCenterX();
            double centerY = GetCanvasCenterY();
            double worldStep = GetViewportGridStep();
            double startWorldX = Math.Floor(ScreenToDataPoint(new Point(0, 0)).X / worldStep) * worldStep;
            double endWorldX = Math.Ceiling(ScreenToDataPoint(new Point(canvasWidth, 0)).X / worldStep) * worldStep;
            double startWorldY = Math.Floor(ScreenToDataPoint(new Point(0, 0)).Y / worldStep) * worldStep;
            double endWorldY = Math.Ceiling(ScreenToDataPoint(new Point(0, canvasHeight)).Y / worldStep) * worldStep;

            Brush minorBrush = new SolidColorBrush(Color.FromArgb(26, 255, 255, 255));
            Brush majorBrush = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255));
            Brush axisBrush = new SolidColorBrush(Color.FromArgb(76, 255, 255, 255));

            for (double x = startWorldX; x <= endWorldX; x += worldStep)
            {
                Point a = DataToScreenPoint(x, startWorldY);
                Point b = DataToScreenPoint(x, endWorldY);
                double haxX = x - centerX;
                bool isAxis = Math.Abs(haxX) < 0.001;
                bool isMajor = Math.Abs(haxX % (worldStep * 5)) < 0.001;
                AddViewportLine(a, b, isAxis ? axisBrush : isMajor ? majorBrush : minorBrush, isAxis ? 1.2 : 0.7, -86, null);
            }

            for (double y = startWorldY; y <= endWorldY; y += worldStep)
            {
                Point a = DataToScreenPoint(startWorldX, y);
                Point b = DataToScreenPoint(endWorldX, y);
                double haxY = y - centerY;
                bool isAxis = Math.Abs(haxY) < 0.001;
                bool isMajor = Math.Abs(haxY % (worldStep * 5)) < 0.001;
                AddViewportLine(a, b, isAxis ? axisBrush : isMajor ? majorBrush : minorBrush, isAxis ? 1.2 : 0.7, -86, null);
            }
        }

        private double GetViewportGridStep()
        {
            if (viewportZoom >= 2.5) return 25;
            if (viewportZoom >= 1.0) return 50;
            if (viewportZoom >= 0.45) return 100;
            return 200;
        }

        private void DrawEditorBackground()
        {
            double canvasWidth = MapCanvas.ActualWidth;
            double canvasHeight = MapCanvas.ActualHeight;

            if (canvasWidth <= 0 || canvasHeight <= 0) return;

            double centerX = GetCanvasCenterX();
            double centerY = GetCanvasCenterY();

            Rectangle outerBackground = new()
            {
                Width = canvasWidth,
                Height = canvasHeight,
                Fill = new SolidColorBrush(Color.FromRgb(59, 46, 41)),
                IsHitTestVisible = false
            };

            Canvas.SetLeft(outerBackground, 0);
            Canvas.SetTop(outerBackground, 0);
            Panel.SetZIndex(outerBackground, -100);
            MapCanvas.Children.Add(outerBackground);

            string bgType = stadium.Bg?.Type?.Trim().ToLowerInvariant() ?? "";
            bool hasHaxBallField = bgType == "grass" || bgType == "hockey";

            Color outerBaseColor = !string.IsNullOrWhiteSpace(stadium.Bg?.Color)
                ? ColorFromHex(stadium.Bg.Color)
                : Color.FromRgb(28, 35, 48);

            Color fieldBaseColor = GetHaxBallBackgroundColor(bgType);

            double stadiumWidth = stadium.Width > 0 ? stadium.Width : (stadium.Bg?.Width ?? 420);
            double stadiumHeight = stadium.Height > 0 ? stadium.Height : (stadium.Bg?.Height ?? 200);

            if (stadium.Bg?.Width != null && stadium.Bg.Width > 0)
            {
                stadiumWidth = Math.Max(stadiumWidth, stadium.Bg.Width.Value);
            }

            if (stadium.Bg?.Height != null && stadium.Bg.Height > 0)
            {
                stadiumHeight = Math.Max(stadiumHeight, stadium.Bg.Height.Value);
            }

            Point outerTopLeft = DataToScreenPoint(centerX - stadiumWidth, centerY - stadiumHeight);
            double outerScreenWidth = Math.Max(1, ScaleLength(stadiumWidth * 2));
            double outerScreenHeight = Math.Max(1, ScaleLength(stadiumHeight * 2));

            Rectangle stadiumBaseArea = new()
            {
                Width = outerScreenWidth,
                Height = outerScreenHeight,
                Fill = new SolidColorBrush(outerBaseColor),
                StrokeThickness = 0,
                IsHitTestVisible = false
            };

            Canvas.SetLeft(stadiumBaseArea, outerTopLeft.X);
            Canvas.SetTop(stadiumBaseArea, outerTopLeft.Y);
            Panel.SetZIndex(stadiumBaseArea, -95);
            MapCanvas.Children.Add(stadiumBaseArea);

            if (showViewportGrid)
            {
                DrawViewportGrid(canvasWidth, canvasHeight);
            }

            if (!hasHaxBallField)
            {
                return;
            }

            double bgWidth = stadium.Bg?.Width ?? stadiumWidth;
            double bgHeight = stadium.Bg?.Height ?? stadiumHeight;
            double cornerRadius = Math.Max(0, stadium.Bg?.CornerRadius ?? 0);
            double kickOffRadius = Math.Max(0, stadium.Bg?.KickOffRadius ?? 0);

            Point innerTopLeft = DataToScreenPoint(centerX - bgWidth, centerY - bgHeight);
            double innerScreenWidth = Math.Max(1, ScaleLength(bgWidth * 2));
            double innerScreenHeight = Math.Max(1, ScaleLength(bgHeight * 2));
            double screenCornerRadius = Math.Max(0, ScaleLength(cornerRadius));

            Rectangle fieldArea = new()
            {
                Width = innerScreenWidth,
                Height = innerScreenHeight,
                RadiusX = screenCornerRadius,
                RadiusY = screenCornerRadius,
                Fill = bgType == "hockey" ? GetHaxBallHockeyBrush(fieldBaseColor) : new SolidColorBrush(fieldBaseColor),
                Stroke = null,
                StrokeThickness = 0,
                IsHitTestVisible = false
            };

            Canvas.SetLeft(fieldArea, innerTopLeft.X);
            Canvas.SetTop(fieldArea, innerTopLeft.Y);
            Panel.SetZIndex(fieldArea, -90);
            MapCanvas.Children.Add(fieldArea);

            if (bgType == "grass" && showViewportGrassStripes)
            {
                AddHaxPuckGrassStripes(innerTopLeft, innerScreenWidth, innerScreenHeight, screenCornerRadius);
            }

            Rectangle fieldBorder = new()
            {
                Width = innerScreenWidth,
                Height = innerScreenHeight,
                RadiusX = screenCornerRadius,
                RadiusY = screenCornerRadius,
                Fill = Brushes.Transparent,
                Stroke = new SolidColorBrush(GetHaxBallBorderColor(bgType)),
                StrokeThickness = Math.Max(1, ScaleLength(2)),
                IsHitTestVisible = false
            };

            Canvas.SetLeft(fieldBorder, innerTopLeft.X);
            Canvas.SetTop(fieldBorder, innerTopLeft.Y);
            Panel.SetZIndex(fieldBorder, -84);
            MapCanvas.Children.Add(fieldBorder);

            Brush borderBrush = new SolidColorBrush(GetHaxBallBorderColor(bgType));
            double lineThickness = Math.Max(1, ScaleLength(2));

            if (kickOffRadius > 0)
            {
                Point top = DataToScreenPoint(centerX, centerY - bgHeight);
                Point upperCircle = DataToScreenPoint(centerX, centerY - kickOffRadius);
                Point lowerCircle = DataToScreenPoint(centerX, centerY + kickOffRadius);
                Point bottom = DataToScreenPoint(centerX, centerY + bgHeight);

                AddViewportLine(top, upperCircle, borderBrush, lineThickness, -85, null);
                AddViewportLine(lowerCircle, bottom, borderBrush, lineThickness, -85, null);

                Point circleTopLeft = DataToScreenPoint(centerX - kickOffRadius, centerY - kickOffRadius);
                double circleSize = Math.Max(1, ScaleLength(kickOffRadius * 2));

                Ellipse kickOffCircle = new()
                {
                    Width = circleSize,
                    Height = circleSize,
                    Fill = Brushes.Transparent,
                    Stroke = borderBrush,
                    StrokeThickness = lineThickness,
                    IsHitTestVisible = false
                };

                Canvas.SetLeft(kickOffCircle, circleTopLeft.X);
                Canvas.SetTop(kickOffCircle, circleTopLeft.Y);
                Panel.SetZIndex(kickOffCircle, -85);
                MapCanvas.Children.Add(kickOffCircle);
            }
            else
            {
                Point top = DataToScreenPoint(centerX, centerY - bgHeight);
                Point bottom = DataToScreenPoint(centerX, centerY + bgHeight);
                AddViewportLine(top, bottom, borderBrush, lineThickness, -85, null);
            }
        }

        private Brush GetBackgroundFieldBrush()
        {
            string type = stadium.Bg?.Type?.Trim().ToLowerInvariant() ?? "";
            string? userColor = stadium.Bg?.Color;

            if (!string.IsNullOrWhiteSpace(userColor))
            {
                return new SolidColorBrush(ColorFromHex(userColor));
            }

            return new SolidColorBrush(GetHaxBallBackgroundColor(type));
        }

        private Color GetHaxBallBackgroundColor(string? type)
        {
            string normalized = type?.Trim().ToLowerInvariant() ?? "";

            if (normalized == "hockey")
            {
                return Color.FromRgb(85, 85, 85);
            }

            return Color.FromRgb(113, 140, 90);
        }

        private Color GetHaxBallBorderColor(string? type)
        {
            string normalized = type?.Trim().ToLowerInvariant() ?? "";

            if (normalized == "hockey")
            {
                return Color.FromRgb(233, 204, 110);
            }

            return Color.FromRgb(199, 230, 189);
        }

        private void AddHaxPuckGrassStripes(Point fieldTopLeft, double fieldWidth, double fieldHeight, double radius)
        {
            if (fieldWidth <= 1 || fieldHeight <= 1)
            {
                return;
            }

            Canvas stripeLayer = new()
            {
                Width = fieldWidth,
                Height = fieldHeight,
                Clip = new RectangleGeometry(new Rect(0, 0, fieldWidth, fieldHeight), radius, radius),
                IsHitTestVisible = false
            };

            Canvas.SetLeft(stripeLayer, fieldTopLeft.X);
            Canvas.SetTop(stripeLayer, fieldTopLeft.Y);
            Panel.SetZIndex(stripeLayer, -89);

            double stripeWidth = Math.Max(14, ScaleLength(36));
            double stripeStep = Math.Max(34, ScaleLength(82));
            double diagonalLength = Math.Sqrt(fieldWidth * fieldWidth + fieldHeight * fieldHeight) + stripeStep * 8;

            SolidColorBrush lightStripe = new(Color.FromArgb(24, 255, 255, 255));

            // HaxPuck-style diagonal grass bands. We draw oversized rotated bands
            // around the center so the whole clipped field is covered, including
            // bottom-right after rotation.
            double centerX = fieldWidth / 2.0;
            double centerY = fieldHeight / 2.0;

            for (double x = -diagonalLength; x <= diagonalLength; x += stripeStep)
            {
                Rectangle stripe = new()
                {
                    Width = stripeWidth,
                    Height = diagonalLength * 2.0,
                    Fill = lightStripe,
                    StrokeThickness = 0,
                    IsHitTestVisible = false,
                    RenderTransform = new RotateTransform(35, centerX - x, diagonalLength)
                };

                Canvas.SetLeft(stripe, centerX + x);
                Canvas.SetTop(stripe, centerY - diagonalLength);
                stripeLayer.Children.Add(stripe);
            }

            MapCanvas.Children.Add(stripeLayer);
        }

        private Brush GetHaxBallGrassBrush(Color baseColor)
        {
            DrawingGroup group = new();

            group.Children.Add(
                new GeometryDrawing(
                    new SolidColorBrush(baseColor),
                    null,
                    new RectangleGeometry(new Rect(0, 0, 96, 96))));

            RectangleGeometry lightStripeGeometry = new(new Rect(-48, -96, 42, 288))
            {
                Transform = new RotateTransform(32, 0, 0)
            };

            GeometryDrawing lightStripe = new(
                new SolidColorBrush(Color.FromArgb(24, 255, 255, 255)),
                null,
                lightStripeGeometry);

            group.Children.Add(lightStripe);

            RectangleGeometry darkStripeGeometry = new(new Rect(0, -96, 42, 288))
            {
                Transform = new RotateTransform(32, 0, 0)
            };

            GeometryDrawing darkStripe = new(
                new SolidColorBrush(Color.FromArgb(10, 0, 0, 0)),
                null,
                darkStripeGeometry);

            group.Children.Add(darkStripe);

            DrawingBrush brush = new(group)
            {
                TileMode = TileMode.Tile,
                ViewportUnits = BrushMappingMode.Absolute,
                Viewport = new Rect(0, 0, Math.Max(36, 96 * viewportZoom), Math.Max(36, 96 * viewportZoom)),
                Stretch = Stretch.Fill
            };

            return brush;
        }

        private Brush GetHaxBallHockeyBrush(Color baseColor)
        {
            DrawingGroup group = new();
            group.Children.Add(new GeometryDrawing(new SolidColorBrush(baseColor), null, new RectangleGeometry(new Rect(0, 0, 64, 64))));
            group.Children.Add(new GeometryDrawing(new SolidColorBrush(Color.FromArgb(18, 255, 255, 255)), null, new RectangleGeometry(new Rect(0, 0, 64, 1))));
            group.Children.Add(new GeometryDrawing(new SolidColorBrush(Color.FromArgb(18, 0, 0, 0)), null, new RectangleGeometry(new Rect(0, 32, 64, 1))));

            DrawingBrush brush = new(group)
            {
                TileMode = TileMode.Tile,
                ViewportUnits = BrushMappingMode.Absolute,
                Viewport = new Rect(0, 0, Math.Max(16, 64 * viewportZoom), Math.Max(16, 64 * viewportZoom)),
                Stretch = Stretch.Fill
            };

            return brush;
        }

        private void AddViewportLine(Point a, Point b, Brush stroke, double thickness, int zIndex, DoubleCollection? dash)
        {
            Line line = new()
            {
                X1 = a.X,
                Y1 = a.Y,
                X2 = b.X,
                Y2 = b.Y,
                Stroke = stroke,
                StrokeThickness = thickness,
                StrokeDashArray = dash,
                IsHitTestVisible = false
            };

            Panel.SetZIndex(line, zIndex);
            MapCanvas.Children.Add(line);
        }

        private Color ColorFromHex(string hex)
        {
            string normalized = NormalizeColorForExport(hex);

            byte r = byte.Parse(normalized.Substring(0, 2), NumberStyles.HexNumber);
            byte g = byte.Parse(normalized.Substring(2, 2), NumberStyles.HexNumber);
            byte b = byte.Parse(normalized.Substring(4, 2), NumberStyles.HexNumber);

            return Color.FromRgb(r, g, b);
        }

        private double GetCanvasCenterX()
        {
            return MapCanvas.ActualWidth / 2.0;
        }

        private double GetCanvasCenterY()
        {
            return MapCanvas.ActualHeight / 2.0;
        }

        private void TranslateCanvasAnchoredStadiumObjects(double deltaX, double deltaY)
        {
            if (Math.Abs(deltaX) < 0.0001 && Math.Abs(deltaY) < 0.0001)
            {
                return;
            }

            foreach (VertexData vertex in stadium.Vertexes)
            {
                vertex.X += deltaX;
                vertex.Y += deltaY;
            }

            foreach (DiscData disc in stadium.Discs)
            {
                disc.X += deltaX;
                disc.Y += deltaY;
            }

            foreach (GoalData goal in stadium.Goals)
            {
                goal.X0 += deltaX;
                goal.Y0 += deltaY;
                goal.X1 += deltaX;
                goal.Y1 += deltaY;
            }

            foreach (SpawnPointData spawn in stadium.RedSpawnPoints)
            {
                spawn.X += deltaX;
                spawn.Y += deltaY;
            }

            foreach (SpawnPointData spawn in stadium.BlueSpawnPoints)
            {
                spawn.X += deltaX;
                spawn.Y += deltaY;
            }

            TranslateViewportDragState(deltaX, deltaY);
        }

        private void TranslateViewportDragState(double deltaX, double deltaY)
        {
            segmentDragStartPoint = new Point(segmentDragStartPoint.X + deltaX, segmentDragStartPoint.Y + deltaY);
            goalDragStartPoint = new Point(goalDragStartPoint.X + deltaX, goalDragStartPoint.Y + deltaY);
            planeDragStartPoint = new Point(planeDragStartPoint.X + deltaX, planeDragStartPoint.Y + deltaY);
            selectedItemsDragStartData = new Point(selectedItemsDragStartData.X + deltaX, selectedItemsDragStartData.Y + deltaY);

            foreach (int key in selectedVertexDragStartPositions.Keys.ToList())
            {
                Point p = selectedVertexDragStartPositions[key];
                selectedVertexDragStartPositions[key] = new Point(p.X + deltaX, p.Y + deltaY);
            }

            foreach (int key in selectedDiscDragStartPositions.Keys.ToList())
            {
                Point p = selectedDiscDragStartPositions[key];
                selectedDiscDragStartPositions[key] = new Point(p.X + deltaX, p.Y + deltaY);
            }

            foreach (int key in selectedGoalDragStartPositions.Keys.ToList())
            {
                (double X0, double Y0, double X1, double Y1) p = selectedGoalDragStartPositions[key];
                selectedGoalDragStartPositions[key] = (p.X0 + deltaX, p.Y0 + deltaY, p.X1 + deltaX, p.Y1 + deltaY);
            }

            foreach (int key in selectedRedSpawnDragStartPositions.Keys.ToList())
            {
                Point p = selectedRedSpawnDragStartPositions[key];
                selectedRedSpawnDragStartPositions[key] = new Point(p.X + deltaX, p.Y + deltaY);
            }

            foreach (int key in selectedBlueSpawnDragStartPositions.Keys.ToList())
            {
                Point p = selectedBlueSpawnDragStartPositions[key];
                selectedBlueSpawnDragStartPositions[key] = new Point(p.X + deltaX, p.Y + deltaY);
            }
        }


        private void ReleaseCanvasMouseIfSafe()
        {
            bool anyDrag =
                isDraggingSegment ||
                isDraggingGoal ||
                isDraggingPlane ||
                isDraggingGoalEndpoint ||
                isDraggingVertex ||
                isDraggingDisc ||
                isDraggingRedSpawn ||
                isDraggingBlueSpawn ||
                isDraggingCurveHandle ||
                isDraggingSelectionRectangle ||
                isDraggingSelectedItems ||
                isPanningViewport;

            if (MapCanvas != null && MapCanvas.IsMouseCaptured && !anyDrag)
            {
                MapCanvas.ReleaseMouseCapture();
            }
        }


        private void MainWindow_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space && isPanningViewport)
            {
                StopViewportPan();
                e.Handled = true;
            }
        }

        private Point DataToScreenPoint(double dataX, double dataY)
        {
            double centerX = GetCanvasCenterX();
            double centerY = GetCanvasCenterY();

            double worldX = dataX - centerX;
            double worldY = dataY - centerY;

            return new Point(
                centerX + viewportPanX + worldX * viewportZoom,
                centerY + viewportPanY + worldY * viewportZoom
            );
        }

        private Point ScreenToDataPoint(Point screenPoint)
        {
            double centerX = GetCanvasCenterX();
            double centerY = GetCanvasCenterY();

            double worldX = (screenPoint.X - centerX - viewportPanX) / viewportZoom;
            double worldY = (screenPoint.Y - centerY - viewportPanY) / viewportZoom;

            return new Point(
                centerX + worldX,
                centerY + worldY
            );
        }

        private double ScaleLength(double value)
        {
            return value * viewportZoom;
        }

        private void ViewportGridButton_Click(object sender, RoutedEventArgs e)
        {
            showViewportGrid = !showViewportGrid;
            RenderStadium();
            UpdateViewportMiniToolbarUi();
            SaveEditorPreferences();
            UpdateStatus(showViewportGrid ? "Viewport grid enabled." : "Viewport grid disabled.");
        }

        private void ViewportSnapButton_Click(object sender, RoutedEventArgs e)
        {
            SetSnapToGrid(!snapToGrid, true, true);
        }

        private void ViewportVertexesButton_Click(object sender, RoutedEventArgs e)
        {
            showViewportVertexes = !showViewportVertexes;
            RenderStadium();
            UpdateViewportMiniToolbarUi();
            SaveEditorPreferences();
            UpdateStatus(showViewportVertexes ? "Vertex display enabled." : "Vertex display disabled.");
        }

        private void ViewportSegmentsButton_Click(object sender, RoutedEventArgs e)
        {
            showViewportSegments = !showViewportSegments;
            RenderStadium();
            UpdateViewportMiniToolbarUi();
            SaveEditorPreferences();
            UpdateStatus(showViewportSegments ? "Segment display enabled." : "Segment display disabled.");
        }

        private void ViewportDiscsButton_Click(object sender, RoutedEventArgs e)
        {
            showViewportDiscs = !showViewportDiscs;
            RenderStadium();
            UpdateViewportMiniToolbarUi();
            SaveEditorPreferences();
            UpdateStatus(showViewportDiscs ? "Disc display enabled." : "Disc display disabled.");
        }

        private void ViewportPlanesButton_Click(object sender, RoutedEventArgs e)
        {
            showViewportPlanes = !showViewportPlanes;
            RenderStadium();
            UpdateViewportMiniToolbarUi();
            SaveEditorPreferences();
            UpdateStatus(showViewportPlanes ? "Plane display enabled." : "Plane display disabled.");
        }

        private void ViewportInvisibleButton_Click(object sender, RoutedEventArgs e)
        {
            showViewportInvisibleObjects = !showViewportInvisibleObjects;
            RenderStadium();
            UpdateViewportMiniToolbarUi();
            SaveEditorPreferences();
            UpdateStatus(showViewportInvisibleObjects ? "Invisible object preview enabled." : "Invisible object preview disabled.");
        }

        private void ViewportMirrorButton_Click(object sender, RoutedEventArgs e)
        {
            autoMirrorPlacement = !autoMirrorPlacement;
            UpdateViewportMiniToolbarUi();
            SaveEditorPreferences();
            UpdateStatus(autoMirrorPlacement
                ? "Mirror Mode enabled. Newly placed objects will be copied to the opposite side."
                : "Mirror Mode disabled.");
        }

        private void ViewportResetButton_Click(object sender, RoutedEventArgs e)
        {
            ResetViewport();
        }

        private void UpdateViewportMiniToolbarUi()
        {
            SetViewportMiniButtonState(ViewportGridButton, showViewportGrid);
            SetViewportMiniButtonState(ViewportSnapButton, snapToGrid);
            SetViewportMiniButtonState(ViewportVertexesButton, showViewportVertexes);
            SetViewportMiniButtonState(ViewportSegmentsButton, showViewportSegments);
            SetViewportMiniButtonState(ViewportDiscsButton, showViewportDiscs);
            SetViewportMiniButtonState(ViewportPlanesButton, showViewportPlanes);
            SetViewportMiniButtonState(ViewportInvisibleButton, showViewportInvisibleObjects);
            SetViewportMiniButtonState(ViewportMirrorButton, autoMirrorPlacement);

            if (ViewportInvisibleButton != null)
            {
                UpdatePackIconKind(ViewportInvisibleButton, showViewportInvisibleObjects ? "EyeOutline" : "EyeOffOutline");
            }
        }

        private void SetViewportMiniButtonState(Button? button, bool isActive)
        {
            if (button == null)
            {
                return;
            }

            button.Style = (Style)FindResource(isActive ? "ViewportMiniButtonActive" : "ViewportMiniButton");
        }

        private static void UpdatePackIconKind(DependencyObject root, string iconName)
        {
            if (!Enum.TryParse(iconName, out PackIconKind parsedKind))
            {
                return;
            }

            if (root is PackIcon icon)
            {
                icon.Kind = parsedKind;
                return;
            }

            int childCount = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < childCount; i++)
            {
                UpdatePackIconKind(VisualTreeHelper.GetChild(root, i), iconName);
            }
        }

        private void ResetViewport()
        {
            viewportZoom = 1.0;
            viewportPanX = 0;
            viewportPanY = 0;
            UpdateViewportInfo();
            RenderStadium();
            UpdateStatus("Viewport reset.");
        }

        private void UpdateViewportInfo()
        {
            if (ViewportInfoText == null) return;
            ViewportInfoText.Text = $"Zoom: {viewportZoom * 100:0}% | Pan: {viewportPanX:0}, {viewportPanY:0}";
        }

        private void MapCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            Point mouseScreen = e.GetPosition(MapCanvas);
            Point beforeZoomData = ScreenToDataPoint(mouseScreen);

            double zoomFactor = e.Delta > 0 ? 1.12 : 1.0 / 1.12;
            viewportZoom *= zoomFactor;
            viewportZoom = Math.Max(0.15, Math.Min(8.0, viewportZoom));

            double centerX = GetCanvasCenterX();
            double centerY = GetCanvasCenterY();
            double worldX = beforeZoomData.X - centerX;
            double worldY = beforeZoomData.Y - centerY;

            viewportPanX = mouseScreen.X - centerX - worldX * viewportZoom;
            viewportPanY = mouseScreen.Y - centerY - worldY * viewportZoom;

            UpdateViewportInfo();
            RenderStadium();
            e.Handled = true;
        }

        private void MapCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle)
            {
                StartViewportPan(e.GetPosition(MapCanvas));
                e.Handled = true;
                return;
            }

            if (e.ChangedButton == MouseButton.Right && currentTool == "Select")
            {
                MapCanvas.Focus();
                BeginSelectionRectangle(e.GetPosition(MapCanvas), true);
                e.Handled = true;
                return;
            }

            if (e.ChangedButton == MouseButton.Left && Keyboard.IsKeyDown(Key.Space))
            {
                StartViewportPan(e.GetPosition(MapCanvas));
                e.Handled = true;
            }
        }

        private void MapCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle && isPanningViewport)
            {
                StopViewportPan();
                e.Handled = true;
                return;
            }

            if (e.ChangedButton == MouseButton.Right && isDraggingSelectionRectangle)
            {
                FinishSelectionRectangle(e.GetPosition(MapCanvas));
                e.Handled = true;
                return;
            }

            if (e.ChangedButton == MouseButton.Left && isPanningViewport)
            {
                StopViewportPan();
                e.Handled = true;
            }
        }

        private void StartViewportPan(Point mousePosition)
        {
            isPanningViewport = true;
            viewportPanStartMouse = mousePosition;
            viewportPanStartX = viewportPanX;
            viewportPanStartY = viewportPanY;
            MapCanvas.CaptureMouse();
            UpdateStatus("Viewport pan started.");
        }

        private void StopViewportPan()
        {
            isPanningViewport = false;
            if (MapCanvas.IsMouseCaptured) MapCanvas.ReleaseMouseCapture();
            UpdateStatus("Viewport pan stopped.");
        }

        private void BeginSelectionRectangle(Point startScreen, bool touchMode)
        {
            isDraggingSelectionRectangle = true;
            selectionRectangleTouchMode = touchMode;
            selectionRectangleStartScreen = startScreen;

            selectionRectangleShape = new Rectangle
            {
                Width = 0,
                Height = 0,
                Fill = touchMode
                    ? new SolidColorBrush(Color.FromArgb(34, 255, 170, 0))
                    : new SolidColorBrush(Color.FromArgb(35, 34, 184, 240)),
                Stroke = touchMode ? Brushes.Orange : Brushes.DeepSkyBlue,
                StrokeThickness = 1,
                StrokeDashArray = touchMode ? new DoubleCollection { 7, 3 } : new DoubleCollection { 4, 4 },
                IsHitTestVisible = false
            };

            Canvas.SetLeft(selectionRectangleShape, startScreen.X);
            Canvas.SetTop(selectionRectangleShape, startScreen.Y);
            Panel.SetZIndex(selectionRectangleShape, 1000);
            MapCanvas.Children.Add(selectionRectangleShape);
            MapCanvas.CaptureMouse();
        }

        private void UpdateSelectionRectangle(Point currentScreen)
        {
            if (selectionRectangleShape == null) return;

            double left = Math.Min(selectionRectangleStartScreen.X, currentScreen.X);
            double top = Math.Min(selectionRectangleStartScreen.Y, currentScreen.Y);
            double width = Math.Abs(currentScreen.X - selectionRectangleStartScreen.X);
            double height = Math.Abs(currentScreen.Y - selectionRectangleStartScreen.Y);

            Canvas.SetLeft(selectionRectangleShape, left);
            Canvas.SetTop(selectionRectangleShape, top);
            selectionRectangleShape.Width = width;
            selectionRectangleShape.Height = height;
        }

        private void FinishSelectionRectangle(Point endScreen)
        {
            Rect selectionRect = new(
                Math.Min(selectionRectangleStartScreen.X, endScreen.X),
                Math.Min(selectionRectangleStartScreen.Y, endScreen.Y),
                Math.Abs(endScreen.X - selectionRectangleStartScreen.X),
                Math.Abs(endScreen.Y - selectionRectangleStartScreen.Y));

            if (selectionRectangleShape != null)
            {
                MapCanvas.Children.Remove(selectionRectangleShape);
                selectionRectangleShape = null;
            }

            isDraggingSelectionRectangle = false;
            if (MapCanvas.IsMouseCaptured) MapCanvas.ReleaseMouseCapture();

            if (selectionRect.Width < 3 && selectionRect.Height < 3)
            {
                ClearSelection();
                RenderStadium();
                UpdateStatus("Selection cleared.");
                return;
            }

            SelectObjectsInRectangle(selectionRect, Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift), Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl), selectionRectangleTouchMode);
            selectionRectangleTouchMode = false;
        }

        private void SelectObjectsInRectangle(Rect selectionRect, bool addToSelection, bool removeFromSelection, bool touchMode)
        {
            List<SelectedItem> hits = new();

            for (int i = 0; i < stadium.Vertexes.Count; i++)
            {
                Point p = DataToScreenPoint(stadium.Vertexes[i].X, stadium.Vertexes[i].Y);
                if (!IsObjectHidden("Vertex", i) && selectionRect.Contains(p)) hits.Add(new SelectedItem("Vertex", i));
            }

            for (int i = 0; i < stadium.Discs.Count; i++)
            {
                DiscData disc = stadium.Discs[i];
                Point p = DataToScreenPoint(disc.X, disc.Y);
                double radius = Math.Max(3, ScaleLength(disc.Radius ?? DefaultDiscRadius));

                bool isHit = touchMode
                    ? DoesCircleTouchRect(p, radius, selectionRect)
                    : selectionRect.Contains(p);

                if (!IsObjectHidden("Disc", i) && isHit) hits.Add(new SelectedItem("Disc", i));
            }

            for (int i = 0; i < stadium.RedSpawnPoints.Count; i++)
            {
                Point p = DataToScreenPoint(stadium.RedSpawnPoints[i].X, stadium.RedSpawnPoints[i].Y);
                if (!IsObjectHidden("RedSpawn", i) && selectionRect.Contains(p)) hits.Add(new SelectedItem("RedSpawn", i));
            }

            for (int i = 0; i < stadium.BlueSpawnPoints.Count; i++)
            {
                Point p = DataToScreenPoint(stadium.BlueSpawnPoints[i].X, stadium.BlueSpawnPoints[i].Y);
                if (!IsObjectHidden("BlueSpawn", i) && selectionRect.Contains(p)) hits.Add(new SelectedItem("BlueSpawn", i));
            }

            for (int i = 0; i < stadium.Segments.Count; i++)
            {
                if (TryGetSegmentPoints(stadium.Segments[i], out Point a, out Point b))
                {
                    Point sa = DataToScreenPoint(a.X, a.Y);
                    Point sb = DataToScreenPoint(b.X, b.Y);

                    bool isHit = touchMode
                        ? DoesLineTouchRect(sa, sb, selectionRect)
                        : selectionRect.Contains(sa) && selectionRect.Contains(sb);

                    if (!IsObjectHidden("Segment", i) && isHit) hits.Add(new SelectedItem("Segment", i));
                }
            }

            for (int i = 0; i < stadium.Goals.Count; i++)
            {
                GoalData g = stadium.Goals[i];
                Point a = DataToScreenPoint(g.X0, g.Y0);
                Point b = DataToScreenPoint(g.X1, g.Y1);

                bool isHit = touchMode
                    ? DoesLineTouchRect(a, b, selectionRect)
                    : selectionRect.Contains(a) && selectionRect.Contains(b);

                if (!IsObjectHidden("Goal", i) && isHit) hits.Add(new SelectedItem("Goal", i));
            }

            for (int i = 0; i < stadium.Planes.Count; i++)
            {
                if (touchMode && DoesPlaneTouchRect(stadium.Planes[i], selectionRect))
                {
                    if (!IsObjectHidden("Plane", i)) hits.Add(new SelectedItem("Plane", i));
                }
            }

            for (int i = 0; i < stadium.Joints.Count; i++)
            {
                JointData j = stadium.Joints[i];
                if (j.D0 >= 0 && j.D0 < stadium.Discs.Count && j.D1 >= 0 && j.D1 < stadium.Discs.Count)
                {
                    Point a = DataToScreenPoint(stadium.Discs[j.D0].X, stadium.Discs[j.D0].Y);
                    Point b = DataToScreenPoint(stadium.Discs[j.D1].X, stadium.Discs[j.D1].Y);

                    bool isHit = touchMode
                        ? DoesLineTouchRect(a, b, selectionRect)
                        : selectionRect.Contains(a) && selectionRect.Contains(b);

                    if (!IsObjectHidden("Joint", i) && isHit) hits.Add(new SelectedItem("Joint", i));
                }
            }

            if (!addToSelection && !removeFromSelection)
            {
                selectedItems.Clear();
            }

            foreach (SelectedItem hit in hits)
            {
                if (removeFromSelection)
                {
                    selectedItems.RemoveAll(item => item.Type == hit.Type && item.Index == hit.Index);
                }
                else if (!selectedItems.Exists(item => item.Type == hit.Type && item.Index == hit.Index))
                {
                    selectedItems.Add(hit);
                }
            }

            ClearSingleSelectionIndexes();
            UpdateMultiSelectionSummary();
            RenderStadium();

            string modeText = touchMode ? "touch-selected" : "box-selected";
            UpdateStatus($"{modeText} {selectedItems.Count} object(s).");
        }

        private bool DoesCircleTouchRect(Point center, double radius, Rect rect)
        {
            double closestX = Math.Max(rect.Left, Math.Min(center.X, rect.Right));
            double closestY = Math.Max(rect.Top, Math.Min(center.Y, rect.Bottom));
            double dx = center.X - closestX;
            double dy = center.Y - closestY;
            return dx * dx + dy * dy <= radius * radius;
        }

        private bool DoesLineTouchRect(Point a, Point b, Rect rect)
        {
            if (rect.Contains(a) || rect.Contains(b))
            {
                return true;
            }

            Point topLeft = new(rect.Left, rect.Top);
            Point topRight = new(rect.Right, rect.Top);
            Point bottomRight = new(rect.Right, rect.Bottom);
            Point bottomLeft = new(rect.Left, rect.Bottom);

            return DoLineSegmentsIntersect(a, b, topLeft, topRight)
                || DoLineSegmentsIntersect(a, b, topRight, bottomRight)
                || DoLineSegmentsIntersect(a, b, bottomRight, bottomLeft)
                || DoLineSegmentsIntersect(a, b, bottomLeft, topLeft);
        }

        private bool DoesPlaneTouchRect(PlaneData plane, Rect rect)
        {
            if (plane.Normal == null || plane.Normal.Count < 2)
            {
                return false;
            }

            double nx = plane.Normal[0];
            double ny = plane.Normal[1];
            double dist = plane.Dist;

            double centerX = GetCanvasCenterX();
            double centerY = GetCanvasCenterY();

            double px = centerX + nx * dist;
            double py = centerY + ny * dist;
            double tx = -ny;
            double ty = nx;
            double lineLength = Math.Max(MapCanvas.ActualWidth, MapCanvas.ActualHeight) * 2.5 / Math.Max(0.15, viewportZoom);

            Point a = DataToScreenPoint(px - tx * lineLength, py - ty * lineLength);
            Point b = DataToScreenPoint(px + tx * lineLength, py + ty * lineLength);

            return DoesLineTouchRect(a, b, rect);
        }

        private bool DoLineSegmentsIntersect(Point p1, Point p2, Point q1, Point q2)
        {
            double d1 = Direction(q1, q2, p1);
            double d2 = Direction(q1, q2, p2);
            double d3 = Direction(p1, p2, q1);
            double d4 = Direction(p1, p2, q2);

            if (((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) &&
                ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0)))
            {
                return true;
            }

            const double epsilon = 0.00001;

            return Math.Abs(d1) < epsilon && IsPointOnSegment(q1, q2, p1)
                || Math.Abs(d2) < epsilon && IsPointOnSegment(q1, q2, p2)
                || Math.Abs(d3) < epsilon && IsPointOnSegment(p1, p2, q1)
                || Math.Abs(d4) < epsilon && IsPointOnSegment(p1, p2, q2);
        }

        private double Direction(Point a, Point b, Point c)
        {
            return ((c.X - a.X) * (b.Y - a.Y)) - ((c.Y - a.Y) * (b.X - a.X));
        }

        private bool IsPointOnSegment(Point a, Point b, Point p)
        {
            return p.X >= Math.Min(a.X, b.X) - 0.00001
                && p.X <= Math.Max(a.X, b.X) + 0.00001
                && p.Y >= Math.Min(a.Y, b.Y) - 0.00001
                && p.Y <= Math.Max(a.Y, b.Y) + 0.00001;
        }

        private bool IsObjectSelected(string type, int index)
        {
            return selectedItems.Exists(item => item.Type == type && item.Index == index);
        }

        private bool HasSingleSelection()
        {
            return selectedVertexIndex != null || selectedSegmentIndex != null || selectedDiscIndex != null || selectedGoalIndex != null || selectedPlaneIndex != null || selectedJointIndex != null || selectedRedSpawnIndex != null || selectedBlueSpawnIndex != null;
        }

        private void RefreshInspectorForCurrentSingleSelection()
        {
            if (selectedVertexIndex != null)
            {
                SelectVertex(selectedVertexIndex.Value);
                return;
            }

            if (selectedSegmentIndex != null)
            {
                SelectSegment(selectedSegmentIndex.Value);
                return;
            }

            if (selectedDiscIndex != null)
            {
                SelectDisc(selectedDiscIndex.Value);
                return;
            }

            if (selectedGoalIndex != null)
            {
                SelectGoal(selectedGoalIndex.Value);
                return;
            }

            if (selectedPlaneIndex != null)
            {
                SelectPlane(selectedPlaneIndex.Value);
                return;
            }

            if (selectedJointIndex != null)
            {
                SelectJoint(selectedJointIndex.Value);
                return;
            }

            if (selectedRedSpawnIndex != null)
            {
                SelectRedSpawn(selectedRedSpawnIndex.Value);
                return;
            }

            if (selectedBlueSpawnIndex != null)
            {
                SelectBlueSpawn(selectedBlueSpawnIndex.Value);
            }
        }

        private void ClearSingleSelectionIndexes()
        {
            selectedVertexIndex = null;
            selectedSegmentIndex = null;
            selectedDiscIndex = null;
            selectedGoalIndex = null;
            selectedPlaneIndex = null;
            selectedJointIndex = null;
            selectedRedSpawnIndex = null;
            selectedBlueSpawnIndex = null;
        }

        private void UpdateMultiSelectionSummary()
        {
            if (selectedItems.Count == 0)
            {
                SelectedObjectText.Text = "None";
                return;
            }

            Dictionary<string, int> counts = new();
            foreach (SelectedItem item in selectedItems)
            {
                counts[item.Type] = counts.TryGetValue(item.Type, out int count) ? count + 1 : 1;
            }

            SelectedObjectText.Text = $"Selected: {selectedItems.Count} objects";
            SelectionInfoTextBlock.Text = BuildMultiSelectionInfoText();
            PositionXTextBox.Text = string.Join(", ", counts);
            PositionYTextBox.Text = "Multi-select";
            RadiusTextBox.Text = "0";
            ObjectColorTextBox.Text = "";
            CurveTextBox.Text = "0";
            BCoefTextBox.Text = "";
            InvMassTextBox.Text = "";
            SetCollisionUiFromData(null, null);
            UpdateInspectorForSelection("Multi");
        }


        private void ClearMultiPropertyInputs()
        {
            if (MultiColorTextBox != null) MultiColorTextBox.Text = "";
            if (MultiTraitTextBox != null) MultiTraitTextBox.Text = "";
            if (MultiCMaskTextBox != null) MultiCMaskTextBox.Text = "";
            if (MultiCGroupTextBox != null) MultiCGroupTextBox.Text = "";
            if (MultiVisComboBox != null) MultiVisComboBox.SelectedIndex = 0;
        }

        private string BuildMultiSelectionInfoText()
        {
            if (selectedItems.Count == 0)
            {
                return "No selection";
            }

            Dictionary<string, int> counts = new();
            foreach (SelectedItem item in selectedItems)
            {
                string label = GetFriendlyLayerTypeName(item.Type);
                counts[label] = counts.TryGetValue(label, out int count) ? count + 1 : 1;
            }

            return $"{selectedItems.Count} object(s) selected | " + string.Join(" | ", counts.Select(pair => $"{pair.Key}: {pair.Value}"));
        }

        private string BuildSelectionInfoText(string type, int index)
        {
            double centerX = GetCanvasCenterX();
            double centerY = GetCanvasCenterY();

            try
            {
                switch (type)
                {
                    case "Vertex":
                        if (index >= 0 && index < stadium.Vertexes.Count)
                        {
                            VertexData v = stadium.Vertexes[index];
                            return $"Vertex #{index} | X: {v.X - centerX:0.##} | Y: {v.Y - centerY:0.##}";
                        }
                        break;
                    case "Segment":
                        if (index >= 0 && index < stadium.Segments.Count)
                        {
                            SegmentData s = stadium.Segments[index];
                            string lengthText = "invalid";
                            if (s.V0 >= 0 && s.V0 < stadium.Vertexes.Count && s.V1 >= 0 && s.V1 < stadium.Vertexes.Count)
                            {
                                VertexData v0 = stadium.Vertexes[s.V0];
                                VertexData v1 = stadium.Vertexes[s.V1];
                                lengthText = GetDistance(new Point(v0.X, v0.Y), new Point(v1.X, v1.Y)).ToString("0.##", CultureInfo.InvariantCulture);
                            }
                            return $"Segment #{index} | v0: {s.V0} | v1: {s.V1} | length: {lengthText} | curve: {s.Curve ?? 0:0.##}";
                        }
                        break;
                    case "Disc":
                        if (index >= 0 && index < stadium.Discs.Count)
                        {
                            DiscData d = stadium.Discs[index];
                            return $"Disc #{index} | X: {d.X - centerX:0.##} | Y: {d.Y - centerY:0.##} | radius: {d.Radius ?? DefaultDiscRadius:0.##}";
                        }
                        break;
                    case "Goal":
                        if (index >= 0 && index < stadium.Goals.Count)
                        {
                            GoalData g = stadium.Goals[index];
                            double length = GetDistance(new Point(g.X0, g.Y0), new Point(g.X1, g.Y1));
                            return $"Goal #{index} | team: {NormalizeGoalTeam(g.Team)} | length: {length:0.##} | p0: {g.X0 - centerX:0.##},{g.Y0 - centerY:0.##} | p1: {g.X1 - centerX:0.##},{g.Y1 - centerY:0.##}";
                        }
                        break;
                    case "Plane":
                        if (index >= 0 && index < stadium.Planes.Count)
                        {
                            PlaneData p = stadium.Planes[index];
                            string normal = p.Normal != null && p.Normal.Count >= 2 ? $"{p.Normal[0]:0.###},{p.Normal[1]:0.###}" : "invalid";
                            return $"Plane #{index} | normal: {normal} | dist: {p.Dist:0.##}";
                        }
                        break;
                    case "Joint":
                        if (index >= 0 && index < stadium.Joints.Count)
                        {
                            JointData j = stadium.Joints[index];
                            return $"Joint #{index} | d0: {j.D0} | d1: {j.D1} | strength: {j.Strength} | length: {j.Length}";
                        }
                        break;
                    case "RedSpawn":
                        if (index >= 0 && index < stadium.RedSpawnPoints.Count)
                        {
                            SpawnPointData sp = stadium.RedSpawnPoints[index];
                            return $"Red Spawn #{index} | X: {sp.X - centerX:0.##} | Y: {sp.Y - centerY:0.##}";
                        }
                        break;
                    case "BlueSpawn":
                        if (index >= 0 && index < stadium.BlueSpawnPoints.Count)
                        {
                            SpawnPointData sp = stadium.BlueSpawnPoints[index];
                            return $"Blue Spawn #{index} | X: {sp.X - centerX:0.##} | Y: {sp.Y - centerY:0.##}";
                        }
                        break;
                }
            }
            catch
            {
                return $"{GetFriendlyLayerTypeName(type)} #{index} | info unavailable";
            }

            return $"{GetFriendlyLayerTypeName(type)} #{index}";
        }

        private string GetFriendlyLayerTypeName(string type)
        {
            return type switch
            {
                "RedSpawn" => "Red Spawn",
                "BlueSpawn" => "Blue Spawn",
                _ => type
            };
        }

        private void ApplyMultiPropertiesButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedItems.Count == 0)
            {
                UpdateStatus("No selected objects for multi-property apply.");
                return;
            }

            string colorText = MultiColorTextBox.Text.Trim();
            string traitText = MultiTraitTextBox.Text.Trim();
            string cMaskText = MultiCMaskTextBox.Text.Trim();
            string cGroupText = MultiCGroupTextBox.Text.Trim();
            bool hasColor = !string.IsNullOrWhiteSpace(colorText);
            bool hasTrait = !string.IsNullOrWhiteSpace(traitText);
            bool hasCMask = !string.IsNullOrWhiteSpace(cMaskText);
            bool hasCGroup = !string.IsNullOrWhiteSpace(cGroupText);
            bool visShouldChange = MultiVisComboBox.SelectedIndex > 0;
            bool? visValue = null;

            if (MultiVisComboBox.SelectedIndex == 1) visValue = true;
            if (MultiVisComboBox.SelectedIndex == 2) visValue = false;
            if (MultiVisComboBox.SelectedIndex == 3) visValue = null;

            if (!hasColor && !hasTrait && !hasCMask && !hasCGroup && !visShouldChange)
            {
                UpdateStatus("No multi-property values entered.");
                return;
            }

            PushUndoState("Apply Multi Properties");

            int affected = 0;
            foreach (SelectedItem item in selectedItems.ToList())
            {
                if (IsObjectLocked(item.Type, item.Index))
                {
                    continue;
                }

                if (TryApplyCommonPropertiesToItem(item, hasColor, colorText, hasTrait, traitText, hasCMask, cMaskText, hasCGroup, cGroupText, visShouldChange, visValue))
                {
                    affected++;
                }
            }

            RenderStadium();
            UpdateObjectsList();
            UpdateJsonPreview();
            UpdateMultiSelectionSummary();
            UpdateStatus($"Applied common properties to {affected} selected object(s). Unsupported fields were skipped.");
        }

        private bool TryApplyCommonPropertiesToItem(SelectedItem item, bool hasColor, string colorText, bool hasTrait, string traitText, bool hasCMask, string cMaskText, bool hasCGroup, string cGroupText, bool visShouldChange, bool? visValue)
        {
            bool touched = false;
            List<string>? cMask = hasCMask ? ParseCollisionText(cMaskText) : null;
            List<string>? cGroup = hasCGroup ? ParseCollisionText(cGroupText) : null;

            switch (item.Type)
            {
                case "Segment":
                    if (item.Index < 0 || item.Index >= stadium.Segments.Count) return false;
                    SegmentData segment = stadium.Segments[item.Index];
                    if (hasColor) { segment.Color = NormalizeColorForExport(colorText); touched = true; }
                    if (hasTrait) { segment.ExtensionData = SetExtensionString(segment.ExtensionData, "trait", traitText); touched = true; }
                    if (visShouldChange) { segment.ExtensionData = SetExtensionBool(segment.ExtensionData, "vis", visValue); touched = true; }
                    if (hasCMask) { segment.CMask = cMask; touched = true; }
                    if (hasCGroup) { segment.CGroup = cGroup; touched = true; }
                    return touched;

                case "Disc":
                    if (item.Index < 0 || item.Index >= stadium.Discs.Count) return false;
                    DiscData disc = stadium.Discs[item.Index];
                    if (hasColor) { disc.Color = NormalizeColorForExport(colorText); touched = true; }
                    if (hasTrait) { disc.ExtensionData = SetExtensionString(disc.ExtensionData, "trait", traitText); touched = true; }
                    if (visShouldChange) { disc.ExtensionData = SetExtensionBool(disc.ExtensionData, "vis", visValue); touched = true; }
                    if (hasCMask) { disc.CMask = cMask; touched = true; }
                    if (hasCGroup) { disc.CGroup = cGroup; touched = true; }
                    return touched;

                case "Plane":
                    if (item.Index < 0 || item.Index >= stadium.Planes.Count) return false;
                    PlaneData plane = stadium.Planes[item.Index];
                    if (hasTrait) { plane.ExtensionData = SetExtensionString(plane.ExtensionData, "trait", traitText); touched = true; }
                    if (visShouldChange) { plane.ExtensionData = SetExtensionBool(plane.ExtensionData, "vis", visValue); touched = true; }
                    if (hasCMask) { plane.CMask = cMask; touched = true; }
                    if (hasCGroup) { plane.CGroup = cGroup; touched = true; }
                    return touched;

                case "Joint":
                    if (item.Index < 0 || item.Index >= stadium.Joints.Count) return false;
                    JointData joint = stadium.Joints[item.Index];
                    if (hasColor) { joint.Color = NormalizeColorForExport(colorText); touched = true; }
                    return touched;
            }

            return false;
        }

        private void DeleteSelectedItems()
        {
            selectedItems.RemoveAll(item => IsObjectLocked(item.Type, item.Index));
            if (selectedItems.Count == 0)
            {
                UpdateStatus("Selected objects are locked.");
                return;
            }

            PushUndoState("Delete Selected Objects");

            HashSet<int> vertexes = GetSelectedIndices("Vertex");
            HashSet<int> segments = GetSelectedIndices("Segment");
            HashSet<int> discs = GetSelectedIndices("Disc");
            HashSet<int> goals = GetSelectedIndices("Goal");
            HashSet<int> planes = GetSelectedIndices("Plane");
            HashSet<int> joints = GetSelectedIndices("Joint");
            HashSet<int> redSpawns = GetSelectedIndices("RedSpawn");
            HashSet<int> blueSpawns = GetSelectedIndices("BlueSpawn");

            if (vertexes.Count > 0)
            {
                List<SegmentData> newSegments = new();
                for (int i = 0; i < stadium.Segments.Count; i++)
                {
                    SegmentData seg = stadium.Segments[i];
                    if (segments.Contains(i) || vertexes.Contains(seg.V0) || vertexes.Contains(seg.V1)) continue;
                    newSegments.Add(CloneSegmentWithAdjustedVertexes(seg, vertexes));
                }
                stadium.Segments = newSegments;
                RemoveIndexes(stadium.Vertexes, vertexes);
            }
            else
            {
                RemoveIndexes(stadium.Segments, segments);
            }

            if (discs.Count > 0)
            {
                List<JointData> newJoints = new();
                for (int i = 0; i < stadium.Joints.Count; i++)
                {
                    JointData joint = stadium.Joints[i];
                    if (joints.Contains(i) || discs.Contains(joint.D0) || discs.Contains(joint.D1)) continue;
                    newJoints.Add(CloneJointWithAdjustedDiscs(joint, discs));
                }
                stadium.Joints = newJoints;
                RemoveIndexes(stadium.Discs, discs);
            }
            else
            {
                RemoveIndexes(stadium.Joints, joints);
            }

            RemoveIndexes(stadium.Goals, goals);
            RemoveIndexes(stadium.Planes, planes);
            RemoveIndexes(stadium.RedSpawnPoints, redSpawns);
            RemoveIndexes(stadium.BlueSpawnPoints, blueSpawns);

            int removedCount = selectedItems.Count;
            ClearSelection();
            RenderStadium();
            UpdateObjectCount();
            UpdateObjectsList();
            UpdateJsonPreview();
            UpdateStatus($"Deleted {removedCount} selected object(s).");
        }

        private HashSet<int> GetSelectedIndices(string type)
        {
            HashSet<int> result = new();
            foreach (SelectedItem item in selectedItems)
            {
                if (item.Type == type) result.Add(item.Index);
            }
            return result;
        }

        private void RemoveIndexes<T>(List<T> list, HashSet<int> indexes)
        {
            if (indexes.Count == 0) return;
            List<int> sorted = new(indexes);
            sorted.Sort((a, b) => b.CompareTo(a));
            foreach (int index in sorted)
            {
                if (index >= 0 && index < list.Count) list.RemoveAt(index);
            }
        }

        private int AdjustIndexAfterRemoval(int oldIndex, HashSet<int> removed)
        {
            int shift = 0;
            foreach (int index in removed)
            {
                if (index < oldIndex) shift++;
            }
            return oldIndex - shift;
        }

        private SegmentData CloneSegmentWithAdjustedVertexes(SegmentData segment, HashSet<int> removedVertexes)
        {
            return new SegmentData
            {
                V0 = AdjustIndexAfterRemoval(segment.V0, removedVertexes),
                V1 = AdjustIndexAfterRemoval(segment.V1, removedVertexes),
                Color = segment.Color,
                Curve = segment.Curve,
                BCoef = segment.BCoef,
                CGroup = segment.CGroup,
                CMask = segment.CMask,
                ExtensionData = CloneExtensionData(segment.ExtensionData)
            };
        }

        private JointData CloneJointWithAdjustedDiscs(JointData joint, HashSet<int> removedDiscs)
        {
            return new JointData
            {
                D0 = AdjustIndexAfterRemoval(joint.D0, removedDiscs),
                D1 = AdjustIndexAfterRemoval(joint.D1, removedDiscs),
                Strength = joint.Strength,
                Length = joint.Length,
                Color = joint.Color,
                ExtensionData = CloneExtensionData(joint.ExtensionData)
            };
        }

        private void LayersSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            layersSearchText = LayersSearchTextBox?.Text?.Trim() ?? "";
            UpdateObjectsList();
        }

        private void LayersTypeFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LayersTypeFilterComboBox?.SelectedItem is ComboBoxItem item && item.Content != null)
            {
                layersTypeFilter = item.Content.ToString() ?? "All";
            }
            else
            {
                layersTypeFilter = "All";
            }

            UpdateObjectsList();
        }

        private void ObjectsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isUpdatingObjectsList)
            {
                return;
            }

            LayerListItem? item = GetSelectedLayerItem();
            if (item == null || item.IsHeader)
            {
                return;
            }

            SelectLayerObject(item, false);
        }

        private void ObjectsListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            LayerListItem? item = GetSelectedLayerItem();
            if (item == null || item.IsHeader)
            {
                return;
            }

            SelectLayerObject(item, true);
        }

        private LayerListItem? GetSelectedLayerItem()
        {
            if (ObjectsListBox.SelectedItem is ListBoxItem listBoxItem && listBoxItem.Tag is LayerListItem layerItem)
            {
                return layerItem;
            }

            return null;
        }

        private void SelectLayerObject(LayerListItem item, bool focusViewport)
        {
            switch (item.Type)
            {
                case "Vertex":
                    SelectVertex(item.Index);
                    break;
                case "Segment":
                    SelectSegment(item.Index);
                    break;
                case "Disc":
                    SelectDisc(item.Index);
                    break;
                case "Goal":
                    SelectGoal(item.Index);
                    break;
                case "Plane":
                    SelectPlane(item.Index);
                    break;
                case "Joint":
                    SelectJoint(item.Index);
                    break;
                case "RedSpawn":
                    SelectRedSpawn(item.Index);
                    break;
                case "BlueSpawn":
                    SelectBlueSpawn(item.Index);
                    break;
                default:
                    return;
            }

            if (focusViewport)
            {
                FocusViewportOnLayerObject(item);
            }
            else
            {
                RenderStadium();
            }

            UpdateObjectsList();
        }

        private void FocusViewportOnLayerObject(LayerListItem item)
        {
            if (!TryGetLayerObjectCenter(item, out Point centerPoint))
            {
                RenderStadium();
                UpdateStatus($"{item.DisplayName} selected.");
                return;
            }

            if (viewportZoom < 1.2)
            {
                viewportZoom = 1.2;
            }

            double centerX = GetCanvasCenterX();
            double centerY = GetCanvasCenterY();

            viewportPanX = -(centerPoint.X - centerX) * viewportZoom;
            viewportPanY = -(centerPoint.Y - centerY) * viewportZoom;

            UpdateViewportInfo();
            RenderStadium();
            UpdateStatus($"Focused {item.DisplayName} in viewport.");
        }

        private bool TryGetLayerObjectCenter(LayerListItem item, out Point centerPoint)
        {
            centerPoint = new Point(GetCanvasCenterX(), GetCanvasCenterY());

            switch (item.Type)
            {
                case "Vertex":
                    if (item.Index >= 0 && item.Index < stadium.Vertexes.Count)
                    {
                        VertexData vertex = stadium.Vertexes[item.Index];
                        centerPoint = new Point(vertex.X, vertex.Y);
                        return true;
                    }
                    break;

                case "Segment":
                    if (item.Index >= 0 && item.Index < stadium.Segments.Count)
                    {
                        SegmentData segment = stadium.Segments[item.Index];
                        if (segment.V0 >= 0 && segment.V0 < stadium.Vertexes.Count && segment.V1 >= 0 && segment.V1 < stadium.Vertexes.Count)
                        {
                            VertexData v0 = stadium.Vertexes[segment.V0];
                            VertexData v1 = stadium.Vertexes[segment.V1];
                            centerPoint = new Point((v0.X + v1.X) / 2.0, (v0.Y + v1.Y) / 2.0);
                            return true;
                        }
                    }
                    break;

                case "Disc":
                    if (item.Index >= 0 && item.Index < stadium.Discs.Count)
                    {
                        DiscData disc = stadium.Discs[item.Index];
                        centerPoint = new Point(disc.X, disc.Y);
                        return true;
                    }
                    break;

                case "Goal":
                    if (item.Index >= 0 && item.Index < stadium.Goals.Count)
                    {
                        GoalData goal = stadium.Goals[item.Index];
                        centerPoint = new Point((goal.X0 + goal.X1) / 2.0, (goal.Y0 + goal.Y1) / 2.0);
                        return true;
                    }
                    break;

                case "Plane":
                    if (item.Index >= 0 && item.Index < stadium.Planes.Count)
                    {
                        PlaneData plane = stadium.Planes[item.Index];
                        if (plane.Normal != null && plane.Normal.Count >= 2)
                        {
                            centerPoint = new Point(
                                GetCanvasCenterX() + plane.Normal[0] * plane.Dist,
                                GetCanvasCenterY() + plane.Normal[1] * plane.Dist);
                            return true;
                        }
                    }
                    break;

                case "Joint":
                    if (item.Index >= 0 && item.Index < stadium.Joints.Count)
                    {
                        JointData joint = stadium.Joints[item.Index];
                        if (joint.D0 >= 0 && joint.D0 < stadium.Discs.Count && joint.D1 >= 0 && joint.D1 < stadium.Discs.Count)
                        {
                            DiscData d0 = stadium.Discs[joint.D0];
                            DiscData d1 = stadium.Discs[joint.D1];
                            centerPoint = new Point((d0.X + d1.X) / 2.0, (d0.Y + d1.Y) / 2.0);
                            return true;
                        }
                    }
                    break;

                case "RedSpawn":
                    if (item.Index >= 0 && item.Index < stadium.RedSpawnPoints.Count)
                    {
                        SpawnPointData spawn = stadium.RedSpawnPoints[item.Index];
                        centerPoint = new Point(spawn.X, spawn.Y);
                        return true;
                    }
                    break;

                case "BlueSpawn":
                    if (item.Index >= 0 && item.Index < stadium.BlueSpawnPoints.Count)
                    {
                        SpawnPointData spawn = stadium.BlueSpawnPoints[item.Index];
                        centerPoint = new Point(spawn.X, spawn.Y);
                        return true;
                    }
                    break;
            }

            return false;
        }

        private bool TryExtractIndex(string text, string prefix, out int index)
        {
            index = -1;
            string rest = text.Substring(prefix.Length);
            string numberText = "";
            foreach (char c in rest)
            {
                if (char.IsDigit(c)) numberText += c;
                else break;
            }
            return int.TryParse(numberText, NumberStyles.Integer, CultureInfo.InvariantCulture, out index);
        }

        private class SelectedItem
        {
            public string Type { get; }
            public int Index { get; }

            public SelectedItem(string type, int index)
            {
                Type = type;
                Index = index;
            }
        }

        private class ClipboardItem
        {
            public string Type { get; }
            public string Json { get; }
            public int OriginalIndex { get; }

            public ClipboardItem(string type, string json, int originalIndex)
            {
                Type = type;
                Json = json;
                OriginalIndex = originalIndex;
            }
        }

        private class ValidationIssue
        {
            public string Severity { get; }
            public string ObjectType { get; }
            public int ObjectIndex { get; }
            public string Message { get; }

            public ValidationIssue(string severity, string objectType, int objectIndex, string message)
            {
                Severity = severity;
                ObjectType = objectType;
                ObjectIndex = objectIndex;
                Message = message;
            }
        }

        private class LayerListItem
        {
            public string Type { get; }
            public int Index { get; }
            public string DisplayName { get; }
            public string SearchText { get; }
            public bool IsHeader { get; }

            public LayerListItem(string type, int index, string displayName, string searchText, bool isHeader = false)
            {
                Type = type;
                Index = index;
                DisplayName = displayName;
                SearchText = searchText;
                IsHeader = isHeader;
            }

            public override string ToString()
            {
                return DisplayName;
            }
        }

        private string CreateEditorSnapshot()
        {
            JsonSerializerOptions options = new()
            {
                WriteIndented = false,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            return JsonSerializer.Serialize(stadium, options);
        }

        private void PushUndoState(string reason)
        {
            if (isRestoringHistory || suppressUndoPush)
            {
                return;
            }

            hasUnsavedChangesForAutoSave = true;

            undoStack.Push(CreateEditorSnapshot());
            undoDescriptionStack.Push(string.IsNullOrWhiteSpace(reason) ? "Edit Stadium" : reason.Trim());
            redoStack.Clear();
            redoDescriptionStack.Clear();

            TrimHistoryStacks();
            UpdateHistoryPanel();
        }

        private void TrimHistoryStacks()
        {
            const int maxHistory = 80;
            TrimStackPair(undoStack, undoDescriptionStack, maxHistory);
            TrimStackPair(redoStack, redoDescriptionStack, maxHistory);
        }

        private static void TrimStackPair(Stack<string> snapshots, Stack<string> descriptions, int maxCount)
        {
            if (snapshots.Count <= maxCount && descriptions.Count <= maxCount)
            {
                return;
            }

            string[] snapshotItems = snapshots.ToArray();
            string[] descriptionItems = descriptions.ToArray();

            snapshots.Clear();
            descriptions.Clear();

            int count = Math.Min(Math.Min(snapshotItems.Length, descriptionItems.Length), maxCount);
            for (int i = count - 1; i >= 0; i--)
            {
                snapshots.Push(snapshotItems[i]);
                descriptions.Push(descriptionItems[i]);
            }
        }

        private void ClearHistoryStacks()
        {
            undoStack.Clear();
            redoStack.Clear();
            undoDescriptionStack.Clear();
            redoDescriptionStack.Clear();
            UpdateHistoryPanel();
        }

        private void RestoreEditorSnapshot(string snapshot)
        {
            JsonSerializerOptions options = new()
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };

            StadiumData? restored = JsonSerializer.Deserialize<StadiumData>(snapshot, options);
            if (restored == null)
            {
                UpdateStatus("History restore failed.");
                return;
            }

            isRestoringHistory = true;
            try
            {
                CancelAllDrags();
                stadium = restored;
                EnsureDefaultBackground();
                ClearSelection();
                UpdateBackgroundUiFromData();
                RenderStadium();
                UpdateObjectCount();
                UpdateObjectsList();
                UpdateJsonPreview();
                UpdateHistoryPanel();
            }
            finally
            {
                isRestoringHistory = false;
            }
        }

        private void UndoLastAction()
        {
            if (undoStack.Count == 0)
            {
                UpdateStatus("Nothing to undo.");
                UpdateHistoryPanel();
                return;
            }

            string undoDescription = undoDescriptionStack.Count > 0 ? undoDescriptionStack.Pop() : "Edit Stadium";

            redoStack.Push(CreateEditorSnapshot());
            redoDescriptionStack.Push(undoDescription);

            RestoreEditorSnapshot(undoStack.Pop());
            UpdateHistoryPanel();
            UpdateStatus($"Undo applied: {undoDescription}.");
        }

        private void RedoLastAction()
        {
            if (redoStack.Count == 0)
            {
                UpdateStatus("Nothing to redo.");
                UpdateHistoryPanel();
                return;
            }

            string redoDescription = redoDescriptionStack.Count > 0 ? redoDescriptionStack.Pop() : "Edit Stadium";

            undoStack.Push(CreateEditorSnapshot());
            undoDescriptionStack.Push(redoDescription);

            RestoreEditorSnapshot(redoStack.Pop());
            UpdateHistoryPanel();
            UpdateStatus($"Redo applied: {redoDescription}.");
        }

        private void UpdateHistoryPanel()
        {
            if (HistoryListBox == null || HistorySummaryText == null)
            {
                return;
            }

            HistoryListBox.Items.Clear();

            if (HistoryUndoButton != null)
            {
                HistoryUndoButton.IsEnabled = undoStack.Count > 0;
            }

            if (HistoryRedoButton != null)
            {
                HistoryRedoButton.IsEnabled = redoStack.Count > 0;
            }

            HistorySummaryText.Text = $"{undoStack.Count} undo action(s) • {redoStack.Count} redo action(s)";

            if (undoStack.Count == 0 && redoStack.Count == 0)
            {
                HistoryListBox.Items.Add(CreateHistoryEmptyItem());
                return;
            }

            int undoIndex = 0;
            foreach (string description in undoDescriptionStack)
            {
                HistoryListBox.Items.Add(CreateHistoryListItem(description, "Undo", undoIndex == 0, undoIndex + 1));
                undoIndex++;
            }

            int redoIndex = 0;
            foreach (string description in redoDescriptionStack)
            {
                HistoryListBox.Items.Add(CreateHistoryListItem(description, "Redo", redoIndex == 0, redoIndex + 1));
                redoIndex++;
            }
        }

        private ListBoxItem CreateHistoryEmptyItem()
        {
            DockPanel row = new()
            {
                Margin = new Thickness(8, 10, 8, 10)
            };

            Border iconBox = new()
            {
                Width = 34,
                Height = 34,
                CornerRadius = new CornerRadius(10),
                Background = new SolidColorBrush(Color.FromRgb(31, 38, 48)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(75, 85, 100)),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 10, 0),
                Child = CreatePackIcon("History", 20, new SolidColorBrush(Color.FromRgb(166, 176, 190)))
            };
            DockPanel.SetDock(iconBox, Dock.Left);
            row.Children.Add(iconBox);

            StackPanel textPanel = new()
            {
                VerticalAlignment = VerticalAlignment.Center
            };

            textPanel.Children.Add(new TextBlock
            {
                Text = "No history yet.",
                Foreground = new SolidColorBrush(Color.FromRgb(232, 237, 244)),
                FontSize = 13,
                FontWeight = FontWeights.SemiBold
            });

            textPanel.Children.Add(new TextBlock
            {
                Text = "Your edits will appear here after the first change.",
                Foreground = new SolidColorBrush(Color.FromRgb(145, 155, 170)),
                FontSize = 11,
                Margin = new Thickness(0, 2, 0, 0)
            });

            row.Children.Add(textPanel);

            return new ListBoxItem
            {
                Content = row,
                IsEnabled = false,
                Padding = new Thickness(6),
                Margin = new Thickness(0, 2, 0, 2),
                Background = new SolidColorBrush(Color.FromRgb(18, 22, 29)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(42, 50, 62)),
                BorderThickness = new Thickness(1)
            };
        }

        private ListBoxItem CreateHistoryListItem(string description, string stackType, bool isNextAction, int index)
        {
            bool isUndo = stackType == "Undo";

            Brush accentBrush = isUndo
                ? new SolidColorBrush(Color.FromRgb(96, 165, 250))
                : new SolidColorBrush(Color.FromRgb(74, 222, 128));

            DockPanel row = new()
            {
                VerticalAlignment = VerticalAlignment.Center
            };

            Border iconBox = new()
            {
                Width = 30,
                Height = 30,
                CornerRadius = new CornerRadius(9),
                Background = isUndo
                    ? new SolidColorBrush(Color.FromRgb(25, 45, 74))
                    : new SolidColorBrush(Color.FromRgb(25, 68, 43)),
                BorderBrush = accentBrush,
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 10, 0),
                Child = CreatePackIcon(isUndo ? "Undo" : "Redo", 17, accentBrush)
            };
            DockPanel.SetDock(iconBox, Dock.Left);
            row.Children.Add(iconBox);

            StackPanel textPanel = new()
            {
                VerticalAlignment = VerticalAlignment.Center
            };

            textPanel.Children.Add(new TextBlock
            {
                Text = description,
                Foreground = new SolidColorBrush(Color.FromRgb(232, 237, 244)),
                FontSize = 12,
                FontWeight = isNextAction ? FontWeights.SemiBold : FontWeights.Normal,
                TextTrimming = TextTrimming.CharacterEllipsis
            });

            textPanel.Children.Add(new TextBlock
            {
                Text = isNextAction ? $"Next {stackType.ToLowerInvariant()} action" : $"{stackType} stack item #{index}",
                Foreground = new SolidColorBrush(Color.FromRgb(145, 155, 170)),
                FontSize = 10.5,
                Margin = new Thickness(0, 2, 0, 0)
            });

            row.Children.Add(textPanel);

            return new ListBoxItem
            {
                Content = row,
                Padding = new Thickness(8, 7, 8, 7),
                Margin = new Thickness(0, 2, 0, 2),
                Background = isNextAction
                    ? new SolidColorBrush(Color.FromRgb(24, 36, 54))
                    : new SolidColorBrush(Color.FromRgb(18, 22, 29)),
                BorderBrush = isNextAction
                    ? accentBrush
                    : new SolidColorBrush(Color.FromRgb(42, 50, 62)),
                BorderThickness = new Thickness(1),
                IsEnabled = false
            };
        }

        private void HistoryUndoButton_Click(object sender, RoutedEventArgs e) => UndoLastAction();
        private void HistoryRedoButton_Click(object sender, RoutedEventArgs e) => RedoLastAction();

        private void UndoButton_Click(object sender, RoutedEventArgs e) => UndoLastAction();
        private void RedoButton_Click(object sender, RoutedEventArgs e) => RedoLastAction();
        private void CopyButton_Click(object sender, RoutedEventArgs e) => CopySelectedObjectsToClipboard();
        private void PasteButton_Click(object sender, RoutedEventArgs e) => PasteClipboardObjects();
        private void DuplicateButton_Click(object sender, RoutedEventArgs e) => DuplicateSelectedObjects();
        private void MirrorSelectedHorizontallyButton_Click(object sender, RoutedEventArgs e) => MirrorSelectedObjects(true);
        private void MirrorSelectedVerticallyButton_Click(object sender, RoutedEventArgs e) => MirrorSelectedObjects(false);

        private void SnapToGridButton_Click(object sender, RoutedEventArgs e)
        {
            if (SnapToGridMenuItem == null)
            {
                return;
            }

            SetSnapToGrid(SnapToGridMenuItem.IsChecked, true, false);
        }

        private void SnapToGridCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            SetSnapToGrid(SnapToGridCheckBox.IsChecked == true, false, true);
        }

        private void SetSnapToGrid(bool enabled, bool updateTopCheckBox, bool updateMenuCheck)
        {
            if (isUpdatingSnapUi)
            {
                return;
            }

            snapToGrid = enabled;
            isUpdatingSnapUi = true;

            try
            {
                if (updateTopCheckBox && SnapToGridCheckBox != null)
                {
                    SnapToGridCheckBox.IsChecked = enabled;
                }

                if (updateMenuCheck && SnapToGridMenuItem != null)
                {
                    SnapToGridMenuItem.IsChecked = enabled;
                }
            }
            finally
            {
                isUpdatingSnapUi = false;
            }

            UpdateViewportMiniToolbarUi();
            SaveEditorPreferences();
            UpdateStatus(snapToGrid ? $"Snap to grid enabled ({snapGridSize:0.##})." : "Snap to grid disabled.");
        }

        private void SnapGridSizeTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            ApplySnapGridSizeFromUi();
        }

        private void SnapGridSizeTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ApplySnapGridSizeFromUi();
                Keyboard.ClearFocus();
                e.Handled = true;
            }
        }

        private void ApplySnapGridSizeFromUi()
        {
            if (TryReadPositiveDouble(SnapGridSizeTextBox.Text, out double size))
            {
                snapGridSize = Math.Max(1, size);
                SnapGridSizeTextBox.Text = snapGridSize.ToString("0.##", CultureInfo.InvariantCulture);
                UpdateStatus($"Snap grid size set to {snapGridSize:0.##}.");
            }
            else
            {
                SnapGridSizeTextBox.Text = snapGridSize.ToString("0.##", CultureInfo.InvariantCulture);
                UpdateStatus("Invalid snap grid size. Previous value kept.");
            }
        }

        private Point SnapDataPoint(Point point)
        {
            if (!snapToGrid || snapGridSize <= 0)
            {
                return point;
            }

            double centerX = GetCanvasCenterX();
            double centerY = GetCanvasCenterY();
            double x = centerX + Math.Round((point.X - centerX) / snapGridSize) * snapGridSize;
            double y = centerY + Math.Round((point.Y - centerY) / snapGridSize) * snapGridSize;
            return new Point(x, y);
        }

        private string GetObjectKey(string type, int index) => $"{type}:{index}";
        private bool IsObjectHidden(string type, int index) => hiddenObjectKeys.Contains(GetObjectKey(type, index));
        private bool IsObjectLocked(string type, int index) => lockedObjectKeys.Contains(GetObjectKey(type, index));

        private void HideSelectedLayerButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedItems.Count == 0)
            {
                UpdateStatus("No layer object selected to hide.");
                return;
            }

            foreach (SelectedItem item in selectedItems)
            {
                hiddenObjectKeys.Add(GetObjectKey(item.Type, item.Index));
            }

            RenderStadium();
            UpdateObjectsList();
            UpdateStatus($"Hidden {selectedItems.Count} object(s) in editor view.");
        }

        private void ShowAllLayersButton_Click(object sender, RoutedEventArgs e)
        {
            hiddenObjectKeys.Clear();
            RenderStadium();
            UpdateObjectsList();
            UpdateStatus("All layer objects are visible.");
        }

        private void LockSelectedLayerButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedItems.Count == 0)
            {
                UpdateStatus("No layer object selected to lock.");
                return;
            }

            foreach (SelectedItem item in selectedItems)
            {
                lockedObjectKeys.Add(GetObjectKey(item.Type, item.Index));
            }

            RenderStadium();
            UpdateObjectsList();
            UpdateStatus($"Locked {selectedItems.Count} object(s).");
        }

        private void UnlockAllLayersButton_Click(object sender, RoutedEventArgs e)
        {
            lockedObjectKeys.Clear();
            RenderStadium();
            UpdateObjectsList();
            UpdateStatus("All layer objects are unlocked.");
        }

        private T CloneData<T>(T source)
        {
            string json = JsonSerializer.Serialize(source, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });
            T? cloned = JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (cloned == null)
            {
                throw new InvalidOperationException("Clone failed.");
            }
            return cloned;
        }

        private void CopySelectedObjectsToClipboard()
        {
            if (selectedItems.Count == 0)
            {
                UpdateStatus("No object selected to copy.");
                return;
            }

            clipboardItems.Clear();
            foreach (SelectedItem item in selectedItems)
            {
                object? data = GetObjectDataForClipboard(item);
                if (data == null) continue;
                clipboardItems.Add(new ClipboardItem(item.Type, JsonSerializer.Serialize(data), item.Index));
            }

            UpdateStatus($"Copied {clipboardItems.Count} object(s).");
        }

        private object? GetObjectDataForClipboard(SelectedItem item)
        {
            return item.Type switch
            {
                "Vertex" when item.Index >= 0 && item.Index < stadium.Vertexes.Count => stadium.Vertexes[item.Index],
                "Segment" when item.Index >= 0 && item.Index < stadium.Segments.Count => stadium.Segments[item.Index],
                "Disc" when item.Index >= 0 && item.Index < stadium.Discs.Count => stadium.Discs[item.Index],
                "Goal" when item.Index >= 0 && item.Index < stadium.Goals.Count => stadium.Goals[item.Index],
                "Plane" when item.Index >= 0 && item.Index < stadium.Planes.Count => stadium.Planes[item.Index],
                "Joint" when item.Index >= 0 && item.Index < stadium.Joints.Count => stadium.Joints[item.Index],
                "RedSpawn" when item.Index >= 0 && item.Index < stadium.RedSpawnPoints.Count => stadium.RedSpawnPoints[item.Index],
                "BlueSpawn" when item.Index >= 0 && item.Index < stadium.BlueSpawnPoints.Count => stadium.BlueSpawnPoints[item.Index],
                _ => null
            };
        }

        private void PasteClipboardObjects()
        {
            if (clipboardItems.Count == 0)
            {
                UpdateStatus("Clipboard is empty.");
                return;
            }

            PushUndoState("Paste");
            selectedItems.Clear();
            ClearSingleSelectionIndexes();
            PasteClipboardItems(clipboardItems, 24);
            FinishPasteOrDuplicate("Pasted");
        }

        private void DuplicateSelectedObjects()
        {
            if (selectedItems.Count == 0)
            {
                UpdateStatus("No object selected to duplicate.");
                return;
            }

            PushUndoState("Duplicate");
            List<ClipboardItem> items = new();
            foreach (SelectedItem item in selectedItems)
            {
                object? data = GetObjectDataForClipboard(item);
                if (data != null)
                {
                    items.Add(new ClipboardItem(item.Type, JsonSerializer.Serialize(data), item.Index));
                }
            }

            selectedItems.Clear();
            ClearSingleSelectionIndexes();
            PasteClipboardItems(items, 24);
            FinishPasteOrDuplicate("Duplicated");
        }

        private void PasteClipboardItems(List<ClipboardItem> items, double offset)
        {
            foreach (ClipboardItem item in items)
            {
                PasteSingleClipboardItem(item, offset);
            }
        }

        private void PasteSingleClipboardItem(ClipboardItem item, double offset)
        {
            try
            {
                switch (item.Type)
                {
                    case "Vertex":
                        VertexData vertex = JsonSerializer.Deserialize<VertexData>(item.Json)!;
                        vertex.X += offset;
                        vertex.Y += offset;
                        stadium.Vertexes.Add(vertex);
                        selectedItems.Add(new SelectedItem("Vertex", stadium.Vertexes.Count - 1));
                        break;

                    case "Disc":
                        DiscData disc = JsonSerializer.Deserialize<DiscData>(item.Json)!;
                        disc.X += offset;
                        disc.Y += offset;
                        stadium.Discs.Add(disc);
                        selectedItems.Add(new SelectedItem("Disc", stadium.Discs.Count - 1));
                        break;

                    case "Goal":
                        GoalData goal = JsonSerializer.Deserialize<GoalData>(item.Json)!;
                        goal.X0 += offset;
                        goal.Y0 += offset;
                        goal.X1 += offset;
                        goal.Y1 += offset;
                        stadium.Goals.Add(goal);
                        selectedItems.Add(new SelectedItem("Goal", stadium.Goals.Count - 1));
                        break;

                    case "Plane":
                        PlaneData plane = JsonSerializer.Deserialize<PlaneData>(item.Json)!;
                        plane.Dist += offset;
                        stadium.Planes.Add(plane);
                        selectedItems.Add(new SelectedItem("Plane", stadium.Planes.Count - 1));
                        break;

                    case "RedSpawn":
                        SpawnPointData redSpawn = JsonSerializer.Deserialize<SpawnPointData>(item.Json)!;
                        redSpawn.X += offset;
                        redSpawn.Y += offset;
                        stadium.RedSpawnPoints.Add(redSpawn);
                        selectedItems.Add(new SelectedItem("RedSpawn", stadium.RedSpawnPoints.Count - 1));
                        break;

                    case "BlueSpawn":
                        SpawnPointData blueSpawn = JsonSerializer.Deserialize<SpawnPointData>(item.Json)!;
                        blueSpawn.X += offset;
                        blueSpawn.Y += offset;
                        stadium.BlueSpawnPoints.Add(blueSpawn);
                        selectedItems.Add(new SelectedItem("BlueSpawn", stadium.BlueSpawnPoints.Count - 1));
                        break;

                    case "Segment":
                        SegmentData segment = JsonSerializer.Deserialize<SegmentData>(item.Json)!;
                        if (segment.V0 >= 0 && segment.V0 < stadium.Vertexes.Count && segment.V1 >= 0 && segment.V1 < stadium.Vertexes.Count)
                        {
                            VertexData v0 = CloneData(stadium.Vertexes[segment.V0]);
                            VertexData v1 = CloneData(stadium.Vertexes[segment.V1]);
                            v0.X += offset;
                            v0.Y += offset;
                            v1.X += offset;
                            v1.Y += offset;
                            stadium.Vertexes.Add(v0);
                            int newV0 = stadium.Vertexes.Count - 1;
                            stadium.Vertexes.Add(v1);
                            int newV1 = stadium.Vertexes.Count - 1;
                            segment.V0 = newV0;
                            segment.V1 = newV1;
                            stadium.Segments.Add(segment);
                            selectedItems.Add(new SelectedItem("Segment", stadium.Segments.Count - 1));
                        }
                        break;

                    case "Joint":
                        JointData joint = JsonSerializer.Deserialize<JointData>(item.Json)!;
                        if (joint.D0 >= 0 && joint.D0 < stadium.Discs.Count && joint.D1 >= 0 && joint.D1 < stadium.Discs.Count)
                        {
                            DiscData d0 = CloneData(stadium.Discs[joint.D0]);
                            DiscData d1 = CloneData(stadium.Discs[joint.D1]);
                            d0.X += offset;
                            d0.Y += offset;
                            d1.X += offset;
                            d1.Y += offset;
                            stadium.Discs.Add(d0);
                            int newD0 = stadium.Discs.Count - 1;
                            stadium.Discs.Add(d1);
                            int newD1 = stadium.Discs.Count - 1;
                            joint.D0 = newD0;
                            joint.D1 = newD1;
                            stadium.Joints.Add(joint);
                            selectedItems.Add(new SelectedItem("Joint", stadium.Joints.Count - 1));
                        }
                        break;
                }
            }
            catch
            {
                UpdateStatus($"Could not paste {item.Type} #{item.OriginalIndex}.");
            }
        }

        private void FinishPasteOrDuplicate(string actionText)
        {
            ClearSingleSelectionIndexes();
            UpdateMultiSelectionSummary();
            RenderStadium();
            UpdateObjectCount();
            UpdateObjectsList();
            UpdateJsonPreview();
            UpdateStatus($"{actionText} {selectedItems.Count} object(s).");
        }

        private void ValidateButton_Click(object sender, RoutedEventArgs e)
        {
            RefreshValidationPanel(true);
        }

        private void ValidationResultsListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ValidationResultsListBox?.SelectedItem is ListBoxItem item && item.Tag is ValidationIssue issue)
            {
                FocusValidationIssue(issue);
            }
        }

        private bool ShouldCancelSaveDueToValidation()
        {
            if (!validationWarningBeforeSaveEnabled)
            {
                return false;
            }

            List<ValidationIssue> issues = ValidateStadiumData();
            int criticalCount = CountCriticalValidationIssues(issues);

            if (criticalCount == 0)
            {
                return false;
            }

            RefreshValidationPanel(false, issues);

            MessageBoxResult result = MessageBox.Show(
                $"This stadium has {criticalCount} critical validation error(s). Save anyway?",
                "Validation Warning",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            return result != MessageBoxResult.Yes;
        }

        private void RefreshValidationPanel(bool showStatus, List<ValidationIssue>? existingIssues = null)
        {
            if (ValidationResultsListBox == null)
            {
                return;
            }

            List<ValidationIssue> issues = existingIssues ?? ValidateStadiumData();
            int criticalCount = CountCriticalValidationIssues(issues);
            int warningCount = issues.Count(issue => issue.Severity == "Warning");
            int infoCount = issues.Count(issue => issue.Severity == "Info");

            ValidationResultsListBox.Items.Clear();

            if (issues.Count == 0)
            {
                ValidationSummaryText.Text = "Clean Stadium";
                ValidationDetailsText.Text = "0 critical issues • 0 warnings • ready to save";

                ValidationResultsListBox.Items.Add(CreateValidationEmptyStateItem());

                if (showStatus)
                {
                    UpdateStatus("Validation passed.");
                }

                return;
            }

            ValidationSummaryText.Text = criticalCount > 0
                ? $"{criticalCount} Critical  •  {warningCount} Warning"
                : $"{warningCount} Warning  •  No critical errors";

            ValidationDetailsText.Text = "Double-click an object-related result to select and focus it.";

            foreach (ValidationIssue issue in issues)
            {
                AddValidationResultItem(issue);
            }

            if (showStatus)
            {
                UpdateStatus($"Validation completed: {criticalCount} critical, {warningCount} warning.");
            }
        }

        private ListBoxItem CreateValidationEmptyStateItem()
        {
            Grid row = new()
            {
                Margin = new Thickness(8, 10, 8, 10)
            };

            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            Border iconBox = new()
            {
                Width = 34,
                Height = 34,
                CornerRadius = new CornerRadius(10),
                Background = new SolidColorBrush(Color.FromRgb(25, 68, 43)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(74, 222, 128)),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 10, 0),
                Child = CreatePackIcon("CheckCircleOutline", 20, new SolidColorBrush(Color.FromRgb(134, 239, 172)))
            };
            Grid.SetColumn(iconBox, 0);
            row.Children.Add(iconBox);

            StackPanel textPanel = new()
            {
                VerticalAlignment = VerticalAlignment.Center
            };

            textPanel.Children.Add(new TextBlock
            {
                Text = "No validation issues found.",
                Foreground = new SolidColorBrush(Color.FromRgb(220, 252, 231)),
                FontSize = 13,
                FontWeight = FontWeights.SemiBold
            });

            textPanel.Children.Add(new TextBlock
            {
                Text = "Your stadium data looks clean.",
                Foreground = new SolidColorBrush(Color.FromRgb(134, 239, 172)),
                FontSize = 11,
                Margin = new Thickness(0, 2, 0, 0)
            });

            Grid.SetColumn(textPanel, 1);
            row.Children.Add(textPanel);

            return new ListBoxItem
            {
                Content = row,
                IsEnabled = false,
                Padding = new Thickness(6),
                Margin = new Thickness(0, 2, 0, 2),
                Background = new SolidColorBrush(Color.FromRgb(18, 42, 29)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(34, 80, 51)),
                BorderThickness = new Thickness(1)
            };
        }

        private void AddValidationResultItem(ValidationIssue issue)
        {
            bool critical = issue.Severity == "Error";
            bool warning = issue.Severity == "Warning";

            Brush accentBrush = critical
                ? new SolidColorBrush(Color.FromRgb(248, 113, 113))
                : warning
                    ? new SolidColorBrush(Color.FromRgb(251, 191, 36))
                    : new SolidColorBrush(Color.FromRgb(96, 165, 250));

            Brush backgroundBrush = critical
                ? new SolidColorBrush(Color.FromRgb(48, 28, 28))
                : warning
                    ? new SolidColorBrush(Color.FromRgb(48, 40, 24))
                    : new SolidColorBrush(Color.FromRgb(24, 35, 52));

            Brush borderBrush = critical
                ? new SolidColorBrush(Color.FromRgb(105, 58, 58))
                : warning
                    ? new SolidColorBrush(Color.FromRgb(105, 88, 45))
                    : new SolidColorBrush(Color.FromRgb(50, 78, 120));

            string target = string.IsNullOrWhiteSpace(issue.ObjectType) || issue.ObjectIndex < 0
                ? "Stadium"
                : $"{GetValidationObjectDisplayName(issue.ObjectType)} #{issue.ObjectIndex}";

            Grid row = new()
            {
                VerticalAlignment = VerticalAlignment.Center
            };

            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            Border iconBox = new()
            {
                Width = 30,
                Height = 30,
                CornerRadius = new CornerRadius(9),
                Background = critical
                    ? new SolidColorBrush(Color.FromRgb(68, 30, 30))
                    : warning
                        ? new SolidColorBrush(Color.FromRgb(66, 48, 18))
                        : new SolidColorBrush(Color.FromRgb(25, 45, 74)),
                BorderBrush = accentBrush,
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 10, 0),
                Child = CreatePackIcon(GetValidationSeverityIconName(issue.Severity), 18, accentBrush)
            };
            Grid.SetColumn(iconBox, 0);
            row.Children.Add(iconBox);

            StackPanel textPanel = new()
            {
                VerticalAlignment = VerticalAlignment.Center
            };

            DockPanel titlePanel = new()
            {
                LastChildFill = true
            };

            Border severityPill = new()
            {
                Background = critical
                    ? new SolidColorBrush(Color.FromRgb(127, 29, 29))
                    : warning
                        ? new SolidColorBrush(Color.FromRgb(113, 63, 18))
                        : new SolidColorBrush(Color.FromRgb(30, 64, 120)),
                BorderBrush = accentBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(7, 2, 7, 2),
                Margin = new Thickness(0, 0, 8, 0),
                Child = new TextBlock
                {
                    Text = GetValidationSeverityLabel(issue.Severity),
                    Foreground = Brushes.White,
                    FontSize = 10,
                    FontWeight = FontWeights.Bold
                }
            };
            DockPanel.SetDock(severityPill, Dock.Left);
            titlePanel.Children.Add(severityPill);

            titlePanel.Children.Add(new TextBlock
            {
                Text = target,
                Foreground = new SolidColorBrush(Color.FromRgb(232, 237, 244)),
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            });

            textPanel.Children.Add(titlePanel);

            textPanel.Children.Add(new TextBlock
            {
                Text = issue.Message,
                Foreground = critical
                    ? new SolidColorBrush(Color.FromRgb(254, 202, 202))
                    : warning
                        ? new SolidColorBrush(Color.FromRgb(253, 230, 138))
                        : new SolidColorBrush(Color.FromRgb(191, 219, 254)),
                FontSize = 11,
                Margin = new Thickness(0, 3, 0, 0),
                TextWrapping = TextWrapping.Wrap
            });

            Grid.SetColumn(textPanel, 1);
            row.Children.Add(textPanel);

            ListBoxItem item = new()
            {
                Content = row,
                Tag = issue,
                Padding = new Thickness(8, 7, 8, 7),
                Margin = new Thickness(0, 2, 0, 2),
                Cursor = string.IsNullOrWhiteSpace(issue.ObjectType) ? Cursors.Arrow : Cursors.Hand,
                Foreground = accentBrush,
                Background = backgroundBrush,
                BorderBrush = borderBrush,
                BorderThickness = new Thickness(1),
                ContextMenu = CreateValidationIssueContextMenu(issue)
            };

            ValidationResultsListBox.Items.Add(item);
        }

        private ContextMenu CreateValidationIssueContextMenu(ValidationIssue issue)
        {
            ContextMenu menu = new()
            {
                Background = new SolidColorBrush(Color.FromRgb(32, 34, 38)),
                Foreground = new SolidColorBrush(Color.FromRgb(232, 237, 244)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(59, 64, 72)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(4),
                Style = TryFindResource("DarkContextMenu") as Style
            };

            menu.Resources.Add(SystemColors.ControlBrushKey, new SolidColorBrush(Color.FromRgb(32, 34, 38)));
            menu.Resources.Add(SystemColors.MenuBrushKey, new SolidColorBrush(Color.FromRgb(32, 34, 38)));
            menu.Resources.Add(SystemColors.MenuTextBrushKey, new SolidColorBrush(Color.FromRgb(232, 237, 244)));
            menu.Resources.Add(SystemColors.HighlightBrushKey, new SolidColorBrush(Color.FromRgb(36, 54, 70)));
            menu.Resources.Add(SystemColors.HighlightTextBrushKey, Brushes.White);

            bool hasObjectTarget = !string.IsNullOrWhiteSpace(issue.ObjectType) && issue.ObjectIndex >= 0;

            MenuItem focusItem = CreateValidationMenuItem("Focus Object", "CrosshairsGps", () => FocusValidationIssue(issue));
            focusItem.IsEnabled = hasObjectTarget;
            menu.Items.Add(focusItem);

            MenuItem selectItem = CreateValidationMenuItem("Select Object", "CursorDefaultClick", () =>
            {
                SelectLayerObject(new LayerListItem(issue.ObjectType, issue.ObjectIndex, $"{issue.ObjectType} #{issue.ObjectIndex}", issue.Message), false);
            });
            selectItem.IsEnabled = hasObjectTarget;
            menu.Items.Add(selectItem);

            menu.Items.Add(CreateLayerSeparator());
            menu.Items.Add(CreateValidationMenuItem("Copy Message", "ContentCopy", () =>
            {
                Clipboard.SetText($"{GetValidationSeverityLabel(issue.Severity)} - {issue.Message}");
                UpdateStatus("Validation message copied.");
            }));

            return menu;
        }

        private MenuItem CreateValidationMenuItem(string header, string iconName, Action action)
        {
            MenuItem menuItem = new()
            {
                Header = header,
                Foreground = new SolidColorBrush(Color.FromRgb(232, 237, 244)),
                Background = Brushes.Transparent,
                Padding = new Thickness(10, 5, 10, 5),
                Cursor = Cursors.Hand,
                Style = TryFindResource("DarkContextMenuItem") as Style,
                Icon = CreatePackIcon(iconName, 15, new SolidColorBrush(Color.FromRgb(166, 176, 190)))
            };

            menuItem.Click += (_, _) => action();
            return menuItem;
        }

        private string GetValidationSeverityIconName(string severity)
        {
            return severity switch
            {
                "Error" => "AlertCircleOutline",
                "Warning" => "AlertOutline",
                "Info" => "InformationOutline",
                _ => "InformationOutline"
            };
        }

        private string GetValidationSeverityLabel(string severity)
        {
            return severity switch
            {
                "Error" => "CRITICAL",
                "Warning" => "WARNING",
                "Info" => "INFO",
                _ => severity.ToUpperInvariant()
            };
        }

        private string GetValidationObjectDisplayName(string objectType)
        {
            return objectType switch
            {
                "RedSpawn" => "Red Spawn",
                "BlueSpawn" => "Blue Spawn",
                _ => objectType
            };
        }

        private void FocusValidationIssue(ValidationIssue issue)
        {
            if (string.IsNullOrWhiteSpace(issue.ObjectType) || issue.ObjectIndex < 0)
            {
                UpdateStatus(issue.Message);
                return;
            }

            SelectLayerObject(new LayerListItem(issue.ObjectType, issue.ObjectIndex, $"{issue.ObjectType} #{issue.ObjectIndex}", issue.Message), true);
        }

        private int CountCriticalValidationIssues(List<ValidationIssue> issues)
        {
            int count = 0;
            foreach (ValidationIssue issue in issues)
            {
                if (issue.Severity == "Error")
                {
                    count++;
                }
            }
            return count;
        }

        private List<ValidationIssue> ValidateStadiumData()
        {
            List<ValidationIssue> issues = new();

            if (stadium.Width <= 0)
            {
                issues.Add(new ValidationIssue("Error", "", -1, "Stadium width must be greater than 0."));
            }

            if (stadium.Height <= 0)
            {
                issues.Add(new ValidationIssue("Error", "", -1, "Stadium height must be greater than 0."));
            }

            ValidateBackground(issues);
            ValidateVertexes(issues);
            ValidateSegments(issues);
            ValidateDiscs(issues);
            ValidateGoals(issues);
            ValidateSpawns(issues);
            ValidatePlanes(issues);
            ValidateJoints(issues);

            return issues;
        }

        private void ValidateBackground(List<ValidationIssue> issues)
        {
            if (stadium.Bg == null)
            {
                issues.Add(new ValidationIssue("Warning", "", -1, "Background data is missing; default bg will be generated on save."));
                return;
            }

            if (stadium.Bg.Width != null && stadium.Bg.Width <= 0)
            {
                issues.Add(new ValidationIssue("Error", "", -1, "Background width must be greater than 0."));
            }

            if (stadium.Bg.Height != null && stadium.Bg.Height <= 0)
            {
                issues.Add(new ValidationIssue("Error", "", -1, "Background height must be greater than 0."));
            }

            if (!IsValidOptionalColor(stadium.Bg.Color))
            {
                issues.Add(new ValidationIssue("Warning", "", -1, $"Background color '{stadium.Bg.Color}' is not a valid hex color."));
            }
        }

        private void ValidateVertexes(List<ValidationIssue> issues)
        {
            for (int i = 0; i < stadium.Vertexes.Count; i++)
            {
                VertexData vertex = stadium.Vertexes[i];
                if (!IsFinite(vertex.X) || !IsFinite(vertex.Y))
                {
                    issues.Add(new ValidationIssue("Error", "Vertex", i, "Vertex coordinates must be finite numbers."));
                }
            }
        }

        private void ValidateSegments(List<ValidationIssue> issues)
        {
            for (int i = 0; i < stadium.Segments.Count; i++)
            {
                SegmentData segment = stadium.Segments[i];

                if (segment.V0 < 0 || segment.V0 >= stadium.Vertexes.Count)
                {
                    issues.Add(new ValidationIssue("Error", "Segment", i, $"v0 index {segment.V0} does not exist."));
                }

                if (segment.V1 < 0 || segment.V1 >= stadium.Vertexes.Count)
                {
                    issues.Add(new ValidationIssue("Error", "Segment", i, $"v1 index {segment.V1} does not exist."));
                }

                if (segment.V0 == segment.V1)
                {
                    issues.Add(new ValidationIssue("Warning", "Segment", i, "v0 and v1 are the same vertex."));
                }

                if (!IsValidOptionalColor(segment.Color))
                {
                    issues.Add(new ValidationIssue("Warning", "Segment", i, $"Color '{segment.Color}' is not a valid hex color."));
                }

                ValidateCollisionValues(issues, "Segment", i, "cMask", segment.CMask);
                ValidateCollisionValues(issues, "Segment", i, "cGroup", segment.CGroup);
            }
        }

        private void ValidateDiscs(List<ValidationIssue> issues)
        {
            for (int i = 0; i < stadium.Discs.Count; i++)
            {
                DiscData disc = stadium.Discs[i];

                if (!IsFinite(disc.X) || !IsFinite(disc.Y))
                {
                    issues.Add(new ValidationIssue("Error", "Disc", i, "Disc position must contain finite numbers."));
                }

                if (disc.Radius != null && disc.Radius <= 0)
                {
                    issues.Add(new ValidationIssue("Error", "Disc", i, "Disc radius must be greater than 0."));
                }

                if (!IsValidOptionalColor(disc.Color))
                {
                    issues.Add(new ValidationIssue("Warning", "Disc", i, $"Color '{disc.Color}' is not a valid hex color."));
                }

                ValidateCollisionValues(issues, "Disc", i, "cMask", disc.CMask);
                ValidateCollisionValues(issues, "Disc", i, "cGroup", disc.CGroup);
            }
        }

        private void ValidateGoals(List<ValidationIssue> issues)
        {
            for (int i = 0; i < stadium.Goals.Count; i++)
            {
                GoalData goal = stadium.Goals[i];

                if (!IsFinite(goal.X0) || !IsFinite(goal.Y0) || !IsFinite(goal.X1) || !IsFinite(goal.Y1))
                {
                    issues.Add(new ValidationIssue("Error", "Goal", i, "Goal points must contain finite numbers."));
                }

                if (GetDistance(new Point(goal.X0, goal.Y0), new Point(goal.X1, goal.Y1)) < 0.001)
                {
                    issues.Add(new ValidationIssue("Warning", "Goal", i, "Goal p0 and p1 are at the same position."));
                }

                string team = NormalizeGoalTeam(goal.Team);
                if (team != goal.Team?.Trim().ToLowerInvariant())
                {
                    issues.Add(new ValidationIssue("Warning", "Goal", i, $"Goal team '{goal.Team}' should be red or blue."));
                }
            }
        }


        private void ValidateSpawns(List<ValidationIssue> issues)
        {
            for (int i = 0; i < stadium.RedSpawnPoints.Count; i++)
            {
                SpawnPointData spawn = stadium.RedSpawnPoints[i];
                if (!IsFinite(spawn.X) || !IsFinite(spawn.Y))
                {
                    issues.Add(new ValidationIssue("Error", "RedSpawn", i, "Red spawn coordinates must be finite numbers."));
                }
            }

            for (int i = 0; i < stadium.BlueSpawnPoints.Count; i++)
            {
                SpawnPointData spawn = stadium.BlueSpawnPoints[i];
                if (!IsFinite(spawn.X) || !IsFinite(spawn.Y))
                {
                    issues.Add(new ValidationIssue("Error", "BlueSpawn", i, "Blue spawn coordinates must be finite numbers."));
                }
            }
        }

        private void ValidatePlanes(List<ValidationIssue> issues)
        {
            for (int i = 0; i < stadium.Planes.Count; i++)
            {
                PlaneData plane = stadium.Planes[i];

                if (plane.Normal == null || plane.Normal.Count < 2)
                {
                    issues.Add(new ValidationIssue("Error", "Plane", i, "Plane normal must contain two values."));
                    continue;
                }

                if (!IsFinite(plane.Normal[0]) || !IsFinite(plane.Normal[1]) || !IsFinite(plane.Dist))
                {
                    issues.Add(new ValidationIssue("Error", "Plane", i, "Plane normal/dist must contain finite numbers."));
                }

                double normalLength = Math.Sqrt(plane.Normal[0] * plane.Normal[0] + plane.Normal[1] * plane.Normal[1]);
                if (normalLength < 0.0001)
                {
                    issues.Add(new ValidationIssue("Error", "Plane", i, "Plane normal cannot be zero."));
                }

                ValidateCollisionValues(issues, "Plane", i, "cMask", plane.CMask);
                ValidateCollisionValues(issues, "Plane", i, "cGroup", plane.CGroup);
            }
        }

        private void ValidateJoints(List<ValidationIssue> issues)
        {
            for (int i = 0; i < stadium.Joints.Count; i++)
            {
                JointData joint = stadium.Joints[i];

                if (joint.D0 < 0 || joint.D0 >= stadium.Discs.Count)
                {
                    issues.Add(new ValidationIssue("Error", "Joint", i, $"d0 index {joint.D0} does not exist."));
                }

                if (joint.D1 < 0 || joint.D1 >= stadium.Discs.Count)
                {
                    issues.Add(new ValidationIssue("Error", "Joint", i, $"d1 index {joint.D1} does not exist."));
                }

                if (joint.D0 == joint.D1)
                {
                    issues.Add(new ValidationIssue("Warning", "Joint", i, "d0 and d1 are the same disc."));
                }

                if (joint.Length != null && joint.Length <= 0)
                {
                    issues.Add(new ValidationIssue("Warning", "Joint", i, "Joint length should be greater than 0."));
                }

                if (!IsValidOptionalColor(joint.Color))
                {
                    issues.Add(new ValidationIssue("Warning", "Joint", i, $"Color '{joint.Color}' is not a valid hex color."));
                }
            }
        }

        private void ValidateCollisionValues(List<ValidationIssue> issues, string objectType, int objectIndex, string propertyName, List<string>? values)
        {
            if (values == null)
            {
                return;
            }

            foreach (string value in values)
            {
                if (!IsSupportedCollisionValue(value))
                {
                    issues.Add(new ValidationIssue("Warning", objectType, objectIndex, $"{propertyName} contains unusual value '{value}'."));
                }
            }
        }

        private bool IsSupportedCollisionValue(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            string normalized = value.Trim();
            if (normalized == "ball" || normalized == "red" || normalized == "blue" || normalized == "wall" || normalized == "all" || normalized == "kick" || normalized == "score" || normalized == "redKO" || normalized == "blueKO")
            {
                return true;
            }

            if (normalized.Length == 2 && normalized[0] == 'c' && normalized[1] >= '0' && normalized[1] <= '3')
            {
                return true;
            }

            return false;
        }

        private bool IsValidOptionalColor(string? color)
        {
            if (string.IsNullOrWhiteSpace(color))
            {
                return true;
            }

            string normalized = color.Trim();
            if (normalized.StartsWith("#"))
            {
                normalized = normalized.Substring(1);
            }

            if (normalized.Length == 3)
            {
                return IsHexString(normalized);
            }

            return normalized.Length == 6 && IsHexString(normalized);
        }

        private bool IsHexString(string value)
        {
            foreach (char c in value)
            {
                bool isHex =
                    (c >= '0' && c <= '9') ||
                    (c >= 'a' && c <= 'f') ||
                    (c >= 'A' && c <= 'F');

                if (!isHex)
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private void UpdateObjectCount()
        {
            ObjectCountText.Text =
                $"Vertexes: {stadium.Vertexes.Count} | Segments: {stadium.Segments.Count} | Goals: {stadium.Goals.Count} | Planes: {stadium.Planes.Count} | Discs: {stadium.Discs.Count} | Joints: {stadium.Joints.Count} | Spawns: {stadium.RedSpawnPoints.Count + stadium.BlueSpawnPoints.Count}";
        }

        private void UpdateObjectsList()
        {
            if (ObjectsListBox == null)
            {
                return;
            }

            isUpdatingObjectsList = true;

            try
            {
                ObjectsListBox.Items.Clear();

                AddLayerSection("Vertexes", "Vertex", BuildVertexLayerItems());
                AddLayerSection("Segments", "Segment", BuildSegmentLayerItems());
                AddLayerSection("Discs", "Disc", BuildDiscLayerItems());
                AddLayerSection("Goals", "Goal", BuildGoalLayerItems());
                AddLayerSection("Planes", "Plane", BuildPlaneLayerItems());
                AddLayerSection("Joints", "Joint", BuildJointLayerItems());
                AddLayerSection("Red Spawn Points", "RedSpawn", BuildRedSpawnLayerItems());
                AddLayerSection("Blue Spawn Points", "BlueSpawn", BuildBlueSpawnLayerItems());

                if (ObjectsListBox.Items.Count == 0)
                {
                    ListBoxItem emptyItem = new()
                    {
                        Content = "No objects match the current filter.",
                        Foreground = new SolidColorBrush(Color.FromRgb(157, 167, 181)),
                        IsEnabled = false,
                        Padding = new Thickness(8, 5, 8, 5)
                    };

                    ObjectsListBox.Items.Add(emptyItem);
                }
            }
            finally
            {
                isUpdatingObjectsList = false;
            }
        }

        private List<LayerListItem> BuildVertexLayerItems()
        {
            List<LayerListItem> items = new();
            for (int i = 0; i < stadium.Vertexes.Count; i++)
            {
                VertexData vertex = stadium.Vertexes[i];
                string text = $"  Vertex #{i}  X:{vertex.X - GetCanvasCenterX():0}  Y:{vertex.Y - GetCanvasCenterY():0}";
                items.Add(new LayerListItem("Vertex", i, text, text));
            }
            return items;
        }

        private List<LayerListItem> BuildSegmentLayerItems()
        {
            List<LayerListItem> items = new();
            for (int i = 0; i < stadium.Segments.Count; i++)
            {
                SegmentData segment = stadium.Segments[i];

                string colorText = string.IsNullOrWhiteSpace(segment.Color) ? "Default" : segment.Color;
                string bCoefText = segment.BCoef?.ToString("0.##", CultureInfo.InvariantCulture) ?? "Default";
                string cGroupText = segment.CGroup != null ? string.Join(",", segment.CGroup) : "Default";
                string cMaskText = segment.CMask != null ? string.Join(",", segment.CMask) : "Default";
                double curve = segment.Curve ?? 0;

                string text = $"  Segment #{i}  V0:{segment.V0}  V1:{segment.V1}  Color:{colorText}  Curve:{curve:0.##}  bCoef:{bCoefText}  cGroup:{cGroupText}  cMask:{cMaskText}";
                items.Add(new LayerListItem("Segment", i, text, text));
            }
            return items;
        }

        private List<LayerListItem> BuildDiscLayerItems()
        {
            List<LayerListItem> items = new();
            for (int i = 0; i < stadium.Discs.Count; i++)
            {
                DiscData disc = stadium.Discs[i];

                string colorText = string.IsNullOrWhiteSpace(disc.Color) ? "Default" : disc.Color;
                string bCoefText = disc.BCoef?.ToString("0.##", CultureInfo.InvariantCulture) ?? "Default";
                string invMassText = disc.InvMass?.ToString("0.##", CultureInfo.InvariantCulture) ?? "Default";
                string cGroupText = disc.CGroup != null ? string.Join(",", disc.CGroup) : "Default";
                string cMaskText = disc.CMask != null ? string.Join(",", disc.CMask) : "Default";
                string radiusText = disc.Radius?.ToString("0.##", CultureInfo.InvariantCulture) ?? "Trait/Default";

                string text = $"  Disc #{i}  X:{disc.X - GetCanvasCenterX():0}  Y:{disc.Y - GetCanvasCenterY():0}  R:{radiusText}  Color:{colorText}  bCoef:{bCoefText}  invMass:{invMassText}  cGroup:{cGroupText}  cMask:{cMaskText}";
                items.Add(new LayerListItem("Disc", i, text, text));
            }
            return items;
        }

        private List<LayerListItem> BuildGoalLayerItems()
        {
            List<LayerListItem> items = new();
            for (int i = 0; i < stadium.Goals.Count; i++)
            {
                GoalData goal = stadium.Goals[i];
                string text = $"  Goal #{i}  Team:{goal.Team}  P0:{goal.X0 - GetCanvasCenterX():0},{goal.Y0 - GetCanvasCenterY():0}  P1:{goal.X1 - GetCanvasCenterX():0},{goal.Y1 - GetCanvasCenterY():0}";
                items.Add(new LayerListItem("Goal", i, text, text));
            }
            return items;
        }

        private List<LayerListItem> BuildPlaneLayerItems()
        {
            List<LayerListItem> items = new();
            for (int i = 0; i < stadium.Planes.Count; i++)
            {
                PlaneData plane = stadium.Planes[i];

                string normalText = plane.Normal != null && plane.Normal.Count >= 2
                    ? $"{plane.Normal[0]:0.###},{plane.Normal[1]:0.###}"
                    : "invalid";

                string bCoefText = plane.BCoef?.ToString("0.##", CultureInfo.InvariantCulture) ?? "Default";
                string cGroupText = plane.CGroup != null ? string.Join(",", plane.CGroup) : "Default";
                string cMaskText = plane.CMask != null ? string.Join(",", plane.CMask) : "Default";

                string text = $"  Plane #{i}  Normal:{normalText}  Dist:{plane.Dist:0.##}  bCoef:{bCoefText}  cGroup:{cGroupText}  cMask:{cMaskText}";
                items.Add(new LayerListItem("Plane", i, text, text));
            }
            return items;
        }

        private List<LayerListItem> BuildJointLayerItems()
        {
            List<LayerListItem> items = new();
            for (int i = 0; i < stadium.Joints.Count; i++)
            {
                JointData joint = stadium.Joints[i];
                string text = $"  Joint #{i}  D0:{joint.D0}  D1:{joint.D1}  Strength:{joint.Strength}  Length:{joint.Length}";
                items.Add(new LayerListItem("Joint", i, text, text));
            }
            return items;
        }

        private List<LayerListItem> BuildRedSpawnLayerItems()
        {
            List<LayerListItem> items = new();
            for (int i = 0; i < stadium.RedSpawnPoints.Count; i++)
            {
                SpawnPointData spawn = stadium.RedSpawnPoints[i];
                string text = $"  Red Spawn #{i}  X:{spawn.X - GetCanvasCenterX():0}  Y:{spawn.Y - GetCanvasCenterY():0}";
                items.Add(new LayerListItem("RedSpawn", i, text, text));
            }
            return items;
        }

        private List<LayerListItem> BuildBlueSpawnLayerItems()
        {
            List<LayerListItem> items = new();
            for (int i = 0; i < stadium.BlueSpawnPoints.Count; i++)
            {
                SpawnPointData spawn = stadium.BlueSpawnPoints[i];
                string text = $"  Blue Spawn #{i}  X:{spawn.X - GetCanvasCenterX():0}  Y:{spawn.Y - GetCanvasCenterY():0}";
                items.Add(new LayerListItem("BlueSpawn", i, text, text));
            }
            return items;
        }

        private void AddLayerSection(string headerText, string sectionType, List<LayerListItem> sourceItems)
        {
            if (!LayerTypeMatches(sectionType))
            {
                return;
            }

            List<LayerListItem> filteredItems = new();
            foreach (LayerListItem item in sourceItems)
            {
                if (LayerSearchMatches(item))
                {
                    filteredItems.Add(item);
                }
            }

            if (filteredItems.Count == 0)
            {
                return;
            }

            AddLayerHeader(headerText);

            foreach (LayerListItem item in filteredItems)
            {
                AddLayerObjectItem(item);
            }
        }

        private bool LayerTypeMatches(string type)
        {
            return layersTypeFilter switch
            {
                "All" => true,
                "Vertexes" => type == "Vertex",
                "Segments" => type == "Segment",
                "Discs" => type == "Disc",
                "Goals" => type == "Goal",
                "Planes" => type == "Plane",
                "Joints" => type == "Joint",
                "Spawn Points" => type == "RedSpawn" || type == "BlueSpawn",
                _ => true
            };
        }

        private bool LayerSearchMatches(LayerListItem item)
        {
            if (string.IsNullOrWhiteSpace(layersSearchText))
            {
                return true;
            }

            string query = layersSearchText.Trim();
            return item.SearchText.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0
                || item.Type.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void AddLayerHeader(string headerText)
        {
            LayerListItem item = new("Header", -1, headerText, headerText, true);

            ListBoxItem listBoxItem = new()
            {
                Content = headerText,
                Tag = item,
                IsEnabled = false,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(47, 183, 232)),
                Background = new SolidColorBrush(Color.FromRgb(22, 26, 32)),
                Padding = new Thickness(7, 5, 7, 5),
                Margin = new Thickness(0, 6, 0, 2)
            };

            ObjectsListBox.Items.Add(listBoxItem);
        }

        private void AddLayerObjectItem(LayerListItem item)
        {
            bool isSelected = IsObjectSelectedInEditor(item.Type, item.Index);
            bool isHidden = IsObjectHidden(item.Type, item.Index);
            bool isLocked = IsObjectLocked(item.Type, item.Index);

            Brush textBrush = isSelected
                ? Brushes.White
                : isHidden
                    ? new SolidColorBrush(Color.FromRgb(130, 138, 150))
                    : new SolidColorBrush(Color.FromRgb(232, 237, 244));

            Brush mutedBrush = isHidden
                ? new SolidColorBrush(Color.FromRgb(93, 101, 113))
                : new SolidColorBrush(Color.FromRgb(145, 155, 170));

            Grid row = new()
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
                },
                VerticalAlignment = VerticalAlignment.Center,
                Opacity = isHidden ? 0.62 : 1.0
            };

            Button visibilityButton = CreateLayerStateButton(
                isHidden ? "EyeOffOutline" : "EyeOutline",
                isHidden ? "Show object" : "Hide object",
                isHidden ? new SolidColorBrush(Color.FromRgb(245, 158, 11)) : new SolidColorBrush(Color.FromRgb(142, 154, 171)));

            visibilityButton.Click += (_, e) =>
            {
                ToggleLayerHidden(item, !IsObjectHidden(item.Type, item.Index));
                e.Handled = true;
            };
            Grid.SetColumn(visibilityButton, 0);
            row.Children.Add(visibilityButton);

            Button lockButton = CreateLayerStateButton(
                isLocked ? "Lock" : "LockOpenVariant",
                isLocked ? "Unlock object" : "Lock object",
                isLocked ? new SolidColorBrush(Color.FromRgb(248, 113, 113)) : new SolidColorBrush(Color.FromRgb(142, 154, 171)));

            lockButton.Click += (_, e) =>
            {
                ToggleLayerLock(item, !IsObjectLocked(item.Type, item.Index));
                e.Handled = true;
            };
            Grid.SetColumn(lockButton, 1);
            row.Children.Add(lockButton);

            Border typeIconBox = new()
            {
                Width = 22,
                Height = 22,
                CornerRadius = new CornerRadius(6),
                Background = isSelected
                    ? new SolidColorBrush(Color.FromRgb(14, 99, 156))
                    : new SolidColorBrush(Color.FromRgb(31, 38, 48)),
                BorderBrush = isSelected
                    ? new SolidColorBrush(Color.FromRgb(47, 183, 232))
                    : new SolidColorBrush(Color.FromRgb(49, 58, 71)),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(4, 0, 8, 0),
                Child = CreatePackIcon(GetLayerTypeIconName(item.Type), 13, GetLayerTypeIconBrush(item.Type, isHidden))
            };
            Grid.SetColumn(typeIconBox, 2);
            row.Children.Add(typeIconBox);

            StackPanel textPanel = new()
            {
                Orientation = Orientation.Vertical,
                VerticalAlignment = VerticalAlignment.Center
            };

            TextBlock titleText = new()
            {
                Text = GetLayerObjectTitle(item),
                Foreground = textBrush,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            textPanel.Children.Add(titleText);

            string detailText = GetLayerObjectDetail(item);
            if (!string.IsNullOrWhiteSpace(detailText))
            {
                textPanel.Children.Add(new TextBlock
                {
                    Text = detailText,
                    Foreground = mutedBrush,
                    FontSize = 10.5,
                    Margin = new Thickness(0, 1, 0, 0),
                    TextTrimming = TextTrimming.CharacterEllipsis
                });
            }

            Grid.SetColumn(textPanel, 3);
            row.Children.Add(textPanel);

            ListBoxItem listBoxItem = new()
            {
                Content = row,
                Tag = item,
                Padding = new Thickness(5, 5, 7, 5),
                Margin = new Thickness(0, 1, 0, 1),
                Cursor = Cursors.Hand,
                Foreground = textBrush,
                Background = isSelected
                    ? new SolidColorBrush(Color.FromRgb(23, 53, 70))
                    : Brushes.Transparent,
                BorderBrush = isSelected
                    ? new SolidColorBrush(Color.FromRgb(47, 183, 232))
                    : Brushes.Transparent,
                BorderThickness = new Thickness(1)
            };

            listBoxItem.ContextMenu = CreateLayerObjectContextMenu(item);

            if (isSelected)
            {
                listBoxItem.IsSelected = true;
            }

            ObjectsListBox.Items.Add(listBoxItem);
        }

        private Button CreateLayerStateButton(string iconName, string tooltip, Brush iconBrush)
        {
            Button button = new()
            {
                Width = 24,
                Height = 24,
                Padding = new Thickness(0),
                Margin = new Thickness(0, 0, 3, 0),
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                ToolTip = tooltip,
                Content = CreatePackIcon(iconName, 15, iconBrush)
            };

            return button;
        }

        private UIElement CreatePackIcon(string iconName, double size, Brush brush)
        {
            PackIcon icon = new()
            {
                Width = size,
                Height = size,
                Foreground = brush,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            if (Enum.TryParse(iconName, out PackIconKind parsedKind))
            {
                icon.Kind = parsedKind;
                return icon;
            }

            return new TextBlock
            {
                Text = "•",
                FontSize = size,
                Foreground = brush,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        private string GetLayerTypeIconName(string type)
        {
            return type switch
            {
                "Vertex" => "VectorPoint",
                "Segment" => "VectorLine",
                "Disc" => "CircleOutline",
                "Goal" => "Soccer",
                "Plane" => "AxisArrow",
                "Joint" => "LinkVariant",
                "RedSpawn" => "AccountArrowRight",
                "BlueSpawn" => "AccountArrowLeft",
                _ => "CircleOutline"
            };
        }

        private Brush GetLayerTypeIconBrush(string type, bool isHidden)
        {
            if (isHidden)
            {
                return new SolidColorBrush(Color.FromRgb(118, 128, 142));
            }

            return type switch
            {
                "Vertex" => new SolidColorBrush(Color.FromRgb(236, 72, 153)),
                "Segment" => new SolidColorBrush(Color.FromRgb(56, 189, 248)),
                "Disc" => new SolidColorBrush(Color.FromRgb(250, 204, 21)),
                "Goal" => new SolidColorBrush(Color.FromRgb(74, 222, 128)),
                "Plane" => new SolidColorBrush(Color.FromRgb(167, 139, 250)),
                "Joint" => new SolidColorBrush(Color.FromRgb(251, 146, 60)),
                "RedSpawn" => new SolidColorBrush(Color.FromRgb(248, 113, 113)),
                "BlueSpawn" => new SolidColorBrush(Color.FromRgb(96, 165, 250)),
                _ => new SolidColorBrush(Color.FromRgb(232, 237, 244))
            };
        }

        private string GetLayerObjectTitle(LayerListItem item)
        {
            return item.Type switch
            {
                "RedSpawn" => $"Red Spawn #{item.Index}",
                "BlueSpawn" => $"Blue Spawn #{item.Index}",
                _ => $"{item.Type} #{item.Index}"
            };
        }

        private string GetLayerObjectDetail(LayerListItem item)
        {
            string displayName = item.DisplayName.Trim();

            if (displayName.StartsWith(GetLayerObjectTitle(item), StringComparison.OrdinalIgnoreCase))
            {
                return displayName.Substring(GetLayerObjectTitle(item).Length).Trim();
            }

            int doubleSpaceIndex = displayName.IndexOf("  ", StringComparison.Ordinal);
            if (doubleSpaceIndex >= 0 && doubleSpaceIndex + 2 < displayName.Length)
            {
                return displayName.Substring(doubleSpaceIndex + 2).Trim();
            }

            return "";
        }


        private ContextMenu CreateLayerObjectContextMenu(LayerListItem item)
        {
            ContextMenu menu = new()
            {
                Background = new SolidColorBrush(Color.FromRgb(32, 34, 38)),
                Foreground = new SolidColorBrush(Color.FromRgb(232, 237, 244)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(59, 64, 72)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(4),
                Style = TryFindResource("DarkContextMenu") as Style
            };

            menu.Resources.Add(SystemColors.ControlBrushKey, new SolidColorBrush(Color.FromRgb(32, 34, 38)));
            menu.Resources.Add(SystemColors.MenuBrushKey, new SolidColorBrush(Color.FromRgb(32, 34, 38)));
            menu.Resources.Add(SystemColors.MenuTextBrushKey, new SolidColorBrush(Color.FromRgb(232, 237, 244)));
            menu.Resources.Add(SystemColors.HighlightBrushKey, new SolidColorBrush(Color.FromRgb(36, 54, 70)));
            menu.Resources.Add(SystemColors.HighlightTextBrushKey, Brushes.White);

            menu.Items.Add(CreateLayerMenuItem("Select", "CursorDefaultClick", () => SelectLayerObject(item, false)));
            menu.Items.Add(CreateLayerMenuItem("Focus in Viewport", "CrosshairsGps", () => SelectLayerObject(item, true)));
            menu.Items.Add(CreateLayerSeparator());
            menu.Items.Add(CreateLayerMenuItem("Duplicate", "ContentDuplicate", () => { SelectLayerObject(item, false); DuplicateSelectedObjects(); }));
            menu.Items.Add(CreateLayerMenuItem("Delete", "TrashCanOutline", () => { SelectLayerObject(item, false); DeleteSelectedItems(); }));
            menu.Items.Add(CreateLayerSeparator());

            if (IsObjectLocked(item.Type, item.Index))
            {
                menu.Items.Add(CreateLayerMenuItem("Unlock", "LockOpenVariant", () => ToggleLayerLock(item, false)));
            }
            else
            {
                menu.Items.Add(CreateLayerMenuItem("Lock", "Lock", () => ToggleLayerLock(item, true)));
            }

            if (IsObjectHidden(item.Type, item.Index))
            {
                menu.Items.Add(CreateLayerMenuItem("Show", "EyeOutline", () => ToggleLayerHidden(item, false)));
            }
            else
            {
                menu.Items.Add(CreateLayerMenuItem("Hide", "EyeOffOutline", () => ToggleLayerHidden(item, true)));
            }

            return menu;
        }

        private Separator CreateLayerSeparator()
        {
            Separator separator = new()
            {
                Style = TryFindResource("DarkContextSeparator") as Style
            };

            return separator;
        }

        private MenuItem CreateLayerMenuItem(string header, string iconName, Action action)
        {
            MenuItem menuItem = new()
            {
                Header = header,
                Foreground = new SolidColorBrush(Color.FromRgb(232, 237, 244)),
                Background = Brushes.Transparent,
                Padding = new Thickness(10, 5, 10, 5),
                Cursor = Cursors.Hand,
                Style = TryFindResource("DarkContextMenuItem") as Style,
                Icon = CreatePackIcon(iconName, 15, new SolidColorBrush(Color.FromRgb(166, 176, 190)))
            };

            menuItem.Click += (_, _) => action();
            return menuItem;
        }

        private void ToggleLayerLock(LayerListItem item, bool locked)
        {
            string key = GetObjectKey(item.Type, item.Index);
            if (locked) lockedObjectKeys.Add(key);
            else lockedObjectKeys.Remove(key);

            RenderStadium();
            UpdateObjectsList();
            UpdateStatus($"{item.DisplayName.Trim()} {(locked ? "locked" : "unlocked")}.");
        }

        private void ToggleLayerHidden(LayerListItem item, bool hidden)
        {
            string key = GetObjectKey(item.Type, item.Index);
            if (hidden) hiddenObjectKeys.Add(key);
            else hiddenObjectKeys.Remove(key);

            RenderStadium();
            UpdateObjectsList();
            UpdateStatus($"{item.DisplayName.Trim()} {(hidden ? "hidden" : "visible")}.");
        }

        private bool IsObjectSelectedInEditor(string type, int index)
        {
            return type switch
            {
                "Vertex" => selectedVertexIndex == index || IsObjectSelected("Vertex", index),
                "Segment" => selectedSegmentIndex == index || IsObjectSelected("Segment", index),
                "Disc" => selectedDiscIndex == index || IsObjectSelected("Disc", index),
                "Goal" => selectedGoalIndex == index || IsObjectSelected("Goal", index),
                "Plane" => selectedPlaneIndex == index || IsObjectSelected("Plane", index),
                "Joint" => selectedJointIndex == index || IsObjectSelected("Joint", index),
                "RedSpawn" => selectedRedSpawnIndex == index || IsObjectSelected("RedSpawn", index),
                "BlueSpawn" => selectedBlueSpawnIndex == index || IsObjectSelected("BlueSpawn", index),
                _ => false
            };
        }

        private void SelectMirroredPair(string type, int originalIndex, int mirroredIndex)
        {
            ClearSingleSelectionIndexes();
            selectedItems.Clear();
            selectedItems.Add(new SelectedItem(type, originalIndex));
            selectedItems.Add(new SelectedItem(type, mirroredIndex));
            UpdateMultiSelectionSummary();
        }

        private void MirrorSelectedObjects(bool horizontally)
        {
            List<SelectedItem> targets = GetCurrentSelectionItems();

            if (targets.Count == 0)
            {
                UpdateStatus("No object selected to mirror.");
                return;
            }

            PushUndoState(horizontally ? "Mirror Selected Horizontally" : "Mirror Selected Vertically");

            HashSet<int> mirroredVertexes = new();
            HashSet<int> mirroredDiscs = new();
            HashSet<int> mirroredGoals = new();
            HashSet<int> mirroredPlanes = new();
            HashSet<int> mirroredRedSpawns = new();
            HashSet<int> mirroredBlueSpawns = new();

            foreach (SelectedItem item in targets)
            {
                if (IsObjectLocked(item.Type, item.Index))
                {
                    continue;
                }

                switch (item.Type)
                {
                    case "Vertex":
                        MirrorVertex(item.Index, horizontally, mirroredVertexes);
                        break;

                    case "Segment":
                        if (item.Index >= 0 && item.Index < stadium.Segments.Count)
                        {
                            SegmentData segment = stadium.Segments[item.Index];
                            MirrorVertex(segment.V0, horizontally, mirroredVertexes);
                            MirrorVertex(segment.V1, horizontally, mirroredVertexes);
                        }
                        break;

                    case "Disc":
                        MirrorDisc(item.Index, horizontally, mirroredDiscs);
                        break;

                    case "Goal":
                        MirrorGoal(item.Index, horizontally, mirroredGoals);
                        break;

                    case "Plane":
                        MirrorPlane(item.Index, horizontally, mirroredPlanes);
                        break;

                    case "Joint":
                        if (item.Index >= 0 && item.Index < stadium.Joints.Count)
                        {
                            JointData joint = stadium.Joints[item.Index];
                            MirrorDisc(joint.D0, horizontally, mirroredDiscs);
                            MirrorDisc(joint.D1, horizontally, mirroredDiscs);
                        }
                        break;

                    case "RedSpawn":
                        MirrorRedSpawn(item.Index, horizontally, mirroredRedSpawns);
                        break;

                    case "BlueSpawn":
                        MirrorBlueSpawn(item.Index, horizontally, mirroredBlueSpawns);
                        break;
                }
            }

            RenderStadium();
            UpdateObjectsList();
            UpdateJsonPreview();

            if (HasSingleSelection())
            {
                RefreshInspectorForCurrentSingleSelection();
            }
            else
            {
                UpdateMultiSelectionSummary();
            }

            UpdateStatus($"{(horizontally ? "Horizontally" : "Vertically")} mirrored {targets.Count} selected object(s).");
        }

        private List<SelectedItem> GetCurrentSelectionItems()
        {
            if (selectedItems.Count > 0)
            {
                return selectedItems
                    .Select(item => new SelectedItem(item.Type, item.Index))
                    .ToList();
            }

            if (selectedVertexIndex != null) return new List<SelectedItem> { new("Vertex", selectedVertexIndex.Value) };
            if (selectedSegmentIndex != null) return new List<SelectedItem> { new("Segment", selectedSegmentIndex.Value) };
            if (selectedDiscIndex != null) return new List<SelectedItem> { new("Disc", selectedDiscIndex.Value) };
            if (selectedGoalIndex != null) return new List<SelectedItem> { new("Goal", selectedGoalIndex.Value) };
            if (selectedPlaneIndex != null) return new List<SelectedItem> { new("Plane", selectedPlaneIndex.Value) };
            if (selectedJointIndex != null) return new List<SelectedItem> { new("Joint", selectedJointIndex.Value) };
            if (selectedRedSpawnIndex != null) return new List<SelectedItem> { new("RedSpawn", selectedRedSpawnIndex.Value) };
            if (selectedBlueSpawnIndex != null) return new List<SelectedItem> { new("BlueSpawn", selectedBlueSpawnIndex.Value) };

            return new List<SelectedItem>();
        }

        private void MirrorVertex(int index, bool horizontally, HashSet<int> mirrored)
        {
            if (index < 0 || index >= stadium.Vertexes.Count || !mirrored.Add(index) || IsObjectLocked("Vertex", index))
            {
                return;
            }

            VertexData vertex = stadium.Vertexes[index];
            if (horizontally)
            {
                vertex.X = MirrorCanvasX(vertex.X);
            }
            else
            {
                vertex.Y = MirrorCanvasY(vertex.Y);
            }
        }

        private void MirrorDisc(int index, bool horizontally, HashSet<int> mirrored)
        {
            if (index < 0 || index >= stadium.Discs.Count || !mirrored.Add(index) || IsObjectLocked("Disc", index))
            {
                return;
            }

            DiscData disc = stadium.Discs[index];
            if (horizontally)
            {
                disc.X = MirrorCanvasX(disc.X);
            }
            else
            {
                disc.Y = MirrorCanvasY(disc.Y);
            }
        }

        private void MirrorGoal(int index, bool horizontally, HashSet<int> mirrored)
        {
            if (index < 0 || index >= stadium.Goals.Count || !mirrored.Add(index) || IsObjectLocked("Goal", index))
            {
                return;
            }

            GoalData goal = stadium.Goals[index];

            if (horizontally)
            {
                goal.X0 = MirrorCanvasX(goal.X0);
                goal.X1 = MirrorCanvasX(goal.X1);
            }
            else
            {
                goal.Y0 = MirrorCanvasY(goal.Y0);
                goal.Y1 = MirrorCanvasY(goal.Y1);
            }
        }

        private void MirrorPlane(int index, bool horizontally, HashSet<int> mirrored)
        {
            if (index < 0 || index >= stadium.Planes.Count || !mirrored.Add(index) || IsObjectLocked("Plane", index))
            {
                return;
            }

            PlaneData plane = stadium.Planes[index];

            if (plane.Normal == null || plane.Normal.Count < 2)
            {
                return;
            }

            if (horizontally)
            {
                plane.Normal[0] = -plane.Normal[0];
            }
            else
            {
                plane.Normal[1] = -plane.Normal[1];
            }

            plane.Dist = -plane.Dist;
        }

        private void MirrorRedSpawn(int index, bool horizontally, HashSet<int> mirrored)
        {
            if (index < 0 || index >= stadium.RedSpawnPoints.Count || !mirrored.Add(index) || IsObjectLocked("RedSpawn", index))
            {
                return;
            }

            SpawnPointData spawn = stadium.RedSpawnPoints[index];
            if (horizontally)
            {
                spawn.X = MirrorCanvasX(spawn.X);
            }
            else
            {
                spawn.Y = MirrorCanvasY(spawn.Y);
            }
        }

        private void MirrorBlueSpawn(int index, bool horizontally, HashSet<int> mirrored)
        {
            if (index < 0 || index >= stadium.BlueSpawnPoints.Count || !mirrored.Add(index) || IsObjectLocked("BlueSpawn", index))
            {
                return;
            }

            SpawnPointData spawn = stadium.BlueSpawnPoints[index];
            if (horizontally)
            {
                spawn.X = MirrorCanvasX(spawn.X);
            }
            else
            {
                spawn.Y = MirrorCanvasY(spawn.Y);
            }
        }

        private double MirrorCanvasX(double x)
        {
            return GetCanvasCenterX() - (x - GetCanvasCenterX());
        }

        private double MirrorCanvasY(double y)
        {
            return GetCanvasCenterY() - (y - GetCanvasCenterY());
        }

        private void UpdateJsonPreview()
        {
            isUpdatingJsonPreviewFromCode = true;

            try
            {
                JsonPreviewTextBox.Text = BuildStadiumJson();
                jsonPreviewUserEdited = false;
            }
            finally
            {
                isUpdatingJsonPreviewFromCode = false;
            }
        }

        private void UpdateStatus(string message)
        {
            StatusText.Text = message;
        }
    }

    public class EditorPreferences
    {
        public bool? AutoSaveEnabled { get; set; }
        public string? CustomAutoSaveFolderPath { get; set; }

        public bool? ValidationWarningBeforeSaveEnabled { get; set; }
        public bool? ValidationPanelAutoRefreshEnabled { get; set; }

        public bool? ShowViewportGrid { get; set; }
        public bool? ShowViewportVertexes { get; set; }
        public bool? ShowViewportSegments { get; set; }
        public bool? ShowViewportDiscs { get; set; }
        public bool? ShowViewportPlanes { get; set; }
        public bool? ShowViewportGrassStripes { get; set; }
        public bool? ShowViewportInvisibleObjects { get; set; }
        public bool? AutoMirrorPlacement { get; set; }

        public bool? SnapToGrid { get; set; }
        public double? SnapGridSize { get; set; }
        public string? ViewportVertexSize { get; set; }

        public double? LeftPanelWidth { get; set; }
        public double? RightPanelWidth { get; set; }
        public double? BottomPanelHeight { get; set; }
        public double? WindowWidth { get; set; }
        public double? WindowHeight { get; set; }

        public Dictionary<string, string>? PanelDockStates { get; set; }
    }

    public class StadiumData
    {
        public string Name { get; set; } = "New Stadium";
        public int Width { get; set; } = 420;
        public int Height { get; set; } = 200;
        public int SpawnDistance { get; set; } = 170;
        public BgData Bg { get; set; } = new();
        public List<VertexData> Vertexes { get; set; } = new();
        public List<SegmentData> Segments { get; set; } = new();
        public List<GoalData> Goals { get; set; } = new();
        public List<DiscData> Discs { get; set; } = new();
        public List<PlaneData> Planes { get; set; } = new();
        public List<JointData> Joints { get; set; } = new();
        public List<SpawnPointData> RedSpawnPoints { get; set; } = new();
        public List<SpawnPointData> BlueSpawnPoints { get; set; } = new();
        public Dictionary<string, TraitData> Traits { get; set; } = new();
        public Dictionary<string, JsonElement>? ExtensionData { get; set; }
    }

    public class ExportStadiumData
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "New Stadium";

        [JsonPropertyName("width")]
        public int Width { get; set; } = 420;

        [JsonPropertyName("height")]
        public int Height { get; set; } = 200;

        [JsonPropertyName("spawnDistance")]
        public int SpawnDistance { get; set; } = 170;

        [JsonPropertyName("bg")]
        public BgData Bg { get; set; } = new();

        [JsonPropertyName("vertexes")]
        public List<ExportVertexData> Vertexes { get; set; } = new();

        [JsonPropertyName("segments")]
        public List<SegmentData> Segments { get; set; } = new();

        [JsonPropertyName("goals")]
        public List<ExportGoalData> Goals { get; set; } = new();

        [JsonPropertyName("discs")]
        public List<ExportDiscData> Discs { get; set; } = new();

        [JsonPropertyName("planes")]
        public List<PlaneData> Planes { get; set; } = new();

        [JsonPropertyName("joints")]
        public List<JointData> Joints { get; set; } = new();

        [JsonPropertyName("redSpawnPoints")]
        public List<List<double>> RedSpawnPoints { get; set; } = new();

        [JsonPropertyName("blueSpawnPoints")]
        public List<List<double>> BlueSpawnPoints { get; set; } = new();

        [JsonPropertyName("traits")]
        public Dictionary<string, TraitData> Traits { get; set; } = new();

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? ExtensionData { get; set; }
    }

    public class BgData
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("width")]
        public double? Width { get; set; }

        [JsonPropertyName("height")]
        public double? Height { get; set; }

        [JsonPropertyName("kickOffRadius")]
        public double? KickOffRadius { get; set; }

        [JsonPropertyName("cornerRadius")]
        public double? CornerRadius { get; set; }

        [JsonPropertyName("color")]
        public string? Color { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? ExtensionData { get; set; }
    }

    public class VertexData
    {
        public double X { get; set; }
        public double Y { get; set; }

        public Dictionary<string, JsonElement>? ExtensionData { get; set; }
    }

    public class ExportVertexData
    {
        [JsonPropertyName("x")]
        public double X { get; set; }

        [JsonPropertyName("y")]
        public double Y { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? ExtensionData { get; set; }
    }

    public class SegmentData
    {
        [JsonPropertyName("v0")]
        public int V0 { get; set; }

        [JsonPropertyName("v1")]
        public int V1 { get; set; }

        [JsonPropertyName("color")]
        public string? Color { get; set; }

        [JsonPropertyName("curve")]
        public double? Curve { get; set; }

        [JsonPropertyName("bCoef")]
        public double? BCoef { get; set; }

        [JsonPropertyName("cGroup")]
        public List<string>? CGroup { get; set; }

        [JsonPropertyName("cMask")]
        public List<string>? CMask { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? ExtensionData { get; set; }
    }

    public class GoalData
    {
        public double X0 { get; set; }
        public double Y0 { get; set; }
        public double X1 { get; set; }
        public double Y1 { get; set; }
        public string Team { get; set; } = "red";
        public Dictionary<string, JsonElement>? ExtensionData { get; set; }
    }

    public class ExportGoalData
    {
        [JsonPropertyName("p0")]
        public List<double> P0 { get; set; } = new();

        [JsonPropertyName("p1")]
        public List<double> P1 { get; set; } = new();

        [JsonPropertyName("team")]
        public string Team { get; set; } = "red";

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? ExtensionData { get; set; }
    }

    public class DiscData
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double? Radius { get; set; }
        public string? Color { get; set; }
        public double? BCoef { get; set; }
        public double? InvMass { get; set; }
        public List<string>? CGroup { get; set; }
        public List<string>? CMask { get; set; }
        public Dictionary<string, JsonElement>? ExtensionData { get; set; }
    }

    public class ExportDiscData
    {
        [JsonPropertyName("pos")]
        public List<double> Pos { get; set; } = new();

        [JsonPropertyName("radius")]
        public double? Radius { get; set; }

        [JsonPropertyName("color")]
        public string? Color { get; set; }

        [JsonPropertyName("bCoef")]
        public double? BCoef { get; set; }

        [JsonPropertyName("invMass")]
        public double? InvMass { get; set; }

        [JsonPropertyName("cGroup")]
        public List<string>? CGroup { get; set; }

        [JsonPropertyName("cMask")]
        public List<string>? CMask { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? ExtensionData { get; set; }
    }

    public class PlaneData
    {
        [JsonPropertyName("normal")]
        public List<double> Normal { get; set; } = new();

        [JsonPropertyName("dist")]
        public double Dist { get; set; }

        [JsonPropertyName("bCoef")]
        public double? BCoef { get; set; }

        [JsonPropertyName("cGroup")]
        public List<string>? CGroup { get; set; }

        [JsonPropertyName("cMask")]
        public List<string>? CMask { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? ExtensionData { get; set; }
    }

    public class JointData
    {
        [JsonPropertyName("d0")]
        public int D0 { get; set; }

        [JsonPropertyName("d1")]
        public int D1 { get; set; }

        [JsonPropertyName("strength")]
        public string? Strength { get; set; }

        [JsonPropertyName("length")]
        public double? Length { get; set; }

        [JsonPropertyName("color")]
        public string? Color { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? ExtensionData { get; set; }
    }

    public class SpawnPointData
    {
        public double X { get; set; }
        public double Y { get; set; }
    }

    public class TraitData
    {
        [JsonPropertyName("vis")]
        public bool? Vis { get; set; }

        [JsonPropertyName("bCoef")]
        public double? BCoef { get; set; }

        [JsonPropertyName("cMask")]
        public List<string>? CMask { get; set; }

        [JsonPropertyName("radius")]
        public double? Radius { get; set; }

        [JsonPropertyName("invMass")]
        public double? InvMass { get; set; }

        [JsonPropertyName("cGroup")]
        public List<string>? CGroup { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? ExtensionData { get; set; }
    }

    public class UpdateManifest
    {
        [JsonPropertyName("version")]
        public string Version { get; set; } = "";

        [JsonPropertyName("downloadUrl")]
        public string? DownloadUrl { get; set; }

        [JsonPropertyName("releasePageUrl")]
        public string? ReleasePageUrl { get; set; }

        [JsonPropertyName("fileName")]
        public string? FileName { get; set; }

        [JsonPropertyName("releaseNotes")]
        public List<string>? ReleaseNotes { get; set; }
    }

    public class UpdateCheckResult
    {
        public string LatestVersion { get; set; } = "";
        public string DownloadUrl { get; set; } = "";
        public string ReleasePageUrl { get; set; } = "";
        public string FileName { get; set; } = "";
        public string ReleaseNotesText { get; set; } = "";
        public bool IsNewerVersionAvailable { get; set; }
    }

}