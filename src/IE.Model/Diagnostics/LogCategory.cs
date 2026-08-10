namespace IE.Model.Diagnostics;

/// <summary>Subsystem a log line belongs to. Levels are set per category.</summary>
public enum LogCategory
{
    Boot,
    Model,
    Controller,
    View,
    Input,
    Undo,
    Save,
}
