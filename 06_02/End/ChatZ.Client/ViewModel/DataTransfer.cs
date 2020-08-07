namespace ChatZ.Client
{
  using date = System.DateTimeOffset;

  public sealed class ActiveUser
  {
    public ActiveUser(string userHandle, string displayName)
    { 
      UserHandle   = userHandle;
      DisplayName  = displayName;
    }

    public string UserHandle  { get; }
    public string DisplayName { get; }
  }

  public sealed class ChatUpdate
  {
    public ChatUpdate(date timestamp, string sender, string content, bool isPrivate = false)
    { 
      IsPrivate = isPrivate;
      Timestamp = timestamp;
      Sender    = sender;
      Content   = content;
    }           

    public date   Timestamp { get; }
    public string Sender    { get; }
    public string Content   { get; }
    public bool   IsPrivate { get; }
    public bool   IsPublic  { get => !IsPrivate; }
  }
}
