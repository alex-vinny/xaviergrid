using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace DynamicMongoAPI.Models
{
    public class EntitySchema
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public ObjectId Id { get; set; }

        /// <summary>
        /// Unique name of the entity (ex: "user", "customer", "order")
        /// </summary>
        [BsonElement("entity")]
        [JsonPropertyName("entity")]
        public string EntityName {
            get => _entityName;
            set {
                Console.WriteLine($"[ENTITY SCHEMA] Setting EntityName from '{_entityName}' to '{value}'");
                _entityName = value;
            }
        }
        private string _entityName = string.Empty;

        /// <summary>
        /// Namespace (helps group entities logically)
        /// </summary>
        [BsonElement("namespace")]
        [JsonPropertyName("namespace")]
        public string Namespace { get; set; } = "default";

        /// <summary>
        /// User-defined data fields
        /// </summary>
        [BsonElement("fields")]
        [JsonPropertyName("fields")]
        public List<SchemaField>? Fields { get; set; }

        /// <summary>
        /// MongoDB relations ($lookup)
        /// </summary>
        [BsonElement("relations")]
        [JsonPropertyName("relations")]
        public List<SchemaRelation>? Relations { get; set; }

        /// <summary>
        /// Fields computed from expressions
        /// </summary>
        [BsonElement("virtualFields")]
        [JsonPropertyName("virtualFields")]
        public List<VirtualField>? VirtualFields { get; set; }

        /// <summary>
        /// Business rules (create/update/delete validations)
        /// </summary>
        [BsonElement("rules")]
        [JsonPropertyName("rules")]
        public List<RuleDefinition>? Rules { get; set; }

        /// <summary>
        /// Validate schema properties before saving.
        /// Ensures naming conventions and prevents conflicts.
        /// </summary>
        public void Validate()
        {
            Console.WriteLine($"[VALIDATION] Before validation - EntityName: '{EntityName}', Namespace: '{Namespace}'");
            EntityName = EntityName.ToLowerInvariant();
            Namespace = Namespace.ToLowerInvariant();
            Console.WriteLine($"[VALIDATION] After case conversion - EntityName: '{EntityName}', Namespace: '{Namespace}'");

            ValidateFields();
            ValidateRelations();
            ValidateVirtualFields();
            Console.WriteLine($"[VALIDATION] After all validations - EntityName: '{EntityName}', Namespace: '{Namespace}'");
        }

        private void ValidateFields()
        {
            Console.WriteLine($"[VALIDATION] Validating fields - Fields count: {Fields?.Count ?? 0}");
            if (Fields == null) {
                Console.WriteLine($"[VALIDATION] Fields is null");
                return;
            }

            var reservedNames = new HashSet<string>
            {
                "id", "_id", "isdeleted", "createdat", "updatedat", "deletedat"
            };

            foreach (var field in Fields)
            {
                Console.WriteLine($"[VALIDATION] Validating field - Name: '{field?.Name}', Type: '{field?.Type}'");
                if (field == null) {
                    Console.WriteLine($"[VALIDATION] Field is null");
                    continue;
                }
                
                Console.WriteLine($"[VALIDATION] Before name conversion - Name: '{field.Name}'");
                field.Name = field.Name.ToLowerInvariant();
                Console.WriteLine($"[VALIDATION] After name conversion - Name: '{field.Name}'");

                if (!Regex.IsMatch(field.Name, @"^[a-z0-9_]+$")) {
                    Console.WriteLine($"[VALIDATION] Invalid field name pattern - Name: '{field.Name}'");
                    throw new ArgumentException(
                        $"Invalid field name '{field.Name}'. Only lowercase letters, numbers, and underscores allowed."
                    );
                }

                if (reservedNames.Contains(field.Name)) {
                    Console.WriteLine($"[VALIDATION] Reserved field name - Name: '{field.Name}'");
                    throw new ArgumentException(
                        $"Field name '{field.Name}' is reserved and cannot be used."
                    );
                }
            }
        }

        private void ValidateRelations()
        {
            Console.WriteLine($"[VALIDATION] Validating relations - Relations count: {Relations?.Count ?? 0}");
            if (Relations == null) {
                Console.WriteLine($"[VALIDATION] Relations is null");
                return;
            }

            foreach (var relation in Relations)
            {
                Console.WriteLine($"[VALIDATION] Validating relation - Collection: '{relation?.Collection}', As: '{relation?.As}'");
                if (relation == null) {
                    Console.WriteLine($"[VALIDATION] Relation is null");
                    continue;
                }
                
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
}