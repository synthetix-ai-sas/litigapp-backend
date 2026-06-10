using LitigApp.Application.Common.Abstractions;
using LitigApp.Application.Features.Catalog.Dtos;
using LitigApp.Application.Features.Catalog.Queries.ListCitiesByDepartment;
using LitigApp.Application.Features.Catalog.Queries.ListCourtsByCity;
using LitigApp.Application.Features.Catalog.Queries.ListDepartments;
using LitigApp.Application.Features.Catalog.Queries.ListEntities;
using LitigApp.Application.Features.Catalog.Queries.ListSpecialties;
using LitigApp.Application.Features.Catalog.Queries.SearchCourts;
using NSubstitute;

namespace LitigApp.Application.UnitTests.Features.Catalog;

public class ListDepartmentsHandlerTests
{
    [Fact]
    public async Task HandleAsync_CallsReaderAndReturnsDtos()
    {
        var reader = Substitute.For<ICatalogReader>();
        var expected = new List<DepartmentDto> { new("17", "Caldas"), new("63", "Quindío") };
        reader.ListDepartmentsAsync(default).Returns(expected);

        var handler = new ListDepartmentsHandler(reader);
        var result = await handler.HandleAsync(new ListDepartmentsQuery());

        Assert.Equal(expected, result);
        await reader.Received(1).ListDepartmentsAsync(default);
    }
}

public class ListCitiesByDepartmentHandlerTests
{
    [Fact]
    public async Task HandleAsync_PassesDepartmentIdToReader()
    {
        var reader = Substitute.For<ICatalogReader>();
        var expected = new List<CityDto> { new("17001", "Manizales") };
        reader.ListCitiesByDepartmentAsync("17", default).Returns(expected);

        var handler = new ListCitiesByDepartmentHandler(reader);
        var result = await handler.HandleAsync(new ListCitiesByDepartmentQuery("17"));

        Assert.Equal(expected, result);
        await reader.Received(1).ListCitiesByDepartmentAsync("17", default);
    }

    [Fact]
    public async Task HandleAsync_UnknownDepartment_ReturnsEmptyList()
    {
        var reader = Substitute.For<ICatalogReader>();
        reader.ListCitiesByDepartmentAsync("99", default).Returns(new List<CityDto>());

        var handler = new ListCitiesByDepartmentHandler(reader);
        var result = await handler.HandleAsync(new ListCitiesByDepartmentQuery("99"));

        Assert.Empty(result);
    }
}

public class ListSpecialtiesHandlerTests
{
    [Fact]
    public async Task HandleAsync_CallsReaderAndReturnsDtos()
    {
        var reader = Substitute.For<ICatalogReader>();
        var expected = new List<SpecialtyDto> { new("03", "CIVIL") };
        reader.ListSpecialtiesAsync(default).Returns(expected);

        var handler = new ListSpecialtiesHandler(reader);
        var result = await handler.HandleAsync(new ListSpecialtiesQuery());

        Assert.Equal(expected, result);
        await reader.Received(1).ListSpecialtiesAsync(default);
    }
}

public class ListEntitiesHandlerTests
{
    [Fact]
    public async Task HandleAsync_CallsReaderAndReturnsDtos()
    {
        var reader = Substitute.For<ICatalogReader>();
        var expected = new List<EntityDto> { new("01", "JUZGADO") };
        reader.ListEntitiesAsync(default).Returns(expected);

        var handler = new ListEntitiesHandler(reader);
        var result = await handler.HandleAsync(new ListEntitiesQuery());

        Assert.Equal(expected, result);
        await reader.Received(1).ListEntitiesAsync(default);
    }
}

public class ListCourtsByCityHandlerTests
{
    [Fact]
    public async Task HandleAsync_PassesAllParamsToReader()
    {
        var reader = Substitute.For<ICatalogReader>();
        var courtId = Guid.NewGuid();
        var expected = new List<CourtDto> { new(courtId, "170014003010", "JUZGADO 001 CIVIL", "01", "03", 1) };
        reader.ListCourtsByCityAsync("17001", "03", null, default).Returns(expected);

        var handler = new ListCourtsByCityHandler(reader);
        var result = await handler.HandleAsync(new ListCourtsByCityQuery("17001", "03"));

        Assert.Equal(expected, result);
        await reader.Received(1).ListCourtsByCityAsync("17001", "03", null, default);
    }

    [Fact]
    public async Task HandleAsync_WithNoFilters_PassesNullsToReader()
    {
        var reader = Substitute.For<ICatalogReader>();
        reader.ListCourtsByCityAsync("17001", null, null, default).Returns(new List<CourtDto>());

        var handler = new ListCourtsByCityHandler(reader);
        await handler.HandleAsync(new ListCourtsByCityQuery("17001"));

        await reader.Received(1).ListCourtsByCityAsync("17001", null, null, default);
    }
}

public class SearchCourtsHandlerTests
{
    [Fact]
    public async Task HandleAsync_PassesNameLikeAndCityIdToReader()
    {
        var reader = Substitute.For<ICatalogReader>();
        var expected = new List<CourtDto> { new(Guid.NewGuid(), "170014003010", "JUZGADO CIVIL MUNICIPAL", null, null, null) };
        reader.SearchCourtsAsync("civil municipal", "17001", default).Returns(expected);

        var handler = new SearchCourtsHandler(reader);
        var result = await handler.HandleAsync(new SearchCourtsQuery("civil municipal", "17001"));

        Assert.Equal(expected, result);
        await reader.Received(1).SearchCourtsAsync("civil municipal", "17001", default);
    }

    [Fact]
    public async Task HandleAsync_WithoutCityId_PassesNullCityId()
    {
        var reader = Substitute.For<ICatalogReader>();
        reader.SearchCourtsAsync("civil", null, default).Returns(new List<CourtDto>());

        var handler = new SearchCourtsHandler(reader);
        await handler.HandleAsync(new SearchCourtsQuery("civil"));

        await reader.Received(1).SearchCourtsAsync("civil", null, default);
    }
}
