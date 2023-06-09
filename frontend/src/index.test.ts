import { describe, expect, it } from 'vitest';

import { webcrypto } from 'crypto';

describe('sum test', () => {
  it('adds 1 + 2 to equal 3', () => {
    expect(1 + 2).toBe(3);
  });

  it('can hash a pw using sha1', async () => {
    const pw = 'h0vJVjQUOfxaEC3Uddz';
    const salt = '266d7346dbf074a28ab29ef730191feb';
    const expectedHash = 'ba205a1131e34030fa1507a77cb9d1bd0c5fb800';

    async function hash(message: string): Promise<string> {
      const msgUint8 = new TextEncoder().encode(message); // encode as (utf-8) Uint8Array
      const hashBuffer = await webcrypto.subtle.digest('SHA-1', msgUint8); // hash the message
      const hashArray = Array.from(new Uint8Array(hashBuffer)); // convert buffer to byte array
      const hashHex = hashArray.map((b) => b.toString(16).padStart(2, '0')).join(''); // convert bytes to hex string
      return hashHex;
    }

    const actualHash = await hash(salt + await hash(pw));
    expect(actualHash).toBe(expectedHash);
  })
});
