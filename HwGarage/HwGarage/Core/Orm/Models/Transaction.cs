namespace HwGarage.Core.Orm.Models;

public class Transaction
{
    [Column("id")]
    public int Id { get; set; }
    [Column("buyer_id")]
    public int Buyer_Id { get; set; }
    [Column("seller_id")]
    public int Seller_Id { get; set; }
    [Column("car_id")]
    public int Car_Id { get; set; }
    [Column("sale_price")]
    public decimal Sale_Price { get; set; }
    [Column("auction_id")]
    public int? Auction_Id { get; set; }
    [Column("listing_id")]
    public int? Listing_Id { get; set; }
    [Column("completed_at")]
    public DateTime Completed_At { get; set; }
}