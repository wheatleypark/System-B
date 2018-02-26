﻿using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bleep
{
  public class Spreadsheet
  {
    public string SpreadsheetId { get; private set; }
    public SpreadsheetsResource Resource { get; set; }

    protected const string DIMENSION_ROWS = "ROWS";

    public Spreadsheet(string spreadsheetId, string keyPath)
    {
      SpreadsheetId = spreadsheetId;

      var credential = GoogleCredential.FromFile(keyPath).CreateScoped(SheetsService.Scope.Spreadsheets);

      var service = new SheetsService(new BaseClientService.Initializer()
      {
        HttpClientInitializer = credential,
        ApplicationName = "Bleep"
      });

      Resource = service.Spreadsheets;
    }


    public async Task<IList<ValueRange>> GetSheetsAsync(params string[] sheetNames)
    {
      var request = Resource.Values.BatchGet(SpreadsheetId);
      request.Ranges = sheetNames;
      var response = await Execute(request);
      return response.ValueRanges;
    }


    public async Task<TResponse> Execute<TResponse>(SheetsBaseServiceRequest<TResponse> request)
    {
      await Task.Delay(1000);

      for (var attempt = 1; attempt <= 5; attempt++)
      {
        try
        {
          return await request.ExecuteAsync();
        }
        catch when (attempt < 5)
        {
          await Task.Delay(2000 * attempt);
        }
      }
      return default(TResponse);
    }
  }
}