using GdeOni.API.Extensions;
using GdeOni.API.Response;
using GdeOni.Application.Common.Shared;
using GdeOni.Application.DeceasedRecords.AddMemory.Model;
using GdeOni.Application.DeceasedRecords.AddMemory.UseCase;
using GdeOni.Application.DeceasedRecords.AddPhoto.Model;
using GdeOni.Application.DeceasedRecords.AddPhoto.UseCase;
using GdeOni.Application.DeceasedRecords.ApproveMemory.Model;
using GdeOni.Application.DeceasedRecords.ApproveMemory.UseCase;
using GdeOni.Application.DeceasedRecords.ApprovePhoto.Model;
using GdeOni.Application.DeceasedRecords.ApprovePhoto.UseCase;
using GdeOni.Application.DeceasedRecords.ClearMetadata.Model;
using GdeOni.Application.DeceasedRecords.ClearMetadata.UseCase;
using GdeOni.Application.DeceasedRecords.Create.Model;
using GdeOni.Application.DeceasedRecords.Create.UseCase;
using GdeOni.Application.DeceasedRecords.Delete.UseCase;
using GdeOni.Application.DeceasedRecords.GetAll.Model;
using GdeOni.Application.DeceasedRecords.GetAll.UseCase;
using GdeOni.Application.DeceasedRecords.GetById.Model;
using GdeOni.Application.DeceasedRecords.GetById.UseCase;
using GdeOni.Application.DeceasedRecords.GetDistance.Model;
using GdeOni.Application.DeceasedRecords.GetDistance.UseCase;
using GdeOni.Application.DeceasedRecords.RejectMemory.Model;
using GdeOni.Application.DeceasedRecords.RejectMemory.UseCase;
using GdeOni.Application.DeceasedRecords.RejectPhoto.Model;
using GdeOni.Application.DeceasedRecords.RejectPhoto.UseCase;
using GdeOni.Application.DeceasedRecords.RemoveMemory.UseCase;
using GdeOni.Application.DeceasedRecords.RemovePhoto.UseCase;
using GdeOni.Application.DeceasedRecords.SetPrimaryPhoto.Model;
using GdeOni.Application.DeceasedRecords.SetPrimaryPhoto.UseCase;
using GdeOni.Application.DeceasedRecords.Update.Model;
using GdeOni.Application.DeceasedRecords.Update.UseCase;
using GdeOni.Application.DeceasedRecords.UpdateMemory.Model;
using GdeOni.Application.DeceasedRecords.UpdateMemory.UseCase;
using GdeOni.Application.DeceasedRecords.UpdateMetadata.Model;
using GdeOni.Application.DeceasedRecords.UpdateMetadata.UseCase;
using GdeOni.Application.DeceasedRecords.UpdatePhoto.Model;
using GdeOni.Application.DeceasedRecords.UpdatePhoto.UseCase;
using GdeOni.Application.DeceasedRecords.GetAgeAtDeath.Model;
using GdeOni.Application.DeceasedRecords.GetAgeAtDeath.UseCase;
using GdeOni.Application.DeceasedRecords.HasPhotos.Model;
using GdeOni.Application.DeceasedRecords.HasPhotos.UseCase;
using GdeOni.Application.DeceasedRecords.HasMemories.Model;
using GdeOni.Application.DeceasedRecords.HasMemories.UseCase;
using GdeOni.Application.DeceasedRecords.Verify.Model;
using GdeOni.Application.DeceasedRecords.Verify.UseCase;
using GdeOni.Application.DeceasedRecords.Unverify.Model;
using GdeOni.Application.DeceasedRecords.Unverify.UseCase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GdeOni.API.Controllers;

/// <summary>
/// 
/// </summary>
[Route("api/deceased-records")]
public sealed class DeceasedRecordsController : ApiControllerBase
{
    /// <summary>
    /// Метод создания умершего
    /// </summary>
    /// <param name="request"></param>
    /// <param name="createDeceasedUseCase"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<CreateDeceasedResponse>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(
        [FromBody] CreateDeceasedRequest request,
        [FromServices] ICreateDeceasedUseCase createDeceasedUseCase,
        CancellationToken cancellationToken)
    {
        var result = await createDeceasedUseCase.Execute(request, cancellationToken);

        return FromResult(
            result,
            value => Created($"/api/deceased-records/{value.Id}",
                ApiResponse<CreateDeceasedResponse>.Success(value)));
    }

    /// <summary>
    /// Метод получения всех умерших
    /// </summary>
    /// <param name="query"></param>
    /// <param name="getAllDeceasedUseCase"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpGet]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<DeceasedListItemResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<DeceasedListItemResponse>>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAll(
        [FromQuery] GetAllDeceasedQuery query,
        [FromServices] IGetAllDeceasedUseCase getAllDeceasedUseCase,
        CancellationToken cancellationToken)
    {
        var result = await getAllDeceasedUseCase.Execute(query, cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Метод получения умершего по Id
    /// </summary>
    /// <param name="id"></param>
    /// <param name="getDeceasedByIdUseCase"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpGet("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<DeceasedDetailsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<DeceasedDetailsResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        Guid id,
        [FromServices] IGetDeceasedByIdUseCase getDeceasedByIdUseCase,
        CancellationToken cancellationToken)
    {
        var result = await getDeceasedByIdUseCase.Execute(id, cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Метод обновления умерших
    /// </summary>
    /// <param name="id"></param>
    /// <param name="request"></param>
    /// <param name="updateDeceasedUseCase"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPut("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UpdateDeceasedResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UpdateDeceasedResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<UpdateDeceasedResponse>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateDeceasedRequest request,
        [FromServices] IUpdateDeceasedUseCase updateDeceasedUseCase,
        CancellationToken cancellationToken)
    {
        request.Id = id;
        var result = await updateDeceasedUseCase.Execute(request, cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Метод удаления умершего
    /// </summary>
    /// <param name="id"></param>
    /// <param name="deleteDeceasedUseCase"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpDelete("{id:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromServices] IDeleteDeceasedUseCase deleteDeceasedUseCase,
        CancellationToken cancellationToken)
    {
        var result = await deleteDeceasedUseCase.Execute(id, cancellationToken);

        if (result.IsFailure)
            return result.Error.ToErrorResponse<object>();

        return NoContent();
    }

    /// <summary>
    /// Метод добавления фотографии для умершего
    /// </summary>
    /// <param name="id"></param>
    /// <param name="request"></param>
    /// <param name="addPhotoUseCase"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPost("{id:guid}/photos")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<AddPhotoResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> AddPhoto(
        Guid id,
        [FromBody] AddPhotoRequest request,
        [FromServices] IAddPhotoUseCase addPhotoUseCase,
        CancellationToken cancellationToken)
    {
        request.DeceasedId = id;
        var result = await addPhotoUseCase.Execute(request, cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Метод обновления фотографии и описания
    /// </summary>
    /// <param name="id"></param>
    /// <param name="photoId"></param>
    /// <param name="request"></param>
    /// <param name="updatePhotoUseCase"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPut("{id:guid}/photos/{photoId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UpdatePhotoResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdatePhoto(
        Guid id,
        Guid photoId,
        [FromBody] UpdatePhotoRequest request,
        [FromServices] IUpdatePhotoUseCase updatePhotoUseCase,
        CancellationToken cancellationToken)
    {
        request.DeceasedId = id;
        request.PhotoId = photoId;

        var result = await updatePhotoUseCase.Execute(request, cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Сделать фотографию основной для умершего
    /// </summary>
    /// <param name="id"></param>
    /// <param name="photoId"></param>
    /// <param name="setPrimaryPhotoUseCase"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPut("{id:guid}/photos/{photoId:guid}/primary")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<SetPrimaryPhotoResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SetPrimaryPhoto(
        Guid id,
        Guid photoId,
        [FromServices] ISetPrimaryPhotoUseCase setPrimaryPhotoUseCase,
        CancellationToken cancellationToken)
    {
        var request = new SetPrimaryPhotoRequest
        {
            DeceasedId = id,
            PhotoId = photoId
        };

        var result = await setPrimaryPhotoUseCase.Execute(request, cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Одобрить фотографию умершего
    /// </summary>
    /// <param name="id"></param>
    /// <param name="photoId"></param>
    /// <param name="approvePhotoUseCase"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPut("{id:guid}/photos/{photoId:guid}/approve")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<ApprovePhotoResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ApprovePhoto(
        Guid id,
        Guid photoId,
        [FromServices] IApprovePhotoUseCase approvePhotoUseCase,
        CancellationToken cancellationToken)
    {
        var request = new ApprovePhotoRequest
        {
            DeceasedId = id,
            PhotoId = photoId
        };

        var result = await approvePhotoUseCase.Execute(request, cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Отклонить фотографию умершего
    /// </summary>
    /// <param name="id"></param>
    /// <param name="photoId"></param>
    /// <param name="rejectPhotoUseCase"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPut("{id:guid}/photos/{photoId:guid}/reject")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(typeof(ApiResponse<RejectPhotoResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RejectPhoto(
        Guid id,
        Guid photoId,
        [FromServices] IRejectPhotoUseCase rejectPhotoUseCase,
        CancellationToken cancellationToken)
    {
        var request = new RejectPhotoRequest
        {
            DeceasedId = id,
            PhotoId = photoId
        };

        var result = await rejectPhotoUseCase.Execute(request, cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Удалить фотографию у умершего
    /// </summary>
    /// <param name="id"></param>
    /// <param name="photoId"></param>
    /// <param name="removePhotoUseCase"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpDelete("{id:guid}/photos/{photoId:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemovePhoto(
        Guid id,
        Guid photoId,
        [FromServices] IRemovePhotoUseCase removePhotoUseCase,
        CancellationToken cancellationToken)
    {
        var result = await removePhotoUseCase.Execute(id, photoId, cancellationToken);

        if (result.IsFailure)
            return result.Error.ToErrorResponse<object>();

        return NoContent();
    }

    /// <summary>
    /// Добавить воспоминание об умершем
    /// </summary>
    /// <param name="id"></param>
    /// <param name="request"></param>
    /// <param name="addMemoryUseCase"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPost("{id:guid}/memories")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<AddMemoryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> AddMemory(
        Guid id,
        [FromBody] AddMemoryRequest request,
        [FromServices] IAddMemoryUseCase addMemoryUseCase,
        CancellationToken cancellationToken)
    {
        request.DeceasedId = id;
        var result = await addMemoryUseCase.Execute(request, cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Обновить воспоминание об умершем
    /// </summary>
    /// <param name="id"></param>
    /// <param name="memoryId"></param>
    /// <param name="request"></param>
    /// <param name="updateMemoryUseCase"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPut("{id:guid}/memories/{memoryId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UpdateMemoryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateMemory(
        Guid id,
        Guid memoryId,
        [FromBody] UpdateMemoryRequest request,
        [FromServices] IUpdateMemoryUseCase updateMemoryUseCase,
        CancellationToken cancellationToken)
    {
        request.DeceasedId = id;
        request.MemoryId = memoryId;

        var result = await updateMemoryUseCase.Execute(request, cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Одобрить воспоминание об умершем
    /// </summary>
    /// <param name="id"></param>
    /// <param name="memoryId"></param>
    /// <param name="approveMemoryUseCase"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPut("{id:guid}/memories/{memoryId:guid}/approve")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(typeof(ApiResponse<ApproveMemoryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ApproveMemory(
        Guid id,
        Guid memoryId,
        [FromServices] IApproveMemoryUseCase approveMemoryUseCase,
        CancellationToken cancellationToken)
    {
        var request = new ApproveMemoryRequest
        {
            DeceasedId = id,
            MemoryId = memoryId
        };

        var result = await approveMemoryUseCase.Execute(request, cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Отклонить воспоминание об умершем
    /// </summary>
    /// <param name="id"></param>
    /// <param name="memoryId"></param>
    /// <param name="rejectMemoryUseCase"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPut("{id:guid}/memories/{memoryId:guid}/reject")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(typeof(ApiResponse<RejectMemoryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RejectMemory(
        Guid id,
        Guid memoryId,
        [FromServices] IRejectMemoryUseCase rejectMemoryUseCase,
        CancellationToken cancellationToken)
    {
        var request = new RejectMemoryRequest
        {
            DeceasedId = id,
            MemoryId = memoryId
        };

        var result = await rejectMemoryUseCase.Execute(request, cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Удалить воспоминание об умершем
    /// </summary>
    /// <param name="id"></param>
    /// <param name="memoryId"></param>
    /// <param name="removeMemoryUseCase"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpDelete("{id:guid}/memories/{memoryId:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveMemory(
        Guid id,
        Guid memoryId,
        [FromServices] IRemoveMemoryUseCase removeMemoryUseCase,
        CancellationToken cancellationToken)
    {
        var result = await removeMemoryUseCase.Execute(id, memoryId, cancellationToken);

        if (result.IsFailure)
            return result.Error.ToErrorResponse<object>();

        return NoContent();
    }

    /// <summary>
    /// Обновить вспомогательные данные об умершем
    /// </summary>
    /// <param name="id"></param>
    /// <param name="request"></param>
    /// <param name="updateMetadataUseCase"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPut("{id:guid}/metadata")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UpdateMetadataResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateMetadata(
        Guid id,
        [FromBody] UpdateMetadataRequest request,
        [FromServices] IUpdateMetadataUseCase updateMetadataUseCase,
        CancellationToken cancellationToken)
    {
        request.DeceasedId = id;
        var result = await updateMetadataUseCase.Execute(request, cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Удалить вспомогательные данные об умершем
    /// </summary>
    /// <param name="id"></param>
    /// <param name="clearMetadataUseCase"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpDelete("{id:guid}/metadata")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<ClearMetadataResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ClearMetadata(
        Guid id,
        [FromServices] IClearMetadataUseCase clearMetadataUseCase,
        CancellationToken cancellationToken)
    {
        var request = new ClearMetadataRequest
        {
            DeceasedId = id
        };

        var result = await clearMetadataUseCase.Execute(request, cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Получить дистанцию до умершего
    /// </summary>
    /// <param name="id"></param>
    /// <param name="latitude"></param>
    /// <param name="longitude"></param>
    /// <param name="getDistanceUseCase"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpGet("{id:guid}/distance")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<GetDistanceResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDistance(
        Guid id,
        [FromQuery] double latitude,
        [FromQuery] double longitude,
        [FromServices] IGetDistanceUseCase getDistanceUseCase,
        CancellationToken cancellationToken)
    {
        var result = await getDistanceUseCase.Execute(id, latitude, longitude, cancellationToken);
        return FromResult(result);
    }
    
    /// <summary>
    /// Получить возраст на момент смерти
    /// </summary>
    /// <param name="id"></param>
    /// <param name="getAgeAtDeathUseCase"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpGet("{id:guid}/age-at-death")]
    [ProducesResponseType(typeof(ApiResponse<GetAgeAtDeathResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<GetAgeAtDeathResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAgeAtDeath(
        Guid id,
        [FromServices] IGetAgeAtDeathUseCase getAgeAtDeathUseCase,
        CancellationToken cancellationToken)
    {
        var result = await getAgeAtDeathUseCase.Execute(id, cancellationToken);
        return FromResult(result);
    }
    
    /// <summary>
    /// Есть ли фото у умершего
    /// </summary>
    /// <param name="id"></param>
    /// <param name="hasPhotosUseCase"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpGet("{id:guid}/has-photos")]
    [ProducesResponseType(typeof(ApiResponse<HasPhotosResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<HasPhotosResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> HasPhotos(
        Guid id,
        [FromServices] IHasPhotosUseCase hasPhotosUseCase,
        CancellationToken cancellationToken)
    {
        var result = await hasPhotosUseCase.Execute(id, cancellationToken);
        return FromResult(result);
    }
    
    /// <summary>
    /// Есть ли записки памяти
    /// </summary>
    /// <param name="id"></param>
    /// <param name="hasMemoriesUseCase"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpGet("{id:guid}/has-memories")]
    [ProducesResponseType(typeof(ApiResponse<HasMemoriesResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<HasMemoriesResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> HasMemories(
        Guid id,
        [FromServices] IHasMemoriesUseCase hasMemoriesUseCase,
        CancellationToken cancellationToken)
    {
        var result = await hasMemoriesUseCase.Execute(id, cancellationToken);
        return FromResult(result);
    }
    
    /// <summary>
    /// Верифицировать запись
    /// </summary>
    /// <param name="id"></param>
    /// <param name="verifyDeceasedUseCase"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPut("{id:guid}/verify")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(typeof(ApiResponse<VerifyDeceasedResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<VerifyDeceasedResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Verify(
        Guid id,
        [FromServices] IVerifyDeceasedUseCase verifyDeceasedUseCase,
        CancellationToken cancellationToken)
    {
        var result = await verifyDeceasedUseCase.Execute(id, cancellationToken);
        return FromResult(result);
    }
    
    /// <summary>
    /// Снять верификацию
    /// </summary>
    /// <param name="id"></param>
    /// <param name="unverifyDeceasedUseCase"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPut("{id:guid}/unverify")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(typeof(ApiResponse<UnverifyDeceasedResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UnverifyDeceasedResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Unverify(
        Guid id,
        [FromServices] IUnverifyDeceasedUseCase unverifyDeceasedUseCase,
        CancellationToken cancellationToken)
    {
        var result = await unverifyDeceasedUseCase.Execute(id, cancellationToken);
        return FromResult(result);
    }
}