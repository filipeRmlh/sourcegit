﻿using Avalonia;
using Avalonia.Data.Converters;

namespace SourceGit.Converters
{
    public static class IntConverters
    {
        public static readonly FuncValueConverter<int, bool> IsGreaterThanZero =
            new FuncValueConverter<int, bool>(v => v > 0);

        public static readonly FuncValueConverter<int, bool> IsZero =
            new FuncValueConverter<int, bool>(v => v == 0);

        public static readonly FuncValueConverter<int, bool> IsOne =
            new FuncValueConverter<int, bool>(v => v == 1);

        public static readonly FuncValueConverter<int, bool> IsNotOne =
            new FuncValueConverter<int, bool>(v => v != 1);

        public static readonly FuncValueConverter<int, bool> IsSubjectLengthBad =
            new FuncValueConverter<int, bool>(v => v > ViewModels.Preference.Instance.SubjectGuideLength);

        public static readonly FuncValueConverter<int, bool> IsSubjectLengthGood =
            new FuncValueConverter<int, bool>(v => v <= ViewModels.Preference.Instance.SubjectGuideLength);

        public static readonly FuncValueConverter<int, Thickness> ToTreeMargin =
            new FuncValueConverter<int, Thickness>(v => new Thickness(v * 16, 0, 0, 0));
    }
}
