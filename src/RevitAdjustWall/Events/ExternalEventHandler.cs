using System;
using Autodesk.Revit.UI;
using RevitAdjustWall.Commands;

public class ExternalEventHandler : IExternalEventHandler
{
    private string? _identifier;
    private readonly ExternalEvent _externalEvent;
    private Action<UIApplication>? _action;

    /// <summary>
    ///     Creates an instance of external event.
    /// </summary>
    public ExternalEventHandler()
    {
        _externalEvent = ExternalEvent.Create(this);
    }

    public void Execute(UIApplication uiApplication)
    {
        if (_action is null) return;

        try
        {
            _action(uiApplication);
        }
        finally
        {
            _action = null;
        }
    }

    public string GetName()
    {
        return _identifier ??= GetType().Name;
    }
    
    public void Raise(Action<UIApplication> action)
    {
        if (AdjustWallCommand.Uiapp.Application.ActiveAddInId is not null)
        {
            action(AdjustWallCommand.Uiapp);
            return;
        }
        
        if (_action is null) _action = action;
        else _action += action;

        Raise();
    }

    private void Raise()
    {
        _externalEvent.Raise();
    }
}