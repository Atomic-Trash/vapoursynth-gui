using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace VapourSynthPortable.Services;

/// <summary>
/// Centralized undo/redo service with transaction support.
/// Provides a generic, reusable undo/redo mechanism for the application.
/// </summary>
public partial class UndoService : ObservableObject, IDisposable
{
    private static readonly ILogger<UndoService> _logger = LoggingService.GetLogger<UndoService>();

    private readonly Stack<IUndoAction> _undoStack = new();
    private readonly Stack<IUndoAction> _redoStack = new();
    private readonly int _maxHistorySize;
    private UndoTransaction? _currentTransaction;
    private bool _isUndoing;
    private bool _isRedoing;
    private bool _disposed;

    /// <summary>
    /// Event raised when undo/redo state changes
    /// </summary>
    public event EventHandler? StateChanged;

    /// <summary>
    /// Event raised when an action is executed (undo or redo)
    /// </summary>
    public event EventHandler<UndoActionEventArgs>? ActionExecuted;

    [ObservableProperty]
    private bool _canUndo;

    [ObservableProperty]
    private bool _canRedo;

    [ObservableProperty]
    private string _undoDescription = "";

    [ObservableProperty]
    private string _redoDescription = "";

    /// <summary>
    /// Gets the undo history for display
    /// </summary>
    public ReadOnlyCollection<string> UndoHistory =>
        new(_undoStack.Select(a => a.Description).ToList());

    /// <summary>
    /// Gets the redo history for display
    /// </summary>
    public ReadOnlyCollection<string> RedoHistory =>
        new(_redoStack.Select(a => a.Description).ToList());

    /// <summary>
    /// Gets whether a transaction is currently in progress
    /// </summary>
    public bool IsInTransaction => _currentTransaction != null;

    /// <summary>
    /// Creates a new UndoService
    /// </summary>
    /// <param name="maxHistorySize">Maximum number of undo actions to keep (default 100)</param>
    public UndoService(int maxHistorySize = 100)
    {
        _maxHistorySize = maxHistorySize;
        _logger.LogInformation("UndoService initialized with max history: {MaxHistory}", maxHistorySize);
    }

    /// <summary>
    /// Record an undoable action
    /// </summary>
    /// <param name="action">The action to record</param>
    public void RecordAction(IUndoAction action)
    {
        if (_isUndoing || _isRedoing)
            return;

        if (_currentTransaction != null)
        {
            _currentTransaction.AddAction(action);
            return;
        }

        PushUndoAction(action);
    }

    /// <summary>
    /// Record a simple action with undo/redo delegates
    /// </summary>
    public void RecordAction(string description, Action undoAction, Action redoAction)
    {
        RecordAction(new DelegateUndoAction(description, undoAction, redoAction));
    }

    /// <summary>
    /// Record a property change action
    /// </summary>
    public void RecordPropertyChange<T>(object target, string propertyName, T oldValue, T newValue, string? description = null)
    {
        var desc = description ?? $"Change {propertyName}";
        RecordAction(new PropertyChangeAction<T>(desc, target, propertyName, oldValue, newValue));
    }

    private void PushUndoAction(IUndoAction action)
    {
        _undoStack.Push(action);
        _redoStack.Clear();

        // Enforce max history size
        while (_undoStack.Count > _maxHistorySize)
        {
            var items = _undoStack.ToArray();
            _undoStack.Clear();
            for (int i = 0; i < items.Length - 1; i++)
            {
                _undoStack.Push(items[i]);
            }
        }

        UpdateState();
        _logger.LogDebug("Recorded action: {Description}", action.Description);
    }

    /// <summary>
    /// Begin a transaction that groups multiple actions into one undoable unit
    /// </summary>
    /// <param name="description">Description for the combined action</param>
    /// <returns>Transaction object - dispose to commit</returns>
    public UndoTransaction BeginTransaction(string description)
    {
        if (_currentTransaction != null)
        {
            _logger.LogWarning("Nested transactions not supported - returning existing transaction");
            return _currentTransaction;
        }

        _currentTransaction = new UndoTransaction(this, description);
        _logger.LogDebug("Started transaction: {Description}", description);
        return _currentTransaction;
    }

    internal void CommitTransaction(UndoTransaction transaction)
    {
        if (_currentTransaction != transaction)
            return;

        if (transaction.Actions.Count > 0)
        {
            if (transaction.Actions.Count == 1)
            {
                PushUndoAction(transaction.Actions[0]);
            }
            else
            {
                PushUndoAction(new CompositeUndoAction(transaction.Description, transaction.Actions.ToArray()));
            }
        }

        _currentTransaction = null;
        _logger.LogDebug("Committed transaction: {Description} ({Count} actions)",
            transaction.Description, transaction.Actions.Count);
    }

    internal void CancelTransaction(UndoTransaction transaction)
    {
        if (_currentTransaction != transaction)
            return;

        // Undo any actions already executed in the transaction
        foreach (var action in transaction.Actions.AsEnumerable().Reverse())
        {
            try
            {
                action.Undo();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to undo action during transaction cancel: {Description}",
                    action.Description);
            }
        }

        _currentTransaction = null;
        _logger.LogDebug("Cancelled transaction: {Description}", transaction.Description);
    }

    /// <summary>
    /// Undo the last action
    /// </summary>
    public void Undo()
    {
        if (!CanUndo || _undoStack.Count == 0)
            return;

        _isUndoing = true;
        try
        {
            var action = _undoStack.Pop();
            action.Undo();
            _redoStack.Push(action);

            UpdateState();
            ActionExecuted?.Invoke(this, new UndoActionEventArgs(action, isUndo: true));
            _logger.LogDebug("Undid action: {Description}", action.Description);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to undo action");
        }
        finally
        {
            _isUndoing = false;
        }
    }

    /// <summary>
    /// Redo the last undone action
    /// </summary>
    public void Redo()
    {
        if (!CanRedo || _redoStack.Count == 0)
            return;

        _isRedoing = true;
        try
        {
            var action = _redoStack.Pop();
            action.Redo();
            _undoStack.Push(action);

            UpdateState();
            ActionExecuted?.Invoke(this, new UndoActionEventArgs(action, isUndo: false));
            _logger.LogDebug("Redid action: {Description}", action.Description);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to redo action");
        }
        finally
        {
            _isRedoing = false;
        }
    }

    /// <summary>
    /// Undo multiple actions at once
    /// </summary>
    public void UndoMultiple(int count)
    {
        for (int i = 0; i < count && CanUndo; i++)
        {
            Undo();
        }
    }

    /// <summary>
    /// Redo multiple actions at once
    /// </summary>
    public void RedoMultiple(int count)
    {
        for (int i = 0; i < count && CanRedo; i++)
        {
            Redo();
        }
    }

    /// <summary>
    /// Clear all undo/redo history
    /// </summary>
    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
        _currentTransaction = null;
        UpdateState();
        _logger.LogInformation("Undo history cleared");
    }

    /// <summary>
    /// Mark the current state as a save point (clears modified flag)
    /// </summary>
    public void MarkSavePoint()
    {
        // Could be used to track "modified since last save" state
        _logger.LogDebug("Save point marked at history depth {Depth}", _undoStack.Count);
    }

    private void UpdateState()
    {
        CanUndo = _undoStack.Count > 0;
        CanRedo = _redoStack.Count > 0;
        UndoDescription = CanUndo ? _undoStack.Peek().Description : "";
        RedoDescription = CanRedo ? _redoStack.Peek().Description : "";
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        Clear();
    }
}

#region Interfaces and Base Classes

/// <summary>
/// Interface for undoable actions
/// </summary>
public interface IUndoAction
{
    string Description { get; }
    void Undo();
    void Redo();
}

/// <summary>
/// Event args for undo/redo action execution
/// </summary>
public class UndoActionEventArgs : EventArgs
{
    public IUndoAction Action { get; }
    public bool IsUndo { get; }
    public bool IsRedo => !IsUndo;

    public UndoActionEventArgs(IUndoAction action, bool isUndo)
    {
        Action = action;
        IsUndo = isUndo;
    }
}

#endregion

#region Transaction Support

/// <summary>
/// Transaction for grouping multiple actions into one undoable unit
/// </summary>
public class UndoTransaction : IDisposable
{
    private readonly UndoService _service;
    private bool _committed;
    private bool _disposed;

    public string Description { get; }
    public List<IUndoAction> Actions { get; } = new();

    internal UndoTransaction(UndoService service, string description)
    {
        _service = service;
        Description = description;
    }

    internal void AddAction(IUndoAction action)
    {
        Actions.Add(action);
    }

    /// <summary>
    /// Commit the transaction
    /// </summary>
    public void Commit()
    {
        if (_committed || _disposed) return;
        _committed = true;
        _service.CommitTransaction(this);
    }

    /// <summary>
    /// Cancel the transaction and undo all actions
    /// </summary>
    public void Cancel()
    {
        if (_committed || _disposed) return;
        _disposed = true;
        _service.CancelTransaction(this);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (!_committed)
        {
            // Auto-commit on dispose if not cancelled
            Commit();
        }
    }
}

#endregion

#region Built-in Action Types

/// <summary>
/// Simple undo action using delegates
/// </summary>
public class DelegateUndoAction : IUndoAction
{
    private readonly Action _undoAction;
    private readonly Action _redoAction;

    public string Description { get; }

    public DelegateUndoAction(string description, Action undoAction, Action redoAction)
    {
        Description = description;
        _undoAction = undoAction;
        _redoAction = redoAction;
    }

    public void Undo() => _undoAction();
    public void Redo() => _redoAction();
}

/// <summary>
/// Action that tracks a property value change
/// </summary>
public class PropertyChangeAction<T> : IUndoAction
{
    private readonly object _target;
    private readonly string _propertyName;
    private readonly T _oldValue;
    private readonly T _newValue;

    public string Description { get; }

    public PropertyChangeAction(string description, object target, string propertyName, T oldValue, T newValue)
    {
        Description = description;
        _target = target;
        _propertyName = propertyName;
        _oldValue = oldValue;
        _newValue = newValue;
    }

    public void Undo()
    {
        SetPropertyValue(_oldValue);
    }

    public void Redo()
    {
        SetPropertyValue(_newValue);
    }

    private void SetPropertyValue(T value)
    {
        var property = _target.GetType().GetProperty(_propertyName);
        property?.SetValue(_target, value);
    }
}

/// <summary>
/// Composite action that groups multiple actions
/// </summary>
public class CompositeUndoAction : IUndoAction
{
    private readonly IUndoAction[] _actions;

    public string Description { get; }

    public CompositeUndoAction(string description, IUndoAction[] actions)
    {
        Description = description;
        _actions = actions;
    }

    public void Undo()
    {
        // Undo in reverse order
        for (int i = _actions.Length - 1; i >= 0; i--)
        {
            _actions[i].Undo();
        }
    }

    public void Redo()
    {
        // Redo in original order
        foreach (var action in _actions)
        {
            action.Redo();
        }
    }
}

/// <summary>
/// Action for collection modifications (add/remove)
/// </summary>
public class CollectionUndoAction<T> : IUndoAction
{
    private readonly IList<T> _collection;
    private readonly T _item;
    private readonly int _index;
    private readonly bool _isAdd;

    public string Description { get; }

    public CollectionUndoAction(string description, IList<T> collection, T item, int index, bool isAdd)
    {
        Description = description;
        _collection = collection;
        _item = item;
        _index = index;
        _isAdd = isAdd;
    }

    public void Undo()
    {
        if (_isAdd)
        {
            // Was an add, so remove to undo
            _collection.Remove(_item);
        }
        else
        {
            // Was a remove, so add back to undo
            if (_index >= 0 && _index <= _collection.Count)
                _collection.Insert(_index, _item);
            else
                _collection.Add(_item);
        }
    }

    public void Redo()
    {
        if (_isAdd)
        {
            // Re-add
            if (_index >= 0 && _index <= _collection.Count)
                _collection.Insert(_index, _item);
            else
                _collection.Add(_item);
        }
        else
        {
            // Re-remove
            _collection.Remove(_item);
        }
    }
}

/// <summary>
/// Action for state snapshot-based undo (captures entire object state)
/// </summary>
public class StateSnapshotAction<T> : IUndoAction where T : class
{
    private readonly T _target;
    private readonly Func<T, T> _cloneFunc;
    private readonly Action<T, T> _restoreFunc;
    private readonly T _beforeState;
    private T? _afterState;

    public string Description { get; }

    public StateSnapshotAction(string description, T target, Func<T, T> cloneFunc, Action<T, T> restoreFunc)
    {
        Description = description;
        _target = target;
        _cloneFunc = cloneFunc;
        _restoreFunc = restoreFunc;
        _beforeState = cloneFunc(target);
    }

    /// <summary>
    /// Call after the change is made to capture the after state
    /// </summary>
    public void CaptureAfterState()
    {
        _afterState = _cloneFunc(_target);
    }

    public void Undo()
    {
        _restoreFunc(_target, _beforeState);
    }

    public void Redo()
    {
        if (_afterState != null)
        {
            _restoreFunc(_target, _afterState);
        }
    }
}

#endregion

#region Extension Methods

/// <summary>
/// Extension methods for easier undo recording
/// </summary>
public static class UndoServiceExtensions
{
    /// <summary>
    /// Record an item being added to a collection
    /// </summary>
    public static void RecordAdd<T>(this UndoService service, IList<T> collection, T item, string? description = null)
    {
        var desc = description ?? $"Add {typeof(T).Name}";
        var index = collection.IndexOf(item);
        service.RecordAction(new CollectionUndoAction<T>(desc, collection, item, index, isAdd: true));
    }

    /// <summary>
    /// Record an item being removed from a collection
    /// </summary>
    public static void RecordRemove<T>(this UndoService service, IList<T> collection, T item, string? description = null)
    {
        var desc = description ?? $"Remove {typeof(T).Name}";
        var index = collection.IndexOf(item);
        service.RecordAction(new CollectionUndoAction<T>(desc, collection, item, index, isAdd: false));
    }
}

#endregion
