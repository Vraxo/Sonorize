using Sonorize.Core.Library;
using Sonorize.Core.Playback;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Sonorize.Tui;

/// <summary>Coordinates browse navigation and UI events with results, presentation and selection actions.</summary>
internal sealed class BrowseController
{
    private readonly TextField _search;
    private readonly ListView _sidebar;
    private readonly TableView _table;
    private readonly BrowseResultsBuilder _builder;
    private readonly BrowseTablePresenter _presenter;
    private readonly BrowseSelectionActions _actions;
    private BrowseMode _mode = BrowseMode.Songs;
    private BrowseDrillDown? _drillDown;
    private BrowseResults _results = null!;
    private bool _suppressSearchRefresh;

    public BrowseController(LibraryService library, IPlayerService player,
        MainWindowLayout layout, AudioEngineNotifier audioNotifier)
    {
        _search = layout.Search;
        _sidebar = layout.Sidebar;
        _table = layout.Table;
        _builder = new BrowseResultsBuilder(library, player);
        _presenter = new BrowseTablePresenter(_table, layout.EmptyHint, player);
        _actions = new BrowseSelectionActions(player, audioNotifier);
        _search.TextChanged += OnSearchTextChanged;
        _sidebar.ValueChanged += OnSidebarValueChanged;
        _sidebar.Accepting += OnSidebarAccepting;
        _table.Accepting += OnTableAccepting;
        _sidebar.Value = (int)BrowseMode.Songs;
        RefreshRows(keepSelection: false);
    }

    public bool IsQueueView => _drillDown is null && _mode == BrowseMode.Queue;
    public bool ShowsSongs => _drillDown is not null || _mode is BrowseMode.Songs or BrowseMode.Queue;

    public void SetMode(BrowseMode mode)
    {
        if (_mode == mode && _drillDown is null)
            return;

        _mode = mode;
        _drillDown = null;
        if (_sidebar.Value != (int)mode)
            _sidebar.Value = (int)mode;
        ClearSearchAndRefresh();
    }

    public bool CloseDrillDown()
    {
        if (_drillDown is null)
            return false;
        _drillDown = null;
        ClearSearchAndRefresh();
        return true;
    }

    private void ClearSearchAndRefresh()
    {
        _suppressSearchRefresh = true;
        _search.Text = string.Empty;
        _suppressSearchRefresh = false;
        RefreshRows(keepSelection: false);
    }

    public void RefreshRows(bool keepSelection)
    {
        _results = _builder.Build(_mode, _drillDown, _search.Text ?? string.Empty);
        _presenter.Present(_results, keepSelection);
    }

    public void ActivateSelectedRow()
    {
        BrowseDrillDown? drillDown = _actions.Activate(_results, _presenter.SelectedRow);
        if (drillDown is not null)
        {
            _drillDown = drillDown;
            ClearSearchAndRefresh();
        }
    }

    public void RemoveSelectedFromQueue()
    {
        if (_actions.RemoveFromQueue(_results, _presenter.SelectedRow))
            RefreshRows(keepSelection: true);
    }

    private void OnSearchTextChanged(object? sender, EventArgs e)
    {
        if (!_suppressSearchRefresh)
            RefreshRows(keepSelection: false);
    }

    private void OnSidebarValueChanged(object? sender, ValueChangedEventArgs<int?> e)
    {
        if (e.NewValue is int index && Enum.IsDefined((BrowseMode)index))
            SetMode((BrowseMode)index);
    }

    private void OnSidebarAccepting(object? sender, CommandEventArgs e)
    {
        _table.SetFocus();
        e.Handled = true;
    }

    private void OnTableAccepting(object? sender, CommandEventArgs e)
    {
        ActivateSelectedRow();
        e.Handled = true;
    }
}
