const subscriptDigits = '₀₁₂₃₄₅₆₇₈₉';

/**
 * Plain-text form of HomographNumber, for places that only take a string (e.g. a `title`).
 * Display only: never append it to headwords used for search, sorting or matching.
 */
export function homographSubscript(homographNumber: number): string {
  if (!(homographNumber > 0)) return '';
  return String(homographNumber).replace(/\d/g, (digit) => subscriptDigits[Number(digit)]);
}

/**
 * Inverse of {@link homographSubscript}: splits a trailing homograph number off a plain-text headword.
 * Subscripts elsewhere (H₂O) stay text, as does a trailing run that wouldn't round-trip (₀, ₀₁).
 */
export function splitHomographSubscript(text: string): {text: string; homographNumber: number} {
  const run = /[₀-₉]+$/u.exec(text)?.[0];
  if (!run) return {text, homographNumber: 0};
  const homographNumber = Number([...run].map((digit) => subscriptDigits.indexOf(digit)).join(''));
  return homographSubscript(homographNumber) === run
    ? {text: text.slice(0, -run.length), homographNumber}
    : {text, homographNumber: 0};
}
