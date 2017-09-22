# Console Feed Reader
Readme file

## About
Cfeed (formerly CRR) is purely textual, console based RSS and Atom feed reader for Windows platform built in C#. It uses System.ServiceModel.Syndication namespace to read RSS 2.0 or Atom 1.0 feeds and HtmlAgilityPack to render textual article content.

This project was inspierd by wonderfull Newsbeuter and a lack of similar tool for Windows platform.

## Basic Usage
Before running cfeed for the first time, you have to do some basic configuration. The only required configuration consists of list of URL's of RSS or Atom feeds.
See [settings.conf](https://github.com/sveco/CRR/blob/master/CRR/settings.conf) for example configuration. You can remove everything except the **Feeds** section. The only required property in **Feeds** colleciton is **FeedUrl**. Minimal *settings.conf* looks like this:

```
{
    Feeds: [{FeedUrl: "http://feeds.newscientist.com/"}]
}
```

Any setting in embedded *default.conf* can be overridden in *settings.conf* placed in application root folder.

More about config in next section.

After setting feeds in config file, you can run the app. Application will list configured feeds, and refresh the feed contents in background.

By default, you can open the feed with **Spacebar** or **Enter** key. This will list articles in the feed. Hitting same kays again will open selected feed item. Use **Arrow Up**, **Arrow Down**, **Page Up** and **Page Down** keys to navigate lists. By default return to previous screen using **Backspace** or **Escape** (can be configured differently in settings).

Hitting **R** while on list of feeds will refresh selected feed, and **Control+R** will refresh all feeds."

When on article, article content will be loaded on background. Hitting **O** will open selected article in default (or configured) browser.

![Article List](screenshot1.png "Article List")



## Configuration

Cfeed uses json files to store app configuration. Embedded *default.conf* provides default settings when no other config file is present. User setting are stored in *settings.conf*. Any setting in latter file overrides default settings.
The *settings.conf* also includes list of feeds, but there are plans to move this to separate *feedlist.conf* file.

Structure of JSON object is

Feeds - list of feeds and feed related settings
UI - look & feel of application
Shortcuts - key bindings
App - app related settings, like external browser etc.

***Feeds***

Basic setting.conf can look like this:

```
{
    Feeds: [
    {
        #URL of RSS otr Atom feed
        FeedUrl: "http://feeds.newscientist.com/",
        #Define filters for html id's and classes. Elements from those classes will be ignored when converting html to text. use '.' prefix for classes and # for id's.
        Filters: ["#main-nav", "#breadcrumbs", ".masthead-container",".signpost", ".entry-meta", ".footer", "#mpu-sidebar", ".leaderboard-container", "#registration-barrier", ".entry-form g50"],
        #Custom title to override default feed title
        Title: "New Scientist - Home Custom"
    }
}
```

Only FeedUrl is required, other settings are optional. Filers can be used to "filter" out unwanted content, like page navigation, links, registration forms etc.
To use filter, look source of the page that you want to display. Prepend all "class" attributes of html elements you want to filter out with ".", and all "id" attributes of html elements with "#".
Any content inside filtered elements will not be rendered.
Title can be used to display custom title of feed, instead of the one defined by feed itself.

***Dynamic Feeds***

Yay! It is now supported to filter articles in online feed by defining FeedQuery, or create dynamic feed by specifying FeedQuery without the FeedUrl. Latter will search all downloaded articles, and crete "virtual" feed from search results.

*Example: Dynamic feed*
```
{ 
    #Dynamic feed - find all articles with word 'Mars' in Title or Summary.
    FeedQuery: "(Summary.Contains(\"Mars\") || Title.Contains(\"Mars\"))",
    Title: "Dynamic feed - Mars"
}
```

*Example: Filterd feed*
```
{ 
    #Filtered feed - find all articles in newscientist feed with word 'Mars' in Title or Summary
    FeedUrl: "http://feeds.newscientist.com/",
    FeedQuery: "(Summary.Contains(\"Mars\") || Title.Contains(\"Mars\"))",
    Title: "Online feed - Mars"
}
```

***UI***

UI section of config can be used to customize look and feel of application.

***UI.Strings***

Formatting for various UI elements. Formatting string must start by % followed by specific identifier, dependant on type of element displayed.

Following tables defines identifiers for each UI.String element

Setting | Meaning | Default value
:------------ | :------------- | :------------
**ApplicationTitleFormat** | Application title shown on feeds list| "cfeed v%V - console feed reader"
**FeedListFormat** | defines how the feed title will be displayed in list. | "%i %n [%u] %t"
**ArticleListFormat** | Defines line to show for each article. | "%i %n %d %t"
**FeedTitleFormat** | Title of feed show when articles are listed. | "cFeed v%V - Articles in \'%t\' %u"
**ArticleTitleFormat** | Title shown when article is displayed | "cFeed v%V - Article:%t"
**ReadStateNew** | string to show when feed contains unread items, or article is new. | "[N]"
**ReadStateRead** |  string to show when all items in feed has been read, or article is not new. | "[ ]"
**LoadingSuffix** | Suffix for feed title to show while loading | " - Loading..."
**LoadingPrefix** | Prefix for feed title to show while loading | ""

Replacement strings for *ApplicationTitleFormat*:

String | Meaning
------------ | -------------
%V | Major.Minor version
%v | Full version, Major.Minor.Revision.Build

Replacement strings for *FeedListFormat* and *FeedTitleFormat*:

String | Meaning
------------ | -------------
%i | Feed index
%l | RSS/ATOM feed url
%n | Read state flag (New/Read)
%u | # of unread / total items
%T | # of total items
%U | # of unread items
%t | CustomTitle ?? Title ?? FeedUrl
%V | Major.Minor version
%v | Full version, Major.Minor.Revision.Build

Replacement strings for *ArticleListFormat* and *ArticleTitleFormat*:

String | Meaning
------------ | -------------
%i | Feed index
%l | RSS/ATOM feed url
%n | Read state flag (New/Read)
%d | Article publish date
%t | Article title
%s | Summary


***UI.Colors***

Colors section can be used to define custom color "theme" for the app. List of available UI elements to customize color is in following table.
For a valid list of color names see this [list on MSDN](https://msdn.microsoft.com/en-us/library/system.consolecolor(v=vs.110).aspx).

Setting | Default value
:------------ | :-------------
DefaultForeground | "White"
DefaultBackground | "Black"
DefaultSelectedForeground | "Black"
DefaultSelectedBackground | "DarkYellow"
FeedListHeaderBackground | "DarkCyan"
FeedListHeaderForeground | "Yellow"
FeedListFooterBackground | "DarkCyan"
FeedListFooterForeground | "Yellow"
ArticleListFooterBackground | "DarkCyan"
ArticleListFooterForeground | "Yellow"
ArticleListHeaderBackground | "DarkCyan"
ArticleListHeaderForeground | "Yellow"
ArticleHeaderBackground | "DarkCyan"
ArticleHeaderForeground | "Yellow"
ArticleFooterBackground | "DarkCyan"
ArticleFooterForeground | "Yellow"
ArticleTextHighlight | "Yellow"
LinkHighlight | "DarkCyan

***UI.Layout***

Defines general layout of the application.

Setting | Default value
:------------ | :-------------
FeedListLeft | 2
FeedListTop | 1
FeedMaxItems | 20
ArticleListLeft | 2
ArticleListTop | 1
ArticleListHeight | -3 (this means bottom of article list is 3 rows above console last line)

***Shortcuts***

Defines keyboard keys and modifiers combination that trigger particular action. For a valid list of **[keys see this link](https://msdn.microsoft.com/en-us/library/system.consolekey(v=vs.110).aspx)**.
For a valid list of **[modifiers see here](https://msdn.microsoft.com/en-us/library/system.consolemodifiers(v=vs.110).aspx)**.
Some key combinations are used by windows itself if you enable advanced console features. **[Avoid those combinations](https://technet.microsoft.com/en-us/library/mt427362.aspx)**.

Each Key and Modifiers object can be an array. If you define more than one Key, pressing any of the keys will trigger action. If you define more than one modifier, all modifiers have to be pressed. For example, definiton {Key: ["S,D"], Modifiers: ["Control", "Alt"] } means it will trigger on CTRL+ALT+S or CTRL+ALT+D.

Setting | Key | Action | Scope
:------------ | :------------- | :------------- | :-------------
QuitApp      | { Key: ["Q"] } | Exits the app | Feed list
Reload       | { Key: ["R"] } | Reloads selected feed or article | Feed list, Article list
ReloadAll    | { Key: ["R"], Modifiers: ["Control"]} | Reolad all feeds | Feed list
OpenArticle  | { Key: ["Enter", "Spacebar"] } | Opens selected article | Article list
OpenBrowser  | { Key: ["O"] } | Opens article in default or configured browser | Article list, Article
OpenFeed     | { Key: ["Enter", "Spacebar"] } | Lists articles in selected feed | Feed list
RefreshView  | { Key: ["F"] } | Redraws the UI | Feed list
NextUnread   | { Key: ["N"] } | Opens next unread article | Article
StepBack     | { Key: ["Escape", "Backspace"]} | Navigates back | Feed list, Article list, Article
SaveArticle  | { Key: ["S"] } | Important! Avoid Control+S, console ignores it | Article
MarkRead     | { Key: ["M"] } | Marks selected article as read | Article list
OpenLink     | { Key: ["L"] } | Prompts for link # and opens selected link in browser | Article

***Other settings***

Setting | Description | Default value
:------------ | :---------------------------------------------- | -----------------------
SavedFileName | Format for file name of saved articles          | ".\\saved\\%d\\%t.txt"
Database      | Name of liteDB database used to store metadata  | "cfeed.db" 
Refresh       | Refresh feeds on load                           | true

Replacement strings for *SavedFileName*. File name will be sanitized. Absolute, relative and network locations are supported, just make sure you have access right to write do defined location.

String | Meaning
------------ | -------------
%i | Feed index
%l | RSS/ATOM feed url
%n | Read state flag (New/Read)
%d | Article publish date
%t | Article title

## Acknowledgments
Big thanks to awesome newsbeuter team for inspiration. This app is built from scratch, and does not use any portion
of newsbeuter code. This is open source project to provide windows users with purely textual Atom and RSS feed reader.

+ This app uses [JsonConfig](https://github.com/Dynalon/JsonConfig) to parse configuration files.
+ [HtmlAgilityPack](https://github.com/zzzprojects/html-agility-pack) is used to parse article content.
+ [LiteDb](https://github.com/mbdavid/LiteDB) is used for local storage of article metadata.

### Todos
+ Write unit tests
+ Add more features
+ Get some rest

License
----

MIT 
