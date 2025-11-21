using ZakYip.NarrowBeltDiverterSorter.Core.Application;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Parcels;
using ZakYip.NarrowBeltDiverterSorter.Core.Tests.Fakes;
using ZakYip.NarrowBeltDiverterSorter.Core.Enums;

namespace ZakYip.NarrowBeltDiverterSorter.Core.Tests.Parcels;

/// <summary>
/// 包裹生命周期追踪器测试
/// </summary>
public class ParcelLifecycleTrackerTests
{
    private static ParcelLifecycleService CreateServiceInRunningState()
    {
        var systemRunStateService = new FakeSystemRunStateService();
        systemRunStateService.SetState(SystemRunState.Running);
        return new ParcelLifecycleService(systemRunStateService);
    }

    /*
     * 注意：UpdateStatus_Should_Update_Parcel_Status_And_Fire_Event 测试已被注释掉，
     * 因为它测试的是已删除的C#事件 LifecycleChanged。
     * 该功能现在通过 IEventBus 发布事件。如果需要测试事件发布，请创建新的测试来验证
     * IEventBus.PublishAsync 被正确调用。
     */

    /*
    [Fact]
    public void UpdateStatus_Should_Update_Parcel_Status_And_Fire_Event()
    {
        // TODO: 更新此测试以使用IEventBus验证事件发布
    }
    */

    [Fact]
    public void GetOnlineParcels_Should_Return_Only_Non_Completed_Parcels()
    {
        // Arrange
        var lifecycleService = CreateServiceInRunningState();
        var tracker = new ParcelLifecycleTracker(lifecycleService);

        var parcel1 = new ParcelId(1);
        var parcel2 = new ParcelId(2);
        var parcel3 = new ParcelId(3);

        lifecycleService.CreateParcel(parcel1, "P1", DateTimeOffset.Now);
        lifecycleService.CreateParcel(parcel2, "P2", DateTimeOffset.Now);
        lifecycleService.CreateParcel(parcel3, "P3", DateTimeOffset.Now);

        // Act
        tracker.UpdateStatus(parcel1, ParcelStatus.OnMainline);
        tracker.UpdateStatus(parcel2, ParcelStatus.DivertedToTarget); // Completed
        tracker.UpdateStatus(parcel3, ParcelStatus.DivertPlanning);

        var onlineParcels = tracker.GetOnlineParcels();

        // Assert
        Assert.Equal(2, onlineParcels.Count);
        Assert.Contains(onlineParcels, p => p.ParcelId.Equals(parcel1));
        Assert.Contains(onlineParcels, p => p.ParcelId.Equals(parcel3));
        Assert.DoesNotContain(onlineParcels, p => p.ParcelId.Equals(parcel2));
    }

    [Fact]
    public void GetStatusDistribution_Should_Return_Correct_Counts()
    {
        // Arrange
        var lifecycleService = CreateServiceInRunningState();
        var tracker = new ParcelLifecycleTracker(lifecycleService);

        // Create multiple parcels with different statuses
        for (int i = 1; i <= 5; i++)
        {
            var parcelId = new ParcelId(i);
            lifecycleService.CreateParcel(parcelId, $"P{i}", DateTimeOffset.Now);

            if (i <= 2)
            {
                tracker.UpdateStatus(parcelId, ParcelStatus.OnMainline);
            }
            else if (i <= 4)
            {
                tracker.UpdateStatus(parcelId, ParcelStatus.DivertedToTarget);
            }
            else
            {
                tracker.UpdateStatus(parcelId, ParcelStatus.Failed, ParcelFailureReason.UpstreamTimeout);
            }
        }

        // Act
        var distribution = tracker.GetStatusDistribution();

        // Assert
        Assert.Equal(2, distribution[ParcelStatus.OnMainline]);
        Assert.Equal(2, distribution[ParcelStatus.DivertedToTarget]);
        Assert.Equal(1, distribution[ParcelStatus.Failed]);
    }

    [Fact]
    public void GetFailureReasonDistribution_Should_Return_Only_Failed_Parcels()
    {
        // Arrange
        var lifecycleService = CreateServiceInRunningState();
        var tracker = new ParcelLifecycleTracker(lifecycleService);

        var parcel1 = new ParcelId(1);
        var parcel2 = new ParcelId(2);
        var parcel3 = new ParcelId(3);

        lifecycleService.CreateParcel(parcel1, "P1", DateTimeOffset.Now);
        lifecycleService.CreateParcel(parcel2, "P2", DateTimeOffset.Now);
        lifecycleService.CreateParcel(parcel3, "P3", DateTimeOffset.Now);

        // Act
        tracker.UpdateStatus(parcel1, ParcelStatus.Failed, ParcelFailureReason.UpstreamTimeout);
        tracker.UpdateStatus(parcel2, ParcelStatus.DivertedToTarget, ParcelFailureReason.None); // Success
        tracker.UpdateStatus(parcel3, ParcelStatus.Failed, ParcelFailureReason.UpstreamTimeout);

        var failureDistribution = tracker.GetFailureReasonDistribution();

        // Assert
        Assert.Single(failureDistribution);
        Assert.True(failureDistribution.ContainsKey(ParcelFailureReason.UpstreamTimeout));
        Assert.Equal(2, failureDistribution[ParcelFailureReason.UpstreamTimeout]);
        Assert.False(failureDistribution.ContainsKey(ParcelFailureReason.None));
    }

    [Fact]
    public void GetRecentCompletedParcels_Should_Return_Limited_Results()
    {
        // Arrange
        var lifecycleService = CreateServiceInRunningState();
        var tracker = new ParcelLifecycleTracker(lifecycleService);

        // Create and complete 10 parcels
        for (int i = 1; i <= 10; i++)
        {
            var parcelId = new ParcelId(i);
            lifecycleService.CreateParcel(parcelId, $"P{i}", DateTimeOffset.Now);
            tracker.UpdateStatus(parcelId, ParcelStatus.DivertedToTarget);
        }

        // Act
        var recentCompleted = tracker.GetRecentCompletedParcels(5);

        // Assert
        Assert.Equal(5, recentCompleted.Count);
        // Most recent should be first (reverse order)
        Assert.Equal(10, recentCompleted[0].ParcelId.Value);
        Assert.Equal(6, recentCompleted[4].ParcelId.Value);
    }

    [Fact]
    public void ClearHistory_Should_Remove_Completed_Parcels()
    {
        // Arrange
        var lifecycleService = CreateServiceInRunningState();
        var tracker = new ParcelLifecycleTracker(lifecycleService);

        // Create parcels: some completed, some online
        for (int i = 1; i <= 5; i++)
        {
            var parcelId = new ParcelId(i);
            lifecycleService.CreateParcel(parcelId, $"P{i}", DateTimeOffset.Now);

            if (i <= 3)
            {
                tracker.UpdateStatus(parcelId, ParcelStatus.DivertedToTarget);
            }
            else
            {
                tracker.UpdateStatus(parcelId, ParcelStatus.OnMainline);
            }
        }

        // Act
        tracker.ClearHistory(keepRecentCount: 1);
        var recentCompleted = tracker.GetRecentCompletedParcels(10);
        var onlineParcels = tracker.GetOnlineParcels();

        // Assert
        Assert.Single(recentCompleted); // Only 1 kept
        Assert.Equal(2, onlineParcels.Count); // Online parcels not affected
    }

    [Fact]
    public void UpdateStatus_Should_Set_Timestamps_For_Completed_Status()
    {
        // Arrange
        var lifecycleService = CreateServiceInRunningState();
        var tracker = new ParcelLifecycleTracker(lifecycleService);
        var parcelId = new ParcelId(123);

        lifecycleService.CreateParcel(parcelId, "TEST", DateTimeOffset.Now);

        // Act
        tracker.UpdateStatus(parcelId, ParcelStatus.DivertedToTarget);

        var completedParcels = tracker.GetRecentCompletedParcels(1);

        // Assert
        Assert.Single(completedParcels);
        var snapshot = completedParcels[0];
        Assert.NotNull(snapshot.DivertedAt);
        Assert.NotNull(snapshot.CompletedAt);
    }
}
