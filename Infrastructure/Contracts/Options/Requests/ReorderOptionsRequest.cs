using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Contracts.Options.Requests;

public sealed record ReorderOptionsRequest(
    [Required] List<OptionOrderItem> Items
);

public sealed record OptionOrderItem(Guid OptionId, int DisplayOrder);
