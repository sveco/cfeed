namespace cFeed.LiteDb
{
  using System;
  using System.Collections.Generic;
  using System.Linq.Expressions;
  using cFeed.Entities;
  using LiteDB;

  public class DbWrapper : IDisposable
  {
    readonly object padlock;
    readonly LiteDatabase db;

    static readonly DbWrapper instance = new DbWrapper();

    public static DbWrapper Instance
    {
      get { return instance; }
    }

    // Explicit static constructor to tell C# compiler
    // not to mark type as beforefieldinit
    static DbWrapper()
    {
    }

    private DbWrapper()
    {
      db = new LiteDatabase("Filename=" + Configuration.Database + ";Mode=Exclusive");

      items = db.GetCollection<FeedItem>("items");
      padlock = new object();
    }

    private bool disposedValue = false;
    private LiteCollection<FeedItem> items;

    private LiteCollection<FeedItem> Items
    {
      get
      {
        return items;
      }
    }

    void IDisposable.Dispose()
    {
      Dispose(true);
    }

    internal IEnumerable<FeedItem> Find(Expression<Func<FeedItem, bool>> predicate)
    {
      //lock (padlock)
      //{
      return items.Find(predicate);
      //}
    }

    internal IEnumerable<FeedItem> FindAll()
    {
      //lock (padlock)
      //{
      return this.Items.FindAll();
      //}
    }

    internal void Insert(FeedItem newItem)
    {
      lock (padlock)
      {
        this.Items.Insert(newItem);
      }
    }

    internal void Purge(Uri feedUrl)
    {
      lock (padlock)
      {
        this.Items.Delete(x => x.FeedUrl == feedUrl && x.Deleted == true);
      }
    }

    internal void Update(FeedItem result)
    {
      lock (padlock)
      {
        this.Items.Update(result);
      }
    }

    protected virtual void Dispose(bool disposing)
    {
      if (disposedValue)
        return;

      if (disposing)
      {
        db.Dispose();
      }

      disposedValue = true;
    }
  }
}