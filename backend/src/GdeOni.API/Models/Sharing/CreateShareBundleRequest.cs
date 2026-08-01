namespace GdeOni.API.Models.Sharing;

/// <summary>
/// D46. Тело запроса «создать подборку»: id выбранных карточек.
/// </summary>
public sealed class CreateShareBundleRequest
{
    public List<Guid> DeceasedIds { get; set; } = new();
}
