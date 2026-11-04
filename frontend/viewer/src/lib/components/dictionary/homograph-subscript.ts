const subscriptDigits = '₀₁₂₃₄₅₆₇₈₉';

/**
 * Plain-text form of HomographNumber, for places that only take a string (e.g. a `title`).
 * Display only: never append it to headwords used for search, sorting or matching.
 */
export function homographSubscript(homographNumber: number): string {
  if (!(homographNumber > 0)) return '';
  return String(homographNumber).replace(/\d/g, (digit) => subscriptDigits[Number(digit)]);
}
