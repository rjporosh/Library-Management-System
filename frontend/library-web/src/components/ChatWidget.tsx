import { useMutation } from '@tanstack/react-query'
import { Mic, MessageCircle, Send, Volume2, VolumeX, X } from 'lucide-react'
import { useEffect, useRef, useState, type FormEvent } from 'react'
import { assistantApi } from '@/api'
import { Button, TextInput } from '@/components/ui'
import { getLang, t } from '@/lib/i18n'
import { normaliseError } from '@/lib/api'
import { cancelSpeech, isSpeechSynthesisSupported, speak, useSpeechRecognition } from '@/lib/speech'

interface ChatMessage {
  role: 'user' | 'assistant'
  text: string
}

const SPEAK_STORAGE_KEY = 'lms.assistant.speak'

function readSpeakPreference(): boolean {
  try {
    return localStorage.getItem(SPEAK_STORAGE_KEY) !== 'off'
  } catch {
    return true
  }
}

/**
 * Librarian-only agentic chat: ask about copy counts (by title, author,
 * publisher, edition), most-borrowed books and top borrowers. Questions can be
 * typed or spoken; a spoken question is sent automatically and the answer is
 * both written in the thread and read aloud (toggle in the header).
 */
export function ChatWidget() {
  const [open, setOpen] = useState(false)
  const [input, setInput] = useState('')
  const [messages, setMessages] = useState<ChatMessage[]>([])
  const [speakAnswers, setSpeakAnswers] = useState(readSpeakPreference)
  const listRef = useRef<HTMLDivElement>(null)
  const speakAnswersRef = useRef(speakAnswers)
  const voiceTurnRef = useRef(false)

  useEffect(() => {
    speakAnswersRef.current = speakAnswers
  }, [speakAnswers])

  useEffect(() => cancelSpeech, [])

  const scrollToEnd = () =>
    requestAnimationFrame(() => listRef.current?.scrollTo({ top: listRef.current.scrollHeight }))

  const reply = (text: string) => {
    setMessages((m) => [...m, { role: 'assistant', text }])
    scrollToEnd()
    if (voiceTurnRef.current && speakAnswersRef.current) speak(text, getLang())
    voiceTurnRef.current = false
  }

  const ask = useMutation({
    mutationFn: (message: string) => assistantApi.chat(message),
    onSuccess: (res) => reply(res.answer),
    onError: (e) => reply(normaliseError(e).message),
  })

  const submit = (raw: string, viaVoice: boolean) => {
    const message = raw.trim()
    if (!message || ask.isPending) return
    cancelSpeech()
    voiceTurnRef.current = viaVoice
    setMessages((m) => [...m, { role: 'user', text: message }])
    setInput('')
    scrollToEnd()
    ask.mutate(message)
  }

  const speech = useSpeechRecognition((transcript) => submit(transcript, true), getLang())

  const send = (e: FormEvent) => {
    e.preventDefault()
    submit(input, false)
  }

  const toggleSpeak = () => {
    const next = !speakAnswers
    setSpeakAnswers(next)
    if (!next) cancelSpeech()
    try {
      localStorage.setItem(SPEAK_STORAGE_KEY, next ? 'on' : 'off')
    } catch {
      /* preference is a convenience only */
    }
  }

  const toggleMic = () => {
    if (speech.listening) {
      speech.stop()
    } else {
      cancelSpeech()
      speech.start()
    }
  }

  if (!open) {
    return (
      <button
        type="button"
        onClick={() => setOpen(true)}
        className="fixed bottom-5 right-5 z-40 flex h-14 w-14 items-center justify-center rounded-full bg-brand-600 text-white shadow-lg hover:bg-brand-700"
        title={t('assistant.open')}
      >
        <MessageCircle size={24} />
      </button>
    )
  }

  return (
    <div className="fixed bottom-5 right-5 z-40 flex h-[28rem] w-80 flex-col overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-2xl sm:w-96">
      <div className="flex items-center justify-between border-b border-slate-100 bg-brand-600 px-4 py-3 text-white">
        <div>
          <p className="text-sm font-semibold">{t('assistant.title')}</p>
          <p className="text-xs text-brand-100">{t('assistant.subtitle')}</p>
        </div>
        <div className="flex items-center gap-3">
          {isSpeechSynthesisSupported() && (
            <button
              type="button"
              onClick={toggleSpeak}
              className="text-brand-100 hover:text-white"
              title={speakAnswers ? t('assistant.speakOn') : t('assistant.speakOff')}
              aria-pressed={speakAnswers}
            >
              {speakAnswers ? <Volume2 size={18} /> : <VolumeX size={18} />}
            </button>
          )}
          <button
            type="button"
            onClick={() => {
              cancelSpeech()
              speech.stop()
              setOpen(false)
            }}
            className="text-brand-100 hover:text-white"
          >
            <X size={18} />
          </button>
        </div>
      </div>

      <div ref={listRef} className="flex-1 space-y-2 overflow-y-auto px-3 py-3">
        {messages.length === 0 && (
          <p className="rounded-lg bg-slate-50 p-3 text-xs text-slate-500">{t('assistant.hint')}</p>
        )}
        {messages.map((m, i) => (
          <div
            key={i}
            className={`max-w-[85%] whitespace-pre-line rounded-xl px-3 py-2 text-sm ${
              m.role === 'user' ? 'ml-auto bg-brand-600 text-white' : 'bg-slate-100 text-slate-800'
            }`}
          >
            {m.text}
          </div>
        ))}
        {speech.listening && (
          <div className="flex items-center gap-2 rounded-xl bg-rose-50 px-3 py-2 text-sm text-rose-700">
            <span className="h-2 w-2 animate-pulse rounded-full bg-rose-500" />
            {t('assistant.listening')}
          </div>
        )}
        {speech.error && !speech.listening && (
          <div className="rounded-xl bg-amber-50 px-3 py-2 text-xs text-amber-800">
            {speech.error === 'not-supported'
              ? t('assistant.voiceUnsupported')
              : t('assistant.voiceError', { error: speech.error })}
          </div>
        )}
        {ask.isPending && <div className="max-w-[85%] rounded-xl bg-slate-100 px-3 py-2 text-sm text-slate-400">{t('assistant.thinking')}</div>}
      </div>

      <form onSubmit={send} className="flex items-center gap-1.5 border-t border-slate-100 p-2">
        <TextInput
          value={input}
          onChange={(e) => setInput(e.target.value)}
          placeholder={t('assistant.placeholder')}
          className="text-sm"
        />
        <Button
          type="button"
          variant={speech.listening ? 'danger' : 'secondary'}
          size="sm"
          onClick={toggleMic}
          title={t('assistant.voice')}
          aria-pressed={speech.listening}
        >
          <Mic size={14} />
        </Button>
        <Button type="submit" size="sm" disabled={ask.isPending || !input.trim()}>
          <Send size={14} />
        </Button>
      </form>
    </div>
  )
}
