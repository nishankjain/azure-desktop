using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;

namespace AzureDesktop.Controls;

public sealed partial class DataTableControl : UserControl
{
    private string _sortColumn = "Name";
    private bool _sortAscending = true;
    private string _searchText = "";
    private CheckBox? _selectAllCheckBox;
    private bool _suppressSelectAll;
    private readonly HashSet<string> _selectedNames = [];
    private List<Dictionary<string, string>> _allRows = [];
    private List<string> _columns = [];

    public DataTableControl()
    {
        InitializeComponent();
        Loaded += (_, _) => Refresh();
    }

    // --- Dependency Properties ---

    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(nameof(ItemsSource), typeof(ObservableCollection<Dictionary<string, string>>),
            typeof(DataTableControl), new PropertyMetadata(null, OnItemsSourceChanged));

    public ObservableCollection<Dictionary<string, string>> ItemsSource
    {
        get => (ObservableCollection<Dictionary<string, string>>)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public static readonly DependencyProperty ColumnsProperty =
        DependencyProperty.Register(nameof(Columns), typeof(string),
            typeof(DataTableControl), new PropertyMetadata("", OnColumnsChanged));

    public string Columns
    {
        get => (string)GetValue(ColumnsProperty);
        set => SetValue(ColumnsProperty, value);
    }

    public static readonly DependencyProperty ShowCheckboxesProperty =
        DependencyProperty.Register(nameof(ShowCheckboxes), typeof(bool),
            typeof(DataTableControl), new PropertyMetadata(false));

    public bool ShowCheckboxes
    {
        get => (bool)GetValue(ShowCheckboxesProperty);
        set => SetValue(ShowCheckboxesProperty, value);
    }

    public static readonly DependencyProperty IsNavigableProperty =
        DependencyProperty.Register(nameof(IsNavigable), typeof(bool),
            typeof(DataTableControl), new PropertyMetadata(false));

    public bool IsNavigable
    {
        get => (bool)GetValue(IsNavigableProperty);
        set => SetValue(IsNavigableProperty, value);
    }

    public static readonly DependencyProperty EmptyMessageProperty =
        DependencyProperty.Register(nameof(EmptyMessage), typeof(string),
            typeof(DataTableControl), new PropertyMetadata("No items."));

    public string EmptyMessage
    {
        get => (string)GetValue(EmptyMessageProperty);
        set => SetValue(EmptyMessageProperty, value);
    }

    public static readonly DependencyProperty ShowAddButtonProperty =
        DependencyProperty.Register(nameof(ShowAddButton), typeof(bool),
            typeof(DataTableControl), new PropertyMetadata(false, OnShowAddButtonChanged));

    public bool ShowAddButton
    {
        get => (bool)GetValue(ShowAddButtonProperty);
        set => SetValue(ShowAddButtonProperty, value);
    }

    // --- Events ---

    public event EventHandler<string>? ItemClick;
    public event EventHandler<List<string>>? DeleteClick;
    public event EventHandler? AddClick;

    // --- Property Changed Handlers ---

    private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DataTableControl ctrl)
        {
            ctrl._selectedNames.Clear();
            if (e.OldValue is ObservableCollection<Dictionary<string, string>> oldColl)
                oldColl.CollectionChanged -= ctrl.OnCollectionChanged;
            if (e.NewValue is ObservableCollection<Dictionary<string, string>> newColl)
                newColl.CollectionChanged += ctrl.OnCollectionChanged;
            ctrl.Refresh();
        }
    }

    private void OnCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        => Refresh();

    private static void OnColumnsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DataTableControl ctrl) ctrl.Refresh();
    }

    private static void OnShowAddButtonChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DataTableControl ctrl)
            ctrl.AddButton.Visibility = ctrl.ShowAddButton ? Visibility.Visible : Visibility.Collapsed;
    }

    // --- Rendering ---

    public void Refresh()
    {
        _columns = string.IsNullOrEmpty(Columns) ? [] : Columns.Split(',').Select(c => c.Trim()).ToList();
        _allRows = ItemsSource?.ToList() ?? [];

        ToolbarGrid.Visibility = ShowCheckboxes ? Visibility.Visible : Visibility.Collapsed;

        BuildHeader();
        BuildRows();
    }

    private void BuildHeader()
    {
        HeaderGrid.Children.Clear();
        HeaderGrid.ColumnDefinitions.Clear();

        if (ShowCheckboxes)
        {
            HeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            _selectAllCheckBox = new CheckBox
            {
                MinWidth = 0, Padding = new Thickness(0),
                Margin = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center,
            };
            _selectAllCheckBox.Checked += SelectAll_Changed;
            _selectAllCheckBox.Unchecked += SelectAll_Changed;
            Grid.SetColumn(_selectAllCheckBox, 0);
            HeaderGrid.Children.Add(_selectAllCheckBox);
        }

        var colOffset = ShowCheckboxes ? 1 : 0;

        for (int c = 0; c < _columns.Count; c++)
        {
            HeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var col = _columns[c];
            var arrow = _sortColumn == col ? (_sortAscending ? " \u2191" : " \u2193") : "";
            var btn = new Button
            {
                Content = col + arrow,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
                BorderThickness = new Thickness(0),
                Padding = new Thickness(8, 4, 8, 4),
                FontSize = 12,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Tag = col,
            };
            btn.Click += HeaderButton_Click;
            Grid.SetColumn(btn, c + colOffset);
            HeaderGrid.Children.Add(btn);
        }

        if (IsNavigable)
        {
            HeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var spacer = new Border { Width = 20 };
            Grid.SetColumn(spacer, _columns.Count + colOffset);
            HeaderGrid.Children.Add(spacer);
        }
    }

    private void BuildRows()
    {
        DataList.Items.Clear();

        var filtered = _allRows.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(_searchText))
        {
            filtered = filtered.Where(r =>
                r.TryGetValue("Name", out var name) &&
                name.Contains(_searchText, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrEmpty(_sortColumn))
        {
            filtered = _sortAscending
                ? filtered.OrderBy(r => r.TryGetValue(_sortColumn, out var v) ? v : "", NaturalStringComparer.Instance)
                : filtered.OrderByDescending(r => r.TryGetValue(_sortColumn, out var v) ? v : "", NaturalStringComparer.Instance);
        }

        var rows = filtered.ToList();

        if (rows.Count == 0)
        {
            EmptyText.Text = EmptyMessage;
            EmptyText.Visibility = Visibility.Visible;
            DataList.Visibility = Visibility.Collapsed;
            return;
        }

        EmptyText.Visibility = Visibility.Collapsed;
        DataList.Visibility = Visibility.Visible;

        foreach (var row in rows)
        {
            var rowName = row.TryGetValue("Name", out var n) ? n : "";
            var rowGrid = new Grid { Tag = rowName };
            var colOffset = 0;

            if (ShowCheckboxes)
            {
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                var cb = new CheckBox
                {
                    Tag = rowName,
                    IsChecked = _selectedNames.Contains(rowName),
                    MinWidth = 0, Padding = new Thickness(0),
                    Margin = new Thickness(0, 0, 8, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                };
                cb.Checked += RowCheckBox_Changed;
                cb.Unchecked += RowCheckBox_Changed;
                Grid.SetColumn(cb, 0);
                rowGrid.Children.Add(cb);
                colOffset = 1;
            }

            // Card border
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var cardBorder = new Border
            {
                Background = (Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"],
                BorderBrush = (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(0, 8, 0, 8),
                Tag = rowName,
                RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5),
                RenderTransform = new ScaleTransform { ScaleX = 1, ScaleY = 1 },
            };
            if (IsNavigable)
            {
                cardBorder.Tapped += CardBorder_Tapped;
                cardBorder.PointerEntered += CardBorder_PointerEntered;
                cardBorder.PointerExited += CardBorder_PointerExited;
            }
            Grid.SetColumn(cardBorder, colOffset);

            // Content grid with data columns
            var contentGrid = new Grid();
            for (int c = 0; c < _columns.Count; c++)
            {
                contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                var value = row.TryGetValue(_columns[c], out var v) ? v : "";
                var cell = new TextBlock
                {
                    Text = value,
                    FontSize = 13,
                    Padding = new Thickness(8, 0, 8, 0),
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    VerticalAlignment = VerticalAlignment.Center,
                };
                if (c == 0) cell.FontWeight = Microsoft.UI.Text.FontWeights.SemiBold;
                else cell.Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"];
                Grid.SetColumn(cell, c);
                contentGrid.Children.Add(cell);
            }

            if (IsNavigable)
            {
                contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20) });
                var chevron = new FontIcon
                {
                    Glyph = "\uE76C", FontSize = 12,
                    Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                };
                Grid.SetColumn(chevron, _columns.Count);
                contentGrid.Children.Add(chevron);
            }

            cardBorder.Child = contentGrid;
            rowGrid.Children.Add(cardBorder);
            DataList.Items.Add(rowGrid);
        }

        UpdateDeleteState();
    }

    // --- Interactions ---

    private void HeaderButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string col })
        {
            if (_sortColumn == col) _sortAscending = !_sortAscending;
            else { _sortColumn = col; _sortAscending = true; }
            Refresh();
        }
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _searchText = SearchBox.Text;
        BuildRows();
    }

    private void CardBorder_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        if (sender is Border { Tag: string name })
            ItemClick?.Invoke(this, name);
    }

    private void CardBorder_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is Border border)
        {
            border.Background = (Brush)Application.Current.Resources["ListViewItemBackgroundPointerOver"];
            if (border.RenderTransform is ScaleTransform st) AnimateScale(st, 1.01, 150);
        }
    }

    private void CardBorder_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is Border border)
        {
            border.Background = (Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"];
            if (border.RenderTransform is ScaleTransform st)
            {
                var name = border.Tag as string;
                var target = name is not null && _selectedNames.Contains(name) ? 0.99 : 1.0;
                AnimateScale(st, target, 150);
            }
        }
    }

    private void RowCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox { Tag: string name } cb)
        {
            if (cb.IsChecked == true) _selectedNames.Add(name);
            else _selectedNames.Remove(name);

            // Animate card
            if (cb.Parent is Grid rowGrid)
            {
                foreach (var child in rowGrid.Children)
                {
                    if (child is Border b && b.RenderTransform is ScaleTransform st)
                    {
                        AnimateScale(st, cb.IsChecked == true ? 0.99 : 1.0, 150);
                        break;
                    }
                }
            }

            UpdateDeleteState();
        }
    }

    private void SelectAll_Changed(object sender, RoutedEventArgs e)
    {
        if (_suppressSelectAll) return;
        _suppressSelectAll = true;
        var selectAll = _selectAllCheckBox?.IsChecked == true;
        _selectedNames.Clear();

        foreach (var item in DataList.Items)
        {
            if (item is Grid rowGrid)
            {
                foreach (var child in rowGrid.Children)
                {
                    if (child is CheckBox cb && cb.Tag is string name)
                    {
                        cb.IsChecked = selectAll;
                        if (selectAll) _selectedNames.Add(name);

                        // Animate card
                        foreach (var sibling in rowGrid.Children)
                        {
                            if (sibling is Border b && b.RenderTransform is ScaleTransform st)
                            {
                                AnimateScale(st, selectAll ? 0.99 : 1.0, 150);
                                break;
                            }
                        }
                        break;
                    }
                }
            }
        }

        UpdateDeleteState();
        _suppressSelectAll = false;
    }

    private void UpdateDeleteState()
    {
        DeleteBtn.IsEnabled = _selectedNames.Count > 0;

        if (!_suppressSelectAll && _selectAllCheckBox is not null)
        {
            _suppressSelectAll = true;
            var total = DataList.Items.Count;
            _selectAllCheckBox.IsChecked = _selectedNames.Count == total && total > 0 ? true
                : _selectedNames.Count == 0 ? false : null;
            _suppressSelectAll = false;
        }
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedNames.Count == 0) return;
        DeleteClick?.Invoke(this, _selectedNames.ToList());
        _selectedNames.Clear();
        DeleteBtn.IsEnabled = false;
        if (_selectAllCheckBox is not null)
        {
            _suppressSelectAll = true;
            _selectAllCheckBox.IsChecked = false;
            _suppressSelectAll = false;
        }
    }

    private void AddButton_Click(object sender, RoutedEventArgs e) => AddClick?.Invoke(this, EventArgs.Empty);

    private static void AnimateScale(ScaleTransform st, double target, int durationMs)
    {
        var animX = new DoubleAnimation { To = target, Duration = TimeSpan.FromMilliseconds(durationMs) };
        animX.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
        var animY = new DoubleAnimation { To = target, Duration = TimeSpan.FromMilliseconds(durationMs) };
        animY.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
        var sb = new Storyboard();
        Storyboard.SetTarget(animX, st);
        Storyboard.SetTargetProperty(animX, "ScaleX");
        Storyboard.SetTarget(animY, st);
        Storyboard.SetTargetProperty(animY, "ScaleY");
        sb.Children.Add(animX);
        sb.Children.Add(animY);
        sb.Begin();
    }
}

internal sealed class NaturalStringComparer : IComparer<string>
{
    public static readonly NaturalStringComparer Instance = new();

    public int Compare(string? x, string? y)
    {
        if (x is null && y is null) return 0;
        if (x is null) return -1;
        if (y is null) return 1;

        int ix = 0, iy = 0;
        while (ix < x.Length && iy < y.Length)
        {
            if (char.IsDigit(x[ix]) && char.IsDigit(y[iy]))
            {
                int startX = ix, startY = iy;
                while (ix < x.Length && char.IsDigit(x[ix])) ix++;
                while (iy < y.Length && char.IsDigit(y[iy])) iy++;
                var lenDiff = (ix - startX) - (iy - startY);
                if (lenDiff != 0) return lenDiff;
                for (int i = 0; i < ix - startX; i++)
                {
                    var diff = x[startX + i] - y[startY + i];
                    if (diff != 0) return diff;
                }
            }
            else
            {
                var cmp = char.ToLowerInvariant(x[ix]).CompareTo(char.ToLowerInvariant(y[iy]));
                if (cmp != 0) return cmp;
                ix++;
                iy++;
            }
        }
        return x.Length - y.Length;
    }
}
