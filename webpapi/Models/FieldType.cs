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
}