import { describe, expect, it } from 'vitest'
import { toSpokenText } from './speech'

describe('toSpokenText', () => {
  it('flattens multi-line answers into sentences without quotes', () => {
    const answer = 'The library has 2 copies of "Clean Code".\n"Clean Code" by Robert C. Martin: 2 total, 2 available, 0 borrowed.'
    expect(toSpokenText(answer)).toBe(
      'The library has 2 copies of Clean Code. Clean Code by Robert C. Martin: 2 total, 2 available, 0 borrowed.',
    )
  })

  it('drops list numbering', () => {
    expect(toSpokenText('Most borrowed:\n1. Refactoring\n2. Clean Code')).toBe(
      'Most borrowed: Refactoring. Clean Code',
    )
  })
})

describe('speak', () => {
  it('reads the cleaned answer aloud in the UI language and cancels any earlier speech', async () => {
    const spoken: { text: string; lang: string }[] = []
    let cancelled = 0
    class FakeUtterance {
      lang = ''
      rate = 0
      text: string
      constructor(text: string) {
        this.text = text
      }
    }
    Object.assign(window, { SpeechSynthesisUtterance: FakeUtterance })
    Object.defineProperty(window, 'speechSynthesis', {
      configurable: true,
      value: { cancel: () => cancelled++, speak: (u: FakeUtterance) => spoken.push({ text: u.text, lang: u.lang }) },
    })

    const { speak } = await import('./speech')
    speak('1 copy of "Refactoring" is currently borrowed.', 'bn')

    expect(cancelled).toBe(1)
    expect(spoken).toEqual([{ text: '1 copy of Refactoring is currently borrowed.', lang: 'bn-BD' }])
  })
})
