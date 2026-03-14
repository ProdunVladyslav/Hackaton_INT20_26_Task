using Application.Repositories.Interfaces;
using Infrastructure.Contracts.Flows.Responses;

namespace Infrastructure.UseCases.Options;

/// <summary>
/// Use case: delete an option from a node.
/// </summary>
public sealed class DeleteOptionUseCase
{
    private readonly IOptionRepository _options;
    private readonly IUnitOfWork _uow;

    public DeleteOptionUseCase(IOptionRepository options, IUnitOfWork uow)
    {
        _options = options;
        _uow = uow;
    }

    public async Task<FlowResult<bool>> ExecuteAsync(
        Guid nodeId,
        Guid optionId,
        CancellationToken ct = default)
    {
        // Load option
        var option = await _options.GetByIdAsync(optionId, ct);
        if (option == null)
            return FlowResult<bool>.NotFound("Option not found.");

        // Verify option belongs to the node
        if (option.NodeId != nodeId)
            return FlowResult<bool>.NotFound("Option not found in this node.");

        // Delete option
        _options.Remove(option);
        await _uow.SaveChangesAsync();

        return FlowResult<bool>.Ok(true);
    }
}
