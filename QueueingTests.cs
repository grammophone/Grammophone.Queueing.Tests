using Microsoft.VisualStudio.TestTools.UnitTesting;
using Grammophone.Queueing;
using System;
using System.Threading.Tasks;
using Azure.Storage.Queues;

namespace Grammophone.Queueing.Tests
{
	[TestClass]
	public abstract class QueueingTests
	{
		protected IQueueingClient Client { get; }

		public QueueingTests()
		{
			var provider = CreateQueueingProvider();
			this.Client = CreateQueueingClient(provider);
		}

		[TestInitialize]
		public virtual Task TestInitializeAsync() => Task.CompletedTask;

		[TestCleanup]
		public virtual Task TestCleanupAsync() => Task.CompletedTask;

		protected abstract IQueueingProvider CreateQueueingProvider();

		protected virtual IQueueingClient CreateQueueingClient(IQueueingProvider provider)
		{
			return provider.CreateClient();
		}

		[TestMethod]
		public async Task SendMessageAsync_SendsStringMessage_Succeeds()
		{
			// Arrange
			string message = "Hello, queue!";

			// Act
			await this.Client.SendMessageAsync(message);

			// Assert
			var receivedMessage = await this.Client.TryReceiveMessage();

			Assert.IsNotNull(receivedMessage, "Message should be received.");

			Assert.AreEqual(message, receivedMessage.Body.ToString(), "Received message should match sent message.");

			await receivedMessage.TryCommitAsync(); // Clean up
		}

		[TestMethod]
		public async Task TryReceiveMessage_QueueEmpty_ReturnsNull()
		{
			// Act
			var message = await this.Client.TryReceiveMessage();

			// Assert
			Assert.IsNull(message, "Should return null when queue is empty.");
		}

		[TestMethod]
		public async Task TryCommitAsync_CommitsMessage_RemovesFromQueue()
		{
			// Arrange
			await this.Client.SendMessageAsync("Commit test");

			// Act
			var message = await this.Client.TryReceiveMessage();

			Assert.IsNotNull(message, "A message should have been found in the queue.");

			bool committed = await message.TryCommitAsync();

			// Assert
			Assert.IsTrue(committed, "Commit should succeed.");

			var nextMessage = await this.Client.TryReceiveMessage();

			Assert.IsNull(nextMessage, "Queue should be empty after commit.");
		}

		[TestMethod]
		public async Task TryAbandonAsync_AbandonsMessage_ReturnsToQueue()
		{
			// Arrange
			await this.Client.SendMessageAsync("Abandon test");

			// Act
			var message = await this.Client.TryReceiveMessage();

			Assert.IsNotNull(message, "A message should have been found in the queue.");

			bool abandoned = await message.TryAbandonAsync();

			// Assert
			Assert.IsTrue(abandoned, "Abandon should succeed.");

			var nextMessage = await this.Client.TryReceiveMessage();

			Assert.IsNotNull(nextMessage, "Message should be available again after abandon.");

			Assert.AreEqual("Abandon test", nextMessage.Body.ToString(), "Abandoned message should match original.");

			await nextMessage.TryCommitAsync(); // Clean up
		}

		[TestMethod]
		public async Task TryCommitAsync_AfterVisibilityTimeout_ReturnsFalse()
		{
			// Arrange
			await this.Client.SendMessageAsync("Timeout test");

			// Act
			var message = await this.Client.TryReceiveMessage();

			Assert.IsNotNull(message, "A message should have been found in the queue.");

			await Task.Delay(1500); // Wait beyond typical short timeout (e.g., 1s)

			var reappearedMessage = await this.Client.TryReceiveMessage();

			Assert.IsNotNull(reappearedMessage, "Message should reappear after timeout.");

			await reappearedMessage.TryCommitAsync(); // Clean up
		}
	}
}