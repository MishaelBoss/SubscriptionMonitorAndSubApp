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
            "Monthly" or Models.BillingCycle.monthly => "Ежемесячно",
            "Quarterly" or Models.BillingCycle.quarterly => "Ежеквартально",
            "Yearly" or Models.BillingCycle.yearly => "Ежегодно",
            "Weekly" or Models.BillingCycle.weekly => "Еженедельно",
            "Custom" or Models.BillingCycle.custom => "Свой период",

            "RUB" or Models.Currency.RUB => "₽ Рубль",
            "USD" or Models.Currency.USD => "$ Доллар",
            "EUR" or Models.Currency.EUR => "€ Евро",
            
            "Active" or Models.SubscriptionStatus.active => "Активный",
            "Paused" or Models.SubscriptionStatus.paused => "Остановленный",
            "Cancelled" or Models.SubscriptionStatus.cancelled => "Отмененный",
            "Expired" or Models.SubscriptionStatus.expired => "Истекший",
            "Trial" or Models.SubscriptionStatus.trial => "Пробный период",

            _ => value?.ToString()
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;
}