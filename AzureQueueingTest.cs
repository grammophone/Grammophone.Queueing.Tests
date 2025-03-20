using Microsoft.VisualStudio.TestTools.UnitTesting;
using Grammophone.Queueing.Azure;
using Azure.Storage.Queues;
using System;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Grammophone.Queueing.Tests
{
	[TestClass]
	public class AzureQueueingTests : QueueingTests
	{
		private const string ConnectionString = "UseDevelopmentStorage=true";
		private const string QueueName = "testqueue"; // Azure queues are lowercase
		private const QueueClientOptions.ServiceVersion AzureServiceVersion = QueueClientOptions.ServiceVersion.V2025_01_05;
		private static Process? azuriteProcess;

		[ClassInitialize]
		public static void Initialize(TestContext context)
		{
			string azuritePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Azurite", "azurite.exe");
			string dataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");

			if (!File.Exists(azuritePath))
			{
				Assert.Fail($"Azurite executable not found at {azuritePath}. Ensure it’s copied to the Azurite subfolder.");
			}

			// Clean any previous Data folder
			if (Directory.Exists(dataDir))
			{
				Directory.Delete(dataDir, true);
			}

			ProcessStartInfo startInfo = new ProcessStartInfo
			{
				FileName = azuritePath,
				Arguments = $"--location \"{dataDir}\" --silent",
				UseShellExecute = false,
				CreateNoWindow = true,
				RedirectStandardOutput = true,
				RedirectStandardError = true
			};

			azuriteProcess = Process.Start(startInfo);
			if (azuriteProcess == null || azuriteProcess.HasExited)
			{
				Assert.Fail("Failed to start Azurite process.");
			}

			var client = CreateAzureQueueClient();

			client.CreateIfNotExists();
		}

		[ClassCleanup]
		public static void Cleanup()
		{
			if (azuriteProcess != null && !azuriteProcess.HasExited)
			{
				azuriteProcess.Kill();
				azuriteProcess.WaitForExit();
				azuriteProcess.Dispose();
				azuriteProcess = null;
			}

			string dataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
			if (Directory.Exists(dataDir))
			{
				try
				{
					Directory.Delete(dataDir, true);
				}
				catch (IOException ex)
				{
					Console.WriteLine($"Failed to delete Data folder: {ex.Message}");
				}
			}
		}

		[TestInitialize]
		public override async Task TestInitializeAsync()
		{
			await base.TestInitializeAsync();
			
			// Add any Azure-specific setup here if needed later
		}

		[TestCleanup]
		public override async Task TestCleanupAsync()
		{
			await base.TestCleanupAsync();

			var client = CreateAzureQueueClient();
			
			await client.ClearMessagesAsync();
		}

		protected override IQueueingProvider CreateQueueingProvider()
		{
			return new AzureQueueingProvider(
					ConnectionString,
					QueueName,
					visibilityTimeout: TimeSpan.FromSeconds(1),
					timeToLive: TimeSpan.FromDays(7),
					AzureServiceVersion);
		}

		private static QueueClient CreateAzureQueueClient()
		{
			var queueClientOptions = new QueueClientOptions(AzureServiceVersion)
			{
			};

			var client = new QueueClient(ConnectionString, QueueName, queueClientOptions);

			return client;
		}
	}
}
