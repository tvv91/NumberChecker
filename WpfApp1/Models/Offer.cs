namespace VodafoneLogin.Models
{
    public class Offer
    {
        public int Discount { get; set; }        // скидка %, 0 если нет
        public decimal MinTopUp { get; set; }    // минимальное пополнение, 0 если нет
        public decimal Gift { get; set; }        // сумма подарка, 0 если нет
        public int ActiveDays { get; set; }      // срок действия подарка, 0 если нет
        public DateTime? ValidUntil { get; set; } // дата окончания
    }
}

