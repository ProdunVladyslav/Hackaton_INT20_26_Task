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
public sealed class CreateOptionUseCase(
    INodeRepository _nodes,
    IOptionRepository _options,
    IFlowRepository _flows,
    IUserProfileRepository _userProfiles,
    IUnitOfWork _uow)
{
    public async Task<FlowResult<OptionResponse>> ExecuteAsync(
        Guid nodeId,
        Guid applicationUserId,
        CreateOptionRequest request,
        CancellationToken ct = default)
    {
        var profile = await _userProfiles.FirstOrDefaultAsync(p => p.ApplicationUserId == applicationUserId, ct);
        if (profile == null)
            return FlowResult<OptionResponse>.NotFound("User profile not found.");

        var node = await _nodes.GetByIdAsync(nodeId, ct);
        if (node == null)
            return FlowResult<OptionResponse>.NotFound("Node not found.");

        var flow = await _flows.FirstOrDefaultAsync(f => f.Id == node.FlowId && f.OwnerId == profile.Id, ct);
        if (flow == null)
            return FlowResult<OptionResponse>.NotFound("Node not found.");

        if (node.Type != NodeType.Question)
            return FlowResult<OptionResponse>.Fail("Only question nodes can have options.", statusCode: 422);

        try
        {
            var option = Option.Create(nodeId, request.Label, request.Value, request.DisplayOrder);

            if (!string.IsNullOrWhiteSpace(request.MediaUrl))
                option.SetMedia(request.MediaUrl);

            if (request.ScoreDelta != 0)
                option.SetScoreDelta(request.ScoreDelta);

            await _options.AddAsync(option, ct);
            await _uow.SaveChangesAsync(ct);

            return FlowResult<OptionResponse>.Ok(ToResponse(option));
        }
        catch (ArgumentException ex)
        {
            return FlowResult<OptionResponse>.Fail(ex.Message, statusCode: 400);
        }
    }

    private static OptionResponse ToResponse(Option option) =>
        new(option.Id, option.NodeId, option.Label, option.Value,
            option.DisplayOrder, option.MediaUrl, option.ScoreDelta);
}