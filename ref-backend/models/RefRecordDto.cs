namespace ref_backend.models;

public class RefRecordDto
{
    public int Id { get; set; }
    public string Title { get; set; }
    public List<string> Creator { get; set; } = new List<string>();
    public string Date { get; set; }
    public string Publisher { get; set; }
    public string CustomerId { get; set; }
    
    public RefRecordDto() { }

    public RefRecordDto(int id, string title, List<string> creators, string date, string publisher, string customerId)
    {
        Title = title;
        Creator = creators;
        Date = date;
        Publisher = publisher;
        CustomerId = customerId;
    }
    
    public RefRecordDto(RefRecord record)
    {
        Title = record.Title;
        Creator = record.Creators;
        Date = record.Date;
        Publisher = record.Publisher;
        CustomerId = record.CustomerId;
    }
}