using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using WalletSystemClient.Protocol.Requests;

namespace WalletSystemClient
{
    class Program
    {
        private static readonly HttpClient client = new HttpClient();
        private static ILogger<Program> _logger;
        private static Guid? _playerId;

        static async Task Main(string[] args)
        {
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
            });
            _logger = loggerFactory.CreateLogger<Program>();

            client.BaseAddress = new Uri("http://localhost:5000/api/Wallet/");

            while (true)
            {
                if (_playerId == null)
                {
                    await Login();
                }
                else
                {
                    DisplayMenu();
                }
            }
        }

        private static void DisplayMenu()
        {
            Console.WriteLine("\nSelect an option:");
            Console.WriteLine("1. Create Wallet");
            Console.WriteLine("2. Add Funds");
            Console.WriteLine("3. Remove Funds");
            Console.WriteLine("4. Query Current State");
            Console.WriteLine("5. Logout");
            Console.WriteLine("6. Simulate Concurrent Requests");
            Console.Write("Enter choice (1-6): ");
            var choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    CreateWallet().GetAwaiter().GetResult();
                    break;
                case "2":
                    AddFunds().GetAwaiter().GetResult();
                    break;
                case "3":
                    RemoveFunds().GetAwaiter().GetResult();
                    break;
                case "4":
                    QueryCurrentState().GetAwaiter().GetResult();
                    break;
                case "5":
                    Logout();
                    break;
                case "6":
                    SimulateConcurrentRequests().GetAwaiter().GetResult();
                    break;
                default:
                    Console.WriteLine("Invalid choice. Please select between 1 and 5.");
                    break;
            }
        }

        private static async Task SimulateConcurrentRequests()
        {
            if (_playerId == null)
            {
                Console.WriteLine("You need to log in first.");
                return;
            }

            Console.WriteLine("\nSimulating concurrent Add and Deduct balance requests...");

            Console.Write("Enter amount to add: ");
            if (!decimal.TryParse(Console.ReadLine(), out var addAmount))
            {
                Console.WriteLine("Invalid amount format.");
                return;
            }

            Console.Write("Enter amount to deduct: ");
            if (!decimal.TryParse(Console.ReadLine(), out var deductAmount))
            {
                Console.WriteLine("Invalid amount format.");
                return;
            }

            var sameKey = Guid.NewGuid().ToString(); // can use this one to test 2 request at the same time with the same key
            var addRequest = new AddBalanceRequest
            {
                PlayerId = _playerId.Value,
                Amount = addAmount,
                IdempotencyKey = Guid.NewGuid().ToString()
            };

            var deductRequest = new DeductBalanceRequest
            {
                PlayerId = _playerId.Value,
                Amount = deductAmount,
                IdempotencyKey = Guid.NewGuid().ToString()
            };

            var addTask = SendRequest("addBalance", addRequest, "Add Funds");
            var deductTask = SendRequest("deductBalance", deductRequest, "Remove Funds");

            await Task.WhenAll(addTask, deductTask);

            Console.WriteLine("Concurrent requests completed.");
        }

        private static async Task Login()
        {
            Console.WriteLine("\nLogin:");
            Console.WriteLine("1. Get a random player ID");
            Console.WriteLine("2. Enter a player ID");
            Console.Write("Choose an option (1-2): ");
            var loginChoice = Console.ReadLine();

            switch (loginChoice)
            {
                case "1":
                    _playerId = Guid.NewGuid();
                    Console.WriteLine($"Generated Player ID: {_playerId}");
                    _logger.LogInformation("Generated random player ID: {PlayerId}", _playerId);
                    break;
                case "2":
                    Console.Write("Enter Player ID (GUID format): ");
                    if (Guid.TryParse(Console.ReadLine(), out Guid parsedId))
                    {
                        _playerId = parsedId;
                        _logger.LogInformation("Using provided player ID: {PlayerId}", _playerId);
                    }
                    else
                    {
                        Console.WriteLine("Invalid Player ID format. Try again.");
                        _logger.LogWarning("Invalid Player ID format entered.");
                    }
                    break;
                default:
                    Console.WriteLine("Invalid choice. Please select 1 or 2.");
                    break;
            }
        }

        private static void Logout()
        {
            _logger.LogInformation("Player {PlayerId} logged out", _playerId);
            _playerId = null;
            Console.WriteLine("You have been logged out.");
        }

        private static async Task CreateWallet()
        {
            if (_playerId == null)
            {
                Console.WriteLine("You need to log in first.");
                return;
            }

            var request = new CreateWalletRequest
            {
                PlayerId = _playerId.Value
            };

            await SendRequest("createWallet", request, "Create Wallet");
        }

        private static async Task AddFunds()
        {
            if (_playerId == null)
            {
                Console.WriteLine("You need to log in first.");
                return;
            }

            Console.Write("Enter amount to add: ");
            if (!decimal.TryParse(Console.ReadLine(), out var amount))
            {
                Console.WriteLine("Invalid amount format.");
                return;
            }

            Console.Write("Enter Idempotency Key: ");
            var idempotencyKey = Console.ReadLine();

            var request = new AddBalanceRequest
            {
                PlayerId = _playerId.Value,
                Amount = amount,
                IdempotencyKey = idempotencyKey
            };

            await SendRequest("addBalance", request, "Add Funds");
        }

        private static async Task RemoveFunds()
        {
            if (_playerId == null)
            {
                Console.WriteLine("You need to log in first.");
                return;
            }

            Console.Write("Enter amount to remove: ");
            if (!decimal.TryParse(Console.ReadLine(), out var amount))
            {
                Console.WriteLine("Invalid amount format.");
                return;
            }

            Console.Write("Enter Idempotency Key: ");
            var idempotencyKey = Console.ReadLine();

            var request = new DeductBalanceRequest
            {
                PlayerId = _playerId.Value,
                Amount = amount,
                IdempotencyKey = idempotencyKey
            };

            await SendRequest("deductBalance", request, "Remove Funds");
        }

        private static async Task QueryCurrentState()
        {
            if (_playerId == null)
            {
                Console.WriteLine("You need to log in first.");
                return;
            }

            var endpoint = $"getBalance?PlayerId={_playerId.Value}";

            try
            {
                var response = await client.GetAsync(endpoint);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Query Current State completed successfully.");
                    Console.WriteLine($"Query Current State completed successfully. Response: {responseContent}");
                }
                else
                {
                    _logger.LogError("Failed to complete Query Current State. Status: {StatusCode}, Response: {Response}", response.StatusCode, responseContent);
                    Console.WriteLine($"Error during Query Current State: {responseContent}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while attempting to Query Current State");
                Console.WriteLine("Error: " + ex.Message);
            }
        }

        private static async Task SendRequest<T>(string endpoint, T request, string actionName)
        {
            try
            {
                var jsonRequest = JsonConvert.SerializeObject(request);
                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                var fullUrl = new Uri(client.BaseAddress, endpoint).ToString();
                _logger.LogInformation("Sending {ActionName} request to {FullUrl}: {Request}", actionName, fullUrl, jsonRequest);

                var response = await client.PostAsync(fullUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("{ActionName} completed successfully.", actionName);
                    Console.WriteLine($"{actionName} completed successfully. Response: {responseContent}");
                }
                else
                {
                    _logger.LogError("Failed to complete {ActionName}. Status: {StatusCode}, Response: {Response}", actionName, response.StatusCode, responseContent);
                    Console.WriteLine($"Error during {actionName}: " + responseContent);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while attempting to {ActionName}", actionName);
                Console.WriteLine("Error: " + ex.Message);
            }
        }
    }
}
