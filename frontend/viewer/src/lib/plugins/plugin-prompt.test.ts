import {describe, expect, it} from 'vitest';
import {buildPluginPrompt} from './plugin-prompt';
import type {IMiniLcmJsInvokable} from '$lib/dotnet-types';

const api = {
  getWritingSystems: async () => ({
    vernacular: [{wsId: 'seh', name: 'Sena', isAudio: false}],
    analysis: [{wsId: 'en', name: 'English', isAudio: false}],
  }),
  getPartsOfSpeech: async () => [{id: 'p1', name: {en: 'Noun'}}],
  getSemanticDomains: async () => [{id: 'd1', code: '1.1', name: {en: 'Sky'}}],
  countEntries: async () => 1583,
} as unknown as IMiniLcmJsInvokable;

const project = {projectName: 'Test Project', projectCode: 'test'};

describe('buildPluginPrompt options', () => {
  it('defaults: writes allowed, mobile required, project-specific, offline, no cultural section', async () => {
    const p = await buildPluginPrompt(api, project);
    expect(p).toContain('fwlite.createEntry');
    expect(p).toContain('fwlite.applyChanges');
    expect(p).toContain('## This project');
    expect(p).toContain('360px');
    expect(p).toContain('No network access');
    expect(p).not.toContain('## Cultural sensitivity');
    expect(p).not.toContain('Read-only plugin');
  });

  it('readOnly omits the writing + recording sections and states read-only', async () => {
    const p = await buildPluginPrompt(api, project, {readOnly: true});
    expect(p).toContain('Read-only plugin');
    expect(p).not.toContain('fwlite.createEntry');
    expect(p).not.toContain('fwlite.applyChanges');
    expect(p).not.toContain('fwlite.saveFile');
  });

  it('internet allows network access', async () => {
    const p = await buildPluginPrompt(api, project, {internet: true});
    expect(p).toContain('Internet is allowed');
    expect(p).not.toContain('No network access');
  });

  it('mobile off softens the layout requirement', async () => {
    const p = await buildPluginPrompt(api, project, {mobile: false});
    expect(p).not.toContain('360px');
    expect(p).toContain('target desktop/tablet');
  });

  it('non-project-specific asks for a portable plugin and drops the project section', async () => {
    const p = await buildPluginPrompt(api, project, {projectSpecific: false});
    expect(p).toContain('Make it portable');
    expect(p).not.toContain('## This project');
  });

  it('cultural sensitivity adds its section', async () => {
    const p = await buildPluginPrompt(api, project, {culturalSensitivity: true});
    expect(p).toContain('## Cultural sensitivity');
  });
});
