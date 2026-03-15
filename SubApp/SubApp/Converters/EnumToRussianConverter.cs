using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace SubApp.Converters;

public class EnumToRussianConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            "monthly" or Models.BillingCycle.monthly => "Ежемесячно",
            "quarterly" or Models.BillingCycle.quarterly => "Ежеквартально",
            "yearly" or Models.BillingCycle.yearly => "Ежегодно",
            "weekly" or Models.BillingCycle.weekly => "Еженедельно",
            "custom" or Models.BillingCycle.custom => "Свой период",

            "RUB" or Models.Currency.RUB => "₽ Рубль",
            "USD" or Models.Currency.USD => "$ Доллар",
            "EUR" or Models.Currency.EUR => "€ Евро",
            
            "active" or Models.SubscriptionStatus.active => "Активный",
            "paused" or Models.SubscriptionStatus.paused => "Остановленный",
            "cancelled" or Models.SubscriptionStatus.cancelled => "Отмененный",
            "expired" or Models.SubscriptionStatus.expired => "Истекший",
            "trial" or Models.SubscriptionStatus.trial => "Пробный период",

            _ => value?.ToString()
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;
}