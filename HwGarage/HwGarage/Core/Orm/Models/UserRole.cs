namespace HwGarage.Core.Orm.Models;

public class UserRole
{
    [Column("user_id")]
    public int User_Id { get; set; }
    [Column("role_id")]
    public int Role_Id { get; set; }
}