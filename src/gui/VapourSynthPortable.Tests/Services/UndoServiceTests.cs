using VapourSynthPortable.Services;

namespace VapourSynthPortable.Tests.Services;

public class UndoServiceTests
{
    [Fact]
    public void RecordAction_WithDelegateAction_CanBeUndone()
    {
        // Arrange
        var service = new UndoService();
        var value = 0;
        service.RecordAction("Set to 1", () => value = 0, () => value = 1);
        value = 1; // Simulate the actual change

        // Act
        service.Undo();

        // Assert
        Assert.Equal(0, value);
        Assert.False(service.CanUndo);
        Assert.True(service.CanRedo);
    }

    [Fact]
    public void Redo_AfterUndo_RestoresState()
    {
        // Arrange
        var service = new UndoService();
        var value = 0;
        service.RecordAction("Set to 1", () => value = 0, () => value = 1);
        value = 1;
        service.Undo();

        // Act
        service.Redo();

        // Assert
        Assert.Equal(1, value);
        Assert.True(service.CanUndo);
        Assert.False(service.CanRedo);
    }

    [Fact]
    public void RecordAction_ClearsRedoStack()
    {
        // Arrange
        var service = new UndoService();
        var value = 0;
        service.RecordAction("Set to 1", () => value = 0, () => value = 1);
        value = 1;
        service.Undo();
        Assert.True(service.CanRedo);

        // Act - record new action
        service.RecordAction("Set to 2", () => value = 0, () => value = 2);
        value = 2;

        // Assert
        Assert.False(service.CanRedo);
    }

    [Fact]
    public void UndoDescription_ReturnsLastActionDescription()
    {
        // Arrange
        var service = new UndoService();
        service.RecordAction("First action", () => { }, () => { });
        service.RecordAction("Second action", () => { }, () => { });

        // Assert
        Assert.Equal("Second action", service.UndoDescription);
    }

    [Fact]
    public void RedoDescription_AfterUndo_ReturnsLastUndoneDescription()
    {
        // Arrange
        var service = new UndoService();
        service.RecordAction("First action", () => { }, () => { });
        service.Undo();

        // Assert
        Assert.Equal("First action", service.RedoDescription);
    }

    [Fact]
    public void Clear_RemovesAllHistory()
    {
        // Arrange
        var service = new UndoService();
        service.RecordAction("Action 1", () => { }, () => { });
        service.RecordAction("Action 2", () => { }, () => { });
        service.Undo();

        // Act
        service.Clear();

        // Assert
        Assert.False(service.CanUndo);
        Assert.False(service.CanRedo);
        Assert.Empty(service.UndoHistory);
        Assert.Empty(service.RedoHistory);
    }

    [Fact]
    public void Transaction_GroupsActionsIntoOne()
    {
        // Arrange
        var service = new UndoService();
        var value1 = 0;
        var value2 = 0;

        // Act
        var transaction = service.BeginTransaction("Combined action");
        service.RecordAction("Set value1", () => value1 = 0, () => value1 = 1);
        value1 = 1;
        service.RecordAction("Set value2", () => value2 = 0, () => value2 = 2);
        value2 = 2;
        transaction.Commit(); // Explicitly commit before dispose

        // Assert - should have only one undo action
        Assert.Equal(1, service.UndoHistory.Count);
        Assert.Equal("Combined action", service.UndoDescription);
    }

    [Fact]
    public void Transaction_Undo_RevertsAllActions()
    {
        // Arrange
        var service = new UndoService();
        var value1 = 0;
        var value2 = 0;

        var transaction = service.BeginTransaction("Combined action");
        service.RecordAction("Set value1", () => value1 = 0, () => value1 = 1);
        value1 = 1;
        service.RecordAction("Set value2", () => value2 = 0, () => value2 = 2);
        value2 = 2;
        transaction.Commit(); // Explicitly commit before undoing

        // Act
        service.Undo();

        // Assert - both values should be reverted
        Assert.Equal(0, value1);
        Assert.Equal(0, value2);
    }

    [Fact]
    public void Transaction_Cancel_UndoesAllActionsImmediately()
    {
        // Arrange
        var service = new UndoService();
        var value1 = 0;
        var value2 = 0;

        var transaction = service.BeginTransaction("Combined action");
        service.RecordAction("Set value1", () => value1 = 0, () => value1 = 1);
        value1 = 1;
        service.RecordAction("Set value2", () => value2 = 0, () => value2 = 2);
        value2 = 2;

        // Act
        transaction.Cancel();

        // Assert - values should be reverted and no undo history added
        Assert.Equal(0, value1);
        Assert.Equal(0, value2);
        Assert.False(service.CanUndo);
    }

    [Fact]
    public void IsInTransaction_TrueWhenTransactionActive()
    {
        // Arrange
        var service = new UndoService();

        // Act & Assert
        Assert.False(service.IsInTransaction);

        var transaction = service.BeginTransaction("Test");
        Assert.True(service.IsInTransaction);
        transaction.Commit(); // Explicitly commit to end transaction

        Assert.False(service.IsInTransaction);
    }

    [Fact]
    public void MaxHistorySize_LimitsUndoStack()
    {
        // Arrange
        var service = new UndoService(maxHistorySize: 5);

        // Act
        for (int i = 0; i < 10; i++)
        {
            service.RecordAction($"Action {i}", () => { }, () => { });
        }

        // Assert - should only keep 5 most recent
        Assert.Equal(5, service.UndoHistory.Count);
    }

    [Fact]
    public void UndoMultiple_UndoesSpecifiedCount()
    {
        // Arrange
        var service = new UndoService();
        var value = 0;
        service.RecordAction("Set to 1", () => value--, () => value++);
        value++;
        service.RecordAction("Set to 2", () => value--, () => value++);
        value++;
        service.RecordAction("Set to 3", () => value--, () => value++);
        value++;

        Assert.Equal(3, value);

        // Act
        service.UndoMultiple(2);

        // Assert
        Assert.Equal(1, value);
        Assert.Equal(1, service.UndoHistory.Count);
        Assert.Equal(2, service.RedoHistory.Count);
    }

    [Fact]
    public void RedoMultiple_RedoesSpecifiedCount()
    {
        // Arrange
        var service = new UndoService();
        var value = 0;
        service.RecordAction("Set to 1", () => value--, () => value++);
        value++;
        service.RecordAction("Set to 2", () => value--, () => value++);
        value++;
        service.RecordAction("Set to 3", () => value--, () => value++);
        value++;

        service.UndoMultiple(3);
        Assert.Equal(0, value);

        // Act
        service.RedoMultiple(2);

        // Assert
        Assert.Equal(2, value);
    }

    [Fact]
    public void PropertyChangeAction_UndosAndRedosPropertyValue()
    {
        // Arrange
        var service = new UndoService();
        var target = new TestPropertyTarget { Name = "Original" };

        service.RecordPropertyChange(target, nameof(TestPropertyTarget.Name), "Original", "Changed");
        target.Name = "Changed";

        // Act
        service.Undo();

        // Assert
        Assert.Equal("Original", target.Name);

        // Act
        service.Redo();

        // Assert
        Assert.Equal("Changed", target.Name);
    }

    [Fact]
    public void CollectionUndoAction_Add_UndoRemovesItem()
    {
        // Arrange
        var service = new UndoService();
        var list = new List<string> { "A", "B" };

        list.Add("C");
        service.RecordAdd(list, "C", "Add C");

        // Act
        service.Undo();

        // Assert
        Assert.Equal(2, list.Count);
        Assert.DoesNotContain("C", list);
    }

    [Fact]
    public void CollectionUndoAction_Remove_UndoRestoresItem()
    {
        // Arrange
        var service = new UndoService();
        var list = new List<string> { "A", "B", "C" };

        var indexOfB = list.IndexOf("B");
        service.RecordRemove(list, "B", "Remove B");
        list.Remove("B");

        // Act
        service.Undo();

        // Assert
        Assert.Equal(3, list.Count);
        Assert.Contains("B", list);
    }

    [Fact]
    public void StateChanged_EventFiredOnUndoRedo()
    {
        // Arrange
        var service = new UndoService();
        var eventCount = 0;
        service.StateChanged += (s, e) => eventCount++;

        // Act
        service.RecordAction("Test", () => { }, () => { });
        service.Undo();
        service.Redo();

        // Assert - should fire on record, undo, and redo
        Assert.Equal(3, eventCount);
    }

    [Fact]
    public void ActionExecuted_EventFiredWithCorrectArgs()
    {
        // Arrange
        var service = new UndoService();
        IUndoAction? executedAction = null;
        bool? wasUndo = null;

        service.ActionExecuted += (s, e) =>
        {
            executedAction = e.Action;
            wasUndo = e.IsUndo;
        };

        service.RecordAction("Test action", () => { }, () => { });

        // Act - Undo
        service.Undo();

        // Assert
        Assert.NotNull(executedAction);
        Assert.Equal("Test action", executedAction.Description);
        Assert.True(wasUndo);

        // Act - Redo
        service.Redo();

        // Assert
        Assert.False(wasUndo);
    }

    [Fact]
    public void EmptyTransaction_DoesNotAddToHistory()
    {
        // Arrange
        var service = new UndoService();

        // Act - empty transaction
        var transaction = service.BeginTransaction("Empty");
        // No actions recorded
        transaction.Commit(); // Explicitly commit

        // Assert
        Assert.False(service.CanUndo);
        Assert.Empty(service.UndoHistory);
    }

    [Fact]
    public void SingleActionTransaction_DoesNotWrapInComposite()
    {
        // Arrange
        var service = new UndoService();

        // Act
        var transaction = service.BeginTransaction("Single");
        service.RecordAction("Inner action", () => { }, () => { });
        transaction.Commit(); // Explicitly commit

        // Assert - should show the inner description, not composite
        Assert.Equal(1, service.UndoHistory.Count);
    }

    [Fact]
    public void NestedTransaction_ReturnsExistingTransaction()
    {
        // Arrange
        var service = new UndoService();

        // Act
        var outer = service.BeginTransaction("Outer");
        var inner = service.BeginTransaction("Inner");

        // Assert - should be same transaction
        Assert.Same(outer, inner);

        outer.Commit(); // Cleanup
    }

    [Fact]
    public void Dispose_ClearsHistory()
    {
        // Arrange
        var service = new UndoService();
        service.RecordAction("Test", () => { }, () => { });

        // Act
        service.Dispose();

        // Assert
        Assert.False(service.CanUndo);
    }

    private class TestPropertyTarget
    {
        public string Name { get; set; } = "";
    }
}
