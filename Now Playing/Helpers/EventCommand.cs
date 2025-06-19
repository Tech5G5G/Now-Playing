namespace Now_Playing.Helpers;

public class EventCommand : ICommand
{
    public event EventCommandExecutedEventHandler Executed;

    public void Execute(object parameter) => Executed?.Invoke(this, parameter);

    public event EventHandler CanExecuteChanged;

    public bool CanExecute(object parameter) => true;
}

public delegate void EventCommandExecutedEventHandler(EventCommand sender, object args);
