// Mirrors MiniLcm's StringExtensions.ContainsDiacritic: a query with a diacritic always matches diacritics exactly
export function hasDiacritic(value: string): boolean {
  return /\p{Mn}/u.test(value.normalize('NFD'));
}
