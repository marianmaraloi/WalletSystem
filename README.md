# WalletSystem

I created a project that implements a simple wallet system for sport betting platform. I designed the solution to handle multiple concurrent users and to prevent race conditions, ensuring that the same funds aren’t spent twice, and that balances remain non-negative.
Also, I created a client console application project to send requests to the server and test different scenarios.

Features

The wallet system provides the following functionality:
Create Wallet: create a new wallet for the user. Only one wallet per user is allowed
Add Funds: adds funds to a user's wallet
Remove Funds: deducts funds from a user's wallet 
Query Balance: retrieves the current balance of the user’s wallet.
Concurrency Handling: I used optimistic concurrency control to handle simultaneous updates to the same wallet (RowVersion).
Idempotency processing: for both add and deduct balance I added an idempotency key to make sure the same request is not processed twice. Working with a wallet (money) this would be a big problem.

Technologies Used

ASP.NET Core for building RESTful APIs.
Entity Framework Core for data persistence and handling transactions.
SQL Lite to easily create a local database that would replicate a SQL Server that would be recommended for production readiness.
Redis for storing idempotency keys, ensuring duplicate requests are not processed.
Docker for starting the Redis server

Instructions on how to run the deliverable

1 clone the repository locally (you will also need .net 8 runtime)
2 run database migrations - dotnet ef database update
3 I used redis to store idempotency keys so it is necessary to set up Redis. I used Docker to start and have my Redis running locally. I used localhost:6379
4 I start my server
5 I start the client console application

How to test

For the client I created an user friendly console application with the following logic.

First you need to “Login” and by that you either press 1 for getting a random player Id or 2 entering a player Id yourself. (playerID is UDID format) The player Id will be stored locally on the client side. It Is not really a login between client-server but more to remember for future calls.
Then you have the option to create the wallet, add funds, remove funds, and query. You will need to create a wallet first, otherwise the other calls will fail,
To add and remove funds you will need to insert parameters such as amount and idempotency key which should be unique per transaction. But you can test sending requests with the same idempotency key so you will see that the server is not processing the same request twice.
One more test case is 6. Sending concurrent requests so you can test the server capabilities of handling concurrent modifications on the wallet of the database.

Improvements for production readiness

1 I used SQL Lite for local development of the assignment but for production I would change to SQL Server because it is more robust and scalable and better suited for handling multiple instances of the service with high concurrency requirements.
2 The application should be deployed in a cloud environment like Kubernetes to support horizontal scaling and load balancing to distribute requests across nodes and support a high number of concurrent users.
3 I would implement a fallback mechanism to allow some functionality when certain components fail, for example when Redis is unavailable, etc.
4 I think the use of centralized logging and monitoring systems are essential for tracking the application health and performance,
5 I would implement API rate limiting to control the number of requests users can make in a short time.
