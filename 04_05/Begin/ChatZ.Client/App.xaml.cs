using System.Threading;
using System.Windows;

using static ChatZ.Client.Properties.Settings;

namespace ChatZ.Client
{
  public partial class App : Application
  {
    // will be used to coordinate with long-running background tasks
    private static readonly CancellationTokenSource cts = new CancellationTokenSource();

    private static (bool OK, string handle) PromptUserForHandle()
    { 
      var editor = new HandleInputWindow();
      var result = editor.ShowDialog();
      return (result.HasValue && result.Value) ? (true, editor.Handle) : (false, null);
    }

    protected override void OnStartup(StartupEventArgs e)
    {
      // temporarily take control
      Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;
      // prompt user for handle
      var (OK, handle) = PromptUserForHandle();
      // if user declined to enter a handle
      if (!OK) 
      { 
        Current.Shutdown(); 
        return; /* early exit */  
      }
      // else we have a valid handle, so...

      // relinquish control
      Current.ShutdownMode  = ShutdownMode.OnMainWindowClose;
      // configure main window and view model
      var control = string.Format($"{Default.HostAddress}:{Default.ControlPort}");
      var publish = string.Format($"{Default.HostAddress}:{Default.PublishPort}");
      var config  = new Proxy.Config(control, publish, (int) Default.IdleMilliseconds, handle);
      Current.MainWindow = new MainWindow() 
      { 
        DataContext = new MainWindowViewModel(config, cts.Token) 
      };
      // run application
      Current.MainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e) 
    {
      // shut down any long-running background tasks
      cts.Cancel();
      cts.Token.WaitHandle.WaitOne();
      base.OnExit(e);
    }
  }
}
