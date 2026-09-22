using GHOST.Application.Sessions;

namespace GHOST.Infrastructure.Sessions;

public sealed class SystemClock : IClock { public DateTimeOffset UtcNow => DateTimeOffset.UtcNow; }
