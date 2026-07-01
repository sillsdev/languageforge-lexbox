import type {ICreateEntryOptions} from '$lib/dotnet-types';

// Mirrors the C# CreateEntryOptions presets (MiniLcm/CreateEntryOptions.cs).
export const createEntryOptions: Record<'asIs' | 'withMainPublication', ICreateEntryOptions> = {
  asIs: {includeComplexFormsAndComponents: true, autoAddMainPublication: false},
  withMainPublication: {includeComplexFormsAndComponents: true, autoAddMainPublication: true},
};
