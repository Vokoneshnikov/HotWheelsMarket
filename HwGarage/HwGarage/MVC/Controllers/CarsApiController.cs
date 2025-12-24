using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using HwGarage.Core.Http;
using HwGarage.Core.Orm;
using HwGarage.Core.Orm.Models;
using HwGarage.MVC.Services;

namespace HwGarage.MVC.Controllers
{
    public class CarsApiController : BaseController
    {
        private readonly DbContext _db;
        private readonly CarService _carService;

        public CarsApiController(ViewRenderer renderer, DbContext db)
            : base(renderer)
        {
            _db = db;
            _carService = new CarService(db);
        }

        // POST /api/cars
        public async Task Create(HttpContext context)
{
    var user = context.User as User;
    if (user == null)
    {
        context.Response.StatusCode = 401;
        await context.WriteAsync(
            "{\"success\":false,\"error\":\"unauthorized\"}",
            "application/json");
        return;
    }

    var form = await context.ReadFormAsync();

    string name        = (form["name"] ?? string.Empty).Trim();
    string description = (form["description"] ?? string.Empty).Trim();
    string status      = (form["status"] ?? "pending").Trim();

    var photo = form.Files.ContainsKey("photo") ? form.Files["photo"] : null;

    string? filePath = null;

    if (photo != null && photo.Length > 0)
    {
        try
        {
            string baseDir = AppContext.BaseDirectory;
            string? projectRoot = Directory.GetParent(baseDir)?
                .Parent?.Parent?.Parent?.FullName ?? baseDir;

            string uploadsDir = Path.Combine(projectRoot, "Public", "uploads", "cars");
            Directory.CreateDirectory(uploadsDir);

            // берём только имя файла и чистим его
            string safeName = Path.GetFileName(photo.FileName);

            // убираем пробелы и прочие "опасные" символы
            // можно просто заменить пробелы на подчёркивания
            safeName = safeName.Replace(" ", "_");

            string uniqueName   = $"{Guid.NewGuid()}_{safeName}";
            string absolutePath = Path.Combine(uploadsDir, uniqueName);

            await using var stream = File.Create(absolutePath);
            await photo.CopyToAsync(stream);

            // относительный путь от корня сайта
            filePath = $"/uploads/cars/{uniqueName}";


        }
        catch (Exception ex)
        {
            context.Response.StatusCode = 500;
            await context.WriteAsync(
                JsonSerializer.Serialize(new
                {
                    success = false,
                    error = "Ошибка при сохранении файла: " + ex.Message
                }),
                "application/json");
            return;
        }
    }

    var result = await _carService.AddCarAsync(user, name, description, filePath);

    if (!result.Success)
    {
        context.Response.StatusCode = 400;
        await context.WriteAsync(
            JsonSerializer.Serialize(new
            {
                success = false,
                error = result.ErrorMessage ?? "Ошибка при сохранении машинки."
            }),
            "application/json");
        return;
    }

    var responseJson = JsonSerializer.Serialize(new
    {
        success = true,
        car = new
        {
            id = 0, // при желании можно вернуть реальный id
            name,
            description,
            status,
            photoUrl = filePath
        }
    });

    await context.WriteAsync(responseJson, "application/json");
}
    }
}
