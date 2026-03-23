import {describe, expect, it, vi} from 'vitest';
import {ViewBase, type ICustomView} from '$lib/dotnet-types';
import {FW_LITE_VIEW, type CustomView, type View} from '$lib/views/view-data';
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

function createMockService(apiViews: ICustomView[] = []) {
  const apiSpy = {
    createCustomView: vi.fn((args: ICustomView) => Promise.resolve({...args, id: args.id ?? 'new-id'})),
    updateCustomView: vi.fn((_id: string, args: ICustomView) => Promise.resolve(args)),
    deleteCustomView: vi.fn(() => Promise.resolve()),
  };
  const refetchSpy = vi.fn(async () => {});
  const mockContext = {
    api: apiSpy,
    apiResource: () => ({
      current: apiViews,
      refetch: refetchSpy,
    }),
  } as unknown as ProjectContext;
  return {service: new CustomViewService(mockContext), apiSpy, refetchSpy};
}

describe('CustomViewService', () => {
  describe('field resolution', () => {
    it('uses built-in field ordering, not API ordering', () => {
      const {service} = createMockService([
        customViewFixture({
          base: ViewBase.FieldWorks,
          entryFields: [
            {fieldId: 'citationForm'},
            {fieldId: 'lexemeForm'},
          ],
        }),
      ]);

      const view: CustomView = service.current[0];
      // Built-in has lexemeForm before citationForm regardless of what the API returns
      expect(view.entryFields.map(f => f.fieldId).slice(0, 2)).toEqual(['lexemeForm', 'citationForm']);
    });

    it('marks fields present in API array as shown, absent ones as hidden', () => {
      const {service} = createMockService([
        customViewFixture({
          base: ViewBase.FieldWorks,
          entryFields: [{fieldId: 'lexemeForm'}],
          senseFields: [],
        }),
      ]);

      const view: CustomView = service.current[0];
      expect(view.entryFields.find(f => f.fieldId === 'lexemeForm')?.show).toBe(true);
      expect(view.entryFields.find(f => f.fieldId === 'note')?.show).toBe(false);
      // Empty senseFields means no sense fields are shown
      expect(view.senseFields.find(f => f.fieldId === 'gloss')?.show).toBe(false);
    });

    it('passes analysis writing systems through unchanged from API', () => {
      const {service} = createMockService([
        customViewFixture({analysis: [{wsId: 'es'}]}),
      ]);

      expect(service.current[0].analysis).toEqual([{wsId: 'es'}]);
    });
  });

  describe('add', () => {
    it('only sends visible fields to the API', async () => {
      const {service, apiSpy} = createMockService();
      const view: View = {

        ...FW_LITE_VIEW,
        name: 'New View',
        entryFields: [
          {fieldId: 'lexemeForm', show: true},
          {fieldId: 'note', show: true},
          {fieldId: 'citationForm', show: false},
        ],
      };

      await service.add(view);

      const sent: ICustomView = apiSpy.createCustomView.mock.calls[0][0];
      const sentFieldIds = sent.entryFields.map(f => f.fieldId);
      expect(sentFieldIds).toContain('lexemeForm');
      expect(sentFieldIds).toContain('note');
      expect(sentFieldIds).not.toContain('citationForm');
    });

    it('returns a custom view with show flags populated', async () => {
      const {service} = createMockService();
      const view: View = {...FW_LITE_VIEW, name: 'New View'};

      const result = await service.add(view);

      expect(result.custom).toBe(true);
      expect(result.name).toBe('New View');
      // show flags are set on each field
      expect(result.entryFields.every(f => 'show' in f)).toBe(true);
    });

    it('calls refetch after creating', async () => {
      const {service, refetchSpy} = createMockService();
      await service.add({...FW_LITE_VIEW, name: 'New View'});
      expect(refetchSpy).toHaveBeenCalledOnce();
    });
  });

  describe('update', () => {
    it('sends the view id and visible fields to the API', async () => {
      const {service, apiSpy} = createMockService();
      const view: View = {
        ...FW_LITE_VIEW,
        id: 'my-view',
        name: 'Updated View',
        entryFields: [
          {fieldId: 'citationForm', show: true},
          {fieldId: 'note', show: true},
          {fieldId: 'lexemeForm', show: false},
        ],
      };

      await service.update('my-view', view);

      expect(apiSpy.updateCustomView).toHaveBeenCalledOnce();
      const [sentId, sentView] = apiSpy.updateCustomView.mock.calls[0];
      expect(sentId).toBe('my-view');
      expect(sentView.name).toBe('Updated View');
      const sentFieldIds = sentView.entryFields.map(f => f.fieldId);
      expect(sentFieldIds).toContain('citationForm');
      expect(sentFieldIds).toContain('note');
      expect(sentFieldIds).not.toContain('lexemeForm');
    });

    it('returns a custom view with show flags populated', async () => {
      const {service} = createMockService();
      const view: View = {
        ...FW_LITE_VIEW,
        id: 'my-view',
        name: 'Updated View',
        entryFields: [
          {fieldId: 'citationForm', show: true},
          {fieldId: 'note', show: true},
          {fieldId: 'lexemeForm', show: false},
        ],
      };

      const result = await service.update('my-view', view);

      expect(result.custom).toBe(true);
      expect(result.name).toBe('Updated View');
      expect(result.entryFields.every(f => 'show' in f)).toBe(true);
      const shownFields = result.entryFields.filter(f => f.show);
      expect(shownFields.length).toBe(2);
      expect(shownFields.length).toBeLessThan(result.entryFields.length);
    });

    it('calls refetch after updating', async () => {
      const {service, refetchSpy} = createMockService();
      await service.update('my-view', {...FW_LITE_VIEW, id: 'my-view', name: 'Updated View'});
      expect(refetchSpy).toHaveBeenCalledOnce();
    });
  });

  describe('delete', () => {
    it('calls deleteCustomView with the view id', async () => {
      const {service, apiSpy} = createMockService();
      await service.delete('my-view');
      expect(apiSpy.deleteCustomView).toHaveBeenCalledWith('my-view');
    });

    it('calls refetch after deleting', async () => {
      const {service, refetchSpy} = createMockService();
      await service.delete('my-view');
      expect(refetchSpy).toHaveBeenCalledOnce();
    });
  });
});
