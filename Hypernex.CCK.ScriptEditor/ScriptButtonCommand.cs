using System;
using System.Windows.Input;

namespace Hypernex.CCK.ScriptEditor;

public class ScriptButtonCommand : ICommand
{
    public bool CanExecute(object? parameter) => true;

    private Action OnShow;

    public ScriptButtonCommand(Action OnShow) => this.OnShow = OnShow;

    public void Execute(object? parameter) => OnShow.Invoke();

    public event EventHandler? CanExecuteChanged = (sender, args) => { };
}