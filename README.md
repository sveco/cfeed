# Console Feed Reader
Readme file

## About
Cfeed (formerly CRR) is purely textual, console based RSS and Atom feed reader for .net. It uses System.ServiceModel.Syndication namespace to read RSS 2.0 or Atom 1.0 feeds and HtmlAgilityPack to render textual article content.

This project was inspierd by wonderfull Newsbeuter and lack of similar tool for Windows platform.

## Basic Usage
Before running cfeed for the first time, you have to do some basic configuration. The only required configuration consists of list of URL's of RSS or Atom feeds.
See [settings.conf](https://github.com/sveco/CRR/blob/master/CRR/settings.conf) for example configuration. You can remove everything except the **Feeds** section. The only required property in **Feeds** colleciton is **FeedUrl**. Minimal *settings.conf* looks like this:

```json
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
TBD

## Acknowledgments
Big thanks to awesome newsbeuter team for inspiration. This app is built from scratch, and do not use any portion
of newsbeuter code. This is open source project to provide windows users with purely textual Atom and RSS feed reader.

This app uses [JsonConfig](https://github.com/Dynalon/JsonConfig) to parse configuration files.
HtmlAgilityPack is used to parse article content.
LiteDb is used as local storage.

### Todos
+ Write unit tests
+ Add more features
+ Get some rest

License
----

MIT 
