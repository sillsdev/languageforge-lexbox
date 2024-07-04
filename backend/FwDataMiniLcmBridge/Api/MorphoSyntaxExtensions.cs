using SIL.LCModel;

namespace FwDataMiniLcmBridge.Api;

public static class MorphoSyntaxExtensions
{
    public static void SetMsaPartOfSpeech(this IMoMorphSynAnalysis msa, IPartOfSpeech? pos)
    {
        switch (msa.ClassID)
        {
            case MoDerivAffMsaTags.kClassId:
                //todo there's a toPartofSpeech in the msa, not sure what we do with that.
                ((IMoDerivAffMsa)msa).FromPartOfSpeechRA = pos;
                break;
            case MoDerivStepMsaTags.kClassId:
                ((IMoDerivStepMsa)msa).PartOfSpeechRA = pos;
                break;
            case MoInflAffMsaTags.kClassId:
                ((IMoInflAffMsa)msa).PartOfSpeechRA = pos;
                break;
            case MoStemMsaTags.kClassId:
                ((IMoStemMsa)msa).PartOfSpeechRA = pos;
                break;
            case MoUnclassifiedAffixMsaTags.kClassId:
                ((IMoUnclassifiedAffixMsa)msa).PartOfSpeechRA = pos;
                break;
            default:
                throw new NotSupportedException($"Cannot set part of speech for MSA of unknown type: {msa.ClassID}");
        }
    }

    public static IPartOfSpeech? GetPartOfSpeech(this IMoMorphSynAnalysis msa)
    {
        switch (msa.ClassID)
        {
            case MoDerivAffMsaTags.kClassId:
                return ((IMoDerivAffMsa)msa).FromPartOfSpeechRA;
            case MoDerivStepMsaTags.kClassId:
                return ((IMoDerivStepMsa)msa).PartOfSpeechRA;
            case MoInflAffMsaTags.kClassId:
                return ((IMoInflAffMsa)msa).PartOfSpeechRA;
            case MoStemMsaTags.kClassId:
                return ((IMoStemMsa)msa).PartOfSpeechRA;
            case MoUnclassifiedAffixMsaTags.kClassId:
                return ((IMoUnclassifiedAffixMsa)msa).PartOfSpeechRA;
            default:
                return null;
        }
    }
}
