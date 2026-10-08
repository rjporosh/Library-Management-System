using System.Globalization;
using System.Text.RegularExpressions;

namespace Library.Application.Features.Assistant;

/// <summary>
/// Deterministic keyword/regex intent matching over <see cref="LibraryQueryTools"/>.
/// Answers questions on copy counts, writer catalogs, borrow statistics across timeframes
/// (today, this week, this month, till now), best borrowed books, and threshold alarms
/// ("buy more books") with native support for both English and Bengali (with numeral conversion).
/// </summary>
public sealed partial class RuleBasedChatProvider(LibraryQueryTools tools) : IChatProvider
{
    public string Name => "RuleBased";

    public async Task<ChatAnswer> AskAsync(string message, CancellationToken cancellationToken = default)
    {
        var text = message.Trim();
        var lower = text.ToLowerInvariant();
        var isBengali = Regex.IsMatch(text, @"[\u0980-\u09FF]")
            || CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("bn", StringComparison.OrdinalIgnoreCase);

        // 1. Threshold Configuration & Low Stock "Buy More" Notification Alarm
        if (IsThresholdOrAlarmIntent(lower, text))
        {
            var configuredThreshold = ExtractThresholdNumber(text, lower);
            if (configuredThreshold.HasValue)
            {
                LibraryQueryTools.GlobalThreshold = configuredThreshold.Value;
            }

            var threshold = configuredThreshold ?? LibraryQueryTools.GlobalThreshold;
            var lowStockBooks = await tools.GetLowStockThresholdBooksAsync(threshold, cancellationToken);
            return new ChatAnswer(FormatThresholdAlarm(lowStockBooks, threshold, isBengali, configuredThreshold.HasValue), Name);
        }

        // 2. Best Borrowed Book (Single most borrowed book title)
        if (IsBestBorrowedIntent(lower, text))
        {
            var timeframe = ExtractTimeframe(lower, text);
            var bestBook = await tools.GetBestBorrowedBookAsync(timeframe, cancellationToken);
            return new ChatAnswer(FormatBestBorrowed(bestBook, timeframe, isBengali), Name);
        }

        // 3. Borrows of a Writer / Author / Publication / All across Timeframe (today, this week, this month, till now)
        if (IsTimeframeBorrowsIntent(lower, text))
        {
            var (target, kind) = ExtractBorrowTarget(lower, text);
            var timeframe = ExtractTimeframe(lower, text);
            var summary = await tools.GetBorrowsByTargetAndTimeframeAsync(target, kind, timeframe, cancellationToken);
            return new ChatAnswer(FormatBorrowTimeframeSummary(summary, isBengali), Name);
        }

        // 4. Single Writer's Book Count (How many books by a specific writer / author)
        if (IsAuthorCatalogIntent(lower, text, out var authorName))
        {
            var authorSummary = await tools.GetAuthorBooksSummaryAsync(authorName, cancellationToken);
            return new ChatAnswer(FormatAuthorSummary(authorSummary, isBengali), Name);
        }

        // 5. Single Publisher's Book Count (How many books by a specific publisher)
        if (IsPublisherCatalogIntent(lower, text, out var publisherName))
        {
            var publisherSummary = await tools.GetPublisherBooksSummaryAsync(publisherName, cancellationToken);
            return new ChatAnswer(FormatPublisherSummary(publisherSummary, isBengali), Name);
        }

        // 6. Parsed Book Copy Question (Existing or standard title/filter copy metric)
        var question = BookQuestionParser.Parse(text);
        if (question is not null)
        {
            var counts = await tools.GetCopyCountAsync(question.Filter, cancellationToken);
            return new ChatAnswer(BookStatsFormatter.Describe(question, counts, isBengali), Name);
        }

        if (CopiesOfPattern().IsMatch(lower))
        {
            return new ChatAnswer(
                isBengali
                    ? "আপনি কোন বই, লেখক, প্রকাশক বা সংস্করণের কপির সংখ্যা জানতে চান?"
                    : "Which book, author, publisher or edition would you like the copy count for?",
                Name);
        }

        // 7. Multiple Most Borrowed Books Ranking
        if (MostBorrowedPattern().IsMatch(lower) || (isBengali && text.Contains("বেশি ধার")))
        {
            var days = ExtractDays(lower) ?? 30;
            var results = await tools.GetMostBorrowedBooksAsync(days, 5, cancellationToken);
            return new ChatAnswer(FormatMostBorrowed(results, days, isBengali), Name);
        }

        // 8. Top Borrowers Ranking
        if (TopBorrowerPattern().IsMatch(lower) || (isBengali && text.Contains("বেশি বই ধার নিয়েছে")))
        {
            var days = ExtractDays(lower) ?? 30;
            var results = await tools.GetTopBorrowersAsync(days, 5, cancellationToken);
            return new ChatAnswer(FormatTopBorrowers(results, days, isBengali), Name);
        }

        // Fallback Help Guidance
        return new ChatAnswer(
            isBengali
                ? "আমি নিম্নলিখিত ধরণের প্রশ্নের উত্তর দিতে পারি:\n" +
                  "- \"Clean Code-এর কয়টি কপি পাওয়া যাচ্ছে?\"\n" +
                  "- \"রবার্ট সি. মার্টিনের কয়টি বই আছে?\"\n" +
                  "- \"আজকে, এই সপ্তাহে, এই মাসে বা এখন পর্যন্ত কতগুলো বই ধার দেওয়া হয়েছে?\"\n" +
                  "- \"সবচেয়ে বেশি ধার নেওয়া বই কোনটি?\"\n" +
                  "- \"কম স্টকের বই বা অ্যালার্ম দেখাও (আরও বই কেনার সতর্কতা)\"\n" +
                  "- \"গত মাসে কে সবচেয়ে বেশি বই ধার নিয়েছে?\"\n" +
                  "দয়া করে আপনার প্রশ্নটি উল্লেখিত নিয়মে জিজ্ঞাসা করুন।"
                : "I can answer questions like:\n" +
                  "- \"How many copies of Clean Code are available?\"\n" +
                  "- \"How many books by Robert C. Martin are borrowed?\"\n" +
                  "- \"How many borrowed copies of the second edition of Refactoring?\"\n" +
                  "- \"What are the most borrowed books this month?\"\n" +
                  "- \"Who borrowed the most books last month?\"\n" +
                  "- \"How many books by writer Robert C. Martin do we have?\"\n" +
                  "- \"How many books of Robert Martin are on borrow today, this week, this month or till now?\"\n" +
                  "- \"Which is the best borrowed book?\"\n" +
                  "- \"Check low stock threshold alarm (buy more books)?\"\n" +
                  "Try rephrasing your question along those lines.",
            Name);
    }

    private static bool IsThresholdOrAlarmIntent(string lower, string text)
    {
        return lower.Contains("threshold")
            || lower.Contains("alarm")
            || lower.Contains("low stock")
            || lower.Contains("buy more")
            || lower.Contains("running out")
            || lower.Contains("few copies")
            || text.Contains("থ্রেশহোল্ড")
            || text.Contains("অ্যালার্ম")
            || text.Contains("সতর্কতা")
            || text.Contains("কম স্টক")
            || text.Contains("কম কপি")
            || text.Contains("আরও কেনা")
            || text.Contains("কেনা দরকার")
            || text.Contains("স্টক শেষ");
    }

    private static int? ExtractThresholdNumber(string text, string lower)
    {
        var match = Regex.Match(lower, @"(?:set\s+)?(?:threshold|limit)\s+(?:of\s+books\s+)?(?:to|is|at)?\s*(\d+)");
        if (match.Success && int.TryParse(match.Groups[1].Value, out var n))
        {
            return n;
        }

        var bnMatch = Regex.Match(text, @"থ্রেশহোল্ড\s*(?:সেট|নির্ধারণ)?\s*([০-৯\d]+)");
        if (bnMatch.Success)
        {
            var raw = bnMatch.Groups[1].Value;
            var enDigits = ConvertBanglaDigitsToAscii(raw);
            if (int.TryParse(enDigits, out var bnN)) return bnN;
        }

        return null;
    }

    private static string ConvertBanglaDigitsToAscii(string input)
    {
        var chars = input.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            if (chars[i] >= '০' && chars[i] <= '৯')
            {
                chars[i] = (char)('0' + (chars[i] - '০'));
            }
        }
        return new string(chars);
    }

    private static string FormatThresholdAlarm(
        IReadOnlyList<LowStockBookAlert> lowStockBooks, int threshold, bool isBengali, bool justUpdatedThreshold)
    {
        if (isBengali)
        {
            var thresholdBn = BengaliNumberHelper.ToBanglaDigits(threshold);
            var prefix = justUpdatedThreshold
                ? $"থ্রেশহোল্ড {thresholdBn} হিসেবে নির্ধারণ করা হয়েছে।\n"
                : "";

            if (lowStockBooks.Count == 0)
            {
                return $"{prefix}গ্রন্থাগারের সকল বই বর্তমানে নির্ধারিত থ্রেশহোল্ড ({thresholdBn}টি কপির বেশি) অনুযায়ী পর্যাপ্ত স্টকে রয়েছে। কোনো সতর্কতা অ্যালার্ম নেই।";
            }

            var lines = lowStockBooks.Select(b =>
                $"- \"{b.Title}\" (লেখক: {b.Author}): মোট {BengaliNumberHelper.ToBanglaDigits(b.TotalCopies)}টি কপি, {BengaliNumberHelper.ToBanglaDigits(b.BorrowedCopies)}টি ধার দেওয়া, গ্রন্থাগারে মাত্র {BengaliNumberHelper.ToBanglaDigits(b.AvailableCopies)}টি কপি অবশিষ্ট রয়েছে! পরামর্শ: অবিলম্বে এই বইটি আরও ক্রয় করুন।");

            return $"{prefix}🚨 অ্যালার্ম / সতর্কতা: {BengaliNumberHelper.ToBanglaDigits(lowStockBooks.Count)}টি বইয়ের স্টক নির্ধারিত থ্রেশহোল্ডের নিচে নেমে গেছে (অনূর্ধ্ব {thresholdBn}টি কপি উপলব্ধ):\n" +
                   string.Join("\n", lines);
        }

        var prefixEn = justUpdatedThreshold
            ? $"Threshold has been set to {threshold}.\n"
            : "";

        if (lowStockBooks.Count == 0)
        {
            return $"{prefixEn}All books are currently well-stocked above the threshold (more than {threshold} available cop{(threshold == 1 ? "y" : "ies")}). No low stock alarm triggered.";
        }

        var linesEn = lowStockBooks.Select(b =>
            $"- \"{b.Title}\" by {b.Author}: {b.TotalCopies} total copies, {b.BorrowedCopies} on borrow, ONLY {b.AvailableCopies} available in the library! Recommendation: Buy more copies of this book immediately.");

        return $"{prefixEn}🚨 ALARM: {lowStockBooks.Count} book(s) have reached critically low stock (<= {threshold} available cop{(threshold == 1 ? "y" : "ies")}):\n" +
               string.Join("\n", linesEn);
    }

    private static bool IsBestBorrowedIntent(string lower, string text)
    {
        return (lower.Contains("best borrowed")
            || lower.Contains("most borrowed book")
            || lower.Contains("which book is borrowed the most")
            || lower.Contains("top borrowed book")
            || lower.Contains("which is the most borrowed")
            || (lower.Contains("best") && lower.Contains("borrow"))
            || text.Contains("সেরা ধার")
            || text.Contains("সবচেয়ে বেশি ধার নেওয়া বই")
            || text.Contains("কোন বইটি সবচেয়ে বেশি ধার")
            || text.Contains("সর্বাধিক ধার নেওয়া বই"))
            && !lower.Contains("who ");
    }

    private static string FormatBestBorrowed(BookBorrowCount? bestBook, Timeframe timeframe, bool isBengali)
    {
        if (bestBook is null)
        {
            return isBengali
                ? $"{DescribeTimeframe(timeframe, true)}-এ কোনো বই ধার নেওয়া হয়নি।"
                : $"No borrows recorded {DescribeTimeframe(timeframe, false)}.";
        }

        if (isBengali)
        {
            var countBn = BengaliNumberHelper.ToBanglaDigits(bestBook.BorrowCount);
            return $"সবচেয়ে বেশি ধার নেওয়া সেরা বইটি হলো {bestBook.Author}-এর \"{bestBook.Title}\", যা {DescribeTimeframe(timeframe, true)} মোট {countBn} বার ধার নেওয়া হয়েছে।";
        }

        return $"The best borrowed book {DescribeTimeframe(timeframe, false)} is \"{bestBook.Title}\" by {bestBook.Author} with {bestBook.BorrowCount} borrow(s).";
    }

    private static bool IsTimeframeBorrowsIntent(string lower, string text)
    {
        var hasTimeframe = lower.Contains("today")
            || lower.Contains("this week")
            || lower.Contains("this month")
            || lower.Contains("till now")
            || lower.Contains("until now")
            || lower.Contains("all time")
            || text.Contains("আজকে")
            || text.Contains("এই সপ্তাহে")
            || text.Contains("এই মাসে")
            || text.Contains("এখন পর্যন্ত");

        var hasBorrow = lower.Contains("borrow") || lower.Contains("on borrow") || text.Contains("ধার");
        return hasTimeframe && hasBorrow && !IsBestBorrowedIntent(lower, text);
    }

    private static Timeframe ExtractTimeframe(string lower, string text)
    {
        if (lower.Contains("today") || text.Contains("আজকে") || text.Contains("আজ"))
            return Timeframe.Today;
        if (lower.Contains("this week") || text.Contains("এই সপ্তাহে"))
            return Timeframe.ThisWeek;
        if (lower.Contains("this month") || text.Contains("এই মাসে"))
            return Timeframe.ThisMonth;
        if (lower.Contains("till now") || lower.Contains("until now") || lower.Contains("all time") || text.Contains("এখন পর্যন্ত"))
            return Timeframe.TillNow;

        return Timeframe.ThisMonth;
    }

    private static string DescribeTimeframe(Timeframe timeframe, bool isBengali)
    {
        if (isBengali)
        {
            return timeframe switch
            {
                Timeframe.Today => "আজকে",
                Timeframe.ThisWeek => "এই সপ্তাহে",
                Timeframe.ThisMonth => "এই মাসে",
                _ => "এখন পর্যন্ত"
            };
        }

        return timeframe switch
        {
            Timeframe.Today => "today",
            Timeframe.ThisWeek => "this week",
            Timeframe.ThisMonth => "this month",
            _ => "till now"
        };
    }

    private static (string? target, BorrowTargetKind kind) ExtractBorrowTarget(string lower, string text)
    {
        var mAuth = Regex.Match(lower, @"(?:books?\s+(?:of|by|written by|authored by)\s+(?:writer|author)?\s*(?<target>[a-zA-Z\s\.\-]+?)(?:\s+(?:is|are|have|has|on|been|borrowed|today|this|till)))");
        if (mAuth.Success)
        {
            return (mAuth.Groups["target"].Value.Trim(), BorrowTargetKind.Author);
        }

        var mPub = Regex.Match(lower, @"(?:books?\s+(?:of|from|published by|publisher|publication)\s+(?<target>[a-zA-Z\s\.\-]+?)(?:\s+(?:is|are|have|has|on|been|borrowed|today|this|till)))");
        if (mPub.Success)
        {
            return (mPub.Groups["target"].Value.Trim(), BorrowTargetKind.Publisher);
        }

        var mBnAuth = Regex.Match(text, @"(?:লেখক\s+)?(?<target>[^\s]+(?:\s+[^\s]+)*?)\s*(?:-এর|এর|ের)?\s*(?:কয়টি|কতগুলো).*ধার");
        if (mBnAuth.Success)
        {
            var target = mBnAuth.Groups["target"].Value.Replace("লেখক", "").Trim();
            target = Regex.Replace(target, @"(?:\s*-\s*এর|\s+এর|ের|এর)$", "").Trim();
            return (target, BorrowTargetKind.Author);
        }

        return (null, BorrowTargetKind.All);
    }

    private static string FormatBorrowTimeframeSummary(BorrowTimeframeSummary summary, bool isBengali)
    {
        var tfText = DescribeTimeframe(summary.Timeframe, isBengali);
        var targetText = string.IsNullOrWhiteSpace(summary.Target) ? "" : $" ({summary.Target})";

        if (isBengali)
        {
            var borrowsBn = BengaliNumberHelper.ToBanglaDigits(summary.TotalBorrowsInPeriod);
            var activeBn = BengaliNumberHelper.ToBanglaDigits(summary.CurrentlyActiveBorrows);

            var headline = string.IsNullOrWhiteSpace(summary.Target)
                ? $"{tfText} লাইব্রেরিতে মোট {borrowsBn}টি বই ধার দেওয়া হয়েছে (বর্তমানে {activeBn}টি ধার চলমান)।"
                : $"{tfText} \"{summary.Target}\"-এর মোট {borrowsBn}টি বই ধার দেওয়া হয়েছে (বর্তমানে {activeBn}টি ধার চলমান)।";

            if (summary.TopBorrowedInPeriod.Count == 0)
            {
                return headline;
            }

            var topLines = summary.TopBorrowedInPeriod.Select(b =>
                $"- \"{b.Title}\" ({b.Author}): {BengaliNumberHelper.ToBanglaDigits(b.BorrowCount)} বার");

            return $"{headline}\nধার দেওয়া বইয়ের তালিকা:\n" + string.Join("\n", topLines);
        }

        var headlineEn = string.IsNullOrWhiteSpace(summary.Target)
            ? $"{summary.TotalBorrowsInPeriod} book(s) were borrowed {tfText} (with {summary.CurrentlyActiveBorrows} currently active borrow(s))."
            : $"{summary.TotalBorrowsInPeriod} book(s) of \"{summary.Target}\" were borrowed {tfText} (with {summary.CurrentlyActiveBorrows} currently active borrow(s)).";

        if (summary.TopBorrowedInPeriod.Count == 0)
        {
            return headlineEn;
        }

        var topLinesEn = summary.TopBorrowedInPeriod.Select(b =>
            $"- \"{b.Title}\" by {b.Author}: {b.BorrowCount} borrow(s)");

        return $"{headlineEn}\nBorrowed titles:\n" + string.Join("\n", topLinesEn);
    }

    private static bool IsAuthorCatalogIntent(string lower, string text, out string authorName)
    {
        authorName = "";
        var m1 = Regex.Match(lower, @"(?:how many\s+books|number of\s+books|book\s+count|books\s+by|books\s+written by|books\s+did)\s+(?:writer|author)?\s*(?<author>[a-zA-Z\s\.\-]+?)(?:\s+(?:write|author|have|do we have|in the library|exist))?$");
        if (m1.Success && !lower.Contains("borrow"))
        {
            var raw = m1.Groups["author"].Value.Trim().TrimEnd('?', '.');
            authorName = Regex.Replace(raw, @"^(?:writer|author)\s+", "", RegexOptions.IgnoreCase).Trim();
            if (!string.IsNullOrWhiteSpace(authorName)) return true;
        }

        var m2 = Regex.Match(lower, @"(?<author>[a-zA-Z\s\.\-]+?)'s\s+(?:how many\s+books|books|book count)");
        if (m2.Success && !lower.Contains("borrow"))
        {
            var raw = m2.Groups["author"].Value.Trim();
            authorName = Regex.Replace(raw, @"^(?:writer|author)\s+", "", RegexOptions.IgnoreCase).Trim();
            if (!string.IsNullOrWhiteSpace(authorName)) return true;
        }

        var mBn = Regex.Match(text, @"(?:লেখক\s+)?(?<author>[^\s]+(?:\s+[^\s]+)*?)\s*(?:-এর|এর)?\s*(?:কয়টি|কতগুলো|মোট)\s*বই");
        if (mBn.Success && !text.Contains("ধার"))
        {
            var raw = mBn.Groups["author"].Value.Replace("লেখক", "").Trim();
            raw = Regex.Replace(raw, @"(?:\s*-\s*এর|\s+এর|ের|এর)$", "").Trim();
            authorName = raw;
            if (!string.IsNullOrWhiteSpace(authorName)) return true;
        }

        return false;
    }

    private static bool IsPublisherCatalogIntent(string lower, string text, out string publisherName)
    {
        publisherName = "";
        var m1 = Regex.Match(lower, @"(?:how many\s+books|number of\s+books|book\s+count)\s+(?:by publisher|published by|from publisher|from publication)\s+(?<pub>[a-zA-Z\s\.\-]+?)(?:\s+(?:have|do we have|in the library|exist))?$");
        if (m1.Success && !lower.Contains("borrow"))
        {
            publisherName = m1.Groups["pub"].Value.Trim().TrimEnd('?', '.');
            if (!string.IsNullOrWhiteSpace(publisherName)) return true;
        }

        var mBn = Regex.Match(text, @"(?:প্রকাশনী|প্রকাশক\s+)?(?<pub>[^\s]+(?:\s+[^\s]+)*?)\s*(?:প্রকাশনীর|এর)?\s*(?:কয়টি|কতগুলো|মোট)\s*বই");
        if (mBn.Success && !text.Contains("ধার") && text.Contains("প্রকাশ"))
        {
            publisherName = mBn.Groups["pub"].Value.Trim();
            if (!string.IsNullOrWhiteSpace(publisherName)) return true;
        }

        return false;
    }

    private static string FormatAuthorSummary(AuthorBooksSummary summary, bool isBengali)
    {
        if (summary.BookCount == 0)
        {
            return isBengali
                ? $"লেখক \"{summary.Author}\"-এর কোনো বই লাইব্রেরি ক্যাটালগে পাওয়া যায়নি।"
                : $"No books by author \"{summary.Author}\" were found in the library catalog.";
        }

        if (isBengali)
        {
            var titles = summary.Books.Select(b =>
                $"- \"{b.Title}\": মোট {BengaliNumberHelper.ToBanglaDigits(b.TotalCopies)}টি কপি, {BengaliNumberHelper.ToBanglaDigits(b.AvailableCopies)}টি উপলব্ধ, {BengaliNumberHelper.ToBanglaDigits(b.BorrowedCopies)}টি ধার দেওয়া");

            return $"লেখক \"{summary.Author}\"-এর গ্রন্থাগারে {BengaliNumberHelper.ToBanglaDigits(summary.BookCount)}টি বই রয়েছে (মোট {BengaliNumberHelper.ToBanglaDigits(summary.TotalCopies)}টি কপি, {BengaliNumberHelper.ToBanglaDigits(summary.AvailableCopies)}টি উপলব্ধ, {BengaliNumberHelper.ToBanglaDigits(summary.BorrowedCopies)}টি ধার দেওয়া):\n" +
                   string.Join("\n", titles);
        }

        var titlesEn = summary.Books.Select(b =>
            $"- \"{b.Title}\": {b.TotalCopies} total, {b.AvailableCopies} available, {b.BorrowedCopies} borrowed");

        return $"Author \"{summary.Author}\" has {summary.BookCount} book(s) in the library catalog ({summary.TotalCopies} total copies, {summary.AvailableCopies} available, {summary.BorrowedCopies} borrowed):\n" +
               string.Join("\n", titlesEn);
    }

    private static string FormatPublisherSummary(PublisherBooksSummary summary, bool isBengali)
    {
        if (summary.BookCount == 0)
        {
            return isBengali
                ? $"প্রকাশনী \"{summary.Publisher}\"-এর কোনো বই লাইব্রেরিতে পাওয়া যায়নি।"
                : $"No books published by \"{summary.Publisher}\" were found in the library.";
        }

        if (isBengali)
        {
            var titles = summary.Books.Select(b =>
                $"- \"{b.Title}\" (লেখক: {b.Author}): মোট {BengaliNumberHelper.ToBanglaDigits(b.TotalCopies)}টি কপি, {BengaliNumberHelper.ToBanglaDigits(b.AvailableCopies)}টি উপলব্ধ");

            return $"প্রকাশনী \"{summary.Publisher}\"-এর গ্রন্থাগারে {BengaliNumberHelper.ToBanglaDigits(summary.BookCount)}টি বই রয়েছে (মোট {BengaliNumberHelper.ToBanglaDigits(summary.TotalCopies)}টি কপি, {BengaliNumberHelper.ToBanglaDigits(summary.AvailableCopies)}টি উপলব্ধ):\n" +
                   string.Join("\n", titles);
        }

        var titlesEn = summary.Books.Select(b =>
            $"- \"{b.Title}\" by {b.Author}: {b.TotalCopies} total, {b.AvailableCopies} available");

        return $"Publisher \"{summary.Publisher}\" has {summary.BookCount} book(s) in the library ({summary.TotalCopies} total copies, {summary.AvailableCopies} available):\n" +
               string.Join("\n", titlesEn);
    }

    private static string FormatMostBorrowed(IReadOnlyList<BookBorrowCount> results, int days, bool isBengali = false)
    {
        if (results.Count == 0)
        {
            return isBengali
                ? $"গত {BengaliNumberHelper.ToBanglaDigits(days)} দিনে কোনো বই ধার নেওয়ার রেকর্ড নেই।"
                : $"No borrows recorded in the last {days} days.";
        }

        if (isBengali)
        {
            var linesBn = results.Select((r, i) =>
                $"{BengaliNumberHelper.ToBanglaDigits(i + 1)}. \"{r.Title}\" (লেখক: {r.Author}) - {BengaliNumberHelper.ToBanglaDigits(r.BorrowCount)} বার ধার");
            return $"গত {BengaliNumberHelper.ToBanglaDigits(days)} দিনে সবচেয়ে বেশি ধার হওয়া বই:\n" + string.Join("\n", linesBn);
        }

        var lines = results.Select((r, i) => $"{i + 1}. \"{r.Title}\" by {r.Author} - {r.BorrowCount} borrow(s)");
        return $"Most borrowed in the last {days} days:\n" + string.Join("\n", lines);
    }

    private static string FormatTopBorrowers(IReadOnlyList<MemberBorrowCount> results, int days, bool isBengali = false)
    {
        if (results.Count == 0)
        {
            return isBengali
                ? $"গত {BengaliNumberHelper.ToBanglaDigits(days)} দিনে কোনো ধারকারী নেই।"
                : $"No borrows recorded in the last {days} days.";
        }

        if (isBengali)
        {
            var linesBn = results.Select((r, i) =>
                $"{BengaliNumberHelper.ToBanglaDigits(i + 1)}. {r.Name} ({r.MembershipNumber}) - {BengaliNumberHelper.ToBanglaDigits(r.BorrowCount)} বার");
            return $"গত {BengaliNumberHelper.ToBanglaDigits(days)} দিনে শীর্ষ ধারকারী:\n" + string.Join("\n", linesBn);
        }

        var lines = results.Select((r, i) => $"{i + 1}. {r.Name} ({r.MembershipNumber}) - {r.BorrowCount} borrow(s)");
        return $"Top borrowers in the last {days} days:\n" + string.Join("\n", lines);
    }

    private static int? ExtractDays(string lower)
    {
        if (lower.Contains("this week") || lower.Contains("last week")) return 7;
        if (lower.Contains("this month") || lower.Contains("last month")) return 30;
        if (lower.Contains("this year") || lower.Contains("last year")) return 365;
        var m = DaysPattern().Match(lower);
        return m.Success ? int.Parse(m.Groups[1].Value) : null;
    }

    [GeneratedRegex(@"(?:how many|available)\s+cop(?:y|ies)\s+(?:of|for)?\s*(?<title>.+)")]
    private static partial Regex CopiesOfPattern();

    [GeneratedRegex(@"most\s+(?:borrowed|popular|demand(?:ed|ing)?)")]
    private static partial Regex MostBorrowedPattern();

    [GeneratedRegex(@"(?:top\s+borrower|who\s+borrowed\s+the\s+most|most\s+active\s+member)")]
    private static partial Regex TopBorrowerPattern();

    [GeneratedRegex(@"last\s+(\d+)\s+days?")]
    private static partial Regex DaysPattern();
}
