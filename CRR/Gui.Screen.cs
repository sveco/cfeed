using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CRR.Gui
{
    public class ScreenEx
    {
        public int HasTitle { get; private set; }
        public bool HasStatus { get; private set; }

        public string Title;
        public string Status;

        private MenuEx _menu;
        public MenuEx Menu {
            get { return _menu; }
            set {
                _menu = value;
                _menu.Parent = this;
            }
        }

        private void setColor(ConsoleColor foregroundColor)
        {
            Console.ForegroundColor = foregroundColor;
        }

        private void setColor(ConsoleColor foregroundColor, ConsoleColor backgroundColor)
        {
            Console.ForegroundColor = foregroundColor;
            Console.BackgroundColor = backgroundColor;
        }

        private void resetColor()
        {
            Console.ForegroundColor = Config.DefaultForeground;
            Console.BackgroundColor = Config.DefaultBackground;
        }

        internal void Clear()
        {
            Console.Clear();
        }

        //public MenuItem Menu(IList<MenuItem> menuItems)
        //{
        //    return Menu(menuItems, null, null, defaultForeground, defaultBackground, defaultSelectedForeground, defaultSelectedBackground);
        //}
        //internal MenuItem Menu(List<MenuItem> menuItems, Action<List<IUpdatable>> backgroundLoad, List<IUpdatable> updatable)
        //{
        //    return Menu(menuItems, backgroundLoad, updatable, defaultForeground, defaultBackground, defaultSelectedForeground, defaultSelectedBackground);
        //}
        /*
        public MenuItem Menu(IList<MenuItem> items,
            Action<List<IUpdatable>> backgroundLoad,
            List<IUpdatable> updatable,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor,
            ConsoleColor selectedForegroundColor, 
            ConsoleColor selectedBackgroundColor)
        {
            Console.Clear();
            UpdateStatus(this.Status);
            UpdateTitle(this.Title);
            Console.CursorVisible = false;

            int count = 0;
            int selectedItem = HasTitle;
            int prevItem = selectedItem;
            int displayedCount = 0;

            foreach (MenuItem feedItem in items)
            {
                count++;
                if (count < Console.WindowHeight)
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
            backgroundLoad?.Invoke(updatable);

            var k = Console.ReadKey(true);

            do {
                //handle keys
                switch (k.Key)
                {
                    case ConsoleKey.DownArrow:
                        {
                            prevItem = selectedItem;
                            if (selectedItem + 1 > displayedCount)
                            {
                                selectedItem = HasTitle;
                            } else {
                                selectedItem++;
                            }
                        }
                        break;
                    case ConsoleKey.UpArrow:
                        {
                            prevItem = selectedItem;
                            if (selectedItem - 1 < (HasTitle))
                            {
                                selectedItem = displayedCount;
                            }
                            else
                            {
                                selectedItem--;
                            }
                        }
                        break;
                    case ConsoleKey.Enter:
                        return items[selectedItem - HasTitle];

                    case ConsoleKey.Escape:
                        return null;
                }

                //render selection
                Console.SetCursorPosition(0, prevItem);
                Console.ForegroundColor = foregroundColor;
                Console.BackgroundColor = backgroundColor;
                Console.WriteLine(items[prevItem-1].DisplayText);

                Console.SetCursorPosition(0, selectedItem);
                Console.ForegroundColor = selectedForegroundColor;
                Console.BackgroundColor = selectedBackgroundColor;
                Console.WriteLine(items[selectedItem-1].DisplayText);

                Console.ForegroundColor = foregroundColor;
                Console.BackgroundColor = backgroundColor;

                k = Console.ReadKey(true);

            } while (true);


            //return -1;
        }
        */
        public void UpdateStatus()
        {
            var prevCursorPos = new Tuple<int, int>(Console.CursorLeft, Console.CursorTop);
            Console.SetCursorPosition(0, Console.WindowHeight - 1);
            Console.WriteLine(this.Status);
            Console.SetCursorPosition(prevCursorPos.Item1, prevCursorPos.Item2);
        }
        public void UpdateStatus(string status)
        {
            HasStatus = true;
            Status = status;
            UpdateStatus();
        }
        public void UpdateTitle()
        {
            Console.SetCursorPosition(0, 0);
            Console.WriteLine(this.Title);
        }
        public void UpdateTitle(string title)
        {
            HasTitle = 1;
            Title = title;
        }

        public void UpdateContent(string content)
        {
            Console.SetCursorPosition(0, HasTitle);
            Console.WriteLine(content);
        }

        public void DisplayContent(string Title, string Content) {
            Console.Clear();
            this.UpdateStatus("BACKSPACE - Exit");
            this.UpdateTitle(Title);
            this.UpdateContent(Content);
            var k = Console.ReadKey(true);

            switch (k.Key)
            {
                case ConsoleKey.Backspace:
                    return;
            }
        }
    }
}
