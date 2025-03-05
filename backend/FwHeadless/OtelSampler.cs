using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using FwLiteProjectSync;
using OpenTelemetry.Trace;

namespace FwHeadless;

public class OtelSampler(ILogger<OtelSampler> logger) : Sampler
{
    private const string SqliteTraceName = "OpenTelemetry.Instrumentation.EntityFrameworkCore.Execute";
    public override SamplingResult ShouldSample(in SamplingParameters samplingParameters)
    {
        if (IsImportEfSpan(samplingParameters))
        {
            return new SamplingResult(SamplingDecision.Drop);
        }

        return new SamplingResult(true);
    }

    private bool IsImportEfSpan(in SamplingParameters samplingParameters)
    {
        var parent = Activity.Current;
        if (logger.IsEnabled(LogLevel.Trace))
            logger.LogTrace("sampling params {Sampling}, current activity {Activity}", Format(samplingParameters), Format(parent));
        if (parent is null) return false;

        if (samplingParameters.Name == SqliteTraceName && IsImportProjectSpan(parent))
        {
            return true;
        }
        return false;
    }

    private bool IsImportProjectSpan(Activity? activity)
    {
        while (activity is not null)
        {
            if (activity.OperationName == nameof(MiniLcmImport.ImportProject))
            {
                return true;
            }
            activity = activity.Parent;
        }
        return false;
    }

    private string Format(in SamplingParameters parameters)
    {
        var sb = new StringBuilder();
        sb.Append("Kind: ").Append(parameters.Kind).AppendLine();
        sb.Append("TraceId: ").Append(parameters.TraceId).AppendLine();
        sb.Append("Name: ").Append(parameters.Name).AppendLine();
        sb.Append("Tags: [").AppendJoin(", ", parameters.Tags ?? []).AppendLine("]");
        sb.Append("ParentContext.TraceFlags: ").Append(parameters.ParentContext.TraceFlags).AppendLine();
        sb.Append("ParentContext.TraceId: ").Append(parameters.ParentContext.TraceId).AppendLine();
        sb.Append("ParentContext.SpanId: ").Append(parameters.ParentContext.SpanId).AppendLine();
        sb.Append("ParentContext.TraceState: ").Append(parameters.ParentContext.TraceState).AppendLine();
        return sb.ToString();
    }

    private string Format(Activity? activity)
    {
        if (activity is null) return "none";
        var sb = new StringBuilder();
        sb.Append("Activity: ").AppendLine(activity.OperationName);

        return sb.ToString();
    }
}
