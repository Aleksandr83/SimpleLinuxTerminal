// Copyright (c) 2022 Lukin Aleksandr
// e-mail: lukin.a.g.spb@gmail.com
using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace SimpleLinuxTerminal
{

    class IsConnectedConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            bool _value = false;
            if (value is bool)
                _value = (bool)value;
            return (_value)?"Connected" : "Disconnected";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}