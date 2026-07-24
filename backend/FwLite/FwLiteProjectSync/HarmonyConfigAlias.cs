// TEMPORARY compat shim: SIL.Harmony renamed CrdtConfig -> SIL.Harmony.Config.HarmonyConfig.
// This global alias lets existing `CrdtConfig` references keep compiling until they're renamed.
global using CrdtConfig = SIL.Harmony.Config.HarmonyConfig;
