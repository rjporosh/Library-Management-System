import { useQuery } from '@tanstack/react-query'
import { booksApi } from '../../api/booksApi'

export default function BooksPage() {
  const {
    data,
    isLoading,
    isError,
    error,
  } = useQuery({
    queryKey: ['books'],
    queryFn: () => booksApi.getAll(),
  })

  if (isLoading) {
    return <div>Loading books...</div>
  }

  if (isError) {
    return (
      <div>
        Failed to load books.
        <pre>{String(error)}</pre>
      </div>
    )
  }

  return (
    <div>
      <h1>Books</h1>

      <pre>
        {JSON.stringify(data, null, 2)}
      </pre>
    </div>
  )
}