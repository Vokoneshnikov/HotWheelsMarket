namespace HwGarage.Core.Orm.Models;

public class Car
{
    [Column("id")]
    public int Id { get; set; }
    [Column("owner_id")]
    public int Owner_Id { get; set; }
    [Column("name")]
    public string Name { get; set; } = "";
    [Column("description")]
    public string? Description { get; set; }
    [Column("status")]
    public string Status { get; set; } = "available"; // available | on_sale | sold
    [Column("created_at")]
    public DateTime Created_At { get; set; }
}