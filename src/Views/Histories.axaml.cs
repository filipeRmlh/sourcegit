using System;
using System.Collections.Generic;
using System.Text;

using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace SourceGit.Views
{
    public class LayoutableGrid : Grid
    {
        public static readonly StyledProperty<bool> UseHorizontalProperty =
            AvaloniaProperty.Register<LayoutableGrid, bool>(nameof(UseHorizontal));

        public bool UseHorizontal
        {
            get => GetValue(UseHorizontalProperty);
            set => SetValue(UseHorizontalProperty, value);
        }

        protected override Type StyleKeyOverride => typeof(Grid);

        static LayoutableGrid()
        {
            UseHorizontalProperty.Changed.AddClassHandler<LayoutableGrid>((o, _) => o.RefreshLayout());
        }

        public override void ApplyTemplate()
        {
            base.ApplyTemplate();
            RefreshLayout();
        }

        private void RefreshLayout()
        {
            if (UseHorizontal)
            {
                var rowSpan = RowDefinitions.Count;
                for (int i = 0; i < Children.Count; i++)
                {
                    var child = Children[i];
                    child.SetValue(RowProperty, 0);
                    child.SetValue(RowSpanProperty, rowSpan);
                    child.SetValue(ColumnProperty, i);
                    child.SetValue(ColumnSpanProperty, 1);

                    if (child is GridSplitter splitter)
                        splitter.BorderThickness = new Thickness(1, 0, 0, 0);
                }
            }
            else
            {
                var colSpan = ColumnDefinitions.Count;
                for (int i = 0; i < Children.Count; i++)
                {
                    var child = Children[i];
                    child.SetValue(RowProperty, i);
                    child.SetValue(RowSpanProperty, 1);
                    child.SetValue(ColumnProperty, 0);
                    child.SetValue(ColumnSpanProperty, colSpan);

                    if (child is GridSplitter splitter)
                        splitter.BorderThickness = new Thickness(0, 1, 0, 0);
                }
            }
        }
    }

    public class CommitStatusIndicator : Control
    {
        public static readonly StyledProperty<Models.Branch> CurrentBranchProperty =
            AvaloniaProperty.Register<CommitStatusIndicator, Models.Branch>(nameof(CurrentBranch));

        public Models.Branch CurrentBranch
        {
            get => GetValue(CurrentBranchProperty);
            set => SetValue(CurrentBranchProperty, value);
        }

        public static readonly StyledProperty<IBrush> AheadBrushProperty =
            AvaloniaProperty.Register<CommitStatusIndicator, IBrush>(nameof(AheadBrush));

        public IBrush AheadBrush
        {
            get => GetValue(AheadBrushProperty);
            set => SetValue(AheadBrushProperty, value);
        }

        public static readonly StyledProperty<IBrush> BehindBrushProperty =
            AvaloniaProperty.Register<CommitStatusIndicator, IBrush>(nameof(BehindBrush));

        public IBrush BehindBrush
        {
            get => GetValue(BehindBrushProperty);
            set => SetValue(BehindBrushProperty, value);
        }

        enum Status
        {
            Normal,
            Ahead,
            Behind,
        }

        public override void Render(DrawingContext context)
        {
            if (_status == Status.Normal)
                return;

            context.DrawEllipse(_status == Status.Ahead ? AheadBrush : BehindBrush, null, new Rect(0, 0, 5, 5));
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            if (DataContext is Models.Commit commit && CurrentBranch is not null)
            {
                var sha = commit.SHA;
                var track = CurrentBranch.TrackStatus;

                if (track.Ahead.Contains(sha))
                    _status = Status.Ahead;
                else if (track.Behind.Contains(sha))
                    _status = Status.Behind;
                else
                    _status = Status.Normal;
            }
            else
            {
                _status = Status.Normal;
            }

            return _status == Status.Normal ? new Size(0, 0) : new Size(9, 5);
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);
            InvalidateMeasure();
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            if (change.Property == CurrentBranchProperty)
                InvalidateMeasure();
        }

        private Status _status = Status.Normal;
    }

    public class CommitSubjectPresenter : TextBlock
    {
        public static readonly StyledProperty<string> SubjectProperty =
            AvaloniaProperty.Register<CommitSubjectPresenter, string>(nameof(Subject));

        public string Subject
        {
            get => GetValue(SubjectProperty);
            set => SetValue(SubjectProperty, value);
        }

        public static readonly StyledProperty<AvaloniaList<Models.IssueTrackerRule>> IssueTrackerRulesProperty =
            AvaloniaProperty.Register<CommitSubjectPresenter, AvaloniaList<Models.IssueTrackerRule>>(nameof(IssueTrackerRules));

        public AvaloniaList<Models.IssueTrackerRule> IssueTrackerRules
        {
            get => GetValue(IssueTrackerRulesProperty);
            set => SetValue(IssueTrackerRulesProperty, value);
        }

        protected override Type StyleKeyOverride => typeof(TextBlock);

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == SubjectProperty || change.Property == IssueTrackerRulesProperty)
            {
                Inlines!.Clear();
                _matches = null;
                ClearHoveredIssueLink();

                var subject = Subject;
                if (string.IsNullOrEmpty(subject))
                    return;

                var rules = IssueTrackerRules;
                if (rules == null || rules.Count == 0)
                {
                    Inlines.Add(new Run(subject));
                    return;
                }

                var matches = new List<Models.Hyperlink>();
                foreach (var rule in rules)
                    rule.Matches(matches, subject);

                if (matches.Count == 0)
                {
                    Inlines.Add(new Run(subject));
                    return;
                }

                matches.Sort((l, r) => l.Start - r.Start);
                _matches = matches;

                int pos = 0;
                foreach (var match in matches)
                {
                    if (match.Start > pos)
                        Inlines.Add(new Run(subject.Substring(pos, match.Start - pos)));

                    var link = new Run(subject.Substring(match.Start, match.Length));
                    link.Classes.Add("issue_link");
                    Inlines.Add(link);

                    pos = match.Start + match.Length;
                }

                if (pos < subject.Length)
                    Inlines.Add(new Run(subject.Substring(pos)));

                InvalidateTextLayout();
            }
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);

            if (_matches != null)
            {
                var point = e.GetPosition(this) - new Point(Padding.Left, Padding.Top);
                var x = Math.Min(Math.Max(point.X, 0), Math.Max(TextLayout.WidthIncludingTrailingWhitespace, 0));
                var y = Math.Min(Math.Max(point.Y, 0), Math.Max(TextLayout.Height, 0));
                point = new Point(x, y);

                var textPosition = TextLayout.HitTestPoint(point).TextPosition;
                foreach (var match in _matches)
                {
                    if (!match.Intersect(textPosition, 1))
                        continue;

                    if (match == _lastHover)
                        return;

                    _lastHover = match;
                    SetCurrentValue(CursorProperty, Cursor.Parse("Hand"));
                    ToolTip.SetTip(this, match.Link);
                    ToolTip.SetIsOpen(this, true);
                    e.Handled = true;
                    return;
                }

                ClearHoveredIssueLink();
            }
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);

            if (_lastHover != null)
                Native.OS.OpenBrowser(_lastHover.Link);
        }

        protected override void OnPointerExited(PointerEventArgs e)
        {
            base.OnPointerExited(e);
            ClearHoveredIssueLink();
        }

        private void ClearHoveredIssueLink()
        {
            if (_lastHover != null)
            {
                ToolTip.SetTip(this, null);
                SetCurrentValue(CursorProperty, Cursor.Parse("Arrow"));
                _lastHover = null;
            }
        }

        private List<Models.Hyperlink> _matches = null;
        private Models.Hyperlink _lastHover = null;
    }

    public class CommitTimeTextBlock : TextBlock
    {
        public static readonly StyledProperty<bool> ShowAsDateTimeProperty =
            AvaloniaProperty.Register<CommitTimeTextBlock, bool>(nameof(ShowAsDateTime), true);

        public bool ShowAsDateTime
        {
            get => GetValue(ShowAsDateTimeProperty);
            set => SetValue(ShowAsDateTimeProperty, value);
        }

        public static readonly StyledProperty<ulong> TimestampProperty =
            AvaloniaProperty.Register<CommitTimeTextBlock, ulong>(nameof(Timestamp));

        public ulong Timestamp
        {
            get => GetValue(TimestampProperty);
            set => SetValue(TimestampProperty, value);
        }

        protected override Type StyleKeyOverride => typeof(TextBlock);

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == TimestampProperty)
            {
                SetCurrentValue(TextProperty, GetDisplayText());
            }
            else if (change.Property == ShowAsDateTimeProperty)
            {
                SetCurrentValue(TextProperty, GetDisplayText());

                if (ShowAsDateTime)
                    StopTimer();
                else
                    StartTimer();
            }
        }

        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);

            if (!ShowAsDateTime)
                StartTimer();
        }

        protected override void OnUnloaded(RoutedEventArgs e)
        {
            base.OnUnloaded(e);
            StopTimer();
        }

        private void StartTimer()
        {
            if (_refreshTimer != null)
                return;

            _refreshTimer = DispatcherTimer.Run(() =>
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    var text = GetDisplayText();
                    if (!text.Equals(Text, StringComparison.Ordinal))
                        Text = text;
                });

                return true;
            }, TimeSpan.FromSeconds(10));
        }

        private void StopTimer()
        {
            if (_refreshTimer != null)
            {
                _refreshTimer.Dispose();
                _refreshTimer = null;
            }
        }

        private string GetDisplayText()
        {
            if (ShowAsDateTime)
                return DateTime.UnixEpoch.AddSeconds(Timestamp).ToLocalTime().ToString("yyyy/MM/dd HH:mm:ss");

            var today = DateTime.Today;
            var committerTime = DateTime.UnixEpoch.AddSeconds(Timestamp).ToLocalTime();

            if (committerTime >= today)
            {
                var now = DateTime.Now;
                var timespan = now - committerTime;
                if (timespan.TotalHours > 1)
                    return App.Text("Period.HoursAgo", (int)timespan.TotalHours);

                return timespan.TotalMinutes < 1 ? App.Text("Period.JustNow") : App.Text("Period.MinutesAgo", (int)timespan.TotalMinutes);
            }

            var diffYear = today.Year - committerTime.Year;
            if (diffYear == 0)
            {
                var diffMonth = today.Month - committerTime.Month;
                if (diffMonth > 0)
                    return diffMonth == 1 ? App.Text("Period.LastMonth") : App.Text("Period.MonthsAgo", diffMonth);

                var diffDay = today.Day - committerTime.Day;
                return diffDay == 1 ? App.Text("Period.Yesterday") : App.Text("Period.DaysAgo", diffDay);
            }

            return diffYear == 1 ? App.Text("Period.LastYear") : App.Text("Period.YearsAgo", diffYear);
        }

        private IDisposable _refreshTimer = null;
    }

    public class CommitGraph : Control
    {
        public static readonly StyledProperty<Models.CommitGraph> GraphProperty =
            AvaloniaProperty.Register<CommitGraph, Models.CommitGraph>(nameof(Graph));

        public Models.CommitGraph Graph
        {
            get => GetValue(GraphProperty);
            set => SetValue(GraphProperty, value);
        }

        public static readonly StyledProperty<IBrush> DotBrushProperty =
            AvaloniaProperty.Register<CommitGraph, IBrush>(nameof(DotBrush), Brushes.Transparent);

        public IBrush DotBrush
        {
            get => GetValue(DotBrushProperty);
            set => SetValue(DotBrushProperty, value);
        }

        static CommitGraph()
        {
            AffectsRender<CommitGraph>(GraphProperty, DotBrushProperty);
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);

            var grid = this.FindAncestorOfType<Histories>()?.CommitDataGrid;
            if (grid == null)
                return;

            var graph = Graph;
            if (graph == null)
                return;

            var rowsPresenter = grid.FindDescendantOfType<DataGridRowsPresenter>();
            if (rowsPresenter == null)
                return;

            // Find the content display offset Y of binding DataGrid.
            double rowHeight = grid.RowHeight;
            double startY = 0;
            foreach (var child in rowsPresenter.Children)
            {
                if (child is DataGridRow { IsVisible: true, Bounds.Top: <= 0 } row && row.Bounds.Top > -rowHeight)
                {
                    var test = rowHeight * row.GetIndex() - row.Bounds.Top;
                    if (startY < test)
                        startY = test;
                }
            }

            var headerHeight = grid.ColumnHeaderHeight;
            startY -= headerHeight;

            // Apply scroll offset.
            context.PushClip(new Rect(Bounds.Left, Bounds.Top + headerHeight, grid.Columns[0].ActualWidth, Bounds.Height));
            context.PushTransform(Matrix.CreateTranslation(0, -startY));

            // Calculate bounds.
            var top = startY;
            var bottom = startY + grid.Bounds.Height + rowHeight * 2;

            // Draw contents
            DrawCurves(context, graph, top, bottom);
            DrawAnchors(context, graph, top, bottom);
        }

        private void DrawCurves(DrawingContext context, Models.CommitGraph graph, double top, double bottom)
        {
            foreach (var line in graph.Paths)
            {
                var last = line.Points[0];
                var size = line.Points.Count;

                if (line.Points[size - 1].Y < top)
                    continue;
                if (last.Y > bottom)
                    break;

                var geo = new StreamGeometry();
                var pen = Models.CommitGraph.Pens[line.Color];

                using (var ctx = geo.Open())
                {
                    var started = false;
                    var ended = false;
                    for (int i = 1; i < size; i++)
                    {
                        var cur = line.Points[i];
                        if (cur.Y < top)
                        {
                            last = cur;
                            continue;
                        }

                        if (!started)
                        {
                            ctx.BeginFigure(last, false);
                            started = true;
                        }

                        if (cur.Y > bottom)
                        {
                            cur = new Point(cur.X, bottom);
                            ended = true;
                        }

                        if (cur.X > last.X)
                        {
                            ctx.QuadraticBezierTo(new Point(cur.X, last.Y), cur);
                        }
                        else if (cur.X < last.X)
                        {
                            if (i < size - 1)
                            {
                                var midY = (last.Y + cur.Y) / 2;
                                ctx.CubicBezierTo(new Point(last.X, midY + 4), new Point(cur.X, midY - 4), cur);
                            }
                            else
                            {
                                ctx.QuadraticBezierTo(new Point(last.X, cur.Y), cur);
                            }
                        }
                        else
                        {
                            ctx.LineTo(cur);
                        }

                        if (ended)
                            break;
                        last = cur;
                    }
                }

                context.DrawGeometry(null, pen, geo);
            }

            foreach (var link in graph.Links)
            {
                if (link.End.Y < top)
                    continue;
                if (link.Start.Y > bottom)
                    break;

                var geo = new StreamGeometry();
                using (var ctx = geo.Open())
                {
                    ctx.BeginFigure(link.Start, false);
                    ctx.QuadraticBezierTo(link.Control, link.End);
                }

                context.DrawGeometry(null, Models.CommitGraph.Pens[link.Color], geo);
            }
        }

        private void DrawAnchors(DrawingContext context, Models.CommitGraph graph, double top, double bottom)
        {
            IBrush dotFill = DotBrush;
            Pen dotFillPen = new Pen(dotFill, 2);

            foreach (var dot in graph.Dots)
            {
                if (dot.Center.Y < top)
                    continue;
                if (dot.Center.Y > bottom)
                    break;

                var pen = Models.CommitGraph.Pens[dot.Color];
                switch (dot.Type)
                {
                    case Models.CommitGraph.DotType.Head:
                        context.DrawEllipse(dotFill, pen, dot.Center, 6, 6);
                        context.DrawEllipse(pen.Brush, null, dot.Center, 3, 3);
                        break;
                    case Models.CommitGraph.DotType.Merge:
                        context.DrawEllipse(pen.Brush, null, dot.Center, 6, 6);
                        context.DrawLine(dotFillPen, new Point(dot.Center.X, dot.Center.Y - 3), new Point(dot.Center.X, dot.Center.Y + 3));
                        context.DrawLine(dotFillPen, new Point(dot.Center.X - 3, dot.Center.Y), new Point(dot.Center.X + 3, dot.Center.Y));
                        break;
                    default:
                        context.DrawEllipse(dotFill, pen, dot.Center, 3, 3);
                        break;
                }
            }
        }
    }

    public partial class Histories : UserControl
    {
        public static readonly StyledProperty<Models.Branch> CurrentBranchProperty =
            AvaloniaProperty.Register<Histories, Models.Branch>(nameof(CurrentBranch));

        public Models.Branch CurrentBranch
        {
            get => GetValue(CurrentBranchProperty);
            set => SetValue(CurrentBranchProperty, value);
        }

        public static readonly StyledProperty<AvaloniaList<Models.IssueTrackerRule>> IssueTrackerRulesProperty =
            AvaloniaProperty.Register<Histories, AvaloniaList<Models.IssueTrackerRule>>(nameof(IssueTrackerRules));

        public AvaloniaList<Models.IssueTrackerRule> IssueTrackerRules
        {
            get => GetValue(IssueTrackerRulesProperty);
            set => SetValue(IssueTrackerRulesProperty, value);
        }

        public static readonly StyledProperty<long> NavigationIdProperty =
            AvaloniaProperty.Register<Histories, long>(nameof(NavigationId));

        public long NavigationId
        {
            get => GetValue(NavigationIdProperty);
            set => SetValue(NavigationIdProperty, value);
        }

        static Histories()
        {
            NavigationIdProperty.Changed.AddClassHandler<Histories>((h, _) =>
            {
                if (h.DataContext == null)
                    return;

                // Force scroll selected item (current head) into view. see issue #58
                var datagrid = h.CommitDataGrid;
                if (datagrid != null && datagrid.SelectedItems.Count == 1)
                    datagrid.ScrollIntoView(datagrid.SelectedItems[0], null);
            });
        }

        public Histories()
        {
            InitializeComponent();
        }

        private void OnCommitDataGridLayoutUpdated(object _1, EventArgs _2)
        {
            CommitGraph.InvalidateVisual();
        }

        private void OnCommitDataGridSelectionChanged(object _, SelectionChangedEventArgs e)
        {
            if (DataContext is ViewModels.Histories histories)
            {
                histories.Select(CommitDataGrid.SelectedItems);
            }
            e.Handled = true;
        }

        private void OnCommitDataGridContextRequested(object sender, ContextRequestedEventArgs e)
        {
            if (DataContext is ViewModels.Histories histories && sender is DataGrid datagrid)
            {
                var menu = histories.MakeContextMenu(datagrid);
                datagrid.OpenContextMenu(menu);
            }
            e.Handled = true;
        }

        private void OnCommitDataGridDoubleTapped(object sender, TappedEventArgs e)
        {
            if (DataContext is ViewModels.Histories histories && sender is DataGrid datagrid && datagrid.SelectedItems is { Count: 1 } selectedItems)
            {
                histories.DoubleTapped(selectedItems[0] as Models.Commit);
            }
            e.Handled = true;
        }

        private void OnCommitDataGridKeyDown(object sender, KeyEventArgs e)
        {
            if (sender is DataGrid grid &&
                grid.SelectedItems is { Count: > 0 } selected &&
                e.Key == Key.C &&
                e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                var builder = new StringBuilder();
                foreach (var item in selected)
                {
                    if (item is Models.Commit commit)
                        builder.AppendLine($"{commit.SHA.Substring(0, 10)} - {commit.Subject}");
                }

                App.CopyText(builder.ToString());
                e.Handled = true;
            }
        }
    }
}
