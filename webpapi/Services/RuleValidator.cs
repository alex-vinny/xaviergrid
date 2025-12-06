using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace DynamicMongoAPI.Services
{
    public class RuleValidator
    {
        // Evaluates a JSONQueryLang filter against a document
        public bool Validate(JsonDocument document, JsonDocument ruleFilter)
        {
            var docBson = BsonDocument.Parse(document.RootElement.GetRawText());
            var filterBson = BsonDocument.Parse(ruleFilter.RootElement.GetRawText());

            return MatchDocument(docBson, filterBson);
        }

        // Basic Mongo-style filter evaluator
        private bool MatchDocument(BsonDocument doc, BsonDocument filter)
        {
            foreach (var element in filter.Elements)
            {
                var field = element.Name;
                var condition = element.Value;

                // Handle logical operators ($and, $or)
                if (field == "$and")
                {
                    var array = condition.AsBsonArray;
                    foreach (var item in array)
                    {
                        if (!MatchDocument(doc, item.AsBsonDocument))
                            return false;
                    }
                    continue;
                }
                
                if (field == "$or")
                {
                    var array = condition.AsBsonArray;
                    bool anyMatch = false;
                    foreach (var item in array)
                    {
                        if (MatchDocument(doc, item.AsBsonDocument))
                        {
                            anyMatch = true;
                            break;
                        }
                    }
                    if (!anyMatch)
                        return false;
                    continue;
                }

                // Handle field conditions
                if (condition.IsBsonDocument)
                {
                    foreach (var cond in condition.AsBsonDocument.Elements)
                    {
                        var op = cond.Name;
                        var value = cond.Value;

                        switch (op)
                        {
                            case "$gt":
                                if (!doc.Contains(field) || !(doc[field].ToDouble() > value.ToDouble()))
                                    return false;
                                break;
                            case "$gte":
                                if (!doc.Contains(field) || !(doc[field].ToDouble() >= value.ToDouble()))
                                    return false;
                                break;
                            case "$lt":
                                if (!doc.Contains(field) || !(doc[field].ToDouble() < value.ToDouble()))
                                    return false;
                                break;
                            case "$lte":
                                if (!doc.Contains(field) || !(doc[field].ToDouble() <= value.ToDouble()))
                                    return false;
                                break;
                            case "$eq":
                                if (!doc.Contains(field) || !(doc[field] == value))
                                    return false;
                                break;
                            case "$ne":
                                if (!doc.Contains(field) || (doc[field] == value))
                                    return false;
                                break;
                            case "$in":
                                if (!doc.Contains(field) || !value.AsBsonArray.Contains(doc[field]))
                                    return false;
                                break;
                            case "$nin":
                                if (doc.Contains(field) && value.AsBsonArray.Contains(doc[field]))
                                    return false;
                                break;
                            default:
                                throw new NotImplementedException($"Operator {op} not supported in rules.");
                        }
                    }
                }
                else
                {
                    // Simple equality check
                    if (!doc.Contains(field) || !(doc[field] == condition))
                        return false;
                }
            }
            return true;
        }
    }
}
