namespace HwGarage.Core.Orm.Models;

public class MarketListing
{
    [Column("id")]
    public int Id { get; set; }
    [Column("car_id")]
    public int Car_Id { get; set; }    
    [Column("seller_id")]
    public int Seller_Id { get; set; }
    [Column("price")]
    public decimal Price { get; set; }
    [Column("status")]
    public string Status { get; set; } = "active"; // active | sold | cancelled
    [Column("created_at")]
    public DateTime Created_At { get; set; }
}