using GdeOni.API.Models.Sharing;
using GdeOni.Application.Sharing.Commands.CreateShareBundle.Model;

namespace GdeOni.API.Mappers;

/// <summary>D46. Request → Command маппинг для «поделиться подборкой».</summary>
public static class ShareMapping
{
    public static CreateShareBundleCommand ToCommand(this CreateShareBundleRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new CreateShareBundleCommand(request.DeceasedIds ?? new List<Guid>());
    }
}
