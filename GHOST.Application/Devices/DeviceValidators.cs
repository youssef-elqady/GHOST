using FluentValidation;

namespace GHOST.Application.Devices;

public sealed class CreateDeviceRequestValidator : AbstractValidator<CreateDeviceRequest>
{
    public CreateDeviceRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DeviceType).NotEmpty().MaximumLength(50);
    }
}

public sealed class CreateRoomRequestValidator : AbstractValidator<CreateRoomRequest>
{
    public CreateRoomRequestValidator() => RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
}
