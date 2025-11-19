using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Parcels;
using ZakYip.NarrowBeltDiverterSorter.Observability.Timeline;

namespace ZakYip.NarrowBeltDiverterSorter.Observability.Tests.Timeline;

public class ParcelTimelineServiceTests
{
    [Fact]
    public void Append_AddsEventToTimeline()
    {
        // Arrange
        var logger = NullLogger<ParcelTimelineService>.Instance;
        var service = new ParcelTimelineService(logger, capacity: 100);
        var eventArgs = new ParcelTimelineEventArgs
        {
            ParcelId = 1,
            EventType = ParcelTimelineEventType.Created,
            OccurredAt = DateTimeOffset.UtcNow,
            Barcode = "TEST001",
            Note = "Test event"
        };

        // Act
        service.Append(eventArgs);
        var events = service.QueryByParcel(1);

        // Assert
        Assert.Single(events);
        Assert.Equal(1, events[0].ParcelId);
        Assert.Equal(ParcelTimelineEventType.Created, events[0].EventType);
        Assert.Equal("TEST001", events[0].Barcode);
    }

    [Fact]
    public void QueryByParcel_ReturnsEventsInChronologicalOrder()
    {
        // Arrange
        var logger = NullLogger<ParcelTimelineService>.Instance;
        var service = new ParcelTimelineService(logger, capacity: 100);
        var now = DateTimeOffset.UtcNow;

        service.Append(new ParcelTimelineEventArgs
        {
            ParcelId = 1,
            EventType = ParcelTimelineEventType.Created,
            OccurredAt = now,
            Barcode = "TEST001"
        });

        service.Append(new ParcelTimelineEventArgs
        {
            ParcelId = 1,
            EventType = ParcelTimelineEventType.UpstreamRequestSent,
            OccurredAt = now.AddSeconds(1),
            Barcode = "TEST001"
        });

        service.Append(new ParcelTimelineEventArgs
        {
            ParcelId = 1,
            EventType = ParcelTimelineEventType.Completed,
            OccurredAt = now.AddSeconds(2),
            Barcode = "TEST001"
        });

        // Act
        var events = service.QueryByParcel(1);

        // Assert
        Assert.Equal(3, events.Count);
        Assert.Equal(ParcelTimelineEventType.Created, events[0].EventType);
        Assert.Equal(ParcelTimelineEventType.UpstreamRequestSent, events[1].EventType);
        Assert.Equal(ParcelTimelineEventType.Completed, events[2].EventType);
    }

    [Fact]
    public void QueryByParcel_ReturnsOnlyEventsForSpecifiedParcel()
    {
        // Arrange
        var logger = NullLogger<ParcelTimelineService>.Instance;
        var service = new ParcelTimelineService(logger, capacity: 100);
        var now = DateTimeOffset.UtcNow;

        service.Append(new ParcelTimelineEventArgs
        {
            ParcelId = 1,
            EventType = ParcelTimelineEventType.Created,
            OccurredAt = now,
            Barcode = "TEST001"
        });

        service.Append(new ParcelTimelineEventArgs
        {
            ParcelId = 2,
            EventType = ParcelTimelineEventType.Created,
            OccurredAt = now,
            Barcode = "TEST002"
        });

        // Act
        var events = service.QueryByParcel(1);

        // Assert
        Assert.Single(events);
        Assert.Equal(1, events[0].ParcelId);
    }

    [Fact]
    public void QueryRecent_ReturnsLatestEvents()
    {
        // Arrange
        var logger = NullLogger<ParcelTimelineService>.Instance;
        var service = new ParcelTimelineService(logger, capacity: 100);
        var now = DateTimeOffset.UtcNow;

        for (int i = 1; i <= 5; i++)
        {
            service.Append(new ParcelTimelineEventArgs
            {
                ParcelId = i,
                EventType = ParcelTimelineEventType.Created,
                OccurredAt = now.AddSeconds(i),
                Barcode = $"TEST{i:D3}"
            });
        }

        // Act
        var events = service.QueryRecent(3);

        // Assert
        Assert.Equal(3, events.Count);
        // Recent should be in descending order
        Assert.Equal(5, events[0].ParcelId);
        Assert.Equal(4, events[1].ParcelId);
        Assert.Equal(3, events[2].ParcelId);
    }

    [Fact]
    public void CircularBuffer_RemovesOldestEventsWhenCapacityReached()
    {
        // Arrange
        var logger = NullLogger<ParcelTimelineService>.Instance;
        var service = new ParcelTimelineService(logger, capacity: 3);
        var now = DateTimeOffset.UtcNow;

        // Act - Add 5 events (capacity is 3)
        for (int i = 1; i <= 5; i++)
        {
            service.Append(new ParcelTimelineEventArgs
            {
                ParcelId = i,
                EventType = ParcelTimelineEventType.Created,
                OccurredAt = now.AddSeconds(i),
                Barcode = $"TEST{i:D3}"
            });
        }

        var allEvents = service.QueryRecent(100);

        // Assert - Should only have 3 latest events (3, 4, 5)
        Assert.Equal(3, allEvents.Count);
        Assert.Equal(5, allEvents[0].ParcelId);
        Assert.Equal(4, allEvents[1].ParcelId);
        Assert.Equal(3, allEvents[2].ParcelId);
    }
}
