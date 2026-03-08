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
            "Monthly" or Models.BillingCycle.Monthly => "Ежемесячно",
            "Quarterly" or Models.BillingCycle.Quarterly => "Ежеквартально",
            "Yearly" or Models.BillingCycle.Yearly => "Ежегодно",
            "Weekly" or Models.BillingCycle.Weekly => "Еженедельно",
            "Custom" or Models.BillingCycle.Custom => "Свой период",

            "RUB" or Models.Currency.RUB => "₽ Рубль",
            "USD" or Models.Currency.USD => "$ Доллар",
            "EUR" or Models.Currency.EUR => "€ Евро",
            
            "Active" or Models.SubscriptionStatus.Active => "Активный",
            "Paused" or Models.SubscriptionStatus.Paused => "Остановленный",
            "Cancelled" or Models.SubscriptionStatus.Cancelled => "Отмененный",
            "Expired" or Models.SubscriptionStatus.Expired => "Истекший",
            "Trial" or Models.SubscriptionStatus.Trial => "Пробный период",

            _ => value?.ToString()
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;
}