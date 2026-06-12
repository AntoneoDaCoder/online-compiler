using FluentValidation;
using MediatR;
using ServerAPIApp.Core.Abstractions;
using ServerAPIApp.Domain.Exceptions.BadRequestExceptions;
using System.Text;

namespace ServerAPIApp.Core.PipelineBehaviours
{
    public class ValidationBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : IValidatableRequest<TResponse>
    {
        private readonly IEnumerable<IValidator<TRequest>> _validators; // коллекция всех зарегистрированных валидаторов для типа TRequest, предоставляемая контейнером зависимостей

        public ValidationBehaviour(IEnumerable<IValidator<TRequest>> validators)
        {
            _validators = validators;
        }

        public async Task<TResponse> Handle
            (TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            if (request is IValidatableRequest<TResponse>) // если запрос помечен интерфейсом-маркером
            {
                if (_validators.Any()) // если для такого типа существуют валидаторы
                {
                    var context = new ValidationContext<TRequest>(request); // создание нового контекста валидации для указанного типа

                    var validationResults = await Task.WhenAll(
                        _validators.Select(v =>
                            v.ValidateAsync(context, cancellationToken))); // запуск всех возможных для данного типа валидаторов используя контекст валидации

                    var failures = validationResults 
                        .SelectMany(r => r.Errors)
                        .Where(f => f != null)
                        .ToList(); // сбор ошибок

                    if (failures.Count != 0)
                    {
                        var sb = new StringBuilder();

                        foreach (var failure in failures)
                            sb.Append($"[{failure.ErrorMessage}]");

                        throw new BadRequestException(sb.ToString());
                    }
                }
            }

            return await next(cancellationToken); // передача запроса следующему обработчику в конвейере
        }
    }
}
