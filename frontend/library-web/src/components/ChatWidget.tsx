import { useMutation } from '@tanstack/react-query'
import { Mic, MessageCircle, Send, X } from 'lucide-react'
import { useRef, useState, type FormEvent } from 'react'
import { assistantApi } from '@/api'
import { Button, TextInput } from '@/components/ui'
import { getLang, t } from '@/lib/i18n'
import { normaliseError } from '@/lib/api'
import { useSpeechRecognition } from '@/lib/speech'

interface ChatMessage {
  role: 'user' | 'assistant'
  text: string
}

/** Librarian-only agentic chat: ask about copy counts, most-borrowed books, top borrowers. */
export function ChatWidget() {
  const [open, setOpen] = useState(false)
  const [input, setInput] = useState('')
  const [messages, setMessages] = useState<ChatMessage[]>([])
  const listRef = useRef<HTMLDivElement>(null)

  const ask = useMutation({
    mutationFn: (message: string) => assistantApi.chat(message),
    onSuccess: (res) => {
      setMessages((m) => [...m, { role: 'assistant', text: res.answer }])
      requestAnimationFrame(() => listRef.current?.scrollTo({ top: listRef.current.scrollHeight }))
    },
    onError: (e) => {
      setMessages((m) => [...m, { role: 'assistant', text: normaliseError(e).message }])
    },
  })

  const speech = useSpeechRecognition((transcript) => setInput(transcript), getLang())

  const send = (e: FormEvent) => {
    e.preventDefault()
    const message = input.trim()
    if (!message || ask.isPending) return
    setMessages((m) => [...m, { role: 'user', text: message }])
    setInput('')
    ask.mutate(message)
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
        <button type="button" onClick={() => setOpen(false)} className="text-brand-100 hover:text-white">
          <X size={18} />
        </button>
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
        {ask.isPending && <div className="max-w-[85%] rounded-xl bg-slate-100 px-3 py-2 text-sm text-slate-400">{t('assistant.thinking')}</div>}
      </div>

      <form onSubmit={send} className="flex items-center gap-1.5 border-t border-slate-100 p-2">
        <TextInput
          value={input}
          onChange={(e) => setInput(e.target.value)}
          placeholder={t('assistant.placeholder')}
          className="text-sm"
        />
        {speech.supported && (
          <Button
            type="button"
            variant={speech.listening ? 'danger' : 'secondary'}
            size="sm"
            onClick={() => (speech.listening ? speech.stop() : speech.start())}
            title={t('assistant.voice')}
          >
            <Mic size={14} />
          </Button>
        )}
        <Button type="submit" size="sm" disabled={ask.isPending || !input.trim()}>
          <Send size={14} />
        </Button>
      </form>
    </div>
  )
}
