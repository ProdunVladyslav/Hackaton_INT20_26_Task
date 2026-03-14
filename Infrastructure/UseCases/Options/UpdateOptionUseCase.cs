using Application.Repositories.Interfaces;
using Domain.Model.Survey;
using Infrastructure.Contracts.Options.Requests;
using Infrastructure.Contracts.Options.Responses;
using Infrastructure.Contracts.Flows.Responses;

namespace Infrastructure.UseCases.Options;

/// <summary>
/// Use case: update an option's properties (label, value, displayOrder, media).
/// Only provided fields are updated.
/// </summary>
public sealed class UpdateOptionUseCase
{
    private readonly IOptionRepository _options;
    private readonly IUnitOfWork _uow;

    public UpdateOptionUseCase(IOptionRepository options, IUnitOfWork uow)
    {
        _options = options;
        _uow = uow;
    }

    public async Task<FlowResult<OptionResponse>> ExecuteAsync(
        Guid nodeId,
        Guid optionId,
        UpdateOptionRequest request,
        CancellationToken ct = default)
    {
        // Load option
        var option = await _options.GetByIdAsync(optionId, ct);
        if (option == null)
            return FlowResult<OptionResponse>.NotFound("Option not found.");

        // Verify option belongs to the node
        if (option.NodeId != nodeId)
            return FlowResult<OptionResponse>.NotFound("Option not found in this node.");

        // Apply updates
        try
        {
            if (!string.IsNullOrWhiteSpace(request.Label))
                option.SetLabel(request.Label);

            if (!string.IsNullOrWhiteSpace(request.Value))
                option.SetValue(request.Value);

            if (request.DisplayOrder.HasValue)
                option.SetDisplayOrder(request.DisplayOrder.Value);

            if (request.MediaUrl != null)
                option.SetMedia(request.MediaUrl);

            _options.Update(option);
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
