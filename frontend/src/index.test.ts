import { describe, expect, it } from 'vitest';

import {hash} from '$lib/util/hash';

describe('password hashing', () => {
  it('can hash a pw using sha1', async () => {
    const pw = 'h0vJVjQUOfxaEC3Uddz';
    const salt = '266d7346dbf074a28ab29ef730191feb';
    const expectedHash = 'ba205a1131e34030fa1507a77cb9d1bd0c5fb800';

    const actualHash = await hash(salt + await hash(pw));
    expect(actualHash).toBe(expectedHash);
  })
});
