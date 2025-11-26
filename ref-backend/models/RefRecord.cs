namespace ref_backend.models;

public class RefRecord
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Creator { get; set; }
    public string Date { get; set; }
    public string Publisher { get; set; }

    public RefRecord() { }

    public RefRecord(int id, string title, string creator, string date, string publisher)
    {
        Id = id;
        Title = title;
        Creator = creator;
        Date = date;
        Publisher = publisher;
    }
    
    public RefRecord(RefRecord record)
    {
        Id = record.Id;
        Title = record.Title;
        Creator = record.Creator;
        Date = record.Date;
        Publisher = record.Publisher;
    }
}