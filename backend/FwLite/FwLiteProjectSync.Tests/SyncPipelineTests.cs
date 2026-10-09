using FwLiteProjectSync.Pipeline;
using MiniLcm;

namespace FwLiteProjectSync.Tests;

public class SyncPipelineTests
{
    private static Task<LexCore.Sync.SyncResult> Run(params SyncStep[] steps) =>
        SyncPipeline.Execute(steps, crdtApi: null!, fwdataApi: null!, ProjectSnapshot.Empty);

    private static SyncStepBuilder RecordingStep(string name, List<string> log) =>
        SyncStep.Named(name, ctx =>
        {
            log.Add(name);
            return Task.CompletedTask;
        });

    [Fact]
    public async Task RunsInRegistrationOrderWhenUnconstrained()
    {
        var log = new List<string>();
        await Run(RecordingStep("a", log), RecordingStep("b", log), RecordingStep("c", log));
        log.Should().Equal("a", "b", "c");
    }

    [Fact]
    public async Task AfterConstraintIsHonored()
    {
        var log = new List<string>();
        // 'first' is registered last but 'second' declares it runs after 'first'.
        await Run(
            SyncStep.Named("second", _ => { log.Add("second"); return Task.CompletedTask; }).After("first"),
            RecordingStep("first", log));
        log.Should().Equal("first", "second");
    }

    [Fact]
    public async Task BeforeConstraintIsHonored()
    {
        var log = new List<string>();
        // 'late' is registered first but declares it must run before 'early'.
        await Run(
            SyncStep.Named("late", _ => { log.Add("late"); return Task.CompletedTask; }).Before("early"),
            RecordingStep("early", log));
        log.Should().Equal("late", "early");
    }

    [Fact]
    public async Task DynamicallyAddedStepRunsAfterItsParent()
    {
        var log = new List<string>();
        var parent = SyncStep.Named("parent", ctx =>
        {
            log.Add("parent");
            ctx.AddStep(RecordingStep("child", log));
            return Task.CompletedTask;
        });
        await Run(parent, RecordingStep("sibling", log));
        // sibling was registered before the child existed, so it runs first; child runs after parent.
        log.Should().Equal("parent", "sibling", "child");
        log.IndexOf("child").Should().BeGreaterThan(log.IndexOf("parent"));
    }

    [Fact]
    public async Task StepCanBeInsertedBetweenTwoDirectionalPhases()
    {
        var log = new List<string>();
        // Two directional phases: "x.crdt" then "x.fwdata". The crdt phase adds a step that must run
        // after itself but before the fwdata phase — i.e. between the two directions.
        var crdt = SyncStep.Named("x.crdt", ctx =>
        {
            log.Add("x.crdt");
            ctx.AddStep(RecordingStep("inserted", log).After("x.crdt").Before("x.fwdata"));
            return Task.CompletedTask;
        });
        var fwdata = RecordingStep("x.fwdata", log).After("x.crdt");
        await Run(crdt, fwdata);
        log.Should().Equal("x.crdt", "inserted", "x.fwdata");
    }

    [Fact]
    public async Task ChangeCountsAccumulateAcrossSteps()
    {
        var result = await Run(
            SyncStep.Named("a", ctx => { ctx.RecordCrdtChanges(2); ctx.RecordFwdataChanges(1); return Task.CompletedTask; }),
            SyncStep.Named("b", ctx => { ctx.RecordCrdtChanges(3); ctx.RecordFwdataChanges(4); return Task.CompletedTask; }));
        result.CrdtChanges.Should().Be(5);
        result.FwdataChanges.Should().Be(5);
    }

    [Fact]
    public async Task CycleThrows()
    {
        var log = new List<string>();
        var act = () => Run(
            RecordingStep("a", log).After("b"),
            RecordingStep("b", log).After("a"));
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*cycle*");
    }

    [Fact]
    public async Task AddingStepThatMustRunBeforeACompletedStepThrows()
    {
        var log = new List<string>();
        var act = () => Run(
            RecordingStep("a", log),
            SyncStep.Named("b", ctx =>
            {
                log.Add("b");
                // 'a' has already completed, so a child that must precede it is unsatisfiable.
                ctx.AddStep(RecordingStep("child", log).Before("a"));
                return Task.CompletedTask;
            }));
        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
