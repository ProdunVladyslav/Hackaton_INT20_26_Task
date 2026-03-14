using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Contracts.Flows.Requests;

public sealed record SetEntryNodeRequest(
    [Required(ErrorMessage = "Entry node ID is required.")]
    Guid EntryNodeId
);
