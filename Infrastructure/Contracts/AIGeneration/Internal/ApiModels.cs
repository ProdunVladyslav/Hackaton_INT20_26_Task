using Domain.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Infrastructure.Contracts.AIGeneration.Internal
{
    internal record ApiMessage(string role, string content);

    internal record ApiRequest(
        string model,
        int max_tokens,
        ApiMessage[] messages,
        string? system = null,
        double? temperature = null,
        bool? stream = null
    );

    internal record ApiResponse(
        string id,
        string model,
        string stop_reason,
        ApiContent[] content,
        ApiUsage usage
    );

    internal record ApiContent(string type, string text);
    internal record ApiUsage(int input_tokens, int output_tokens);

    internal record StreamDelta(string type, string? text);
    internal record StreamEvent(string type, StreamDelta? delta);


    public enum JobStatus { Pending, Running, Done, Failed }

    public sealed class FlowGenerationJob
    {
        public Guid JobId { get; init; } = Guid.NewGuid();
        public JobStatus Status { get; set; } = JobStatus.Pending;
        public Guid? FlowId { get; set; }
        public string? Error { get; set; }
        public DateTime CreatedAt { get; init; }

        public FlowGenerationJob(IDateTimeProvider time)
        {
            CreatedAt = time.UtcNow;
        }
    }

    public sealed class FlowGenerationJobStore
    {
        private readonly ConcurrentDictionary<Guid, FlowGenerationJob> _jobs = new();
        private readonly IDateTimeProvider _time;

        public FlowGenerationJobStore(IDateTimeProvider time)
        {
            _time = time;
        }

        public FlowGenerationJob Create()
        {
            var job = new FlowGenerationJob(_time);
            _jobs[job.JobId] = job;
            return job;
        }

        public FlowGenerationJob? Get(Guid jobId) =>
            _jobs.GetValueOrDefault(jobId);
    }
}
