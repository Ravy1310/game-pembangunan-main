using System;
using System.Collections.Generic;

namespace CozyGlade.Core
{
    public class CommandHistory
    {
        private readonly Stack<ICommand> _undoStack = new();
        private readonly Stack<ICommand> _redoStack = new();

        public event Action OnHistoryChanged;

        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;
        public int UndoCount => _undoStack.Count;
        public int RedoCount => _redoStack.Count;

        public void Execute(ICommand command)
        {
            if (command == null) return;

            command.Do();
            _undoStack.Push(command);
            _redoStack.Clear();
            OnHistoryChanged?.Invoke();
        }

        public bool Undo()
        {
            if (!CanUndo) return false;

            var command = _undoStack.Pop();
            command.Undo();
            _redoStack.Push(command);
            OnHistoryChanged?.Invoke();
            return true;
        }

        public bool Redo()
        {
            if (!CanRedo) return false;

            var command = _redoStack.Pop();
            command.Do();
            _undoStack.Push(command);
            OnHistoryChanged?.Invoke();
            return true;
        }

        public void Clear()
        {
            _undoStack.Clear();
            _redoStack.Clear();
            OnHistoryChanged?.Invoke();
        }
    }
}
