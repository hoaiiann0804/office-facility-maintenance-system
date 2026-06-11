namespace InternalMaintenance.Api.Models;

public class Role
{
    public int Id {get;set;}
    public string Name {get;set;} = string.Empty;
    // string.Emty : giá trị mặc định là chuỗi rỗng "", thay vì null
    public ICollection<User> Users {get;set;} = new List<User>();
    // Quan hệ giữa Role và User: Một role có nhiều User
}