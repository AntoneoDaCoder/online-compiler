using MediatR;
using ServerAPIApp.Core.UseCases.Problems;
using ServerAPIApp.Core.Helpers;

namespace ServerAPIApp.Core.UseCaseHandlers.Problems
{
    public class GenerateTaskSlugCaseHandler:IRequestHandler<GenerateTaskSlugCase,string>
    {
        public async Task<string> Handle(GenerateTaskSlugCase command,CancellationToken cancellationToken)
        {
            return Utils.GenerateTaskToken();
        }
    }
}
