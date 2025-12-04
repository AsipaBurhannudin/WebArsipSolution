using System.ComponentModel.DataAnnotations;

namespace WebArsip.Core.Entities
{
    public class SerialNumberFormat
    {
        [Key]
        public int Id { get; set; }

        // Nama yang tampil di admin, contoh "Surat Tugas"
        public string Name { get; set; } = string.Empty;

        // Pre-defined type key (unique) e.g. "SURAT_TUGAS", "SURAT_RCA"
        public string Key { get; set; } = string.Empty;

        // Pattern, e.g. "HO-PCJLM/SK/{DATE}/{NUMBER:3}"
        public string Pattern { get; set; } = string.Empty;

        // Current number counter
        public long CurrentNumber { get; set; } = 1;

        // apakah aktif
        public bool IsActive { get; set; } = true;

        // optional: keterangan
        public string? Note { get; set; }
    }
}