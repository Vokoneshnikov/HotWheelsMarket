using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Threading.Tasks;
using HwGarage.Core.Http;
using HwGarage.Core.Orm;
using HwGarage.Core.Orm.Models;
using HwGarage.MVC.Services;
using Stripe;

namespace HwGarage.MVC.Controllers
{
    public class WalletController : BaseController
    {
        private readonly DbContext _db;
        private readonly StripeClient _stripeClient;
        private readonly WalletService _walletService;

        public WalletController(ViewRenderer renderer, DbContext db, string? stripeKey)
            : base(renderer)
        {
            _db = db;

            if (string.IsNullOrWhiteSpace(stripeKey))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[STRIPE ERROR] No Stripe key provided to WalletController!");
                Console.ResetColor();

                _stripeClient = new StripeClient("sk_test_EMPTY_KEY");
            }
            else
            {
                _stripeClient = new StripeClient(stripeKey);
                Console.WriteLine("[STRIPE] WalletController initialized with environment key.");
            }

            _walletService = new WalletService(db, _stripeClient);
        }

        // HTML-страница кошелька
        public async Task Index(HttpContext context)
        {
            var user = context.User as User;
            if (user == null)
            {
                context.Redirect("/login");
                return;
            }

            await RenderView(context, "wallet/index.html", new Dictionary<string, object>
            {
                ["balance"] = user.Balance,
                ["error"]   = "",
                ["amount"]  = ""
            });
        }

        // HTML-обработчик формы (старый сценарий)
        public async Task CreateSession(HttpContext context)
        {
            var user = context.User as User;
            if (user == null)
            {
                context.Redirect("/login");
                return;
            }

            var form = await context.ReadFormAsync();
            var amountRaw = form["amount"] ?? "";

            if (!decimal.TryParse(amountRaw, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount) ||
                amount <= 0)
            {
                await context.WriteAsync("Некорректная сумма пополнения.", "text/plain; charset=utf-8", 400);
                return;
            }

            string successUrl = "http://localhost:8080/wallet/success?session_id={CHECKOUT_SESSION_ID}";
            string cancelUrl  = "http://localhost:8080/wallet/cancel";

            var result = await _walletService.CreateTopUpSessionAsync(user, amount, successUrl, cancelUrl);

            if (!result.Success || string.IsNullOrWhiteSpace(result.Data))
            {
                await context.WriteAsync(
                    result.ErrorMessage ?? "Ошибка при создании платёжной сессии.",
                    "text/plain; charset=utf-8",
                    500);
                return;
            }

            context.Response.StatusCode = 303;
            context.Redirect(result.Data);
        }

        // API для SPA: POST /api/wallet/create-session
        // ожидает JSON: { amount: long } (в копейках)
        public async Task CreateSessionApi(HttpContext context)
        {
            var user = context.User as User;
            if (user == null)
            {
                context.Response.StatusCode = 401;
                await context.WriteAsync(
                    JsonSerializer.Serialize(new { success = false, error = "unauthorized" }),
                    "application/json");
                return;
            }

            long amountMinorUnits = 50000; // 500 ₽ по умолчанию

            try
            {
                using var doc = await JsonDocument.ParseAsync(context.Request.InputStream);
                var root = doc.RootElement;

                if (root.TryGetProperty("amount", out var amountEl) &&
                    long.TryParse(amountEl.ToString(), out var parsed) &&
                    parsed > 0)
                {
                    amountMinorUnits = parsed;
                }
            }
            catch
            {
                // битое тело — используем значение по умолчанию
            }

            decimal amountRub = amountMinorUnits / 100m;

            string successUrl = "http://localhost:8080/wallet/success?session_id={CHECKOUT_SESSION_ID}";
            string cancelUrl  = "http://localhost:8080/wallet/cancel";

            var result = await _walletService.CreateTopUpSessionAsync(user, amountRub, successUrl, cancelUrl);

            if (!result.Success || string.IsNullOrWhiteSpace(result.Data))
            {
                context.Response.StatusCode = 500;
                await context.WriteAsync(
                    JsonSerializer.Serialize(new
                    {
                        success = false,
                        error = result.ErrorMessage ?? "Ошибка при создании платёжной сессии."
                    }),
                    "application/json");
                return;
            }

            await context.WriteAsync(
                JsonSerializer.Serialize(new { success = true, url = result.Data }),
                "application/json");
        }

        public async Task Success(HttpContext context)
        {
            var user = context.User as User;
            if (user == null)
            {
                context.Redirect("/login");
                return;
            }

            var sessionId = context.Request.QueryString["session_id"];

            var result = await _walletService.ProcessSuccessSessionAsync(user, sessionId ?? "");

            if (!result.Success)
            {
                await context.WriteAsync(
                    result.ErrorMessage ?? "Ошибка обработки транзакции.",
                    "text/plain; charset=utf-8",
                    500);
                return;
            }

            context.Redirect("/profile");
        }

        public async Task Cancel(HttpContext context)
        {
            await context.WriteAsync("Оплата отменена.", "text/plain; charset=utf-8", 200);
        }
    }
}
