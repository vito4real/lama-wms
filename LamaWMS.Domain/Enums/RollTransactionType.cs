namespace LamaWMS.Domain.Enums;

public enum RollTransactionType
{
    Receipt = 0,      // Приход ткани на склад
    Consumption = 1,  // Списание ткани в производство
    Return = 2,       // Возврат ткани на склад
    WriteOff = 3,     // Списание ткани (брак, повреждение, потеря)
    Adjustment = 4    // Корректировка остатка по результатам инвентаризации
}