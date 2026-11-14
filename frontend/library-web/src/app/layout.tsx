import type { Metadata, Viewport } from 'next'
import { Hind_Siliguri, Instrument_Sans, Literata } from 'next/font/google'
import './globals.css'
import { Providers } from './providers'

const ui = Instrument_Sans({ subsets: ['latin'], variable: '--font-ui', display: 'swap' })
const book = Literata({ subsets: ['latin'], variable: '--font-book', display: 'swap' })
const bn = Hind_Siliguri({
  subsets: ['bengali', 'latin'],
  weight: ['400', '500', '600', '700'],
  variable: '--font-bn',
  display: 'swap',
})

export const metadata: Metadata = {
  title: { default: 'Libra - Library Management System', template: '%s | Libra' },
  description: 'Catalog, members and lending for modern libraries.',
  icons: { icon: '/favicon.svg' },
}

export const viewport: Viewport = { themeColor: '#0e5a4a' }

export default function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return (
    <html lang="en" className={`${ui.variable} ${book.variable} ${bn.variable}`} suppressHydrationWarning>
      <body>
        <Providers>{children}</Providers>
      </body>
    </html>
  )
}
