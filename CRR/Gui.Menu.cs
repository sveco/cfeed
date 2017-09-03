using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CRR.Gui
{
    public class MenuEx
    {
        public ScreenEx Parent;

        IList<MenuItem> Items = new List<MenuItem>();
        Action<List<IUpdatable>> BackgroundLoad = null;
        List<IUpdatable> Updatable = null;

        ConsoleColor selectedForegroundColor = Config.DefaultSelectedForeground;
        ConsoleColor selectedBackgroundColor = Config.DefaultSelectedBackground;
        ConsoleColor foregroundColor = Config.DefaultForeground;
        ConsoleColor backgroundColor = Config.DefaultBackground;

        int Offset = 0;

        int HasTitle { get { return Parent.Title.Length > 0 ? 1 : 0; } }
        int HasStatus { get { return Parent.Status.Length > 0 ? 1 : 0; } }

        int SelectedItem;

        int MaxItems { get { return Console.WindowHeight - HasStatus - HasTitle; } }

        public MenuEx(IList<MenuItem> menuItems)
        {
            this.Items = menuItems;
        }
        public MenuEx(List<MenuItem> menuItems, Action<List<IUpdatable>> backgroundLoad, List<IUpdatable> updatable)
        {
            this.Items = menuItems;
            this.BackgroundLoad = backgroundLoad;
            this.Updatable = updatable;
        }
        public void ScrollDown() {
            Offset++;
            Parent.Clear();
            Parent.UpdateStatus();
            Parent.UpdateTitle();
            Console.CursorVisible = false;
            int displayedCount = 0;
            for (int i = Offset; i < Console.WindowHeight - HasTitle - HasStatus; i++)
            {
                if (displayedCount == SelectedItem)
                {
                    Console.ForegroundColor = selectedForegroundColor;
                    Console.BackgroundColor = selectedBackgroundColor;
                }
                Console.WriteLine(Items[i+1].DisplayText);
                if (displayedCount == SelectedItem)
                {
                    Console.ForegroundColor = foregroundColor;
                    Console.BackgroundColor = backgroundColor;
                }
            }
        }

        public void ScrollUp()
        {
            Offset--;
            Parent.Clear();
            Parent.UpdateStatus();
            Parent.UpdateTitle();
            Console.CursorVisible = false;
            int displayedCount = 0;
            for (int i = Offset; i < Console.WindowHeight - HasTitle - HasStatus; i++)
            {
                if (displayedCount == SelectedItem)
                {
                    Console.ForegroundColor = selectedForegroundColor;
                    Console.BackgroundColor = selectedBackgroundColor;
                }
                Console.WriteLine(Items[i].DisplayText);
                if (displayedCount == SelectedItem)
                {
                    Console.ForegroundColor = foregroundColor;
                    Console.BackgroundColor = backgroundColor;
                }
            }
        }

        public void UpdateItem(int index, string text)
        {
            Items[index].DisplayText = text;
            Console.SetCursorPosition(0, (index + HasTitle));
            if (SelectedItem == index + HasTitle)
            {
                Console.ForegroundColor = selectedForegroundColor;
                Console.BackgroundColor = selectedBackgroundColor;
            }
            Console.WriteLine(text.PadRight(Console.WindowWidth));
            Console.ForegroundColor = foregroundColor;
            Console.BackgroundColor = backgroundColor;
        }
        public MenuItem Show()
        {
            Parent.Clear();
            Parent.UpdateStatus();
            Parent.UpdateTitle();
            Console.CursorVisible = false;

            int count = 0;
            SelectedItem = HasTitle;
            int prevItem = SelectedItem;
            int displayedCount = 0;

            foreach (MenuItem feedItem in Items)
            {
                count++;
                if (count < MaxItems)
                {
                    displayedCount++;
                    if (displayedCount == 1)
                    {
                        Console.ForegroundColor = selectedForegroundColor;
                        Console.BackgroundColor = selectedBackgroundColor;
                    }
                    Console.WriteLine(feedItem.DisplayText);
                    if (displayedCount == 1)
                    {
                        Console.ForegroundColor = foregroundColor;
                        Console.BackgroundColor = backgroundColor;
                    }
                }
            }

            //load feeds in paralell here?
            BackgroundLoad?.Invoke(Updatable);

            var k = Console.ReadKey(true);

            do
            {
                //handle keys
                switch (k.Key)
                {
                    case ConsoleKey.DownArrow:
                        {
                            prevItem = SelectedItem;
                            if (SelectedItem + 1 > Items.Count())
                            {
                                SelectedItem = HasTitle;
                            }
                            else if (SelectedItem + 1 > MaxItems)
                            {
                                ScrollDown();
                            }
                            else
                            {
                                SelectedItem++;
                            }
                        }
                        break;
                    case ConsoleKey.UpArrow:
                        {
                            prevItem = SelectedItem;
                            if (SelectedItem - 1 < (HasTitle))
                            {
                                SelectedItem = displayedCount;
                            }
                            else
                            {
                                SelectedItem--;
                            }
                        }
                        break;
                    case ConsoleKey.Enter:
                        return Items[SelectedItem - HasTitle];
                    case ConsoleKey.O:
                        System.Diagnostics.Process.Start(Items[SelectedItem - HasTitle].Link.ToString());
                        break;
                    case ConsoleKey.Escape:
                    case ConsoleKey.Backspace:
                        return null;
                }

                //render selection
                Console.SetCursorPosition(0, prevItem);
                Console.ForegroundColor = foregroundColor;
                Console.BackgroundColor = backgroundColor;
                Console.WriteLine(Items[prevItem - 1].DisplayText.PadRight(Console.WindowWidth));

                Console.SetCursorPosition(0, SelectedItem);
                Console.ForegroundColor = selectedForegroundColor;
                Console.BackgroundColor = selectedBackgroundColor;
                Console.WriteLine(Items[SelectedItem - 1].DisplayText.PadRight(Console.WindowWidth));

                Console.ForegroundColor = foregroundColor;
                Console.BackgroundColor = backgroundColor;

                k = Console.ReadKey(true);

            } while (true);
        }
    }
}
