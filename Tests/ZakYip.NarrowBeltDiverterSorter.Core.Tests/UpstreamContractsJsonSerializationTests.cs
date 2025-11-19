using System.Text.Json;
using ZakYip.NarrowBeltDiverterSorter.UpstreamContracts.Models;

namespace ZakYip.NarrowBeltDiverterSorter.Core.Tests;

/// <summary>
/// 测试上游契约的JSON序列化，确保字段名与WheelDiverterSorter保持一致
/// </summary>
public class UpstreamContractsJsonSerializationTests
{
    private readonly JsonSerializerOptions _jsonOptions;

    public UpstreamContractsJsonSerializationTests()
    {
        // 使用Web默认设置（camelCase）
        _jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
    }

    [Fact]
    public void ChuteAssignmentRequest_Should_Serialize_With_CamelCase_Fields()
    {
        // Arrange
        var request = new ChuteAssignmentRequest
        {
            ParcelId = 1234567890123,
            RequestTime = DateTimeOffset.Parse("2025-11-16T08:00:00Z")
        };

        // Act
        var json = JsonSerializer.Serialize(request, _jsonOptions);

        // Assert
        Assert.Contains("\"parcelId\"", json);
        Assert.Contains("\"requestTime\"", json);
        Assert.Contains("1234567890123", json);
    }

    [Fact]
    public void ChuteAssignmentRequest_Should_Deserialize_From_CamelCase_Json()
    {
        // Arrange
        var json = """
        {
            "parcelId": 1234567890123,
            "requestTime": "2025-11-16T08:00:00Z"
        }
        """;

        // Act
        var request = JsonSerializer.Deserialize<ChuteAssignmentRequest>(json, _jsonOptions);

        // Assert
        Assert.NotNull(request);
        Assert.Equal(1234567890123, request.ParcelId);
        Assert.Equal(DateTimeOffset.Parse("2025-11-16T08:00:00Z"), request.RequestTime);
    }

    [Fact]
    public void ChuteAssignmentResponse_Should_Serialize_With_CamelCase_Fields()
    {
        // Arrange
        var response = new ChuteAssignmentResponse
        {
            ParcelId = 1234567890123,
            ChuteId = 5,
            IsSuccess = true,
            ErrorMessage = null,
            ResponseTime = DateTimeOffset.Parse("2025-11-16T08:00:01Z")
        };

        // Act
        var json = JsonSerializer.Serialize(response, _jsonOptions);

        // Assert
        Assert.Contains("\"parcelId\"", json);
        Assert.Contains("\"chuteId\"", json);
        Assert.Contains("\"isSuccess\"", json);
        Assert.Contains("\"responseTime\"", json);
        Assert.Contains("1234567890123", json);
        Assert.Contains("5", json);
        Assert.Contains("true", json);
    }

    [Fact]
    public void ChuteAssignmentResponse_Should_Deserialize_From_CamelCase_Json()
    {
        // Arrange
        var json = """
        {
            "parcelId": 1234567890123,
            "chuteId": 5,
            "isSuccess": true,
            "errorMessage": "测试错误",
            "responseTime": "2025-11-16T08:00:01Z"
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize<ChuteAssignmentResponse>(json, _jsonOptions);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(1234567890123, response.ParcelId);
        Assert.Equal(5, response.ChuteId);
        Assert.True(response.IsSuccess);
        Assert.Equal("测试错误", response.ErrorMessage);
        Assert.Equal(DateTimeOffset.Parse("2025-11-16T08:00:01Z"), response.ResponseTime);
    }

    [Fact]
    public void ParcelDetectionNotification_Should_Serialize_With_CamelCase_Fields()
    {
        // Arrange
        var notification = new ParcelDetectionNotification
        {
            ParcelId = 1234567890123,
            DetectionTime = DateTimeOffset.Parse("2025-11-16T07:59:59Z"),
            Metadata = new Dictionary<string, string>
            {
                { "source", "sensor-1" },
                { "lane", "A" }
            }
        };

        // Act
        var json = JsonSerializer.Serialize(notification, _jsonOptions);

        // Assert
        Assert.Contains("\"parcelId\"", json);
        Assert.Contains("\"detectionTime\"", json);
        Assert.Contains("\"metadata\"", json);
        Assert.Contains("\"source\"", json);
        Assert.Contains("\"sensor-1\"", json);
    }

    [Fact]
    public void ParcelDetectionNotification_Should_Deserialize_From_CamelCase_Json()
    {
        // Arrange
        var json = """
        {
            "parcelId": 1234567890123,
            "detectionTime": "2025-11-16T07:59:59Z",
            "metadata": {
                "source": "sensor-1",
                "lane": "A"
            }
        }
        """;

        // Act
        var notification = JsonSerializer.Deserialize<ParcelDetectionNotification>(json, _jsonOptions);

        // Assert
        Assert.NotNull(notification);
        Assert.Equal(1234567890123, notification.ParcelId);
        Assert.Equal(DateTimeOffset.Parse("2025-11-16T07:59:59Z"), notification.DetectionTime);
        Assert.NotNull(notification.Metadata);
        Assert.Equal("sensor-1", notification.Metadata["source"]);
        Assert.Equal("A", notification.Metadata["lane"]);
    }

    [Fact]
    public void ChuteAssignmentNotificationEventArgs_Should_Serialize_With_CamelCase_Fields()
    {
        // Arrange
        var eventArgs = new ChuteAssignmentNotificationEventArgs
        {
            ParcelId = 1234567890123,
            ChuteId = 5,
            NotificationTime = DateTimeOffset.Parse("2025-11-16T08:00:00Z"),
            Metadata = new Dictionary<string, string> { { "priority", "high" } }
        };

        // Act
        var json = JsonSerializer.Serialize(eventArgs, _jsonOptions);

        // Assert
        Assert.Contains("\"parcelId\"", json);
        Assert.Contains("\"chuteId\"", json);
        Assert.Contains("\"notificationTime\"", json);
        Assert.Contains("\"metadata\"", json);
        Assert.Contains("\"priority\"", json);
    }

    [Fact]
    public void EmcLockEvent_Should_Serialize_With_CamelCase_Fields()
    {
        // Arrange
        var lockEvent = new EmcLockEvent
        {
            EventId = "test-event-123",
            InstanceId = "instance-1",
            NotificationType = EmcLockNotificationType.RequestLock,
            CardNo = 1,
            Timestamp = DateTime.Parse("2025-11-16T08:00:00Z"),
            Message = "测试消息",
            TimeoutMs = 5000
        };

        // Act
        var json = JsonSerializer.Serialize(lockEvent, _jsonOptions);

        // Assert
        Assert.Contains("\"eventId\"", json);
        Assert.Contains("\"instanceId\"", json);
        Assert.Contains("\"notificationType\"", json);
        Assert.Contains("\"cardNo\"", json);
        Assert.Contains("\"timestamp\"", json);
        Assert.Contains("\"message\"", json);
        Assert.Contains("\"timeoutMs\"", json);
        Assert.Contains("\"test-event-123\"", json);
        Assert.Contains("\"instance-1\"", json);
    }

    [Fact]
    public void EmcLockEvent_Should_Deserialize_From_CamelCase_Json()
    {
        // Arrange
        var json = """
        {
            "eventId": "test-event-123",
            "instanceId": "instance-1",
            "notificationType": 0,
            "cardNo": 1,
            "timestamp": "2025-11-16T08:00:00Z",
            "message": "测试消息",
            "timeoutMs": 5000
        }
        """;

        // Act
        var lockEvent = JsonSerializer.Deserialize<EmcLockEvent>(json, _jsonOptions);

        // Assert
        Assert.NotNull(lockEvent);
        Assert.Equal("test-event-123", lockEvent.EventId);
        Assert.Equal("instance-1", lockEvent.InstanceId);
        Assert.Equal(EmcLockNotificationType.RequestLock, lockEvent.NotificationType);
        Assert.Equal((ushort)1, lockEvent.CardNo);
        Assert.Equal("测试消息", lockEvent.Message);
        Assert.Equal(5000, lockEvent.TimeoutMs);
    }

    [Fact]
    public void EmcLockNotificationType_Should_Serialize_As_Integer()
    {
        // Arrange
        var notificationType = EmcLockNotificationType.ColdReset;

        // Act
        var json = JsonSerializer.Serialize(notificationType, _jsonOptions);

        // Assert
        Assert.Equal("2", json);
    }

    [Fact]
    public void EmcLockNotificationType_Should_Deserialize_From_Integer()
    {
        // Arrange
        var json = "3";

        // Act
        var notificationType = JsonSerializer.Deserialize<EmcLockNotificationType>(json, _jsonOptions);

        // Assert
        Assert.Equal(EmcLockNotificationType.HotReset, notificationType);
    }

    [Fact]
    public void ParcelRoutingRequestDto_Should_Serialize_With_CamelCase_Fields()
    {
        // Arrange
        var request = new ParcelRoutingRequestDto
        {
            ParcelId = 1234567890123,
            RequestTime = DateTimeOffset.Parse("2025-11-16T08:00:00Z")
        };

        // Act
        var json = JsonSerializer.Serialize(request, _jsonOptions);

        // Assert
        Assert.Contains("\"parcelId\"", json);
        Assert.Contains("\"requestTime\"", json);
        Assert.Contains("1234567890123", json);
    }

    [Fact]
    public void ParcelRoutingRequestDto_Should_Deserialize_From_CamelCase_Json()
    {
        // Arrange
        var json = """
        {
            "parcelId": 1234567890123,
            "requestTime": "2025-11-16T08:00:00Z"
        }
        """;

        // Act
        var request = JsonSerializer.Deserialize<ParcelRoutingRequestDto>(json, _jsonOptions);

        // Assert
        Assert.NotNull(request);
        Assert.Equal(1234567890123, request.ParcelId);
        Assert.Equal(DateTimeOffset.Parse("2025-11-16T08:00:00Z"), request.RequestTime);
    }

    [Fact]
    public void ParcelRoutingResponseDto_Should_Serialize_With_CamelCase_Fields()
    {
        // Arrange
        var response = new ParcelRoutingResponseDto
        {
            ParcelId = 1234567890123,
            ChuteId = 5,
            IsSuccess = true,
            ErrorMessage = null,
            ResponseTime = DateTimeOffset.Parse("2025-11-16T08:00:01Z")
        };

        // Act
        var json = JsonSerializer.Serialize(response, _jsonOptions);

        // Assert
        Assert.Contains("\"parcelId\"", json);
        Assert.Contains("\"chuteId\"", json);
        Assert.Contains("\"isSuccess\"", json);
        Assert.Contains("\"responseTime\"", json);
        Assert.Contains("1234567890123", json);
        Assert.Contains("5", json);
        Assert.Contains("true", json);
    }

    [Fact]
    public void ParcelRoutingResponseDto_Should_Deserialize_From_CamelCase_Json()
    {
        // Arrange
        var json = """
        {
            "parcelId": 1234567890123,
            "chuteId": 5,
            "isSuccess": true,
            "errorMessage": "测试错误",
            "responseTime": "2025-11-16T08:00:01Z"
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize<ParcelRoutingResponseDto>(json, _jsonOptions);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(1234567890123, response.ParcelId);
        Assert.Equal(5, response.ChuteId);
        Assert.True(response.IsSuccess);
        Assert.Equal("测试错误", response.ErrorMessage);
        Assert.Equal(DateTimeOffset.Parse("2025-11-16T08:00:01Z"), response.ResponseTime);
    }

    [Fact]
    public void SortingResultReportDto_Should_Serialize_With_CamelCase_Fields()
    {
        // Arrange
        var report = new SortingResultReportDto
        {
            ParcelId = 1234567890123,
            ChuteId = 5,
            IsSuccess = true,
            FailureReason = null,
            ReportTime = DateTimeOffset.Parse("2025-11-16T08:00:02Z")
        };

        // Act
        var json = JsonSerializer.Serialize(report, _jsonOptions);

        // Assert
        Assert.Contains("\"parcelId\"", json);
        Assert.Contains("\"chuteId\"", json);
        Assert.Contains("\"isSuccess\"", json);
        Assert.Contains("\"reportTime\"", json);
        Assert.Contains("1234567890123", json);
        Assert.Contains("5", json);
        Assert.Contains("true", json);
    }

    [Fact]
    public void SortingResultReportDto_Should_Deserialize_From_CamelCase_Json()
    {
        // Arrange
        var json = """
        {
            "parcelId": 1234567890123,
            "chuteId": 5,
            "isSuccess": false,
            "failureReason": "格口满载",
            "reportTime": "2025-11-16T08:00:02Z"
        }
        """;

        // Act
        var report = JsonSerializer.Deserialize<SortingResultReportDto>(json, _jsonOptions);

        // Assert
        Assert.NotNull(report);
        Assert.Equal(1234567890123, report.ParcelId);
        Assert.Equal(5, report.ChuteId);
        Assert.False(report.IsSuccess);
        Assert.Equal("格口满载", report.FailureReason);
        Assert.Equal(DateTimeOffset.Parse("2025-11-16T08:00:02Z"), report.ReportTime);
    }
}
