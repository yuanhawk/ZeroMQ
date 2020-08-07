using ChatZ.Common;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Data;
 
using static ChatZ.Common.Protocol;

namespace ChatZ.Client
{
  public class MainWindowViewModel : INotifyPropertyChanged
  {
    private static readonly ActiveUser Lobby = new ActiveUser(GroupSender, "::Lobby::");
    
    private readonly string handle_;
    
    private string filter_;
    private string input_;

    private readonly ObservableCollection<ChatUpdate> chatItems = new ObservableCollection<ChatUpdate>();
    private readonly ObservableCollection<ActiveUser> userItems = new ObservableCollection<ActiveUser>(new []{ Lobby });

    private void Notify(string property) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
    
    private void UpdateActiveUsers(ServerMessage.List msg)
    { 
      var temp = Filter; // cache current filter to avoid "flickering"

      // remove any clients which are no longer connected
      var drop =  from user in this.userItems
                  where !msg.Users.Contains(user.UserHandle) && user.UserHandle != Lobby.UserHandle
                  select user;
    
      // append any newly arrived cliented
      var push =  from client in msg.Users
                  where !this.userItems.Any(a => a.UserHandle == client)
                  select new ActiveUser(client, client);

      // update actual collection
      foreach (var u in drop.ToList()) { this.userItems.Remove(u); }
      foreach (var u in push.ToList()) { this.userItems.Add(u); }
      
      // re-apply filter
      Filter = temp;
    }

    private void UpdateChatHistory(ServerMessage.News msg)
    { 
      var isPrivate = (msg.Topic != GroupSender);
      var updateMsg = new ChatUpdate(msg.Stamp, msg.Sender, msg.Detail, isPrivate);
      this.chatItems.Add(updateMsg); 
    }

    public MainWindowViewModel(Proxy.Config proxyConfig, CancellationToken token)
    {
      this.handle_  = proxyConfig.Handle;
      this.filter_  = Lobby.UserHandle;
      this.input_   = string.Empty;

      this.chatItems.CollectionChanged += (sender, e) => { Notify(nameof(ChatUpdate)); };
      this.userItems.CollectionChanged += (sender, e) => { Notify(nameof(ActiveUser )); };
      
      ChatMessages.SortDescriptions.Add(new SortDescription("UserHandle", ListSortDirection.Ascending));
      ChatMessages.Filter = (item) => (item is ChatUpdate msg) 
                                   && (msg.IsPrivate && Filter == msg.Sender
                                   ||  msg.IsPublic  && Filter == GroupSender);
      
      Proxy.Start(proxyConfig, token);
    }

    public event PropertyChangedEventHandler PropertyChanged;
    
    public ICollectionView ChatMessages { get => CollectionViewSource.GetDefaultView(this.chatItems); }
    
    public ICollectionView ActiveUsers  { get => CollectionViewSource.GetDefaultView(this.userItems); }
    
    public string Handle { get => this.handle_; }

    public string Filter 
    {
      get => this.filter_;
      set
      { 
        this.filter_ = value;
        ChatMessages.Refresh();
        Notify(nameof(Filter));
      }
    }

    public string Input 
    {
      get => this.input_;
      set
      { 
        this.input_ = value;
        Notify(nameof(Input));
      }
    }
    
    public RelayCommand SendMessage
    {
      get => new RelayCommand((_) => {}, canExecute: (_) => false);
    }
  }
}
