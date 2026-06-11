using FluentAssertions;
using GdeOni.Mobile.Shared.Routing;
using GdeOni.Mobile.Shared.Utils;
using Xunit;

namespace GdeOni.Mobile.Tests.Utils;

public class RouteOptimizerTests
{
    // ───────────────────── Тривиальные случаи ─────────────────────

    [Fact]
    public void Empty_input_returns_empty()
    {
        var result = RouteOptimizer.OptimizeOrder(null, Array.Empty<RoutePoint>());
        result.Should().BeEmpty();
    }

    [Fact]
    public void Single_point_input_returns_same_single_point()
    {
        var p = new RoutePoint(55.0, 37.0);
        var result = RouteOptimizer.OptimizeOrder(null, new[] { p });
        result.Should().ContainSingle().Which.Should().Be(p);
    }

    // ───────────────────── Nearest-neighbor с origin ─────────────────────

    [Fact]
    public void With_origin_visits_geographically_closest_first()
    {
        // Origin — Москва. Точки: Питер (~635 км), Тула (~170 км),
        // Калуга (~150 км). NN должен пойти Калуга → Тула → Питер.
        var moscow = new RoutePoint(55.7558, 37.6173);
        var spb    = new RoutePoint(59.9311, 30.3609);
        var tula   = new RoutePoint(54.1931, 37.6173);
        var kaluga = new RoutePoint(54.5293, 36.2754);

        var result = RouteOptimizer.OptimizeOrder(moscow, new[] { spb, tula, kaluga });

        result.Should().HaveCount(3);
        result[0].Should().Be(kaluga); // ближайший к Москве
        result[1].Should().Be(tula);   // затем Тула
        result[2].Should().Be(spb);    // и в конце Питер
    }

    [Fact]
    public void Without_origin_starts_with_first_point_and_orders_remaining_by_proximity()
    {
        // Без origin: NN стартует с первой точки (Питер), потом тяга к
        // Москве, потом к Туле.
        var spb    = new RoutePoint(59.9311, 30.3609);
        var moscow = new RoutePoint(55.7558, 37.6173);
        var tula   = new RoutePoint(54.1931, 37.6173);

        var result = RouteOptimizer.OptimizeOrder(null, new[] { spb, moscow, tula });

        result.Should().HaveCount(3);
        result[0].Should().Be(spb);
        result[1].Should().Be(moscow);
        result[2].Should().Be(tula);
    }

    [Fact]
    public void Does_not_mutate_input_list()
    {
        var origin = new RoutePoint(55.0, 37.0);
        var input = new List<RoutePoint>
        {
            new(56.0, 38.0),
            new(54.0, 36.0),
            new(57.0, 39.0)
        };
        var snapshot = input.ToList();

        RouteOptimizer.OptimizeOrder(origin, input);

        input.Should().BeEquivalentTo(snapshot,
            "OptimizeOrder must not mutate the caller's list");
    }

    // ───────────────────── HaversineKm: sanity ─────────────────────

    [Fact]
    public void HaversineKm_zero_for_same_point()
    {
        var p = new RoutePoint(55.0, 37.0);
        var d = RouteOptimizer.HaversineKm(p, p);
        d.Should().BeApproximately(0.0, 0.000001);
    }

    [Fact]
    public void HaversineKm_moscow_to_spb_around_635km()
    {
        var moscow = new RoutePoint(55.7558, 37.6173);
        var spb    = new RoutePoint(59.9311, 30.3609);

        var d = RouteOptimizer.HaversineKm(moscow, spb);

        // Реально ~635 км по большому кругу, допуск ±5 км.
        d.Should().BeInRange(630, 640);
    }

    [Fact]
    public void HaversineKm_is_symmetric()
    {
        var a = new RoutePoint(55.0, 37.0);
        var b = new RoutePoint(40.0, -74.0);

        var ab = RouteOptimizer.HaversineKm(a, b);
        var ba = RouteOptimizer.HaversineKm(b, a);

        ab.Should().BeApproximately(ba, 0.000001);
    }
}
