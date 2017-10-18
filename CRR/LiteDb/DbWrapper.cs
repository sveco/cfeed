using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using cFeed.Entities;
using LiteDB;

namespace cFeed.LiteDb
{
  public class DbWrapper
  {
    private readonly object padlock;
    LiteDatabase db;
    #region Singleton implementation
    private static readonly DbWrapper instance = new DbWrapper();

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

    public static DbWrapper Instance
    {
      get { return instance; }
    }
    #endregion

    private LiteCollection<FeedItem> items;
    private LiteCollection<FeedItem> Items {
      get
      {
        return items;
      }
    }

    internal IEnumerable<FeedItem> Find(Expression<Func<FeedItem, bool>> predicate)
    {
      //lock (padlock)
      //{
        return items.Find(predicate);
      //}
    }

    internal void Update(FeedItem result)
    {
      lock (padlock)
      {
        this.Items.Update(result);
      }
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

    internal void Purge(string feedUrl)
    {
      lock (padlock)
      {
        this.Items.Delete(x => x.FeedUrl == feedUrl && x.Deleted == true);
      }
    }
  }
}
