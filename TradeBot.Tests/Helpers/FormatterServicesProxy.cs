using System;
using System.Runtime.Serialization;

namespace TradeBot.Tests.Helpers
{
    internal static class FormatterServicesProxy
    {
        public static object GetUninitializedObject(Type type) =>
            FormatterServices.GetUninitializedObject(type);
    }
}
