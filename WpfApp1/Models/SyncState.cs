using System.ComponentModel.DataAnnotations;

namespace VodafoneLogin.Models
{
    public class SyncState
    {
        [Key]
        public int Id { get; set; } = 1;                // всегда 1, единственная запись
        public int? LastProcessedPhoneId { get; set; }  // Id последнего обработанного номера
    }
}

