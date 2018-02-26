using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace Bleep
{
  public class Students
  {
    private TimeSpan _cacheTime = TimeSpan.FromDays(1);
    private IMemoryCache _cache { get; set; }
    private Spreadsheet _spreadsheet { get; set; }

    public Students(string sheetId, string contentRootPath, IMemoryCache cache)
    {
      _spreadsheet = new Spreadsheet(sheetId, Path.Combine(contentRootPath, "key.json"));
      _cache = cache;
    }

    public async Task<IList<string>> GetAsync()
    {
      if (!_cache.TryGetValue(nameof(Students), out IList<string> data))
      {
        var sheets = await _spreadsheet.GetSheetsAsync(nameof(Students));
        data = sheets[0].Values.Skip(1).Select(o => $"{o[0].ToString().ToTitleCase()} {o[1].ToString().ToUpperInvariant()}, {o[2]}").ToList();
        _cache.Set(nameof(Students), data, _cacheTime);
      }
      return data;
    }

  }
}