using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using cFeed.Entities;
using CGui.Gui;
using CGui.Gui.Primitives;
using JsonConfig;

namespace cFeed.Util
{
  public static class ControlFactory
  {
    public static GuiElement Get(dynamic control)
    {
      if (control is NullExceptionPreventer) { return null; }

      GuiElement guiElement = null;

      if (control.Type is NullExceptionPreventer)
      {
        throw new ArgumentException("Type of control is not defined in control Layout");
      }
      
      Type T = Assembly.Load("CGui").GetTypes().First(t => t.Name == control.Type);
      if (typeof(GuiElement).IsAssignableFrom(T))
      {
        if (T.ContainsGenericParameters)
        {
          Type X = Assembly.GetCallingAssembly().GetTypes().First(t => t.Name == control.ItemType);
          Type Y = T.MakeGenericType(X);
          guiElement = Activator.CreateInstance(Y, new object[] { }) as GuiElement;
        }
        else
        {
          guiElement = Activator.CreateInstance(T, new object[] { }) as GuiElement;
        }

        foreach (var propertyName in control.Keys)
        {
          if (propertyName == "Type") continue;
          PropertyInfo prop = guiElement.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
          if (null != prop && prop.CanWrite)
          {
            if (prop.PropertyType == typeof(ConsoleColor))
            {
              prop.SetValue(guiElement, Configuration.GetColor(control[propertyName]), null);
            }
            else
            {
              prop.SetValue(guiElement, Convert.ChangeType(control[propertyName], prop.PropertyType), null);
            }
          }
        }
      }
      
      return guiElement;
    }
  }
}
