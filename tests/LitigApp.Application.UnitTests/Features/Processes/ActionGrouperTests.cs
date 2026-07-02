using LitigApp.Application.Features.Processes.Sync;
using LitigApp.Domain.Processes;

namespace LitigApp.Application.UnitTests.Features.Processes;

public class ActionGrouperTests
{
    private static readonly DateOnly Day = new(2026, 3, 20);

    private static ProcessAction Action(int consecutive, string type, DateOnly recorded) => new()
    {
        Id = Guid.NewGuid(),
        ProcessId = Guid.Empty,
        ExternalActionId = consecutive,
        ConsecutiveNumber = consecutive,
        Action = type,
        RecordedAt = recorded,
    };

    [Fact]
    public void Fijacion_GroupsWithPrecedingAuto_SameRecordedDate()
    {
        var auto = Action(81, "Auto ordena algo", Day);
        var fijacion = Action(82, "Fijacion estado", Day);

        ActionGrouper.AssignGroups([auto, fijacion], []);

        Assert.Equal(auto.Id, fijacion.GroupedWithId);
        Assert.Null(auto.GroupedWithId);
    }

    [Fact]
    public void Fijacion_GroupsWithAuto_FromPriorActions()
    {
        var auto = Action(81, "Auto de sustanciacion", Day); // already persisted
        var fijacion = Action(82, "Fijacion estado", Day);   // new this run

        ActionGrouper.AssignGroups([fijacion], [auto]);

        Assert.Equal(auto.Id, fijacion.GroupedWithId);
    }

    [Fact]
    public void Fijacion_NoMatchingAuto_StaysUngrouped()
    {
        var fijacion = Action(82, "Fijacion estado", Day);

        ActionGrouper.AssignGroups([fijacion], []);

        Assert.Null(fijacion.GroupedWithId);
    }

    [Fact]
    public void Fijacion_DifferentRecordedDate_DoesNotGroup()
    {
        var auto = Action(81, "Auto", new DateOnly(2026, 3, 19));
        var fijacion = Action(82, "Fijacion estado", Day);

        ActionGrouper.AssignGroups([fijacion], [auto]);

        Assert.Null(fijacion.GroupedWithId);
    }

    [Fact]
    public void Fijacion_MultipleAutos_PicksImmediatelyPreceding()
    {
        var auto1 = Action(78, "Auto uno", Day);
        var auto2 = Action(81, "Auto dos", Day);
        var fijacion = Action(82, "Fijacion estado", Day);

        ActionGrouper.AssignGroups([fijacion], [auto1, auto2]);

        Assert.Equal(auto2.Id, fijacion.GroupedWithId);
    }

    [Fact]
    public void Grouping_IsAccentAndCaseInsensitive()
    {
        var auto = Action(81, "AUTO admisorio", Day);
        var fijacion = Action(82, "Fijación Estado", Day);

        ActionGrouper.AssignGroups([fijacion], [auto]);

        Assert.Equal(auto.Id, fijacion.GroupedWithId);
    }

    [Fact]
    public void NonFijacion_IsNeverGrouped()
    {
        var auto = Action(81, "Auto", Day);
        var sentencia = Action(82, "Sentencia", Day);

        ActionGrouper.AssignGroups([auto, sentencia], []);

        Assert.Null(auto.GroupedWithId);
        Assert.Null(sentencia.GroupedWithId);
    }

    [Fact]
    public void AutoAfterFijacion_IsNotPicked()
    {
        var fijacion = Action(82, "Fijacion estado", Day);
        var autoAfter = Action(83, "Auto posterior", Day);

        ActionGrouper.AssignGroups([fijacion, autoAfter], []);

        Assert.Null(fijacion.GroupedWithId);
    }
}
