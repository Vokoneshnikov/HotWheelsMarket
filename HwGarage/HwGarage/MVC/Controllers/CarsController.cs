using HwGarage.Core.Http;
using HwGarage.Core.Orm;
using HwGarage.Core.Orm.Models;
using HwGarage.MVC.Services;
using HwGarage.MVC.Validation.Cars;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace HwGarage.MVC.Controllers
{
    public class CarsController : BaseController
    {
        private readonly DbContext _db;
        private readonly CarService _carService;

        public CarsController(ViewRenderer renderer, DbContext db) : base(renderer)
        {
            _db = db;
            _carService = new CarService(db);
        }

        public async Task AddPost(HttpContext context)
        {
            var user = context.User as User;
            if (user == null)
            {
                context.Redirect("/login");
                return;
            }

            var form = await context.ReadFormAsync();
            string name        = (form["name"] ?? string.Empty).Trim();
            string description = (form["description"] ?? string.Empty).Trim();
            var photo          = form.Files.ContainsKey("photo") ? form.Files["photo"] : null;

            bool hasPhoto       = photo != null && photo.Length > 0;
            long photoLength    = photo?.Length ?? 0;
            string? fileName    = photo?.FileName;
            string? contentType = photo?.ContentType;

            var validationResult = CarAddValidator.Validate(
                name,
                description,
                hasPhoto,
                photoLength,
                fileName,
                contentType);

            if (!validationResult.IsValid)
            {
                await context.WriteAsync(
                    validationResult.ErrorMessage ?? "Validation error.",
                    "text/plain",
                    400);
                return;
            }
            
            string? filePath = null;

            try
            {
                string baseDir = AppContext.BaseDirectory;
                string? projectRoot = Directory.GetParent(baseDir)?.Parent?.Parent?.Parent?.FullName ?? baseDir;

                string uploadsDir = Path.Combine(projectRoot, "Public", "uploads", "cars");
                Directory.CreateDirectory(uploadsDir);

                string safeName   = Path.GetFileName(photo!.FileName);
                string uniqueName = $"{Guid.NewGuid()}_{safeName}";
                string absolutePath = Path.Combine(uploadsDir, uniqueName);

                await using var stream = File.Create(absolutePath);
                await photo.CopyToAsync(stream);

                filePath = $"/uploads/cars/{uniqueName}";
            }
            catch (Exception ex)
            {
                await context.WriteAsync(
                    "Ошибка при сохранении файла: " + ex.Message,
                    "text/plain",
                    500);
                return;
            }

            var serviceResult = await _carService.AddCarAsync(user, name, description, filePath);
            if (!serviceResult.Success)
            {
                await context.WriteAsync(
                    serviceResult.ErrorMessage ?? "Ошибка при сохранении машинки.",
                    "text/plain",
                    500);
                return;
            }

            await context.WriteAsync("Car added successfully.", "text/plain", 200);
        }

        public async Task DeletePost(HttpContext context)
        {
            var user = context.User as User;
            if (user == null)
            {
                context.Redirect("/login");
                return;
            }

            var form = await context.ReadFormAsync();
            if (!int.TryParse(form["car_id"], out int carId))
            {
                await context.WriteAsync("Invalid car ID", "text/plain", 400);
                return;
            }

            var serviceResult = await _carService.DeleteCarAsync(user, carId);
            if (!serviceResult.Success)
            {
                await context.WriteAsync(
                    serviceResult.ErrorMessage ?? "Access denied",
                    "text/plain",
                    403);
                return;
            }

            context.Redirect("/profile");
        }
    }
}
