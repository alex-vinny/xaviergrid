using MongoDB.Bson;

namespace DynamicMongoAPI.Services
{
    public static class BsonDocumentExtensions
    {
        public static bool DocumentMatches(this BsonDocument filter, BsonDocument doc)
        {
            // Simple equality-only matcher
            foreach (var elem in filter.Elements)
            {
                if (!doc.Contains(elem.Name)) return false;

                if (!doc[elem.Name].Equals(elem.Value)) return false;
            }
            return true;
        }
    }
}