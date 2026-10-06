namespace IE.Controller.Undo;

public interface IUndoEntry
{
    void Undo();
    void Redo();
}
