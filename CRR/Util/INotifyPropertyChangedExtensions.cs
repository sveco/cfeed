using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace cFeed.Util
{
  public static class INotifyPropertyChangedExtensions
  {
    public static void Notify(
        this INotifyPropertyChanged sender,
        PropertyChangedEventHandler handler,
        [CallerMemberName] string propertyName = "")
    {
      if (handler != null)
      {
        PropertyChangedEventArgs args = new PropertyChangedEventArgs(propertyName);
        handler(sender, args);
      }
    }
  }
}
