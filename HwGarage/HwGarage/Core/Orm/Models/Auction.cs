namespace HwGarage.Core.Orm.Models;

public class Auction
{
    [Column("id")]
    public int Id { get; set; }
    [Column("car_id")]
    public int Car_Id { get; set; }
    [Column("seller_id")]
    public int Seller_Id { get; set; }
    [Column("start_price")]
    public decimal Start_Price { get; set; }
    [Column("bid_step")]
    public decimal Bid_Step { get; set; }
    [Column("current_bid")]
    public decimal Current_Bid { get; set; }
    [Column("current_bidder_id")]
    public int? Current_Bidder_Id { get; set; }
    [Column("status")]
    public string Status { get; set; } = "active"; // active | finished | cancelled
    [Column("started_at")]
    public DateTime Started_At { get; set; }
    [Column("ends_at")]
    public DateTime Ends_At { get; set; }
}