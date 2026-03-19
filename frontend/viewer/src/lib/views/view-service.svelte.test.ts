import {afterEach, beforeEach, describe, expect, it} from 'vitest';
import {hasVisibleFields, objectTemplateAreas} from './view-service.svelte';
import {fieldRecord, isCustomView, BUILT_IN_VIEWS, FW_LITE_VIEW, FW_CLASSIC_VIEW, type View, type TypedViewField} from './view-data';
import {ViewService} from './view-service.svelte';
import {ViewBase} from '$lib/dotnet-types';
import type {CustomView} from './view-data';
import type {CustomViewService} from '$project/data/custom-view-service.svelte';
import type {FieldId} from './entity-config';

// -- Pure utility tests --

describe('hasVisibleFields', () => {
  it('returns true when at least one field is visible', () => {
    const fields: TypedViewField<FieldId>[] = [
      {fieldId: 'a' as FieldId, show: false},
      {fieldId: 'b' as FieldId, show: true},
    ];
    expect(hasVisibleFields(fields)).toBe(true);
  });

  it('returns false when no fields are visible', () => {
    const fields: TypedViewField<FieldId>[] = [
      {fieldId: 'a' as FieldId, show: false},
      {fieldId: 'b' as FieldId, show: false},
    ];
    expect(hasVisibleFields(fields)).toBe(false);
  });

  it('returns false for an empty array', () => {
    expect(hasVisibleFields([])).toBe(false);
  });
});

describe('objectTemplateAreas', () => {
  it('generates grid template areas for visible fields only', () => {
    const fields: TypedViewField<FieldId>[] = [
      {fieldId: 'word' as FieldId, show: true},
      {fieldId: 'hidden' as FieldId, show: false},
      {fieldId: 'gloss' as FieldId, show: true},
    ];
    expect(objectTemplateAreas(fields)).toBe('"word word word" "gloss gloss gloss"');
  });

  it('returns empty string when no fields are visible', () => {
    const fields: TypedViewField<FieldId>[] = [{fieldId: 'a' as FieldId, show: false}];
    expect(objectTemplateAreas(fields)).toBe('');
  });

  it('returns empty string for empty array', () => {
    expect(objectTemplateAreas([])).toBe('');
  });
});

describe('fieldRecord', () => {
  it('converts field array to a record keyed by fieldId', () => {
    const fields = FW_LITE_VIEW.entryFields;
    const record = fieldRecord(fields);
    expect(record.lexemeForm).toEqual(fields.find(f => f.fieldId === 'lexemeForm'));
    expect(record.note).toEqual(fields.find(f => f.fieldId === 'note'));
  });
});

describe('isCustomView', () => {
  it('returns false for built-in views', () => {
    expect(isCustomView(FW_LITE_VIEW)).toBe(false);
    expect(isCustomView(FW_CLASSIC_VIEW)).toBe(false);
  });

  it('returns true when the view has a custom property', () => {
    const custom: View = {
      ...FW_LITE_VIEW,
      id: 'custom-1',
      custom: true,
    } as CustomView;
    expect(isCustomView(custom)).toBe(true);
  });
});

// -- ViewService tests --

class MockCustomViewService {
  current = $state<CustomView[]>([]);
  constructor(views: CustomView[] = []) {
    this.current = views;
  }
}

function createViewService(customViews: CustomView[] = []): {service: ViewService, mockCustomViewService: MockCustomViewService} {
  const mockCustomViewService = new MockCustomViewService(customViews);
  return {service: new ViewService(mockCustomViewService as unknown as CustomViewService, {persist: false}), mockCustomViewService};
}

describe('ViewService', () => {
  let cleanup: () => void;

  beforeEach(() => {
    cleanup = $effect.root(() => {});
  });

  afterEach(() => {
    cleanup();
  });

  it('defaults to the first built-in view', () => {
    const {service} = createViewService();
    expect(service.currentView.id).toBe(BUILT_IN_VIEWS[0].id);
  });

  it('includes built-in and custom views in the views list', () => {
    const custom: CustomView = {
      ...FW_LITE_VIEW,
      id: 'custom-1',
      name: 'My Custom View',
      custom: true,
    };
    const {service} = createViewService([custom]);
    expect(service.views.map(v => v.id)).toContain('custom-1');
    expect(service.views.map(v => v.id)).toContain(ViewBase.FwLite);
  });

  it('selectView selects a view from the list by ID', () => {
    const {service} = createViewService();
    service.selectView(ViewBase.FieldWorks);
    expect(service.currentView.id).toBe(ViewBase.FieldWorks);
  });

  it('selectView with unknown ID throw', () => {
    const {service} = createViewService();
    expect(() => service.selectView('nonexistent')).toThrow();
  });

  it('overrideView sets a transient synthetic view', () => {
    const {service} = createViewService();
    const syntheticView: View = {
      ...FW_LITE_VIEW,
      name: 'Override',
      entryFields: FW_LITE_VIEW.entryFields.filter(f => f.fieldId === 'lexemeForm'),
      senseFields: [],
      exampleFields: [],
    };

    service.overrideView(syntheticView);
    expect(service.currentView.name).toBe('Override');
    expect(service.currentView.entryFields).toHaveLength(1);
    expect(service.currentView.entryFields[0].fieldId).toBe('lexemeForm');
  });

  it('selectView clears a previous override', () => {
    const {service} = createViewService();
    service.overrideView({...FW_LITE_VIEW, name: 'Override'});
    expect(service.currentView.name).toBe('Override');

    service.selectView(ViewBase.FieldWorks);
    expect(service.currentView.id).toBe(ViewBase.FieldWorks);
    expect(service.currentView.name).toBe('FieldWorks Classic');
  });

  it('rootView reflects the base of the current view', () => {
    const {service} = createViewService();
    service.selectView(ViewBase.FwLite);
    expect(service.rootView.id).toBe(ViewBase.FwLite);

    service.selectView(ViewBase.FieldWorks);
    expect(service.rootView.id).toBe(ViewBase.FieldWorks);
  });

  it('deleting the currently selected custom view reverts to the first built-in view', () => {
    const custom: CustomView = {
      ...FW_CLASSIC_VIEW,
      id: 'custom-1',
      name: 'My Custom View',
      custom: true,
    };
    const {service, mockCustomViewService} = createViewService([custom]);
    service.selectView(custom.id);
    expect(service.currentView.id).toBe(custom.id);

    // Simulate deletion by clearing reactive custom views; currentView falls back to views[0]
    mockCustomViewService.current = [];
    expect(service.currentView.id).toBe(BUILT_IN_VIEWS[0].id);
  });
});
