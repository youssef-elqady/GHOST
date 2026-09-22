# Development

Restore with `dotnet restore GHOST.slnx --configfile NuGet.Config`, build with `dotnet build GHOST.slnx --no-restore`, and run tests with `dotnet test GHOST.Tests/GHOST.Tests.csproj --no-build --no-restore`.

The default administrator is deliberately not seeded. First-run setup must create it with a password of at least twelve characters; passwords use PBKDF2-SHA512 with a random salt and 600,000 iterations.
