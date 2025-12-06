using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace DynamicMongoAPI.Models
{
    public enum FieldType
    {
        String,
        Number,
        Boolean,
        Date,
        Object,
        Array
    }

    public enum RuleAction
    {
        Create,
        Update,
        Delete
    }

    public enum RelationType
    {
        OneToOne,
        ManyToOne,
        ManyToMany
    }

    public class SchemaField
    {
        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;

        [BsonElement("type")]
        [BsonRepresentation(BsonType.String)]
        public FieldType Type { get; set; }

        [BsonElement("required")]
        public bool Required { get; set; }

        [BsonElement("enum")]
        public List<string>? EnumValues { get; set; }

        [BsonElement("function")]
        public string? Function { get; set; }
    }

    public class SchemaRelation
    {
        public string Collection { get; set; } = string.Empty;
        public string ForeignField { get; set; } = "_id";
        public string As { get; set; } = string.Empty;

        public RelationType Type { get; set; } = RelationType.ManyToOne;
        public bool IsNullable { get; set; } = true;
        public object? DefaultValue { get; set; } = null;
    }

    public class VirtualField
    {
        public string Name { get; set; } = string.Empty;

        public string Expression { get; set; } = string.Empty;
    }

    public class RuleDefinition
    {
        [BsonRepresentation(BsonType.String)]
        public RuleAction Action { get; set; }

        public RuleFilter? Rule { get; set; }

        public string Message { get; set; } = "Business rule violated";
    }

    public class RuleFilter
    {
        /// <summary>
        /// JSON Query Language payload from jsonquerylang.org
        /// Example:
        /// { "eq": ["status", "active"] }
        /// </summary>
        [BsonElement("jql")]
        [BsonRepresentation(BsonType.Document)]
        public JsonNode? Jql { get; set; }
    }

    public abstract class BaseEntity
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public ObjectId Id { get; set; } = ObjectId.GenerateNewId();

        public bool IsDeleted { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public DateTime? DeletedAt { get; set; }
    }

    public class EntityHistory
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public ObjectId Id { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public ObjectId DocumentId { get; set; }

        [BsonElement("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [BsonElement("version")]
        public int Version { get; set; }

        // Flattened document snapshot
        [BsonExtraElements]
        public Dictionary<string, BsonValue> Data { get; set; } = new();
    }

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

    public class EntitySchema
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public ObjectId Id { get; set; }

        /// <summary>
        /// Unique name of the entity (ex: "user", "customer", "order")
        /// </summary>
        [BsonElement("entity")]
        public string EntityName { get; set; } = string.Empty;

        /// <summary>
        /// Namespace (helps group entities logically)
        /// </summary>
        [BsonElement("namespace")]
        public string Namespace { get; set; } = "default";

        /// <summary>
        /// User-defined data fields
        /// </summary>
        [BsonElement("fields")]
        public List<SchemaField>? Fields { get; set; }

        /// <summary>
        /// MongoDB relations ($lookup)
        /// </summary>
        [BsonElement("relations")]
        public List<SchemaRelation>? Relations { get; set; }

        /// <summary>
        /// Fields computed from expressions
        /// </summary>
        [BsonElement("virtualFields")]
        public List<VirtualField>? VirtualFields { get; set; }

        /// <summary>
        /// Business rules (create/update/delete validations)
        /// </summary>
        [BsonElement("rules")]
        public List<RuleDefinition>? Rules { get; set; }

        /// <summary>
        /// Validate schema properties before saving.
        /// Ensures naming conventions and prevents conflicts.
        /// </summary>
        public void Validate()
        {
            EntityName = EntityName.ToLowerInvariant();
            Namespace = Namespace.ToLowerInvariant();

            ValidateFields();
            ValidateRelations();
            ValidateVirtualFields();
        }

        private void ValidateFields()
        {
            if (Fields == null) return;

            var reservedNames = new HashSet<string>
            {
                "id", "_id", "isdeleted", "createdat", "updatedat", "deletedat"
            };

            foreach (var field in Fields)
            {
                field.Name = field.Name.ToLowerInvariant();

                if (!Regex.IsMatch(field.Name, @"^[a-z0-9_]+$"))
                    throw new ArgumentException(
                        $"Invalid field name '{field.Name}'. Only lowercase letters, numbers, and underscores allowed."
                    );

                if (reservedNames.Contains(field.Name))
                    throw new ArgumentException(
                        $"Field name '{field.Name}' is reserved and cannot be used."
                    );
            }
        }

        private void ValidateRelations()
        {
            if (Relations == null) return;

            foreach (var relation in Relations)
            {
                relation.Collection = relation.Collection.ToLowerInvariant();
                relation.ForeignField = relation.ForeignField.ToLowerInvariant();
                relation.As = relation.As.ToLowerInvariant();

                if (!Regex.IsMatch(relation.As, @"^[a-z0-9_]+$"))
                    throw new ArgumentException($"Invalid relation alias '{relation.As}'.");
            }
        }

        private void ValidateVirtualFields()
        {
            if (VirtualFields == null) return;

            foreach (var vf in VirtualFields)
            {
                vf.Name = vf.Name.ToLowerInvariant();

                if (!Regex.IsMatch(vf.Name, @"^[a-z0-9_]+$"))
                    throw new ArgumentException($"Invalid virtual field name '{vf.Name}'.");
            }
        }
    }

    public class NamespaceDefinition
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public ObjectId Id { get; set; }

        [BsonElement("name")]
        public string Name { get; set; } = default!;
    }

    public class EntityDefinition
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public ObjectId Id { get; set; }

        [BsonElement("name")]
        public string Name { get; set; } = default!;

        [BsonElement("namespace")]
        public string Namespace { get; set; } = default!;

        [BsonElement("id_namespace")]
        public ObjectId NamespaceId { get; set; }

        [BsonElement("id_schema")]
        public ObjectId? SchemaId { get; set; }
    }

    public class EntityResponse
    {
        public DynamicEntity Document { get; set; } = default!;
        public IList<string> Warnings { get; set; } = new List<string>();
    }
}