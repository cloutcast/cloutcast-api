using System.IO.Abstractions;
using System.Net;
using FluentValidation;
using MediatR;

namespace CloutCast.Requests
{
    public class GetJsonWebKeySetRequest : IRequest<string>
    {
        public string CachePath { get; set; }
        public bool ForceFetch { get; set; } = false;
        public string KeySetUrl { get; set; }
    }

    public class GetJsonWebKeySetRequestValidator : AbstractValidator<GetJsonWebKeySetRequest>
    {
        public GetJsonWebKeySetRequestValidator(IFileSystem fileSystem)
        {
            CascadeMode = CascadeMode.Stop;

            RuleFor(request => request.KeySetUrl).NotEmpty().Url();
            RuleFor(request => request.CachePath).NotEmpty()
                .DependentRules(() =>
                    RuleFor(request => request.CachePath)
                        .Must(path => fileSystem.Directory.Exists(path))
                        .WithMessage(r => $"Path does not exist; [{r.CachePath}]")
                        .WithState(r => HttpStatusCode.NotFound)
                );
        }
    }
}