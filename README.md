# Grammophone.Queueing.Tests
This is a test suite for implementations of the queueing contract defined in the `Grammophone.Queueing` class library.

An abstract test class is defined in the `QueueingTests` type. Tests for implementstions will 
iherit from this class and override the `CreateQueueingProvider` abstract method, optionally
overriding the `TestInitializeAsync` and `TestCleanupAsync` methods if needed.

This test project expects to be in a sibling directory to the following:
* Grammophone.Queueing
* Grammophone.Queueing.Azure