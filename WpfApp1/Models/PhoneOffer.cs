using System.ComponentModel.DataAnnotations;

namespace VodafoneLogin.Models
{
    public class PhoneOffer
    {
        [Key]
        public int Id { get; set; }                     // Primary Key

        [Required]
        [MaxLength(50)]
        public string PhoneNumber { get; set; } = null!;       // оригинальный номер

        [Required]
        [MaxLength(50)]
        public string PhoneNormalized { get; set; } = null!;  // нормализованный номер для уникальности

        public int DiscountPercent { get; set; } = 0;        // процентная скидка
        public decimal MinTopupAmount { get; set; } = 0;     // минимальное пополнение
        public decimal GiftAmount { get; set; } = 0;         // подарочный бонус

        public int ActiveDays { get; set; } = 0;             // срок действия оффера в днях
        public DateTime? ValidUntil { get; set; }            // действует до
        public string? FullText { get; set; }                // полный текст оффера

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;  // дата добавления
        public DateTime? UpdatedAt { get; set; }                    // дата последнего обновления

        public bool IsSynchronized { get; set; } = false;          // синхронизировано ли
        public DateTime? SyncedAt { get; set; }                     // когда синхронизировано
        public DateTime? LastSyncAttempt { get; set; }             // дата последней попытки синхронизации
        public int SyncAttempts { get; set; } = 0;                 // количество попыток синхронизации
        public string? SyncError { get; set; }                     // текст ошибки последней синхронизации

        public bool IsProcessed { get; set; } = false;             // обработан ли локально
    }
}

