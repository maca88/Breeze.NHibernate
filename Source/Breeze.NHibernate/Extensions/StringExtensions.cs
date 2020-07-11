
namespace Breeze.NHibernate.Extensions
{
    internal static class StringExtensions
    {
        public static string ToLowerFirstChar(this string value)
        {
            return char.ToLower(value[0]) + value.Substring(1);
        }
    }
}
