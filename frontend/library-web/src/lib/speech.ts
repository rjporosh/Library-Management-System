import { useCallback, useEffect, useRef, useState } from 'react'

// Minimal ambient typing for the Web Speech API - not yet part of lib.dom.d.ts.
interface SpeechRecognitionEvent extends Event {
  results: { [index: number]: { [index: number]: { transcript: string } }; length: number }
}
interface SpeechRecognitionErrorEvent extends Event {
  error: string
}
interface SpeechRecognitionLike extends EventTarget {
  lang: string
  interimResults: boolean
  maxAlternatives: number
  start: () => void
  stop: () => void
  onresult: ((event: SpeechRecognitionEvent) => void) | null
  onerror: ((event: SpeechRecognitionErrorEvent) => void) | null
  onend: (() => void) | null
}

function getRecognitionCtor(): (new () => SpeechRecognitionLike) | null {
  const w = window as unknown as {
    SpeechRecognition?: new () => SpeechRecognitionLike
    webkitSpeechRecognition?: new () => SpeechRecognitionLike
  }
  return w.SpeechRecognition ?? w.webkitSpeechRecognition ?? null
}

export const isSpeechRecognitionSupported = (): boolean => getRecognitionCtor() !== null

/**
 * Browser-native voice search (Web Speech API) - the default, zero-backend
 * speech-to-text path. See lib/covers.ts sibling docs / guide.md for the
 * configurable Hugging Face server-side alternative.
 */
export function useSpeechRecognition(onResult: (text: string) => void, lang: 'en' | 'bn' = 'en') {
  const [listening, setListening] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const recognitionRef = useRef<SpeechRecognitionLike | null>(null)
  const onResultRef = useRef(onResult)

  useEffect(() => {
    onResultRef.current = onResult
  }, [onResult])

  useEffect(() => () => recognitionRef.current?.stop(), [])

  const start = useCallback(() => {
    const Ctor = getRecognitionCtor()
    if (!Ctor) {
      setError('not-supported')
      return
    }

    setError(null)
    const recognition = new Ctor()
    recognition.lang = lang === 'bn' ? 'bn-BD' : 'en-US'
    recognition.interimResults = false
    recognition.maxAlternatives = 1

    recognition.onresult = (event: SpeechRecognitionEvent) => {
      const transcript = event.results[event.results.length - 1]?.[0]?.transcript
      if (transcript) onResultRef.current(transcript)
    }
    recognition.onerror = (event: SpeechRecognitionErrorEvent) => setError(event.error)
    recognition.onend = () => setListening(false)

    recognitionRef.current = recognition
    setListening(true)
    recognition.start()
  }, [lang])

  const stop = useCallback(() => {
    recognitionRef.current?.stop()
    setListening(false)
  }, [])

  return { supported: isSpeechRecognitionSupported(), listening, error, start, stop }
}
