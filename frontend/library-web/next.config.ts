import type { NextConfig } from 'next'

// Server-side target for the same-origin proxy (/api, /hubs, /scalar...). In Docker this is the
// API container on the compose network; locally it is the dotnet run port.
const apiOrigin = (process.env.API_INTERNAL_URL ?? 'http://localhost:5254').replace(/\/$/, '')

const nextConfig: NextConfig = {
  output: 'standalone',
  reactStrictMode: true,
  poweredByHeader: false,
  async rewrites() {
    return [
      { source: '/api/:path*', destination: `${apiOrigin}/api/:path*` },
      { source: '/hubs/:path*', destination: `${apiOrigin}/hubs/:path*` },
      { source: '/scalar/:path*', destination: `${apiOrigin}/scalar/:path*` },
      { source: '/openapi/:path*', destination: `${apiOrigin}/openapi/:path*` },
      { source: '/health', destination: `${apiOrigin}/health` },
    ]
  },
}

export default nextConfig
