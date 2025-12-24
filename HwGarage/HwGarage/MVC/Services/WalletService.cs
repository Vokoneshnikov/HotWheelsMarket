using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using HwGarage.Core.Orm;
using HwGarage.Core.Orm.Models;
using Stripe;
using Stripe.Checkout;

namespace HwGarage.MVC.Services
{
    public class WalletService
    {
        private readonly DbContext _db;
        private readonly StripeClient _stripeClient;

        public WalletService(DbContext db, StripeClient stripeClient)
        {
            _db = db;
            _stripeClient = stripeClient;
        }
        
        public async Task<ServiceResult<string>> CreateTopUpSessionAsync(
            User user,
            decimal amount,
            string successUrlTemplate,
            string cancelUrl)
        {
            if (amount <= 0)
            {
                return ServiceResult<string>.Fail("Некорректная сумма пополнения.");
            }

            long amountInCents = (long)(amount * 100m);

            var options = new SessionCreateOptions
            {
                Mode = "payment",
                SuccessUrl = successUrlTemplate,
                CancelUrl = cancelUrl,
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        Quantity = 1,
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = "rub",
                            UnitAmount = amountInCents,
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = "HwGarage balance top-up"
                            }
                        }
                    }
                },
                Metadata = new Dictionary<string, string>
                {
                    ["user_id"] = user.Id.ToString(),
                    ["amount"] = amount.ToString(CultureInfo.InvariantCulture)
                }
            };

            try
            {
                var service = new SessionService(_stripeClient);
                var session = await service.CreateAsync(options);

                if (string.IsNullOrWhiteSpace(session.Url))
                {
                    return ServiceResult<string>.Fail("Не удалось получить URL платёжной сессии.");
                }

                return ServiceResult<string>.Ok(session.Url);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[STRIPE ERROR] " + ex);
                return ServiceResult<string>.Fail("Ошибка при создании платёжной сессии.");
            }
        }
        
        public async Task<ServiceResult<decimal>> ProcessSuccessSessionAsync(
            User currentUser,
            string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return ServiceResult<decimal>.Fail("Отсутствует session_id.");
            }

            try
            {
                var service = new SessionService(_stripeClient);
                var session = await service.GetAsync(sessionId);

                if (session.PaymentStatus != "paid")
                {
                    return ServiceResult<decimal>.Fail("Платёж не завершён.");
                }

                decimal amount = session.AmountTotal.GetValueOrDefault() / 100m;

                // Пытаемся взять user_id из метаданных
                int targetUserId = currentUser.Id;

                if (session.Metadata != null &&
                    session.Metadata.TryGetValue("user_id", out var userIdStr) &&
                    int.TryParse(userIdStr, out int userIdFromMeta))
                {
                    targetUserId = userIdFromMeta;
                }

                var dbUser = await _db.Users.FindAsync(targetUserId);
                if (dbUser == null)
                {
                    return ServiceResult<decimal>.Fail("Пользователь не найден.");
                }

                dbUser.Balance += amount;
                await _db.Users.UpdateAsync(dbUser.Id, dbUser);

                return ServiceResult<decimal>.Ok(amount);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[STRIPE SUCCESS ERROR] " + ex);
                return ServiceResult<decimal>.Fail("Ошибка обработки транзакции.");
            }
        }
    }
}
