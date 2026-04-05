using FluentValidation;
using MediatR;
using ServerAPIApp.Core.Abstractions;
using ServerAPIApp.Domain.Exceptions.BadRequestExceptions;
using System.Text;

namespace ServerAPIApp.Core.PipelineBehaviours
{
    public class ValidationBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : IValidatableRequest<TResponse>
    {
        private readonly IEnumerable<IValidator<TRequest>> _validators;

        public ValidationBehaviour(IEnumerable<IValidator<TRequest>> validators)
        {
            _validators = validators;
        }

        public async Task<TResponse> Handle
            (TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            if (request is IValidatableRequest<TResponse>)
            {
                if (_validators.Any())
                {
                    var context = new ValidationContext<TRequest>(request);

                    var validationResults = await Task.WhenAll(
                        _validators.Select(v =>
                            v.ValidateAsync(context, cancellationToken)));

                    var failures = validationResults
                        .SelectMany(r => r.Errors)
                        .Where(f => f != null)
                        .ToList();

                    if (failures.Count != 0)
                    {
                        var sb = new StringBuilder();

                        foreach (var failure in failures)
                            sb.Append($"[{failure.ErrorMessage}]");

                        throw new BadRequestException(sb.ToString());
                    }
                }
            }

            return await next(cancellationToken);
        }
    }
}
