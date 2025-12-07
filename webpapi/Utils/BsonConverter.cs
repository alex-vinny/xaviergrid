using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using System.Text.Json;

namespace DynamicMongoAPI.Utils
{
    public static class BsonConverter
    {
        public static ObjectId ToObjectId(string id) =>
            ObjectId.TryParse(id, out var oid) ? oid : ObjectId.Empty;

        // Helper method to convert JsonElement to BsonDocument
        public static BsonDocument JsonToBson(JsonElement json)
        {
            var jsonString = json.GetRawText();
            return BsonSerializer.Deserialize<BsonDocument>(jsonString);
        }
        
        // Helper method to convert JsonElement array to BsonArray
        public static BsonArray JsonToBsonArray(JsonElement json)
        {
            var jsonString = json.GetRawText();
            return BsonSerializer.Deserialize<BsonArray>(jsonString);
        }
        
        // Helper method to convert BsonDocument to Dictionary for proper JSON serialization
        public static Dictionary<string, object> BsonToDictionary(BsonDocument doc)
        {
            var dict = new Dictionary<string, object>();
            foreach (var element in doc)
            {
                dict[element.Name] = BsonValueToNative(element.Value);
            }
            return dict;
        }
        
        // Helper method to convert BsonValue to native types
        public static object BsonValueToNative(BsonValue value)
        {
            switch (value.BsonType)
            {
                case BsonType.ObjectId:
                    return value.AsObjectId.ToString();
                case BsonType.String:
                    return value.AsString;
                case BsonType.Boolean:
                    return value.AsBoolean;
                case BsonType.DateTime:
                    return value.ToUniversalTime();
                case BsonType.Double:
                    return value.AsDouble;
                case BsonType.Int32:
                    return value.AsInt32;
                case BsonType.Int64:
                    return value.AsInt64;
                case BsonType.Decimal128:
                    return value.AsDecimal;
                case BsonType.Array:
                    var array = value.AsBsonArray;
                    var list = new List<object>();
                    foreach (var item in array)
                    {
                        list.Add(BsonValueToNative(item));
                    }
                    return list;
                case BsonType.Document:
                    return BsonToDictionary(value.AsBsonDocument);
                default:
                    return value?.ToString() ?? string.Empty;
            }
        }
        
        // Check if the entity name is a reserved route
        public static bool IsReservedRoute(string entity)
        {
            var reservedRoutes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "swagger",
                "system",
                "health",
                "api"
            };
            
            return reservedRoutes.Contains(entity);
        }
    }
}