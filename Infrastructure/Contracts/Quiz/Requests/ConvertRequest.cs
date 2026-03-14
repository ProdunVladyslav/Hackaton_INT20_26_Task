using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Contracts.Quiz.Requests;

public sealed record ConvertRequest([Required] Guid OfferId);
