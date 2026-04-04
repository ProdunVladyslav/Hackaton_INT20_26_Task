using Infrastructure.Contracts.AIGeneration.Internal;
using Infrastructure.Contracts.AIGeneration.Requests;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.UseCases.AIGeneration
{
    public sealed class StartGenerateFlowUseCase(
        FlowGenerationJobStore jobStore,
        IServiceScopeFactory scopeFactory)
    {
        public Guid Execute(GenerateFlowRequest request)
        {
            var job = jobStore.Create();
            job.Status = JobStatus.Running;

            _ = Task.Run(async () =>
            {
                try
                {
                    // GenerateFlowUseCase has scoped dependencies — run in its own scope
                    await using var scope = scopeFactory.CreateAsyncScope();
                    var useCase = scope.ServiceProvider.GetRequiredService<GenerateFlowUseCase>();

                    var flowId = await useCase.ExecuteAsync(request, CancellationToken.None);
                    job.FlowId = flowId;
                    job.Status = JobStatus.Done;
                }
                catch (Exception ex)
                {
                    job.Error = ex.Message;
                    job.Status = JobStatus.Failed;
                }
            });

            return job.JobId;
        }
    }
}
