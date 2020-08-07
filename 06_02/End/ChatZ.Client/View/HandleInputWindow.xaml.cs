using System.Windows;

namespace ChatZ.Client
{
  /// <summary>
  /// Interaction logic for HandleInputWindow.xaml
  /// </summary>
  public partial class HandleInputWindow : Window
  {
    public string Handle { get; private set; }

    public HandleInputWindow() : base()
    { 
      InitializeComponent();
    }

    private void Okay_Click(object sender, RoutedEventArgs e)
    {
      // validate user input and pass to caller
      if (!string.IsNullOrWhiteSpace(Input.Text))
      { 
        Handle        = Input.Text;
        DialogResult  = true;
  
      }
    }
  
    private void  Cancel_Click(object sender, RoutedEventArgs e)
    { 
      // user chooses to abort app by Not providing a handle
      Handle       = null;
      DialogResult = false;  
    }
  }
}
