namespace AuthenticationConsoleSystem;

using System.ComponentModel.DataAnnotations.Schema;

public class UserRole
{
	public int Id { get; set; }

	// Foreign keys
	public int UserId { get; set; }
	public int RoleId { get; set; }

	// Navigation properties
	[ForeignKey(nameof(UserId))]
	public User? User { get; set; }

	[ForeignKey(nameof(RoleId))]
	public Role? Role { get; set; }
}
