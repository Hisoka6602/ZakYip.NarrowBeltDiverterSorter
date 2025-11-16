using ZakYip.NarrowBeltDiverterSorter.Core.Domain;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Carts;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.MainLine;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Parcels;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Sorting;
using ZakYip.NarrowBeltDiverterSorter.Core.Domain.Tracking;

namespace ZakYip.NarrowBeltDiverterSorter.Execution.Sorting;

/// <summary>
/// 分拣规划器实现
/// 根据小车位置、包裹状态、格口配置规划吐件动作
/// </summary>
public class SortingPlanner : ISortingPlanner
{
    private readonly ICartRingBuilder _cartRingBuilder;
    private readonly ICartPositionTracker _cartPositionTracker;
    private readonly ICartLifecycleService _cartLifecycleService;
    private readonly IParcelLifecycleService _parcelLifecycleService;
    private readonly IChuteConfigProvider _chuteConfigProvider;
    private readonly IMainLineSpeedProvider _mainLineSpeedProvider;
    private readonly SortingPlannerOptions _options;

    public SortingPlanner(
        ICartRingBuilder cartRingBuilder,
        ICartPositionTracker cartPositionTracker,
        ICartLifecycleService cartLifecycleService,
        IParcelLifecycleService parcelLifecycleService,
        IChuteConfigProvider chuteConfigProvider,
        IMainLineSpeedProvider mainLineSpeedProvider,
        SortingPlannerOptions options)
    {
        _cartRingBuilder = cartRingBuilder;
        _cartPositionTracker = cartPositionTracker;
        _cartLifecycleService = cartLifecycleService;
        _parcelLifecycleService = parcelLifecycleService;
        _chuteConfigProvider = chuteConfigProvider;
        _mainLineSpeedProvider = mainLineSpeedProvider;
        _options = options;
    }

    /// <inheritdoc/>
    public IReadOnlyList<EjectPlan> PlanEjects(DateTimeOffset now, TimeSpan horizon)
    {
        var plans = new List<EjectPlan>();

        // Check if cart ring is built
        var cartRing = _cartRingBuilder.CurrentSnapshot;
        if (cartRing == null)
        {
            return plans;
        }

        // Check if main line speed is stable
        if (!_mainLineSpeedProvider.IsSpeedStable)
        {
            return plans;
        }

        var currentSpeed = _mainLineSpeedProvider.CurrentMmps;
        if (currentSpeed <= 0)
        {
            return plans;
        }

        // Get all chute configs
        var chuteConfigs = _chuteConfigProvider.GetAllConfigs();

        // Calculate time per cart (assuming fixed cart spacing)
        var timePerCartMs = ((double)_options.CartSpacingMm / (double)currentSpeed) * 1000.0;

        foreach (var chuteConfig in chuteConfigs)
        {
            if (!chuteConfig.IsEnabled)
            {
                continue;
            }

            // Calculate which cart is currently at this chute
            var cartIndexAtChute = _cartPositionTracker.CalculateCartIndexAtOffset(
                chuteConfig.CartOffsetFromOrigin, 
                cartRing.RingLength);

            if (cartIndexAtChute == null)
            {
                continue;
            }

            // Get cart ID from index
            var cartId = cartRing.CartIds[cartIndexAtChute.Value.Value];
            var cartSnapshot = _cartLifecycleService.Get(cartId);

            if (cartSnapshot == null)
            {
                continue;
            }

            if (chuteConfig.IsForceEject)
            {
                // Force eject: plan to eject any loaded cart
                if (cartSnapshot.IsLoaded)
                {
                    var openDuration = chuteConfig.MaxOpenDuration;
                    
                    plans.Add(new EjectPlan
                    {
                        ParcelId = cartSnapshot.CurrentParcelId ?? new ParcelId(0), // Shouldn't be null if IsLoaded is true
                        CartId = cartId,
                        ChuteId = chuteConfig.ChuteId,
                        OpenAt = now,
                        OpenDuration = openDuration,
                        IsForceEject = true
                    });
                }
            }
            else
            {
                // Normal eject: check if cart has a parcel destined for this chute
                if (cartSnapshot.IsLoaded && cartSnapshot.CurrentParcelId != null)
                {
                    var parcel = _parcelLifecycleService.Get(cartSnapshot.CurrentParcelId.Value);
                    
                    if (parcel != null && 
                        parcel.TargetChuteId.HasValue && 
                        parcel.TargetChuteId.Value.Value == chuteConfig.ChuteId.Value &&
                        parcel.RouteState == ParcelRouteState.Sorting)
                    {
                        // Calculate open duration based on number of carts to eject
                        // For now, assume we want to eject just this one cart
                        var openDuration = TimeSpan.FromMilliseconds(timePerCartMs);
                        
                        // Apply max limit
                        if (openDuration > chuteConfig.MaxOpenDuration)
                        {
                            openDuration = chuteConfig.MaxOpenDuration;
                        }

                        plans.Add(new EjectPlan
                        {
                            ParcelId = parcel.ParcelId,
                            CartId = cartId,
                            ChuteId = chuteConfig.ChuteId,
                            OpenAt = now,
                            OpenDuration = openDuration,
                            IsForceEject = false
                        });
                    }
                }
            }
        }

        return plans;
    }
}
