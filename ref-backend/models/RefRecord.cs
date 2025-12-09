namespace ref_backend.models;

public class RefRecord
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Creator { get; set; }
    public string Date { get; set; }
    public string Publisher { get; set; }
    public string CustomerId { get; set; }

    public RefRecord() { }

    public RefRecord(int id, string title, string creator, string date, string publisher, string customerId)
    {
        Title = title;
        Creator = creator;
        Date = date;
        Publisher = publisher;
        CustomerId = customerId;
    }
    
    public RefRecord(RefRecord record)
    {
        Title = record.Title;
        Creator = record.Creator;
        Date = record.Date;
        Publisher = record.Publisher;
        CustomerId = record.CustomerId;
    }
}