using Application.Repositories.Interfaces;
using Domain.Model.Survey;
using Infrastructure.Contracts.Options.Requests;
using Infrastructure.Contracts.Options.Responses;
using Infrastructure.Contracts.Flows.Responses;

namespace Infrastructure.UseCases.Options;

/// <summary>
/// Use case: reorder options for a node.
/// Updates the DisplayOrder for each option in the request.
/// </summary>
public sealed class ReorderOptionsUseCase(
    IOptionRepository _options,
    INodeRepository _nodes,
    IFlowRepository _flows,
    IUserProfileRepository _userProfiles,
    IUnitOfWork _uow)
{
    public async Task<FlowResult<List<OptionResponse>>> ExecuteAsync(
        Guid nodeId,
        Guid applicationUserId,
        ReorderOptionsRequest request,
        CancellationToken ct = default)
    {
        var profile = await _userProfiles.FirstOrDefaultAsync(p => p.ApplicationUserId == applicationUserId, ct);
        if (profile == null)
            return FlowResult<List<OptionResponse>>.NotFound("User profile not found.");

        var node = await _nodes.GetByIdAsync(nodeId, ct);
        if (node == null)
            return FlowResult<List<OptionResponse>>.NotFound("Node not found.");

        var flow = await _flows.FirstOrDefaultAsync(f => f.Id == node.FlowId && f.OwnerId == profile.Id, ct);
        if (flow == null)
            return FlowResult<List<OptionResponse>>.NotFound("Node not found.");

        var optionsList = await _options.GetOptionsByNodeIdAsync(nodeId, ct);

        try
        {
            foreach (var item in request.Items)
            {
                var option = optionsList.FirstOrDefault(o => o.Id == item.OptionId);
                if (option != null)
                {
                    option.SetDisplayOrder(item.DisplayOrder);
                    _options.Update(option);
                }
            }

            await _uow.SaveChangesAsync(ct);

            var reorderedOptions = await _options.GetOptionsByNodeIdAsync(nodeId, ct);
            return FlowResult<List<OptionResponse>>.Ok(reorderedOptions.Select(ToResponse).ToList());
        }
        catch (ArgumentException ex)
        {
            return FlowResult<List<OptionResponse>>.Fail(ex.Message, statusCode: 400);
        }
    }

    private static OptionResponse ToResponse(Option option) =>
        new(option.Id, option.NodeId, option.Label, option.Value,
            option.DisplayOrder, option.MediaUrl, option.ScoreDelta);
}
