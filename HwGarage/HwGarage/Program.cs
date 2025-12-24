using System;
using System.IO;
using System.Threading.Tasks;
using HwGarage.Core.Auth;
using HwGarage.Core.Http;
using HwGarage.Core.Orm;
using HwGarage.MVC;
using HwGarage.MVC.Controllers;
using Stripe;
using Stripe.Checkout;
internal class Program
{
    private static async Task Main()
    {
        var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");
        var connectionString = $"Host=localhost;Port=5432;Username=postgres;Password={dbPassword};Database=hwdatabase;Client Encoding=UTF8;";
       
        
        var stripeSecret = Environment.GetEnvironmentVariable("STRIPE_SECRET_KEY");

        if (string.IsNullOrWhiteSpace(stripeSecret))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("[STRIPE ERROR] STRIPE_SECRET_KEY NOT FOUND in environment!");
            Console.ResetColor();
        }
        else
        {
            Console.WriteLine("[STRIPE] Secret key loaded from environment.");
        }
        
        var router = new Router();
        var sessions = new SessionManager();
        var db = new DbContext(connectionString);

        //подняться вверх по папкам
        string projectRoot = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "../../../"
        ));

        string viewsPath = Path.Combine(projectRoot, "MVC", "Views");
        string staticPath = Path.Combine(projectRoot, "Public");


        Console.WriteLine($"[SERVER] Using views from: {viewsPath}");
        Console.WriteLine($"[SERVER] Serving static from: {staticPath}");

        var renderer = new ViewRenderer(viewsPath);
        
        var homeController = new HomeController(renderer);
        var profileController = new ProfileController(renderer, db);
        var authController = new AuthController(renderer, db, sessions);
        var carsController = new CarsController(renderer, db);
        var marketController = new MarketplaceController(renderer, db);
        var adminController = new AdminController(renderer, db);
        var auctionController = new AuctionController(renderer, db);
        var profileApiController = new ProfileApiController(renderer, db);
        var walletController = new WalletController(renderer, db, stripeSecret);
        var carsApiController = new CarsApiController(renderer, db);
        var auctionApiController = new AuctionsApiController(renderer, db);
        var marketApiController = new MarketApiController(renderer, db);
        var myCarsApiController = new MyCarsApiController(renderer, db);

        
        
        router.Get("/", homeController.Index);
        
        router.Get("/profile", profileController.Index);
        router.Post("/profile", profileController.Update);

        router.Get("/login", authController.Login);
        router.Post("/login", authController.LoginPost);
        router.Get("/register", authController.Register);
        router.Post("/register", authController.RegisterPost);
        router.Get("/logout", authController.Logout);

        router.Post("/cars/add", carsController.AddPost);
        router.Post("/cars/delete", carsController.DeletePost);

        router.Get("/market", marketController.Index);
        router.Get("/market/add", marketController.Add);
        router.Post("/market/add", marketController.AddPost);
        router.Post("/market/buy", marketController.BuyPost);

        router.Get("/admin/moderation", adminController.Moderation);
        router.Post("/admin/moderation", adminController.ModerationPost);

        router.Get("/auction", auctionController.Index);
        router.Get("/auction/create", auctionController.Create);
        router.Post("/auction/create", auctionController.CreatePost);
        router.Get("/auction/view", auctionController.View);
        router.Post("/auction/bid", auctionController.BidPost);

        router.Get("/wallet", walletController.Index);
        router.Post("/wallet/create-session", walletController.CreateSession);
        router.Get("/wallet/success", walletController.Success);
        router.Get("/wallet/cancel", walletController.Cancel);

        router.Get("/api/profile", profileApiController.GetProfile); // <-- добавить
        router.Post("/api/cars", carsApiController.Create);
        router.Get("/api/auctions", auctionApiController.GetActiveAuctions);
        router.Post("/api/auctions", auctionApiController.CreateAuction);
        router.Get("/api/market", marketApiController.GetListings);
        router.Post("/api/market/add", marketApiController.AddListing);
        router.Post("/api/market/buy", marketApiController.Buy);
        router.Get("/api/my-cars/available", myCarsApiController.GetAvailable);
        router.Post("/api/wallet/create-session", walletController.CreateSessionApi);
        
        var server = new HttpServer("http://localhost:8080/", router, sessions, db, staticPath);
        await server.StartAsync();
    }
}
