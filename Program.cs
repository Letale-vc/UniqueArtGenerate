using AngleSharp;
using System.Collections.Concurrent;
using System.Text;

Console.OutputEncoding = Encoding.UTF8;

var scraper = new PoeDbScraper();
await scraper.ScrapeUniquesAsync();

public class PoeDbScraper
{
    private const string BaseUrl = "https://poedb.tw";
    private const int MaxConcurrentRequests = 50; // Number of concurrent requests
    private readonly HttpClient _httpClient;
    private readonly IBrowsingContext _browsingContext;
    private readonly ConcurrentDictionary<string, UniqueItem> _uniqueItems;
    private int _processedCount = 0;
    private int _successCount = 0;
    private int _failedCount = 0;
    private int _totalCount = 0;
    private readonly object _consoleLock = new object();

    public PoeDbScraper()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

        var config = Configuration.Default;
        _browsingContext = BrowsingContext.New(config);

        _uniqueItems = new ConcurrentDictionary<string, UniqueItem>();
    }

    public async Task ScrapeUniquesAsync()
    {
        Console.WriteLine("Starting to scrape unique items from poedb.tw...");
        Console.WriteLine($"Max concurrent requests: {MaxConcurrentRequests}\n");

        await ScrapeUniqueListPageAsync("/us/Unique_item");

        Console.WriteLine($"\n\n=== SUMMARY ===");
        Console.WriteLine($"Total items processed: {_processedCount}");
        Console.WriteLine($"Successful: {_successCount}");
        Console.WriteLine($"Failed: {_failedCount}");
        Console.WriteLine($"Unique items saved: {_uniqueItems.Count}");
        Console.WriteLine("\nWriting to output file...");

        await WriteOutputAsync("unique_items_output.txt");

        Console.WriteLine($"\nDone! Results saved to: unique_items_output.txt");
    }

    private async Task ScrapeUniqueListPageAsync(string pageUrl)
    {
        try
        {
            var url = $"{BaseUrl}{pageUrl}";
            Console.WriteLine($"Fetching page: {url}");

            var html = await _httpClient.GetStringAsync(url);
            var document = await _browsingContext.OpenAsync(req => req.Content(html));

            // Find all <a> elements with class "uniqueitem" on the entire page
            var uniqueItemElements = document.QuerySelectorAll("a.uniqueitem");

            if (uniqueItemElements == null || !uniqueItemElements.Any())
            {
                Console.WriteLine("No unique items found on page");
                return;
            }

            Console.WriteLine($"Found {uniqueItemElements.Length} total <a> elements with class 'uniqueitem'");

            // Collect all items to process (remove duplicates by href)
            var itemsToProcess = new Dictionary<string, string>(); // href -> name

            foreach (var itemElement in uniqueItemElements)
            {
                var nameElement = itemElement.QuerySelector("span.uniqueName");
                if (nameElement == null)
                    continue;

                var itemName = nameElement.TextContent.Trim();
                var itemHref = itemElement.GetAttribute("href");

                if (string.IsNullOrEmpty(itemHref))
                    continue;

                // Filter: only process hrefs that start with /us/ and don't contain # or ?
                if (!itemHref.StartsWith("/us/") || itemHref.Contains("#") || itemHref.Contains("?"))
                    continue;

                // Filter: skip if href is just /us/Unique_item
                if (itemHref == "/us/Unique_item")
                    continue;

                // Add only if this href hasn't been seen yet
                if (!itemsToProcess.ContainsKey(itemHref))
                {
                    itemsToProcess[itemHref] = itemName;
                }
            }

            _totalCount = itemsToProcess.Count;
            _processedCount = 0;
            _successCount = 0;
            _failedCount = 0;

            Console.WriteLine($"After filtering and deduplication: {_totalCount} unique items to process");
            Console.WriteLine($"Starting parallel processing...\n");

            // Process all items in parallel
            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = MaxConcurrentRequests
            };

            var startTime = DateTime.Now;

            await Parallel.ForEachAsync(itemsToProcess, parallelOptions, async (item, ct) =>
            {
                await ScrapeUniqueItemAsync(item.Key, item.Value);

                // Show progress every 50 items
                if (_processedCount % 50 == 0)
                {
                    var elapsed = DateTime.Now - startTime;
                    lock (_consoleLock)
                    {
                        Console.WriteLine($"\n>>> Progress: {_processedCount}/{_totalCount} ({_successCount} success, {_failedCount} failed) - Elapsed: {elapsed.TotalSeconds:F1}s\n");
                    }
                }
            });

            var totalElapsed = DateTime.Now - startTime;
            Console.WriteLine($"\nCompleted in {totalElapsed.TotalSeconds:F1} seconds");
            Console.WriteLine($"Success: {_successCount}, Failed: {_failedCount}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR scraping page {pageUrl}: {ex.Message}");
        }
    }

    private async Task ScrapeUniqueItemAsync(string itemHref, string itemName)
    {
        var processed = 0;
        try
        {
            // Use the href directly from the HTML
            var url = itemHref.StartsWith("http") ? itemHref : $"{BaseUrl}{itemHref}";

            var html = await _httpClient.GetStringAsync(url);
            var document = await _browsingContext.OpenAsync(req => req.Content(html));

            // Look for the Icon row in the table - find td with text "Icon" and get the next sibling
            var allTableCells = document.QuerySelectorAll("td");
            string? iconPath = null;

            foreach (var cell in allTableCells)
            {
                if (cell.TextContent.Trim() == "Icon")
                {
                    var nextSibling = cell.NextElementSibling;
                    if (nextSibling != null && !string.IsNullOrWhiteSpace(nextSibling.TextContent))
                    {
                        iconPath = nextSibling.TextContent.Trim();
                        break;
                    }
                }
            }

            if (!string.IsNullOrEmpty(iconPath))
            {
                var uniqueItem = new UniqueItem
                {
                    Name = itemName,
                    IconPath = iconPath
                };

                // Use TryAdd to avoid duplicates
                if (_uniqueItems.TryAdd(itemName, uniqueItem))
                {
                    processed = Interlocked.Increment(ref _processedCount);
                    Interlocked.Increment(ref _successCount);

                    lock (_consoleLock)
                    {
                        Console.WriteLine($"[{processed}/{_totalCount}] ✓ {itemName}");
                    }
                }
                else
                {
                    processed = Interlocked.Increment(ref _processedCount);
                }
            }
            else
            {
                processed = Interlocked.Increment(ref _processedCount);
                Interlocked.Increment(ref _failedCount);

                lock (_consoleLock)
                {
                    Console.WriteLine($"[{processed}/{_totalCount}] ✗ {itemName} -> Icon not found");
                }
            }
        }
        catch (TaskCanceledException)
        {
            processed = Interlocked.Increment(ref _processedCount);
            Interlocked.Increment(ref _failedCount);

            lock (_consoleLock)
            {
                Console.WriteLine($"[{processed}/{_totalCount}] ⏱ {itemName} -> Timeout");
            }
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            processed = Interlocked.Increment(ref _processedCount);
            Interlocked.Increment(ref _failedCount);

            lock (_consoleLock)
            {
                Console.WriteLine($"[{processed}/{_totalCount}] ✗ {itemName} -> 404");
            }
        }
        catch (Exception ex)
        {
            processed = Interlocked.Increment(ref _processedCount);
            Interlocked.Increment(ref _failedCount);

            lock (_consoleLock)
            {
                Console.WriteLine($"[{processed}/{_totalCount}] ✗ {itemName} -> {ex.Message}");
            }
        }
    }

    private async Task WriteOutputAsync(string filePath)
    {
        // Use semicolon as delimiter for easy parsing
        var lines = _uniqueItems.Values
            .OrderBy(item => item.Name)
            .Select(item => $"{item.Name};{item.IconPath}");

        await File.WriteAllLinesAsync(filePath, lines, Encoding.UTF8);

        Console.WriteLine($"\nSuccessfully saved {_uniqueItems.Count} unique items");
        Console.WriteLine($"Format: ItemName;IconPath");
    }
}

public class UniqueItem
{
    public string Name { get; set; } = string.Empty;
    public string IconPath { get; set; } = string.Empty;
}
