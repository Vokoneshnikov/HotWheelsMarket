using System;
using System.IO;
using System.Text.RegularExpressions;

namespace HwGarage.MVC.Validation.Cars
{
    using HwGarage.MVC.Validation;

    public static class CarAddValidator
    {
        private const long MaxPhotoSize = 5 * 1024 * 1024; // 5 MB

        private static readonly string[] AllowedExtensions =
            { ".jpg", ".jpeg", ".png", ".webp" };

        private static readonly Regex NameRegex =
            new(@"^[A-Za-zА-Яа-яЁё0-9\- ]{2,100}$", RegexOptions.Compiled);

        public static ValidationResult Validate(
            string name,
            string description,
            bool hasPhoto,
            long photoLength,
            string? photoFileName,
            string? photoContentType)
        {
            name = (name ?? string.Empty).Trim();
            description = description ?? string.Empty;

            if (string.IsNullOrWhiteSpace(name))
            {
                return ValidationResult.Fail("Название машинки обязательно.");
            }

            if (!NameRegex.IsMatch(name))
            {
                return ValidationResult.Fail(
                    "Название может содержать только английские буквы, цифры, пробелы и дефис, от 2 до 100 символов.");
            }

            if (description.Length > 500)
            {
                return ValidationResult.Fail(
                    "Описание не должно превышать 500 символов.");
            }

            if (!hasPhoto || photoLength <= 0)
            {
                return ValidationResult.Fail("Загрузите фотографию машинки.");
            }

            if (photoLength > MaxPhotoSize)
            {
                return ValidationResult.Fail(
                    "Размер файла слишком большой. Максимум 5 МБ.");
            }

            var ext = Path.GetExtension(photoFileName ?? string.Empty)
                ?.ToLowerInvariant() ?? string.Empty;

            if (Array.IndexOf(AllowedExtensions, ext) < 0)
            {
                return ValidationResult.Fail(
                    "Недопустимый формат файла. Разрешены: .jpg, .jpeg, .png, .webp");
            }

            if (string.IsNullOrWhiteSpace(photoContentType) ||
                !photoContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                return ValidationResult.Fail("Файл должен быть изображением.");
            }

            return ValidationResult.Ok();
        }
    }
}
