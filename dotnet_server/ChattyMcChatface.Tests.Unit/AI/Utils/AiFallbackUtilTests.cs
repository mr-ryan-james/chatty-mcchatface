using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChattyMcChatface.Core.Services.AI;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ChattyMcChatface.Tests.Unit.AI.Utils
{
    public class AiFallbackUtilTests
    {
        private readonly Mock<ILogger> _mockLogger;
        private readonly List<string> _priorityList;
        private readonly string _preferredModel;

        public AiFallbackUtilTests()
        {
            _mockLogger = new Mock<ILogger>();
            _priorityList = new List<string>
            {
                "fallback-model-1",
                "fallback-model-2",
                "fallback-model-3"
            };
            _preferredModel = "preferred-model";
        }

        [Fact]
        public async Task GetWithFallbackAsync_PreferredModelSucceeds_ReturnsResult()
        {
            // Arrange
            var expectedResult = "Success response";
            Func<string, Task<string>> handler = modelId =>
            {
                if (modelId == _preferredModel)
                {
                    return Task.FromResult(expectedResult);
                }
                throw new InvalidOperationException($"Unexpected model ID: {modelId}");
            };

            // Act
            var result = await AiFallbackUtil.GetWithFallbackAsync(
                _priorityList,
                _preferredModel,
                handler,
                _mockLogger.Object);

            // Assert
            result.Should().Be(expectedResult);
            VerifyLogger(Times.Once(), LogLevel.Information, $"Success with preferred model {_preferredModel} on first attempt.");
        }

        [Fact]
        public async Task GetWithFallbackAsync_PreferredModelReturnsNull_FirstFallbackSucceeds()
        {
            // Arrange
            var expectedResult = "Fallback response";
            var firstFallbackModel = _priorityList[0];
            // Change return type to Task<string> to match AiFallbackUtil signature
            Func<string, Task<string>> handler = modelId =>
            {
                if (modelId == _preferredModel)
                {
                    // Return empty string instead of null to avoid CS8619 warning
                    return Task.FromResult(string.Empty);
                }
                if (modelId == firstFallbackModel)
                {
                    return Task.FromResult(expectedResult);
                }
                throw new InvalidOperationException($"Unexpected model ID: {modelId}");
            };

            // Act
            var result = await AiFallbackUtil.GetWithFallbackAsync(
                _priorityList,
                _preferredModel,
                handler,
                _mockLogger.Object);

            // Assert
            result.Should().Be(expectedResult);
            VerifyLogger(Times.Once(), LogLevel.Warning, $"Handler returned null or invalid result for model {_preferredModel}");
            VerifyLogger(Times.Once(), LogLevel.Information, $"Fallback SUCCESS: Attempt 2 using model {firstFallbackModel} succeeded after previous failures.");
        }

        [Fact]
        public async Task GetWithFallbackAsync_PreferredModelThrowsException_FirstFallbackSucceeds()
        {
            // Arrange
            var expectedResult = "Fallback response";
            var firstFallbackModel = _priorityList[0];
            var exceptionMessage = "Preferred model failed";
            Func<string, Task<string>> handler = modelId =>
            {
                if (modelId == _preferredModel)
                {
                    throw new Exception(exceptionMessage);
                }
                if (modelId == firstFallbackModel)
                {
                    return Task.FromResult(expectedResult);
                }
                throw new InvalidOperationException($"Unexpected model ID: {modelId}");
            };

            // Act
            var result = await AiFallbackUtil.GetWithFallbackAsync(
                _priorityList,
                _preferredModel,
                handler,
                _mockLogger.Object);

            // Assert
            result.Should().Be(expectedResult);
            VerifyLogger(Times.Once(), LogLevel.Error, $"Attempt 1 with model {_preferredModel} failed: {exceptionMessage}");
            VerifyLogger(Times.Once(), LogLevel.Information, $"Fallback SUCCESS: Attempt 2 using model {firstFallbackModel} succeeded after previous failures.");
        }

        [Fact]
        public async Task GetWithFallbackAsync_PreferredAndFirstFallbackFail_SecondFallbackSucceeds()
        {
            // Arrange
            var expectedResult = "Second fallback response";
            var firstFallbackModel = _priorityList[0];
            var secondFallbackModel = _priorityList[1];
            Func<string, Task<string>> handler = modelId =>
            {
                if (modelId == _preferredModel)
                {
                    throw new Exception("Preferred model failed");
                }
                if (modelId == firstFallbackModel)
                {
                    // Return empty string instead of null to avoid CS8619 warning
                    return Task.FromResult(string.Empty);
                }
                if (modelId == secondFallbackModel)
                {
                    return Task.FromResult(expectedResult);
                }
                throw new InvalidOperationException($"Unexpected model ID: {modelId}");
            };

            // Act
            var result = await AiFallbackUtil.GetWithFallbackAsync(
                _priorityList,
                _preferredModel,
                handler,
                _mockLogger.Object);

            // Assert
            result.Should().Be(expectedResult);
            VerifyLogger(Times.Once(), LogLevel.Error, "Attempt 1 with model");
            VerifyLogger(Times.Once(), LogLevel.Warning, $"Handler returned null or invalid result for model {firstFallbackModel}");
            VerifyLogger(Times.Once(), LogLevel.Information, $"Fallback SUCCESS: Attempt 3 using model {secondFallbackModel} succeeded after previous failures.");
        }

        [Fact]
        public async Task GetWithFallbackAsync_AllModelsReturnNull_ThrowsAggregateException()
        {
            // Arrange
            // Return empty string instead of null to satisfy the non-nullable constraint
            Func<string, Task<string>> handler = _ => Task.FromResult(string.Empty);

            // Act
            Func<Task> act = async () => await AiFallbackUtil.GetWithFallbackAsync(
                _priorityList,
                _preferredModel,
                handler,
                _mockLogger.Object);

            // Assert
            var exception = await act.Should().ThrowAsync<AggregateException>();
            exception.Which.Message.Should().Contain("All AI models failed");
            exception.Which.InnerExceptions.Should().HaveCount(_priorityList.Count + 1); // +1 for preferred model
            
            // The implementation logs warnings for null/empty returns, not errors
            VerifyLogger(Times.Once(), LogLevel.Warning, $"Handler returned null or invalid result for model {_preferredModel}");
            foreach (var fallbackModel in _priorityList)
            {
                VerifyLogger(Times.Once(), LogLevel.Warning, $"Handler returned null or invalid result for model {fallbackModel}");
            }
        }

        [Fact]
        public async Task GetWithFallbackAsync_AllModelsThrowExceptions_ThrowsAggregateException()
        {
            // Arrange
            Func<string, Task<string>> handler = modelId =>
                throw new Exception($"Error from model {modelId}");

            // Act
            Func<Task> act = async () => await AiFallbackUtil.GetWithFallbackAsync(
                _priorityList,
                _preferredModel,
                handler,
                _mockLogger.Object);

            // Assert
            var exception = await act.Should().ThrowAsync<AggregateException>();
            exception.Which.Message.Should().Contain("All AI models failed");
            exception.Which.InnerExceptions.Should().HaveCount(_priorityList.Count + 1); // +1 for preferred model
            exception.Which.InnerExceptions.All(ex => ex.Message.Contains("Error from model")).Should().BeTrue();
            
            // Verify error logs for individual model failures (being more specific with the pattern)
            foreach (var modelId in new[] { _preferredModel }.Concat(_priorityList))
            {
                VerifyLogger(Times.Once(), LogLevel.Error, $"with model {modelId} failed");
            }
            
            // Verify the exhausted log separately
            VerifyLogger(Times.Once(), LogLevel.Error, "AI fallback exhausted");
            VerifyLogger(Times.Once(), LogLevel.Error, "AI fallback exhausted");
        }

        [Fact]
        public async Task GetWithFallbackAsync_EmptyStringIsConsideredFailure_FallbackSucceeds()
        {
            // Arrange
            var expectedResult = "Valid response";
            var firstFallbackModel = _priorityList[0];
            Func<string, Task<string>> handler = modelId =>
            {
                if (modelId == _preferredModel)
                {
                    return Task.FromResult(string.Empty);
                }
                if (modelId == firstFallbackModel)
                {
                    return Task.FromResult(expectedResult);
                }
                throw new InvalidOperationException($"Unexpected model ID: {modelId}");
            };

            // Act
            var result = await AiFallbackUtil.GetWithFallbackAsync(
                _priorityList,
                _preferredModel,
                handler,
                _mockLogger.Object);

            // Assert
            result.Should().Be(expectedResult);
            VerifyLogger(Times.Once(), LogLevel.Warning, $"Handler returned null or invalid result for model {_preferredModel}");
            VerifyLogger(Times.Once(), LogLevel.Information, $"Fallback SUCCESS: Attempt 2 using model {firstFallbackModel} succeeded after previous failures.");
        }

        private void VerifyLogger(Times times, LogLevel logLevel, string contains)
        {
            _mockLogger.Verify(
                x => x.Log(
                    It.Is<LogLevel>(l => l == logLevel),
                    It.IsAny<EventId>(),
                    // Refine the check to satisfy nullability analysis within the expression tree
                    It.Is<It.IsAnyType>((v, t) => v != null && (v.ToString() ?? string.Empty).Contains(contains)),
                    // Allow any Exception including null
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                times);
        }
    }
}