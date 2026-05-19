using GdeOni.API.Extensions;
using GdeOni.API.Response;
using GdeOni.Application.Legal.Queries.GetLegalDocument.Model;
using GdeOni.Application.Legal.Queries.GetLegalDocument.UseCase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GdeOni.API.Controllers;

/// <summary>
/// D19. Эндпоинты юридических документов: Privacy Policy и Terms of
/// Use. Все маршруты <c>[AllowAnonymous]</c> — документ должен быть
/// доступен ДО регистрации (юзер обязан прочитать перед чекбоксом
/// "принимаю"). Версию клиент использует при <c>POST /accept-legal</c>.
/// </summary>
[ApiController]
[Route("api/legal")]
public sealed class LegalController : ApiControllerBase
{
    /// <summary>
    /// Текущая версия и публичный URL Privacy Policy. Body документа
    /// в данной итерации не возвращается — клиент сам ходит по URL
    /// за markdown.
    /// </summary>
    [HttpGet("privacy-policy")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<LegalDocumentResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPrivacyPolicy(
        [FromServices] IGetLegalDocumentUseCase getLegalDocument,
        CancellationToken cancellationToken)
    {
        var result = await getLegalDocument.Execute(
            new GetLegalDocumentQuery(LegalDocumentKey.PrivacyPolicy),
            cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Текущая версия и публичный URL Terms of Use.
    /// </summary>
    [HttpGet("terms-of-use")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<LegalDocumentResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTermsOfUse(
        [FromServices] IGetLegalDocumentUseCase getLegalDocument,
        CancellationToken cancellationToken)
    {
        var result = await getLegalDocument.Execute(
            new GetLegalDocumentQuery(LegalDocumentKey.TermsOfUse),
            cancellationToken);
        return FromResult(result);
    }
}
