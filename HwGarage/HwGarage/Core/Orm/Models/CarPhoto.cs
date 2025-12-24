namespace HwGarage.Core.Orm.Models;

public class CarPhoto
{
    [Column("id")]
    public int Id { get; set; }
    [Column("car_id")]
    public int Car_Id { get; set; }
    [Column("photo_url")]
    public string Photo_Url { get; set; } = "";
    [Column("uploaded_at")]
    public DateTime Uploaded_At { get; set; }
}