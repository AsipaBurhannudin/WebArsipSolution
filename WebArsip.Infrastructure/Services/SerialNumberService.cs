using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using WebArsip.Core.Entities;
using WebArsip.Infrastructure.DbContexts;

namespace WebArsip.Infrastructure.Services
{
    public class SerialNumberService
    {
        private readonly AppDbContext _db;

        private static readonly Regex NumberTokenRx = new(@"\{NUMBER:(\d+)\}", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex DateTokenRx = new(@"\{DATE(?::([^}]+))?\}", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private const int MaxRetries = 5;

        public SerialNumberService(AppDbContext db)
        {
            _db = db;
        }

        // PREVIEW
        public async Task<string?> PreviewAsync(string key, DateTime? date = null)
        {
            var fmt = await _db.SerialNumberFormats.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Key == key && x.IsActive);

            if (fmt == null) return null;

            return Build(fmt.Pattern, fmt.CurrentNumber, date ?? DateTime.UtcNow);
        }

        // BULLETPROOF GENERATOR
        public async Task<(bool ok, string title, long usedNumber)> GenerateAsync(
            string key,
            Func<string, Task<bool>> uniquenessCheck)
        {
            for (int attempt = 1; attempt <= MaxRetries; attempt++)
            {
                using var tx = await _db.Database.BeginTransactionAsync();

                try
                {
                    // STEP 1 — Lock row of counter only
                    var fmt = await _db.SerialNumberFormats
                        .FromSqlRaw(@"SELECT * FROM SerialNumberFormats 
                                      WITH (UPDLOCK, HOLDLOCK, ROWLOCK)
                                      WHERE [Key] = {0}", key)
                        .FirstOrDefaultAsync();

                    if (fmt == null)
                    {
                        await tx.RollbackAsync();
                        return (false, "", 0);
                    }

                    long number = fmt.CurrentNumber;

                    for (int guard = 0; guard < 10000; guard++)
                    {
                        string candidate = Build(fmt.Pattern, number, DateTime.UtcNow);

                        bool unique = await uniquenessCheck(candidate);
                        if (unique)
                        {
                            fmt.CurrentNumber = number + 1;
                            _db.Update(fmt);

                            try
                            {
                                await _db.SaveChangesAsync();
                                await tx.CommitAsync();
                                return (true, candidate, number);
                            }
                            catch (DbUpdateException ex)
                            {
                                // UNIQUE INDEX violation — continue next number
                                if (ex.InnerException?.Message.Contains("duplicate") == true)
                                {
                                    number++;
                                    continue;
                                }

                                throw;
                            }
                        }

                        number++;
                    }

                    await tx.RollbackAsync();
                    return (false, "", 0);
                }
                catch (Exception)
                {
                    await tx.RollbackAsync();

                    if (attempt == MaxRetries) throw;

                    await Task.Delay(120 * attempt);
                }
            }

            return (false, "", 0);
        }

        private static string Build(string pattern, long number, DateTime at)
        {
            var withDate = DateTokenRx.Replace(pattern, m =>
            {
                var fmt = m.Groups[1].Success ? m.Groups[1].Value : "dd-MM-yyyy";
                return at.ToString(fmt);
            });

            return NumberTokenRx.Replace(withDate, m =>
            {
                int pad = int.Parse(m.Groups[1].Value);
                return number.ToString().PadLeft(pad, '0');
            });
        }
    }
}