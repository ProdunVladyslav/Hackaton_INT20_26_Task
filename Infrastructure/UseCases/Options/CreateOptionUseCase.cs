using Application.Repositories.Interfaces;
using Domain.Model.Survey;
using Infrastructure.Contracts.Options.Requests;
using Infrastructure.Contracts.Options.Responses;
using Infrastructure.Contracts.Flows.Responses;

namespace Infrastructure.UseCases.Options;

/// <summary>
/// Use case: create a new option for a question node.
/// Only question nodes can have options.
/// </summary>
public sealed class CreateOptionUseCase
{
    private readonly INodeRepository _nodes;
    private readonly IOptionRepository _options;
    private readonly IUnitOfWork _uow;

    public CreateOptionUseCase(INodeRepository nodes, IOptionRepository options, IUnitOfWork uow)
    {
        _nodes = nodes;
        _options = options;
        _uow = uow;
    }

    public async Task<FlowResult<OptionResponse>> ExecuteAsync(
        Guid nodeId,
        CreateOptionRequest request,
        CancellationToken ct = default)
    {
        // Load node
        var node = await _nodes.GetByIdAsync(nodeId, ct);
        if (node == null)
            return FlowResult<OptionResponse>.NotFound("Node not found.");

        // Verify node is a Question type
        if (node.Type != NodeType.Question)
            return FlowResult<OptionResponse>.Fail(
                "Only question nodes can have options.",
                statusCode: 422);

        // Create option
        try
        {
            var option = Option.Create(nodeId, request.Label, request.Value, request.DisplayOrder);

            if (!string.IsNullOrWhiteSpace(request.MediaUrl))
                option.SetMedia(request.MediaUrl);

            await _options.AddAsync(option, ct);
            await _uow.SaveChangesAsync();

            return FlowResult<OptionResponse>.Ok(ToResponse(option));
        }
        catch (ArgumentException ex)
        {
            return FlowResult<OptionResponse>.Fail(ex.Message, statusCode: 400);
        }
    }

    private static OptionResponse ToResponse(Option option) =>
        new(
            option.Id,
            option.NodeId,
            option.Label,
            option.Value,
            option.DisplayOrder,
            option.MediaUrl);
}
