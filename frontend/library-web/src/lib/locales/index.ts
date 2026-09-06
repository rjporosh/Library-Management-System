import { en, type MessageKey } from './en'
import { bn } from './bn'
import type { Lang } from '../i18n'

export type { MessageKey }

export const dictionaries: Record<Lang, Record<MessageKey, string>> = { en, bn }
