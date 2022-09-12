using VoltoChallenge.Models;
using Microsoft.EntityFrameworkCore;
using VoltoChallenge.Data;
using VoltoChallenge.Records;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<AutoContext> (options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("vonto")));
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.MapPost("/car", async(RecordAuto nuevoAuto, AutoContext db) =>
{
    if (nuevoAuto.tipo is null || string.IsNullOrEmpty(nuevoAuto.tipo) || nuevoAuto.placa is null || string.IsNullOrEmpty(nuevoAuto.placa))
    {
        return Results.BadRequest();
    }

    Auto auto = new Auto();

    // 1 == oficial
    // 2 == residentes
    // 3 == no residente
    try
    {
        var typeOfClient = (
            from item in db.TipoCliente
            where item.nombre == nuevoAuto.tipo
            select item
        ).FirstOrDefault();

        if (typeOfClient is null)
        {
            return Results.NotFound($"Not exists the type of client: {nuevoAuto.tipo}");
        }

        auto.placa = nuevoAuto.placa;
        auto.id_tipo_cliente = typeOfClient.id;
        auto.total_minutos = 0;
        db.Auto.Add(auto);
        await db.SaveChangesAsync();

        return Results.Created("/car", auto);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Can't create the record for the car: {auto.placa}", "POST record a car", 500);
    }
});

app.MapPost("/car-in", async (RecordPlaca placa, AutoContext db) =>
{
    if (placa.placa is null || string.IsNullOrEmpty(placa.placa))
    {
        return Results.BadRequest();
    }

    RegistroEntradas registro = new RegistroEntradas();

    try
    {
        var existsAuto = (
            from item in db.Auto
            where item.placa == placa.placa
            select item
        ).FirstOrDefault();

        if (existsAuto is null)
        {
            Console.WriteLine($"Not exists a car with this placa: {placa.placa}, please first create a record this car");
            return Results.NotFound($"Not exists a car with this placa: {placa.placa}, please first create a record this car");
        }

        var existsRegistro = (
            from item in db.RegistroEntradas
            where item.id_auto == existsAuto.id && item.activo == true
            select item
        ).FirstOrDefault();

        if (existsRegistro is not null)
        {
            Console.WriteLine($"Already exists active register for the car: {placa.placa}, please update their exit from the parking");
            return Results.Conflict($"Already exists active register for the car: {placa.placa}, please update their exit from the parking");
        }

        registro.id_auto = existsAuto.id;
        registro.activo = true;
        registro.entrada = DateTime.UtcNow;
        registro.salida = null;

        db.RegistroEntradas.Add(registro);
        await db.SaveChangesAsync();

        return Results.Created("/car-in", registro);
    }
    catch(Exception ex)
    {
        Console.WriteLine(ex.Message);
        return Results.Problem($"Can't create the record for the car in: {placa.placa}", "POST record a car in", 500);
    }
});

app.MapPut("/restart-month", async (AutoContext db) =>
{
    try
    {
        var registers = (
            from register in db.RegistroEntradas
            join auto in db.Auto on register.id_auto equals auto.id
            join tipo in db.TipoCliente on auto.id_tipo_cliente equals tipo.id
            where tipo.id == 2 || tipo.id == 3
            select register
        ).ToArray();

        foreach (var element in registers)
        {
            element.activo = false;
        }
        await db.SaveChangesAsync();

        return Results.Ok();
    }
    catch (Exception ex)
    {
        Console.Write($"Error while executing the task restart-month", ex.Message);
        return Results.Problem($"Error while executing the task restart-month", "PUT /restart-month", 500);
    }
});

app.MapPut("/car-out", async (string placa, AutoContext db) =>
{
    if (placa is null || string.IsNullOrEmpty(placa))
    {
        return Results.BadRequest();
    }

    try
    {
        var auto = (
            from item in db.Auto
            where item.placa == placa
            select item
        ).FirstOrDefault();

        if (auto is null)
        {
            Console.Write($"Not exists a record for this car: {placa} in the table Auto");
            return Results.NotFound($"Not exists a record for this car: {placa} in the table Auto");
        }

        var existsRegistro = (
            from item in db.RegistroEntradas
            where item.id_auto == auto.id && item.activo == true
            select item
        ).FirstOrDefault();

        if (existsRegistro is null)
        {
            Console.WriteLine($"Not exists a record for the operation POST /car-out for this car: {placa}");
            return Results.NotFound($"Not exists a record for the operation POST /car-out for this car: {placa}");
        }

        existsRegistro.salida = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Results.Ok();
    }
    catch(Exception ex)
    {
        Console.WriteLine($"Error in the record of out a car: {placa}", ex.Message);
        return Results.Problem("Internal server error", "PUT /car-out", 500);
    }
});

// Query the payment by the parameter placa
app.MapGet("/payment", async (AutoContext db) =>
{
    try
    {
        var autos = (
            from auto in db.Auto
            join tipo in db.TipoCliente on auto.id_tipo_cliente equals tipo.id
            join registro in db.RegistroEntradas on auto.id equals registro.id_auto
            where (tipo.id == 2 || tipo.id == 3) && registro.activo == true
            select new { auto.id, auto.placa, auto.total_minutos, tipo.nombre, registro.entrada, registro.salida, tipo.tarifa, id_registro = registro.id }
            //select new { auto, tipo, registro }
        ).ToArray();

        if (autos is null)
        {
            return Results.NoContent();
        }

        foreach (var item in autos)
        {
            // If exists a record without a datetime in entrada or salida then skip the process for the next item
            if (item.entrada is null || item.salida is null)
            {
                continue;
            }

            TimeSpan ts = (DateTime)item.salida - (DateTime)item.entrada;
            var auto = (from a in db.Auto where a.id == item.id select a).FirstOrDefault();
            var registro = (from b in db.RegistroEntradas where b.id == item.id_registro select b).FirstOrDefault();

            if (auto != null)
            {
                auto.total_minutos += (int)ts.TotalMinutes;
                registro.activo = false;
                await db.SaveChangesAsync();
            }
        }

        var reporte = (
            from auto in db.Auto
            join tipo in db.TipoCliente on auto.id_tipo_cliente equals tipo.id
            select new { auto.placa, auto.total_minutos, totalPago = (double)(auto.total_minutos * tipo.tarifa) }
        ).ToArray();

        return Results.Ok(reporte);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Can't create a report for the payments", ex.Message);
        return Results.Problem($"Can't create a report for the payments", "GET record a car in", 500);
    }
});

app.UseHttpsRedirection( );

app.Run();