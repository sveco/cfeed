namespace cFeed.Entities
{
  /// <summary>
  /// Used to import OPML xml as feed list
  /// </summary>
  public class Outline
  {
    private string[] _tags = { };

    public string FeedUrl { get; set; }
    public string[] Tags { get => _tags; set => _tags = value; }
    public string Title { get; set; }
  }
}