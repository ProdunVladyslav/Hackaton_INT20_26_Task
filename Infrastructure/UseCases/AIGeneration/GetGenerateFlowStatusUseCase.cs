using Infrastructure.Contracts.AIGeneration.Internal;
using Infrastructure.Contracts.AIGeneration.Responses;

namespace Infrastructure.UseCases.AIGeneration
{
    public sealed class GetGenerateFlowStatusUseCase(FlowGenerationJobStore jobStore)
    {
        public GenerateFlowStatusResponse? Execute(Guid jobId)
        {
            var job = jobStore.Get(jobId);
            if (job is null) return null;

            return new GenerateFlowStatusResponse(
                JobId: job.JobId,
                Status: job.Status.ToString(),
                FlowId: job.FlowId,
                Error: job.Error);
        }
    }
}
