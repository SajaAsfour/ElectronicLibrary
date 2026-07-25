using ElectronicLibrary.DAL.Models.Identity;

namespace ElectronicLibrary.DAL.Models.Shopping;

public class Cart
{
    public int CartId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string UserId { get; set; } = null!;

    public ApplicationUser User { get; set; } = null!;

    public ICollection<CartItem> CartItems { get; set; } = [];
}