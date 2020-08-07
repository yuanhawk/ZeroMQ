using System.Collections.Specialized;
using System.Windows.Controls;

namespace ChatZ.Client
{
  /// <summary>
  /// A sub-class of ListBox which always scrolls to the bottom when new content is appended
  /// </summary>
  public class ScrollBottomListBox : ListBox
  {
    protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
    {
      base.OnItemsChanged(e);

      if (e.NewItems?.Count > 0)
      {
        UpdateLayout();
        Items.MoveCurrentToLast();
        ScrollIntoView(Items.CurrentItem);
        UpdateLayout();
      }
    }
  }
}
