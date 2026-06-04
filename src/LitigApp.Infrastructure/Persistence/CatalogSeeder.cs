using LitigApp.Domain.Catalog;
using Microsoft.EntityFrameworkCore;

namespace LitigApp.Infrastructure.Persistence;

/// <summary>
/// Seeds the geographic/judicial catalog (departments, cities, entities, specialties)
/// with official DANE codes. Idempotent per table: only inserts when the table is empty.
/// Invoked via: dotnet run --project src/LitigApp.Api -- seed-catalog
/// IDs are natural keys (char(N)) — leading zeros must be preserved ("05", "08", "17001").
/// </summary>
public static class CatalogSeeder
{
    public static async Task SeedAsync(AppDbContext db, CancellationToken ct = default)
    {
        // Order matters: departments before cities (FK department_id -> departments.id).
        await SeedDepartmentsAsync(db, ct);
        await SeedCitiesAsync(db, ct);
        await SeedEntitiesAsync(db, ct);
        await SeedSpecialtiesAsync(db, ct);
    }

    private static async Task SeedDepartmentsAsync(AppDbContext db, CancellationToken ct)
    {
        if (await db.Departments.AnyAsync(ct))
            return;

        db.Departments.AddRange(
            new Department { Id = "05", Name = "ANTIOQUIA" },
            new Department { Id = "08", Name = "ATLÁNTICO" },
            new Department { Id = "11", Name = "BOGOTÁ" },
            new Department { Id = "13", Name = "BOLÍVAR" },
            new Department { Id = "15", Name = "BOYACÁ" },
            new Department { Id = "17", Name = "CALDAS" },
            new Department { Id = "18", Name = "CAQUETÁ" },
            new Department { Id = "19", Name = "CAUCA" },
            new Department { Id = "20", Name = "CESAR" },
            new Department { Id = "23", Name = "CÓRDOBA" },
            new Department { Id = "25", Name = "CUNDINAMARCA" },
            new Department { Id = "27", Name = "CHOCÓ" },
            new Department { Id = "41", Name = "HUILA" },
            new Department { Id = "44", Name = "LA GUAJIRA" },
            new Department { Id = "47", Name = "MAGDALENA" },
            new Department { Id = "50", Name = "META" },
            new Department { Id = "52", Name = "NARIÑO" },
            new Department { Id = "54", Name = "NORTE DE SANTANDER" },
            new Department { Id = "63", Name = "QUINDÍO" },
            new Department { Id = "66", Name = "RISARALDA" },
            new Department { Id = "68", Name = "SANTANDER" },
            new Department { Id = "70", Name = "SUCRE" },
            new Department { Id = "73", Name = "TOLIMA" },
            new Department { Id = "76", Name = "VALLE DEL CAUCA" },
            new Department { Id = "81", Name = "ARAUCA" },
            new Department { Id = "85", Name = "CASANARE" },
            new Department { Id = "86", Name = "PUTUMAYO" },
            new Department { Id = "88", Name = "SAN ANDRÉS" },
            new Department { Id = "91", Name = "AMAZONAS" },
            new Department { Id = "94", Name = "GUAINÍA" },
            new Department { Id = "95", Name = "GUAVIARE" },
            new Department { Id = "97", Name = "VAUPÉS" },
            new Department { Id = "99", Name = "VICHADA" });

        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedCitiesAsync(AppDbContext db, CancellationToken ct)
    {
        if (await db.Cities.AnyAsync(ct))
            return;

        // department_id = first 2 chars of the 5-char DANE code.
        City C(string id, string name) => new() { Id = id, DepartmentId = id[..2], Name = name };

        db.Cities.AddRange(
            // RISARALDA (66)
            C("66001", "PEREIRA"),
            C("66045", "APÍA"),
            C("66075", "BALBOA"),
            C("66088", "BELÉN DE UMBRÍA"),
            C("66170", "DOSQUEBRADAS"),
            C("66318", "GUÁTICA"),
            C("66383", "LA CELIA"),
            C("66400", "LA VIRGINIA"),
            C("66440", "MARSELLA"),
            C("66456", "MISTRATÓ"),
            C("66572", "PUEBLO RICO"),
            C("66594", "QUINCHIA"),
            C("66682", "SANTA ROSA DE CABAL"),
            C("66687", "SANTUARIO"),
            // CALDAS (17)
            C("17001", "MANIZALES"),
            C("17013", "AGUADAS"),
            C("17042", "ANSERMA"),
            C("17050", "ARANZAZU"),
            C("17088", "BELALCÁZAR"),
            C("17174", "CHINCHINÁ"),
            C("17272", "FILADELFIA"),
            C("17380", "LA DORADA"),
            C("17388", "LA MERCED"),
            C("17433", "MANZANARES"),
            C("17442", "MARMATO"),
            C("17444", "MARQUETALIA"),
            C("17446", "MARULANDA"),
            C("17486", "NEIRA"),
            C("17495", "NORCASIA"),
            C("17513", "PÁCORA"),
            C("17524", "PALESTINA"),
            C("17541", "PENSILVANIA"),
            C("17614", "RIOSUCIO"),
            C("17616", "RISARALDA"),
            C("17653", "SALAMINA"),
            C("17662", "SAMANÁ"),
            C("17665", "SAN JOSÉ"),
            C("17777", "SUPÍA"),
            C("17867", "VICTORIA"),
            C("17873", "VILLAMARÍA"),
            C("17877", "VITERBO"),
            // QUINDÍO (63)
            C("63001", "ARMENIA"),
            C("63111", "BUENAVISTA"),
            C("63130", "CALARCÁ"),
            C("63190", "CIRCASIA"),
            C("63212", "CÓRDOBA"),
            C("63272", "FILANDIA"),
            C("63302", "GÉNOVA"),
            C("63401", "LA TEBAIDA"),
            C("63470", "MONTENEGRO"),
            C("63548", "PIJAO"),
            C("63594", "QUIMBAYA"),
            C("63690", "SALENTO"));

        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedEntitiesAsync(AppDbContext db, CancellationToken ct)
    {
        if (await db.Entities.AnyAsync(ct))
            return;

        db.Entities.AddRange(
            new Entity { Code = "02", Name = "CORTE SUPREMA DE JUSTICIA" },
            new Entity { Code = "03", Name = "CONSEJO DE ESTADO" },
            new Entity { Code = "04", Name = "CORTE CONSTITUCIONAL" },
            new Entity { Code = "10", Name = "CONSEJO SUPERIOR DE LA JUDICATURA" },
            new Entity { Code = "12", Name = "DIRECCION EJECUTIVA SECCIONAL DE ADMINISTRACION JUDICIAL DESAJ" },
            new Entity { Code = "13", Name = "CONSEJO SECCIONAL" },
            new Entity { Code = "22", Name = "TRIBUNAL SUPERIOR" },
            new Entity { Code = "23", Name = "TRIBUNAL ADMINISTRATIVO" },
            new Entity { Code = "31", Name = "JUZGADO DE CIRCUITO" },
            new Entity { Code = "33", Name = "JUZGADO ADMINISTRATIVO" },
            new Entity { Code = "34", Name = "JUZGADO CIRCUITO DE EJECUCIÓN" },
            new Entity { Code = "40", Name = "JUZGADO MUNICIPAL" },
            new Entity { Code = "41", Name = "JUZGADO DE PEQUEÑAS CAUSAS" },
            new Entity { Code = "43", Name = "JUZGADO MUNICIPAL DE EJECUCIÓN" },
            new Entity { Code = "70", Name = "CENTRO DE SERVICIOS ADMINISTRATIVOS" },
            new Entity { Code = "71", Name = "CENTRO DE SERVICIOS JUDICIALES" });

        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedSpecialtiesAsync(AppDbContext db, CancellationToken ct)
    {
        if (await db.Specialties.AnyAsync(ct))
            return;

        db.Specialties.AddRange(
            new Specialty { Code = "00", Name = "CONSTITUCIONAL" },
            new Specialty { Code = "01", Name = "ADMINISTRATIVA" },
            new Specialty { Code = "03", Name = "CIVIL" },
            new Specialty { Code = "04", Name = "PENAL" },
            new Specialty { Code = "05", Name = "LABORAL" },
            new Specialty { Code = "07", Name = "PENAL ESPECIALIZADO" },
            new Specialty { Code = "08", Name = "SALA ÚNICA" },
            new Specialty { Code = "09", Name = "PENAL CON FUNCIÓN DE CONOCIMIENTO" },
            new Specialty { Code = "10", Name = "FAMILIA" },
            new Specialty { Code = "12", Name = "CIVIL - LABORAL" },
            new Specialty { Code = "13", Name = "CIVIL - FAMILIA" },
            new Specialty { Code = "14", Name = "CIVIL - FAMILIA - LABORAL" },
            new Specialty { Code = "15", Name = "SECRETARÍA GENERAL" },
            new Specialty { Code = "18", Name = "PENAL PARA ADOLESCENTES CON FUNCIÓN DE CONOCIMIENTO" },
            new Specialty { Code = "19", Name = "PENAL JUSTICIA Y PAZ" },
            new Specialty { Code = "20", Name = "PENAL ESPECIALIZADOS DE EXTINCIÓN DE DOMINIO" },
            new Specialty { Code = "21", Name = "CIVIL RESTITUCIÓN DE TIERRAS" },
            new Specialty { Code = "24", Name = "SECCIÓN PRIMERA" },
            new Specialty { Code = "25", Name = "SECCIÓN SEGUNDA" },
            new Specialty { Code = "26", Name = "SECCIÓN TERCERA" },
            new Specialty { Code = "27", Name = "SECCIÓN CUARTA" },
            new Specialty { Code = "31", Name = "SIN SECCIÓN" },
            new Specialty { Code = "32", Name = "OFICINA JUDICIAL" },
            new Specialty { Code = "33", Name = "SIN SECCIÓN - ORAL" },
            new Specialty { Code = "34", Name = "SECCIÓN PRIMERA - ORAL" },
            new Specialty { Code = "35", Name = "SECCIÓN SEGUNDA - ORAL" },
            new Specialty { Code = "36", Name = "SECCIÓN TERCERA - ORAL" },
            new Specialty { Code = "37", Name = "SECCIÓN CUARTA - ORAL" },
            new Specialty { Code = "39", Name = "SECCIÓN ÚNICA MIXTA (ESCRIT-ORAL)" },
            new Specialty { Code = "40", Name = "SIN SECCIÓN - MIXTA" },
            new Specialty { Code = "42", Name = "SECCIÓN SEGUNDA MIXTA - ORAL" },
            new Specialty { Code = "43", Name = "SECCIÓN TERCERA MIXTA - ORAL" },
            new Specialty { Code = "46", Name = "PENAL MIXTO(LEYES 600, 906 Y 1098)" },
            new Specialty { Code = "53", Name = "CIVIL ORALIDAD" },
            new Specialty { Code = "60", Name = "FAMILIA ORALIDAD" },
            new Specialty { Code = "70", Name = "CENTRO DE SERVICIOS ADMINISTRATIVOS" },
            new Specialty { Code = "71", Name = "PENAL PARA ADOLESCENTES CON FUNCIÓN DE CONTROL DE GARANTÍAS" },
            new Specialty { Code = "72", Name = "CENTRO DE SERVICIOS JUDICIALES" },
            new Specialty { Code = "82", Name = "OFICINA DE SERVICIOS" },
            new Specialty { Code = "84", Name = "PROMISCUO DE FAMILIA" },
            new Specialty { Code = "87", Name = "EJECUCIÓN DE PENAS Y MEDIDAS DE SEGURIDAD" },
            new Specialty { Code = "88", Name = "PENAL CON FUNCIÓN DE CONTROL DE GARANTÍAS" },
            new Specialty { Code = "89", Name = "PROMISCUO / COMPETENCIA MÚLTIPLE" },
            new Specialty { Code = "98", Name = "CONSEJO SUPERIOR" });

        await db.SaveChangesAsync(ct);
    }
}
