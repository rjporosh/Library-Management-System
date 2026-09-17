import { useQuery } from '@tanstack/react-query'
import { ArrowLeft, BookOpen, ExternalLink, Headphones, ShoppingCart } from 'lucide-react'
import { Link, useParams } from 'react-router-dom'
import { booksApi } from '@/api'
import { Badge, Card, ErrorState, PageHeader, Spinner } from '@/components/ui'
import { t } from '@/lib/i18n'
import { coverImageSrc, PLACEHOLDER_COVER } from '@/lib/covers'

export default function BookDetailPage() {
  const { id = '' } = useParams()

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['books', id, 'detail'],
    queryFn: () => booksApi.detail(id),
  })

  if (isLoading) return <Spinner />
  if (isError || !data)
    return <ErrorState message={t('bookDetail.loadError')} onRetry={() => void refetch()} />

  const { book, availability } = data

  return (
    <>
      <Link to="/books" className="mb-3 inline-flex items-center gap-1 text-sm text-slate-500 hover:text-slate-700">
        <ArrowLeft size={14} /> {t('bookDetail.back')}
      </Link>

      <PageHeader title={book.title} subtitle={`${book.author} · ${book.isbn}`} />

      <div className="grid gap-4 lg:grid-cols-[200px_1fr]">
        <Card className="overflow-hidden p-0">
          <img
            src={coverImageSrc(book)}
            alt={book.title}
            className="aspect-[2/3] w-full object-cover"
            onError={(e) => {
              e.currentTarget.onerror = null
              e.currentTarget.src = PLACEHOLDER_COVER
            }}
          />
        </Card>

        <div className="space-y-4">
          <Card className="p-5">
            <div className="flex flex-wrap gap-x-8 gap-y-2 text-sm text-slate-700">
              <Field label={t('books.col.category')} value={book.category} />
              <Field label={t('books.col.publisher')} value={book.publisher} />
              <Field label={t('books.field.publishedYear')} value={String(book.publishedYear)} />
              {book.edition && <Field label={t('bookDetail.edition')} value={book.edition} />}
            </div>
            {book.description && <p className="mt-3 text-sm text-slate-600">{book.description}</p>}
          </Card>

          <Card className="p-5">
            <p className="mb-2 text-xs font-semibold uppercase text-slate-400">
              {t('bookDetail.availability')}
            </p>
            <AvailabilityPanel status={availability.status} accessUrl={availability.accessUrl}
              externalBuyUrl={availability.externalBuyUrl} externalPdfUrl={availability.externalPdfUrl} />
            <p className="mt-3 text-xs text-slate-400">
              {t('bookDetail.copyCount', {
                available: availability.availableCopies,
                total: availability.totalCopies,
              })}
            </p>
          </Card>
        </div>
      </div>
    </>
  )
}

function Field({ label, value }: Readonly<{ label: string; value: string }>) {
  return (
    <span>
      <span className="text-xs text-slate-400">{label}: </span>
      <span className="font-medium text-slate-800">{value}</span>
    </span>
  )
}

function AvailabilityPanel({
  status,
  accessUrl,
  externalBuyUrl,
  externalPdfUrl,
}: Readonly<{
  status: string
  accessUrl?: string | null
  externalBuyUrl?: string | null
  externalPdfUrl?: string | null
}>) {
  if (status === 'PhysicalAvailable') {
    return <Badge tone="green">{t('bookDetail.status.physical')}</Badge>
  }

  if (status === 'Ebook') {
    return (
      <div className="flex items-center gap-3">
        <Badge tone="blue">{t('bookDetail.status.ebook')}</Badge>
        {accessUrl && (
          <a href={accessUrl} target="_blank" rel="noreferrer" className="inline-flex items-center gap-1 text-sm font-medium text-brand-600 hover:underline">
            <BookOpen size={14} /> {t('bookDetail.readEbook')}
          </a>
        )}
      </div>
    )
  }

  if (status === 'Audiobook') {
    return (
      <div className="flex items-center gap-3">
        <Badge tone="blue">{t('bookDetail.status.audiobook')}</Badge>
        {accessUrl && (
          <a href={accessUrl} target="_blank" rel="noreferrer" className="inline-flex items-center gap-1 text-sm font-medium text-brand-600 hover:underline">
            <Headphones size={14} /> {t('bookDetail.listenAudiobook')}
          </a>
        )}
      </div>
    )
  }

  return (
    <div className="space-y-2">
      <Badge tone="amber">{t('bookDetail.status.unavailable')}</Badge>
      <p className="text-sm text-slate-500">{t('bookDetail.unavailableHint')}</p>
      <div className="flex flex-wrap gap-3">
        {externalBuyUrl && (
          <a href={externalBuyUrl} target="_blank" rel="noreferrer" className="inline-flex items-center gap-1 text-sm font-medium text-brand-600 hover:underline">
            <ShoppingCart size={14} /> {t('bookDetail.buyOnline')}
          </a>
        )}
        {externalPdfUrl && (
          <a href={externalPdfUrl} target="_blank" rel="noreferrer" className="inline-flex items-center gap-1 text-sm font-medium text-brand-600 hover:underline">
            <ExternalLink size={14} /> {t('bookDetail.viewPdf')}
          </a>
        )}
        {!externalBuyUrl && !externalPdfUrl && (
          <p className="text-xs text-slate-400">{t('bookDetail.noSuggestion')}</p>
        )}
      </div>
    </div>
  )
}
