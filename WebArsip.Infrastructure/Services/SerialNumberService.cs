using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using WebArsip.Core.Entities;
using WebArsip.Infrastructure.DbContexts;

namespace WebArsip.Infrastructure.Services
{
    public class SerialNumberService
    {
        private readonly AppDbContext _db;

        private static readonly Regex NumberTokenRx =
            new(@"\{NUMBER:(\d+)\}", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex DateTokenRx =
            new(@"\{DATE(?::([^}]+))?\}", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private const int MaxRetries = 5;

        public SerialNumberService(AppDbContext db)
        {
            _db = db;
        }

        // ==========================
        // PREVIEW (NO INCREMENT)
        // ==========================
        public async Task<string?> PreviewAsync(string key, DateTime? date = null)
        {
            var fmt = await _db.SerialNumberFormats
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Key == key && x.IsActive);

            if (fmt == null) return null;

            DateTime at = date ?? DateTime.UtcNow;

            // Ambil counter bulan
            var counter = await _db.SerialNumberMonthlyCounters
                .AsNoTracking()
                .FirstOrDefaultAsync(c =>
                    c.FormatKey == key &&
                    c.Year == at.Year &&
                    c.Month == at.Month);

            long numberToPreview =
                counter != null
                ? counter.CurrentNumber + 1
                : fmt.CurrentNumber + 1;

            return Build(fmt.Pattern, numberToPreview, at);
        }

        // ==========================
        // GENERATE (REAL)
        // ==========================
        public async Task<(bool ok, string generated, long usedNumber)>
            GenerateAsync(string key, Func<string, Task<bool>> uniqCheck, DateTime? docDate = null)
        {
            DateTime at = docDate ?? DateTime.UtcNow;

            for (int attempt = 1; attempt <= MaxRetries; attempt++)
            {
                using var tx = await _db.Database.BeginTransactionAsync();

                try
                {
                    // Lock format
                    var fmt = await _db.SerialNumberFormats
                        .FromSqlRaw(@"
                            SELECT * FROM SerialNumberFormats
                            WITH (UPDLOCK, HOLDLOCK, ROWLOCK)
                            WHERE [Key] = {0} AND IsActive = 1", key)
                        .FirstOrDefaultAsync();

                    if (fmt == null)
                    {
                        await tx.RollbackAsync();
                        return (false, "", 0);
                    }

                    // Lock counter bulan
                    var counter = await _db.SerialNumberMonthlyCounters
                        .FromSqlRaw(@"
                            SELECT * FROM SerialNumberMonthlyCounters
                            WITH (UPDLOCK, HOLDLOCK, ROWLOCK)
                            WHERE FormatKey = {0} AND [Year] = {1} AND [Month] = {2}",
                            key, at.Year, at.Month)
                        .FirstOrDefaultAsync();

                    if (counter == null)
                    {
                        counter = new SerialNumberMonthlyCounter
                        {
                            FormatKey = key,
                            Year = at.Year,
                            Month = at.Month,
                            CurrentNumber = 1
                        };

                        _db.SerialNumberMonthlyCounters.Add(counter);
                    }
                    else
                    {
                        counter.CurrentNumber++;
                    }

                    long currentNum = counter.CurrentNumber;

                    // Build serial
                    string serial = Build(fmt.Pattern, currentNum, at);

                    // Check unique
                    if (!await uniqCheck(serial))
                    {
                        counter.CurrentNumber++;
                        await _db.SaveChangesAsync();
                        await tx.CommitAsync();
                        return await GenerateAsync(key, uniqCheck, at);
                    }

                    // Sync format
                    fmt.CurrentNumber = currentNum;

                    await _db.SaveChangesAsync();
                    await tx.CommitAsync();

                    return (true, serial, currentNum);
                }
                catch
                {
                    await tx.RollbackAsync();
                    if (attempt == MaxRetries) throw;
                    await Task.Delay(100 * attempt);
                }
            }

            return (false, "", 0);
        }

        // ==========================
        // BUILD PATTERN
        // ==========================
        private static string Build(string pattern, long number, DateTime at)
        {
            string withDate = DateTokenRx.Replace(pattern, m =>
            {
                string? fmt = m.Groups[1].Value;

                if (string.IsNullOrWhiteSpace(fmt))
                    return $"{Roman(at.Month)}-{at.Year}";

                return at.ToString(fmt);
            });

            string final = NumberTokenRx.Replace(withDate, m =>
            {
                int pad = int.Parse(m.Groups[1].Value);
                return number.ToString().PadLeft(pad, '0');
            });

            return final;
        }

        private static string Roman(int month) => month switch
        {
            1 => "I",
            2 => "II",
            3 => "III",
            4 => "IV",
            5 => "V",
            6 => "VI",
            7 => "VII",
            8 => "VIII",
            9 => "IX",
            10 => "X",
            11 => "XI",
            12 => "XII",
            _ => ""
        };
    }
}
