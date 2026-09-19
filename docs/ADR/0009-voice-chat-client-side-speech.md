# ADR 0009 - Voice chat: browser speech for STT and TTS, deterministic parser for questions

**Status:** accepted (2026-09-19)

**Context.** Librarians must ask stock questions by voice (title / author / edition /
publisher, total or borrowed) and hear the answer, with the text also shown.

**Decision.**
- Speech-to-text: Web Speech API (already the default); the transcript is submitted
  automatically. Text-to-speech: `speechSynthesis` in the UI language, on by default,
  toggle persisted in `localStorage`; a synthesis failure is swallowed so the text
  answer always appears.
- Understanding: `BookQuestionParser` (pure, deterministic) maps the sentence to a
  metric + `BookFilter`; `LibraryQueryTools.GetCopyCountAsync(BookFilter)` is the single
  query used by the rule-based engine and the LLM `get_copy_count` tool.

**Trade-offs.** Zero cost, zero keys, works offline of any vendor, but needs
Chrome/Edge/Safari (Firefox lacks recognition; the mic then shows a clear message) and
the rule-based parser understands English phrasing only - Bangla questions need
`Chat:Provider=Anthropic|OpenAI`. Server-side STT (`/api/assistant/transcribe`) exists
but is not wired into the widget as a Firefox fallback yet.
