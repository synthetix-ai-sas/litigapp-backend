using LitigApp.Application.Features.Catalog.Dtos;

namespace LitigApp.Application.Common.Abstractions;

public interface ICatalogReader
{
    Task<List<DepartmentDto>> ListDepartmentsAsync(CancellationToken ct = default);
    Task<List<CityDto>> ListCitiesByDepartmentAsync(string departmentId, CancellationToken ct = default);
    Task<List<SpecialtyDto>> ListSpecialtiesAsync(CancellationToken ct = default);
    Task<List<EntityDto>> ListEntitiesAsync(CancellationToken ct = default);
    Task<List<CourtDto>> ListCourtsByCityAsync(string cityId, string? specialtyCode, string? entityCode, CancellationToken ct = default);
    Task<List<CourtDto>> SearchCourtsAsync(string nameLike, string? cityId, CancellationToken ct = default);
}
