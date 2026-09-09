import { describe, expect, test } from 'vitest';
import { buildSendReceiveUrl, resumableSendReceiveOrigin } from './sendReceiveUrl';

describe('resumableSendReceiveOrigin', () => {
  test('uses resumable.localhost over http in local dev', () => {
    expect(resumableSendReceiveOrigin('localhost:3000', true)).toEqual({
      protocol: 'http',
      hostname: 'resumable.localhost',
    });
  });

  test('maps develop, staging, and production hosts', () => {
    expect(resumableSendReceiveOrigin('develop.lexbox.org', false).hostname).toBe('resumable.lexbox.dev.languagetechnology.org');
    expect(resumableSendReceiveOrigin('lexbox.dev', false).hostname).toBe('resumable.lexbox.dev.languagetechnology.org');
    expect(resumableSendReceiveOrigin('staging.languagedepot.org', false).hostname).toBe('resumable-staging.languagedepot.org');
    expect(resumableSendReceiveOrigin('languageforge.org', false).hostname).toBe('resumable.languageforge.org');
  });
});

describe('buildSendReceiveUrl', () => {
  test('omits userinfo when the password is empty', () => {
    expect(buildSendReceiveUrl('user@example.com', '', 'proj-code', 'languageforge.org', false))
      .toBe('https://resumable.languageforge.org/proj-code');
  });

  test('percent-encodes email @ and special password characters', () => {
    expect(buildSendReceiveUrl('user@example.com', 'p:a@s/s% x', 'proj-code', 'languageforge.org', false))
      .toBe('https://user%40example.com:p%3Aa%40s%2Fs%25%20x@resumable.languageforge.org/proj-code');
  });
});
