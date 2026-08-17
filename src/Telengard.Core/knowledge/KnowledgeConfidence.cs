namespace Telengard.Core.Knowledge;

public sealed record KnowledgeConfidenceConfiguration
{
    public KnowledgeConfidenceConfiguration(
        int rumorSampleCount = 1,
        int suspectedSampleCount = 2,
        int probableSampleCount = 4,
        int highConfidenceSampleCount = 7,
        int rumorConfidence = 25,
        int suspectedConfidence = 50,
        int probableConfidence = 75,
        int highConfidence = 100)
    {
        if (rumorSampleCount < 1 ||
            suspectedSampleCount <= rumorSampleCount ||
            probableSampleCount <= suspectedSampleCount ||
            highConfidenceSampleCount <= probableSampleCount)
        {
            throw new ArgumentException("Confidence sample thresholds must be increasing and start at one.");
        }

        if (rumorConfidence is < 0 or > 100 ||
            suspectedConfidence is < 0 or > 100 ||
            probableConfidence is < 0 or > 100 ||
            highConfidence is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(rumorConfidence), "Confidence values must be between zero and one hundred.");
        }

        if (rumorConfidence > suspectedConfidence ||
            suspectedConfidence > probableConfidence ||
            probableConfidence > highConfidence)
        {
            throw new ArgumentException("Confidence values must not decrease as samples accumulate.");
        }

        RumorSampleCount = rumorSampleCount;
        SuspectedSampleCount = suspectedSampleCount;
        ProbableSampleCount = probableSampleCount;
        HighConfidenceSampleCount = highConfidenceSampleCount;
        RumorConfidence = rumorConfidence;
        SuspectedConfidence = suspectedConfidence;
        ProbableConfidence = probableConfidence;
        HighConfidence = highConfidence;
    }

    public int RumorSampleCount { get; }
    public int SuspectedSampleCount { get; }
    public int ProbableSampleCount { get; }
    public int HighConfidenceSampleCount { get; }
    public int RumorConfidence { get; }
    public int SuspectedConfidence { get; }
    public int ProbableConfidence { get; }
    public int HighConfidence { get; }

    public int Resolve(int sampleCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(sampleCount);

        if (sampleCount >= HighConfidenceSampleCount) return HighConfidence;
        if (sampleCount >= ProbableSampleCount) return ProbableConfidence;
        if (sampleCount >= SuspectedSampleCount) return SuspectedConfidence;
        if (sampleCount >= RumorSampleCount) return RumorConfidence;
        return 0;
    }
}
