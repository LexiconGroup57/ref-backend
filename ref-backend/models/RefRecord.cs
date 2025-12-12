namespace ref_backend.models;

public class RefRecord
{
    public int Id { get; set; }
    public string Title { get; set; }
    public List<string> Creators { get; set; } = new List<string>();
    public string Date { get; set; }
    public string Publisher { get; set; }
    public string CustomerId { get; set; }

    public RefRecord() { }

    public RefRecord(int id, string title, List<string> creators, string date, string publisher, string customerId)
    {
        Title = title;
        Creators = creators;
        Date = date;
        Publisher = publisher;
        CustomerId = customerId;
    }
    
    public RefRecord(RefRecord record)
    {
        Title = record.Title;
        Creators = record.Creators;
        Date = record.Date;
        Publisher = record.Publisher;
        CustomerId = record.CustomerId;
    }
}