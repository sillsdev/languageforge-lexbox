import type {ICreateEntryOptions} from '$lib/dotnet-types';

// Mirrors the C# CreateEntryOptions presets (MiniLcm/CreateEntryOptions.cs).
export const createEntryOptions: Record<'asIs' | 'withMainPublication', ICreateEntryOptions> = {
  asIs: {includeEntryReferences: true, autoAddMainPublication: false},
  withMainPublication: {includeEntryReferences: true, autoAddMainPublication: true},
};
