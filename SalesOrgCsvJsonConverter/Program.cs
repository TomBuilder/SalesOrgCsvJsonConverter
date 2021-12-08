using System.Globalization;
using System.Text.Json;

if (args.Length < 2)
{
   Console.WriteLine("Call: SalesOrgCsvJsonConverter.exe inputfile.csv outputfile.json");
   return;
}

if (!File.Exists(args[0]))
{
   Console.WriteLine($"File {args[0]} missing");
   return;
}

var csvLines = File.ReadAllLines(args[0]);
var colNames = csvLines[0].Split(',');

var weeksList = colNames[4..];
Root? root = null;

for (var line = 1; line < csvLines.Length; line++)
{
   var values = csvLines[line].Split('\u002C');

   root ??= new Root
   {
      SalesOrg = "##SalesOrg##",
      Type = values[2]
   };

   var year = Convert.ToInt32(values[3][(values[3].LastIndexOf('/') + 1)..]);
   var accountId = values[0];
   var productId = values[1];
   for (var i = 0; i < weeksList.Length; i++)
   {
      var startDate = FirstDateOfWeekISO8601(year, Convert.ToInt32(weeksList[i][4..])).ToString("yyyy-MM-dd");
      if (!startDate.StartsWith(year.ToString()))
      { // week is in next year
         continue;
      }
      var volume = root.Volumes.FirstOrDefault(v => v.StartDate == startDate);

      if (volume == null)
      {
         volume = new Volume
         {
            StartDate = startDate
         };
         root.Volumes.Add(volume);
      }
      var row = new Row
      {
         Prd = productId,
         Acc = accountId,
         Value = Convert.ToDouble(values[i + 4], CultureInfo.InvariantCulture)
      };
      volume.Rows.Add(row);
   }

   root.Volumes = root.Volumes.OrderBy(v => v.StartDate).ToList();
}

var options = new JsonSerializerOptions
{
   WriteIndented = true
};
string jsonString = JsonSerializer.Serialize(root, options);
File.WriteAllText(args[1], jsonString);


//Quelle: https://stackoverflow.com/questions/662379/calculate-date-from-week-number
static DateTime FirstDateOfWeekISO8601(int year, int weekOfYear)
{
   DateTime jan1 = new DateTime(year, 1, 1);
   int daysOffset = DayOfWeek.Thursday - jan1.DayOfWeek;

   // Use first Thursday in January to get first week of the year as
   // it will never be in Week 52/53
   DateTime firstThursday = jan1.AddDays(daysOffset);
   var cal = CultureInfo.CurrentCulture.Calendar;
   int firstWeek = cal.GetWeekOfYear(firstThursday, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);

   var weekNum = weekOfYear;
   // As we're adding days to a date in Week 1,
   // we need to subtract 1 in order to get the right date for week #1
   if (firstWeek == 1)
   {
      weekNum -= 1;
   }

   // Using the first Thursday as starting week ensures that we are starting in the right year
   // then we add number of weeks multiplied with days
   var result = firstThursday.AddDays(weekNum * 7);

   // Subtract 3 days from Thursday to get Monday, which is the first weekday in ISO8601
   return result.AddDays(-3);
}

public class Row
{
   public string? Prd { get; set; }
   public string? Acc { get; set; }
   public double Value { get; set; }
}

public class Volume
{
   public string? StartDate { get; set; }
   public List<Row> Rows { get; set; } = new();

}

public class Root
{
   public string? Type { get; set; }
   public string? SalesOrg { get; set; }
   public List<Volume> Volumes { get; set; } = new();
}

