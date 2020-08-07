using System;
using System.Windows.Input;

namespace ChatZ.Client
{
  /// <summary>
  /// Generic command that delegates behavior to functions provided at construction
  /// </summary>
  public sealed class RelayCommand : ICommand
  {
    private readonly Action<object>     execute_;
    private readonly Func<object,bool>  canExecute_;

    public RelayCommand(Action<object> execute, Func<object,bool> canExecute = null)
    { 
      this.execute_    = execute;
      this.canExecute_ = canExecute;
    }

#pragma warning disable 0067
      /*
      :: NOTE ::    
      we're fixing the "can execute" mechanics at construction; 
      so this event will never get raised
      */
    public event EventHandler CanExecuteChanged;
#pragma warning restore 0067

    public bool CanExecute(object parameter) 
      => (this.canExecute_ == null) ? true : this.canExecute_(parameter);

    public void Execute(object parameter) => this.execute_(parameter);
  }
}
