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
public sealed class ReorderOptionsUseCase
{
    private readonly IOptionRepository _options;
    private readonly IUnitOfWork _uow;

    public ReorderOptionsUseCase(IOptionRepository options, IUnitOfWork uow)
    {
        _options = options;
        _uow = uow;
    }

    public async Task<FlowResult<List<OptionResponse>>> ExecuteAsync(
        Guid nodeId,
        ReorderOptionsRequest request,
        CancellationToken ct = default)
    {
        // Load all options for the node
        var optionsList = await _options.GetOptionsByNodeIdAsync(nodeId, ct);

        try
        {
            // Update display order for each option
            foreach (var item in request.Items)
            {
                var option = optionsList.FirstOrDefault(o => o.Id == item.OptionId);
                if (option != null)
                {
                    option.SetDisplayOrder(item.DisplayOrder);
                    _options.Update(option);
                }
            }

            await _uow.SaveChangesAsync();

            // Reload options sorted by DisplayOrder
            var reorderedOptions = await _options.GetOptionsByNodeIdAsync(nodeId, ct);
            var responses = reorderedOptions.Select(ToResponse).ToList();

            return FlowResult<List<OptionResponse>>.Ok(responses);
        }
        catch (ArgumentException ex)
        {
            return FlowResult<List<OptionResponse>>.Fail(ex.Message, statusCode: 400);
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
