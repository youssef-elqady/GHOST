using FluentValidation;

namespace GHOST.Application.Devices;

public sealed class CreateDeviceRequestValidator : AbstractValidator<CreateDeviceRequest>
{
    public CreateDeviceRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DeviceType).NotEmpty().MaximumLength(50);
        RuleFor(x => x.SingleRate).InclusiveBetween(0m, 10000m)
            .Must(v => decimal.Round(v, 2) == v).WithMessage("Rate must have at most 2 decimal places.");
        RuleFor(x => x.MultiRate).InclusiveBetween(0m, 10000m)
            .Must(v => decimal.Round(v, 2) == v).WithMessage("Rate must have at most 2 decimal places.");
    }
}

public sealed class CreateRoomRequestValidator : AbstractValidator<CreateRoomRequest>
{
    public CreateRoomRequestValidator() => RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
}