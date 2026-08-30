import { useEffect, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import {
  ArrowUpRight,
  BookOpen,
  Check,
  ChevronDown,
  ChevronLeft,
  ChevronRight,
  Filter,
  Loader2,
  Search,
  SlidersHorizontal,
} from "lucide-react";

import { booksApi } from "../../api/booksApi";
import { Link } from "react-router-dom";

const SEARCH_OPTIONS = [
  { value: "title", label: "Title" },
  { value: "isbn", label: "ISBN" },
  { value: "publishedYear", label: "Published Year" },
  { value: "description", label: "Description" },
] as const;

const SORT_OPTIONS = [
  { value: "title", label: "Title" },
  { value: "isbn", label: "ISBN" },
  { value: "publishedYear", label: "Published Year" },
  { value: "description", label: "Description" },
] as const;

export default function BooksPage() {
  const [searchInput, setSearchInput] = useState("");
  const [search, setSearch] = useState("");

  // Default search field = Title
  const [searchBy, setSearchBy] = useState<string[]>(["title"]);

  // Default sorting = Title descending
  const [sortBy, setSortBy] = useState<string[]>(["title"]);
  const [sortDirection, setSortDirection] = useState("desc");

  const [pageNumber, setPageNumber] = useState(1);

  const [searchOpen, setSearchOpen] = useState(false);
  const [sortOpen, setSortOpen] = useState(false);
  const [directionOpen, setDirectionOpen] = useState(false);

  // -------------------------------------------------------
  // Debounced search
  // -------------------------------------------------------

  useEffect(() => {
    const timer = window.setTimeout(() => {
      setSearch(searchInput.trim());
      setPageNumber(1);
    }, 350);

    return () => {
      window.clearTimeout(timer);
    };
  }, [searchInput]);

  // -------------------------------------------------------
  // Books query
  // -------------------------------------------------------

  const { data, isLoading, isFetching, isError, refetch } = useQuery({
    queryKey: ["books", search, searchBy, sortBy, sortDirection, pageNumber],

    queryFn: () =>
      booksApi.getAll({
        pageNumber,
        pageSize: 10,
        search: search || undefined,
        searchBy: searchBy.join(","),
        sortBy: sortBy.join(","),
        sortDirection,
      }),
  });

  // -------------------------------------------------------
  // Derived values
  // -------------------------------------------------------

  const books = data?.items ?? [];
  const totalItems = data?.totalItems ?? 0;
  const totalPages = data?.totalPages ?? 1;

  const selectedSearchLabel =
    searchBy.length === 1
      ? (SEARCH_OPTIONS.find((option) => option.value === searchBy[0])?.label ??
        "Title")
      : `${searchBy.length} fields`;

  const selectedSortLabel =
    sortBy.length === 1
      ? (SORT_OPTIONS.find((option) => option.value === sortBy[0])?.label ??
        "Title")
      : sortBy.length + " fields";

  // -------------------------------------------------------
  // Handlers
  // -------------------------------------------------------

  const toggleSearchField = (value: string) => {
    setSearchBy((current) => {
      // Don't allow zero selected search fields.
      if (current.includes(value) && current.length === 1) {
        return current;
      }

      if (current.includes(value)) {
        return current.filter((item) => item !== value);
      }

      return [...current, value];
    });

    setPageNumber(1);
  };


  const handleDirectionChange = (direction: "asc" | "desc") => {
    setSortDirection(direction);
    setPageNumber(1);
    setDirectionOpen(false);
  };

  const clearSearch = () => {
    setSearchInput("");
    setSearch("");
    setPageNumber(1);
  };

  // -------------------------------------------------------
  // Render
  // -------------------------------------------------------

  return (
    <section className="books-page">
      {/* Header */}
      <header className="books-page__header">
        <div>
          <div className="books-page__eyebrow">
            <BookOpen size={14} />
            Library collection
          </div>

          <h1>Books</h1>

          <p>
            Discover, search and manage every title in your library collection.
          </p>
        </div>

        <Link
          className="books-add-button"
          to="/books/add"
          style={{ textDecoration: "none" }}
        >
          <BookOpen size={17} />
          Add Book
        </Link>
      </header>

      {/* Toolbar */}
      <div className="books-toolbar">
        {/* Search input */}
        <div className="books-search">
          <Search size={18} />

          <input
            value={searchInput}
            onChange={(event) => {
              setSearchInput(event.target.value);
            }}
            placeholder="Search books..."
            type="search"
          />

          {isFetching && !isLoading && (
            <Loader2 className="books-search__loader" size={16} />
          )}
        </div>

        {/* Search By */}
        <div className="books-dropdown">
          <button
            type="button"
            className="books-dropdown__trigger"
            onClick={() => {
              setSearchOpen((current) => !current);
              setSortOpen(false);
              setDirectionOpen(false);
            }}
          >
            <Filter size={15} />

            <span>Search: {selectedSearchLabel}</span>

            <ChevronDown size={14} />
          </button>

          {searchOpen && (
            <div className="books-dropdown__menu">
              <div className="books-dropdown__heading">Search in</div>

              {SEARCH_OPTIONS.map((option) => {
                const selected = searchBy.includes(option.value);

                return (
                  <button
                    key={option.value}
                    type="button"
                    className={`books-dropdown__option ${
                      selected ? "is-selected" : ""
                    }`}
                    onClick={() => {
                      toggleSearchField(option.value);
                    }}
                  >
                    <span>{option.label}</span>

                    {selected && <Check size={15} />}
                  </button>
                );
              })}
            </div>
          )}
        </div>

        {/* Sort By - Multi Select */}
        <div className="books-dropdown">
          <button
            type="button"
            className="books-dropdown__trigger"
            onClick={() => {
              setSortOpen((current) => !current);
              setSearchOpen(false);
              setDirectionOpen(false);
            }}
          >
            <SlidersHorizontal size={15} />

            <span>Sort: {selectedSortLabel}</span>

            <ChevronDown size={14} />
          </button>

          {sortOpen && (
            <div className="books-dropdown__menu">
              <div className="books-dropdown__heading">Sort by</div>

              {SORT_OPTIONS.map((option) => {
                const selected = sortBy.includes(option.value);

                return (
                  <button
                    key={option.value}
                    type="button"
                    className={`books-dropdown__option ${
                      selected ? "is-selected" : ""
                    }`}
                    onClick={() => {
                      setSortBy((current) => {
                        // Never allow zero sort fields.
                        if (
                          current.includes(option.value) &&
                          current.length === 1
                        ) {
                          return current;
                        }

                        if (current.includes(option.value)) {
                          return current.filter(
                            (value) => value !== option.value,
                          );
                        }

                        return [...current, option.value];
                      });

                      setPageNumber(1);
                    }}
                  >
                    <span>{option.label}</span>

                    {selected && <Check size={15} />}
                  </button>
                );
              })}
            </div>
          )}
        </div>

        {/* Sort Direction */}
        <div className="books-dropdown">
          <button
            type="button"
            className="books-dropdown__trigger"
            onClick={() => {
              setDirectionOpen((current) => !current);
              setSearchOpen(false);
              setSortOpen(false);
            }}
          >
            <span>{sortDirection === "desc" ? "Descending" : "Ascending"}</span>

            <ChevronDown size={14} />
          </button>

          {directionOpen && (
            <div className="books-dropdown__menu">
              <button
                type="button"
                className={`books-dropdown__option ${
                  sortDirection === "desc" ? "is-selected" : ""
                }`}
                onClick={() => {
                  handleDirectionChange("desc");
                }}
              >
                <span>Descending</span>

                {sortDirection === "desc" && <Check size={15} />}
              </button>

              <button
                type="button"
                className={`books-dropdown__option ${
                  sortDirection === "asc" ? "is-selected" : ""
                }`}
                onClick={() => {
                  handleDirectionChange("asc");
                }}
              >
                <span>Ascending</span>

                {sortDirection === "asc" && <Check size={15} />}
              </button>
            </div>
          )}
        </div>
      </div>

      {/* Results summary */}
      {!isLoading && !isError && (
        <div className="books-results-summary">
          <span>
            {totalItems} {totalItems === 1 ? "book" : "books"}
          </span>

          {isFetching && (
            <span className="books-results-summary__loading">Updating...</span>
          )}
        </div>
      )}

      {/* Loading */}
      {isLoading && (
        <div className="books-grid">
          {Array.from({ length: 6 }).map((_, index) => (
            <div className="book-card book-card--skeleton" key={index}>
              <div className="skeleton skeleton--cover" />

              <div className="book-card__content">
                <div className="skeleton skeleton--title" />
                <div className="skeleton skeleton--text" />
                <div className="skeleton skeleton--text short" />
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Error */}
      {!isLoading && isError && (
        <div className="books-state books-state--error">
          <div className="books-state__icon">!</div>

          <h2>Unable to load the collection</h2>

          <p>Something went wrong while contacting the library service.</p>

          <button
            type="button"
            onClick={() => {
              refetch();
            }}
          >
            Try again
          </button>
        </div>
      )}

      {/* Empty */}
      {!isLoading && !isError && books.length === 0 && (
        <div className="books-state">
          <div className="books-state__icon">
            <Search size={22} />
          </div>

          <h2>No books found</h2>

          <p>We couldn't find any books matching your search.</p>

          {(search || searchInput) && (
            <button type="button" onClick={clearSearch}>
              Clear search
            </button>
          )}
        </div>
      )}

      {/* Books */}
      {!isLoading && !isError && books.length > 0 && (
        <>
          <div className="books-grid">
            {books.map((book) => (
              <article className="book-card" key={book.id}>
                {/* Cover */}
                <div className="book-card__cover">
                  <div className="book-card__cover-glow" />

                  <BookOpen size={38} />

                  <span>LIBRA</span>
                </div>

                {/* Content */}
                <div className="book-card__content">
                  <div className="book-card__meta">
                    <span>BOOK</span>

                    <span className="book-card__year">
                      {book.publishedYear}
                    </span>
                  </div>

                  <h2>{book.title}</h2>

                  <p className="book-card__author">{book.author}</p>

                  <div className="book-card__isbn">
                    <span>ISBN</span>

                    <strong>{book.isbn}</strong>
                  </div>

                  {/* Actions */}
                  <div className="book-card__footer">
                    <Link
                      className="book-card__details"
                      to={`/books/${book.id}`}
                      style={{ textDecoration: "none" }}
                    >
                      View details
                      <ArrowUpRight size={15} />
                    </Link>

                    <button className="book-card__issue" type="button">
                      Issue
                    </button>
                  </div>
                </div>
              </article>
            ))}
          </div>

          {/* Pagination */}
          {totalPages > 1 && (
            <div className="books-pagination">
              <span>
                Page {pageNumber} of {totalPages}
              </span>

              <div>
                <button
                  type="button"
                  disabled={pageNumber <= 1}
                  onClick={() => {
                    setPageNumber((current) => Math.max(1, current - 1));
                  }}
                >
                  <ChevronLeft size={17} />
                </button>

                <button
                  type="button"
                  disabled={pageNumber >= totalPages}
                  onClick={() => {
                    setPageNumber((current) =>
                      Math.min(totalPages, current + 1),
                    );
                  }}
                >
                  <ChevronRight size={17} />
                </button>
              </div>
            </div>
          )}
        </>
      )}
    </section>
  );
}
