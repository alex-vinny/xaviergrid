using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Nodes;
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
}