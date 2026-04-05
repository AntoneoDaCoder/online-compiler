using MediatR;

namespace ServerAPIApp.Core.Abstractions
{
    public interface IValidatableRequest<TResponse> : IRequest<TResponse>
    {
    }
}
