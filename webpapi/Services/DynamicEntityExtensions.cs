using DynamicMongoAPI.Models;
using DynamicMongoAPI.Utils;
using MongoDB.Bson;

namespace DynamicMongoAPI.Services
{
    public static class DynamicEntityExtensions
    {
        /// <summary>
        /// Convert a DynamicEntity to a BsonDocument, including system fields and dynamic fields.
        /// </summary>
        public static BsonDocument ToBsonDocument(this DynamicEntity entity)
        {
            var doc = new BsonDocument
            {
                ["_id"] = entity.Id != ObjectId.Empty ? entity.Id : ObjectId.GenerateNewId(),
                ["isDeleted"] = entity.IsDeleted,
                ["createdAt"] = entity.CreatedAt != default ? entity.CreatedAt : DateTime.UtcNow,
                ["updatedAt"] = entity.UpdatedAt ?? DateTime.UtcNow
            };

            if (entity.DynamicFields != null)
            {
                foreach (var kv in entity.DynamicFields)
                {
                    doc[kv.Key] = kv.Value ?? BsonNull.Value;
                }
            }

            return doc;
        }

        /// <summary>
        /// Create a DynamicEntity from a BsonDocument
        /// </summary>
        public static DynamicEntity FromBsonDocument(BsonDocument doc)
        {
            if (doc == null)
                throw new ArgumentNullException(nameof(doc));

            var entity = new DynamicEntity
            {
                Id = doc.GetValue("_id", ObjectId.GenerateNewId()).AsObjectId,
                IsDeleted = doc.GetValue("isDeleted", false).AsBoolean,
                CreatedAt = doc.Contains("createdAt") ? doc["createdAt"].ToUniversalTime() : DateTime.UtcNow,
                UpdatedAt = doc.Contains("updatedAt") ? doc["updatedAt"].ToUniversalTime() : (DateTime?)null,
                DynamicFields = new Dictionary<string, BsonValue>()
            };

            foreach (var element in doc.Elements)
            {
                if (element.Name == "_id" || element.Name == "isDeleted" || element.Name == "createdAt" || element.Name == "updatedAt")
                    continue;

                entity.DynamicFields[element.Name] = element.Value;
            }

            return entity;
        }

        /// <summary>
        /// Convert a DynamicEntity to a dictionary for JSON serialization (nested conversion handled)
        /// </summary>
        public static Dictionary<string, object> ToDictionary(this DynamicEntity entity)
        {
            var doc = entity.ToBsonDocument();
            return BsonConverter.BsonToDictionary(doc);
        }

        /// <summary>
        /// Create a DynamicEntity from a dictionary (inverse of ToDictionary)
        /// </summary>
        public static DynamicEntity FromDictionary(Dictionary<string, object> dict)
        {
            var doc = new BsonDocument();
            foreach (var kv in dict)
            {
                doc[kv.Key] = BsonValue.Create(kv.Value ?? BsonNull.Value);
            }

            return FromBsonDocument(doc);
        }

        /// <summary>
        /// Convert a DynamicEntity to a dictionary of BsonValues (keeps Bson types, safe for history)
        /// </summary>
        public static Dictionary<string, BsonValue> ToBsonDictionary(this DynamicEntity entity)
        {
            var dict = new Dictionary<string, BsonValue>
            {
                ["_id"] = entity.Id != ObjectId.Empty ? entity.Id : ObjectId.GenerateNewId(),
                ["isDeleted"] = entity.IsDeleted,
                ["createdAt"] = entity.CreatedAt != default ? entity.CreatedAt : DateTime.UtcNow,
                ["updatedAt"] = entity.UpdatedAt ?? DateTime.UtcNow
            };

            if (entity.DynamicFields != null)
            {
                foreach (var kv in entity.DynamicFields)
                {
                    dict[kv.Key] = kv.Value ?? BsonNull.Value;
                }
            }

            return dict;
        }

        /// <summary>
        /// Create a DynamicEntity from a dictionary of BsonValues (used by HistoryService)
        /// </summary>
        public static DynamicEntity FromBsonDictionary(Dictionary<string, BsonValue> dict)
        {
            var doc = new BsonDocument();

            foreach (var kv in dict)
            {
                doc[kv.Key] = kv.Value ?? BsonNull.Value;
            }

            return FromBsonDocument(doc);
        }
    }
}