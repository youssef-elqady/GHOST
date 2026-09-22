namespace GHOST.Domain.Entities;

public sealed class Room
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Name { get; set; }
    public ICollection<Device> Devices { get; set; } = new List<Device>();
}
