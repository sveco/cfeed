using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cFeed.Util
{
  public static class ConsoleKeyInfoExtansions
  {
    internal static Collection<ConsoleKey> GetKeys(string[] keys)
    {
      Collection<ConsoleKey> result = new Collection<ConsoleKey>();
      foreach (var key in keys)
      {
        if (Enum.TryParse(key, out ConsoleKey k))
        {
          result.Add(k);
        }
        else
        {
          throw new ArgumentException("Unknow color name, see https://msdn.microsoft.com/en-us/library/system.consolekey(v=vs.110).aspx for valid key names.");
        }
      }
      return result;
    }

    public static ConsoleModifiers GetModifier(string modifier)
    {
      ConsoleModifiers result = new ConsoleModifiers();
      if (Enum.TryParse(modifier, out result))
      {
        return result;
      }
      else
      {
        throw new ArgumentException("Unknow color name, see https://msdn.microsoft.com/en-us/library/system.consolekey(v=vs.110).aspx for valid key names.");
      }
    }

    public static ConsoleModifiers GetBitwiseModifiers(string[] modifiers)
    {
      return modifiers.Select(x => GetModifier(x)).Aggregate<ConsoleModifiers>((running, next) => (running | next));
    }

    public static bool VerifyKey(this ConsoleKeyInfo key, dynamic keyConfig)
    {
      string[] keys = keyConfig.Key;
      string[] modifiers = keyConfig.Modifiers;
      if (GetKeys(keys).Contains(key.Key))
      {
        if (modifiers.Length > 0)
        {
          if ((key.Modifiers == GetBitwiseModifiers(modifiers)))
          {
            return true;
          }
          else { return false; }
        }
        if (key.Modifiers != 0)
        {
          return false;
        }
        return true;
      }
      return false;
    }
  }
}
