import {describe, expect, it} from 'vitest';
import {ViewBase, type ICustomView} from '$lib/dotnet-types';
import type {CustomView} from '$lib/views/view-data';
import {CustomViewService} from './custom-view-service.svelte';
import type {ProjectContext} from '$project/project-context.svelte';

function customViewFixture(overrides: Partial<ICustomView> = {}): ICustomView {
  return {
    id: 'custom-1',
    name: 'Custom 1',
    base: ViewBase.FwLite,
    entryFields: [],
    senseFields: [],
    exampleFields: [],
    ...overrides,
  };
}

function createMockService(apiViews: ICustomView[] = []): CustomViewService {
  const mockContext = {
    api: {
      createCustomView: (args: ICustomView) => Promise.resolve({...args, id: args.id ?? 'new-id'}),
      updateCustomView: (_id: string, args: ICustomView) => Promise.resolve(args),
      deleteCustomView: () => Promise.resolve(),
    },
    apiResource: () => ({
      current: apiViews,
      refetch: () => Promise.resolve(apiViews),
    }),
  } as unknown as ProjectContext;
  return new CustomViewService(mockContext);
}

describe('custom-view-service', () => {
  it('resolves custom views with parent defaults for missing entity fields', () => {
    const service = createMockService([
      customViewFixture({
        base: ViewBase.FieldWorks,
        entryFields: [
          {fieldId: 'citationForm'},
          {fieldId: 'lexemeForm'},
        ],
        senseFields: [],
        analysis: [{wsId: 'es'}],
      }),
    ]);

    const view: CustomView = service.current[0];
    expect(view.parentView.id).toBe(ViewBase.FieldWorks);
    // Builtin order is used, not API order
    expect(view.entryFields.map(f => f.fieldId).slice(0, 2)).toEqual(['lexemeForm', 'citationForm']);
    expect(view.entryFields.find(f => f.fieldId === 'lexemeForm')?.show).toBe(true);
    expect(view.entryFields.find(f => f.fieldId === 'citationForm')?.show).toBe(true);
    // Fields not in the API array are hidden
    expect(view.entryFields.find(f => f.fieldId === 'note')?.show).toBe(false);
    // Empty senseFields means all defaults are shown (no overrides)
    expect(view.senseFields.find(f => f.fieldId === 'gloss')?.show).toBe(true);
    expect(view.analysis).toEqual([{wsId: 'es'}]);
  });

  it('filters out deleted custom views', () => {
    const service = createMockService([
      customViewFixture({id: 'active'}),
      customViewFixture({id: 'deleted', deletedAt: new Date().toISOString()}),
    ]);

    expect(service.current).toHaveLength(1);
    expect(service.current[0].id).toBe('active');
  });
});
