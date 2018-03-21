#r "Microsoft.WindowsAzure.Storage"
using Microsoft.WindowsAzure.Storage.Table;

public class Subscription : TableEntity
{
    public char? Type { get; set; }
    public char? Category { get; set; }
    public char? SubCategory { get; set; }
}