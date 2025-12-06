using MongoDB.Bson;

namespace DynamicMongoAPI.Services
{
    public static class BsonValueExtensions
    {
        public static bool IsValidDateTime(this BsonValue value)
        {
            if (value == null || value.IsBsonNull) return false;
            return value.IsValidDateTime || value.IsString && DateTime.TryParse(value.AsString, out _);
        }

        public static DateTime ToUniversalTime(this BsonValue value)
        {
            if (value.IsValidDateTime) return value.ToUniversalTime();
            if (value.IsString && DateTime.TryParse(value.AsString, out var dt)) return dt.ToUniversalTime();
            return DateTime.UtcNow;
        }
    }
}