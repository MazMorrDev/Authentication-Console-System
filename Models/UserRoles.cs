namespace AuthenticationConsoleSystem;

using System.ComponentModel.DataAnnotations.Schema;

public class UserRoles
{
	public int Id { get; set; }

	// Foreign keys
	public int UserId { get; set; }
	public int RoleId { get; set; }

	// Navigation properties
	[ForeignKey(nameof(UserId))]
	public Users? User { get; set; }

	[ForeignKey(nameof(RoleId))]
	public Roles? Role { get; set; }
}
