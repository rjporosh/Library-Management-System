import * as signalR from '@microsoft/signalr'
import { API_BASE_URL, getStoredAuth } from './api'

export interface LibraryNotification {
  id: string
  type: 'Borrow' | 'Purchase' | 'LowStock' | string
  title: string
  message: string
  timestamp: string
  requestId?: string | null
  bookId?: string | null
  bookTitle?: string | null
  memberName?: string | null
  membershipNumber?: string | null
  status?: string | null
  suggestedAuthor?: string | null
  note?: string | null
}

const getHubUrl = () => {
  const baseUrl = API_BASE_URL.replace(/\/api\/?$/, '')
  return `${baseUrl}/hubs/notifications`
}

let connection: signalR.HubConnection | null = null
const listeners = new Set<(notification: LibraryNotification) => void>()

export function getSignalRConnection(): signalR.HubConnection {
  if (!connection) {
    connection = new signalR.HubConnectionBuilder()
      .withUrl(getHubUrl(), {
        accessTokenFactory: () => getStoredAuth()?.accessToken ?? '',
        transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.LongPolling,
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Warning)
      .build()

    const handleNotification = (data: LibraryNotification) => {
      listeners.forEach((listener) => {
        try {
          listener(data)
        } catch {
          /* ignore listener errors */
        }
      })
    }

    connection.on('ReceiveNotification', handleNotification)
    connection.on('ReceiveBorrowRequest', handleNotification)
  }

  return connection
}

export async function startSignalR(): Promise<void> {
  const auth = getStoredAuth()
  if (!auth?.accessToken) return

  const conn = getSignalRConnection()
  if (conn.state === signalR.HubConnectionState.Disconnected) {
    try {
      await conn.start()
    } catch {
      // Automatic reconnect will retry if initial attempt fails
    }
  }
}

export async function stopSignalR(): Promise<void> {
  if (connection && connection.state !== signalR.HubConnectionState.Disconnected) {
    try {
      await connection.stop()
    } catch {
      /* ignore */
    }
  }
}

export function subscribeToNotifications(callback: (notification: LibraryNotification) => void): () => void {
  listeners.add(callback)
  void startSignalR()
  return () => {
    listeners.delete(callback)
  }
}

