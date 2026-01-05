using System.ComponentModel.DataAnnotations;

namespace VodafoneNumberChecker.Models
{
    public class PhoneOffer
    {
        [Key]
        public int Id { get; set; }                     // Primary Key

        [Required]
        [MaxLength(50)]
        public string PhoneNumber { get; set; } = null!;       // оригинальный номер

        public int DiscountPercent { get; set; } = 0;        // процентная скидка
        public decimal MinTopupAmount { get; set; } = 0;     // минимальное пополнение
        public decimal GiftAmount { get; set; } = 0;         // подарочный бонус

        public int ActiveDays { get; set; } = 0;             // срок действия оффера в днях
        public DateTime? ValidUntil { get; set; }            // действует до

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;  // дата добавления
        public DateTime? UpdatedAt { get; set; }                    // дата последнего обновления

        public bool IsProcessed { get; set; } = false;             // обработан ли локально
        public bool IsError { get; set; } = false;                 // есть ли ошибка при обработке
        public string? ErrorDescription { get; set; }              // описание ошибки
        public int IterationCount { get; set; } = 0;                // количество итераций обработки
        public bool IsPropositionsNotFound { get; set; } = false;  // предложений не найдено (текст "Пропозицій не знайдено" найден)
        public bool IsPropositionsNotSuitable { get; set; } = false; // предложения не подходят (нет "Пропозицій не знайдено", но нет процента/подарка)
    }
}

