namespace HwGarage.Core.Orm.Models;

public class Bid
{
    [Column("id")]
    public int Id { get; set; }
    [Column("auction_id")]
    public int Auction_Id { get; set; }
    [Column("bidder_id")]
    public int Bidder_Id { get; set; }
    [Column("amount")]
    public decimal Amount { get; set; }
    [Column("created_at")]
    public DateTime Created_At { get; set; }
}