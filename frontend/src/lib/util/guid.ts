import type { UUID } from 'crypto';

const guidRegex = /^[0-9a-f]{8}-?[0-9a-f]{4}-?[0-5][0-9a-f]{3}-?[089ab][0-9a-f]{3}-?[0-9a-f]{12}$/i;

export function isGuid(val?: string): val is UUID {
  // only match strings of the exact length of a GUID, with or without dashes
  return !!val && (val.length == 32 || val.length == 36) && guidRegex.test(val);
}
