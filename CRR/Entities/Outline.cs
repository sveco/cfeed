namespace cFeed.Entities
{
  public class Outline
  {
    private string[] _tags = new string[] { };

    public string Title { get; set; }
    public string FeedUrl { get; set; }
    public string[] Tags { get => _tags; set => _tags = value; }
  }
}
