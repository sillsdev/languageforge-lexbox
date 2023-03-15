import { browser } from '$app/environment'
import phrases from './phrases.json'

export default function t(key: string, arg?: any): string {
  const phrase = phrases[key]
  if (phrase === undefined) {
    return '⤂ translation key not found! ⤃'
  }

  const langOnlyNoVariant = browser && navigator.language.substring(0,2) || 'en' // TODO: determine arch for these, right now just default to client-side only.

  return phrase[langOnlyNoVariant].replace('%1', arg)
}
