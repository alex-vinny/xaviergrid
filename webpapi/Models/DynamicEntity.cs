using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace DynamicMongoAPI.Models
{
    public class DynamicEntity : IDictionary<string, BsonValue>
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public ObjectId Id { get; set; }

        public bool IsDeleted { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// All non-system fields are stored here.
        /// This keeps MongoDB serialization reliable.
        /// </summary>
        [BsonExtraElements]
        public Dictionary<string, BsonValue> DynamicFields { get; set; } = new();

        /// <summary>
        /// Runtime-only warnings; not stored in DB.
        /// Added only on GET/list/query operations.
        /// </summary>
        [BsonIgnore]
        public IList<string> Warnings { get; set; } = new List<string>();

        // Allow serializer to receive warnings into the document body if needed
        public void InjectWarnings()
        {
            if (Warnings is { Count: > 0 })
                DynamicFields["warnings"] = new BsonArray(Warnings);
        }

        // -------------------------------------------------------
        // IDictionary<string, BsonValue> Implementation
        // -------------------------------------------------------
        public BsonValue this[string key]
        {
            get => DynamicFields[key];
            set => DynamicFields[key] = value;
        }

        public ICollection<string> Keys => DynamicFields.Keys;
        public ICollection<BsonValue> Values => DynamicFields.Values;
        public int Count => DynamicFields.Count;
        public bool IsReadOnly => false;

        public void Add(string key, BsonValue value) => DynamicFields.Add(key, value);
        public bool ContainsKey(string key) => DynamicFields.ContainsKey(key);
        public bool Remove(string key) => DynamicFields.Remove(key);
        public bool TryGetValue(string key, out BsonValue value) => DynamicFields.TryGetValue(key, out value);

        public void Add(KeyValuePair<string, BsonValue> item) => Add(item.Key, item.Value);
        public void Clear() => DynamicFields.Clear();
        public bool Contains(KeyValuePair<string, BsonValue> item) => DynamicFields.Contains(item);
        public void CopyTo(KeyValuePair<string, BsonValue>[] array, int arrayIndex) =>
            ((IDictionary<string, BsonValue>)DynamicFields).CopyTo(array, arrayIndex);

        public bool Remove(KeyValuePair<string, BsonValue> item) =>
            ((IDictionary<string, BsonValue>)DynamicFields).Remove(item);

        public IEnumerator<KeyValuePair<string, BsonValue>> GetEnumerator() => DynamicFields.GetEnumerator();

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}