// JavaTokenParser.java
package com.mems.helpers;

import com.mems.manifest.TypeDescriptor;
import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.node.ArrayNode;

import java.util.stream.Collectors;
import java.util.Iterator;

public final class JavaTokenParser {
    private JavaTokenParser() {}

    public static String render(JsonNode node, TypeDescriptor type) {
        if (node == null || node.isNull()) return "null";
        if (node.isInt() || node.isLong()) {
            return Long.toString(node.longValue());
        }
        if (node.isDouble() || node.isFloat()) {
            return Double.toString(node.doubleValue());
        }
        if (node.isBoolean()) {
            return node.booleanValue() ? "true" : "false";
        }
        if (node.isTextual()) {
            String s = node.textValue().replace("\\", "\\\\").replace("\"", "\\\"");
            return "\"" + s + "\"";
        }
        if (node.isArray()) {
            ArrayNode arr = (ArrayNode) node;
            TypeDescriptor itemType = (type != null && "array".equals(type.kind) && type.items != null) ? type.items : null;
            String itemTypeName = itemType == null ? "Object" : renderTypeName(itemType);
            // for primitive item types, use primitive arrays when possible (int[] etc)
            if ("int".equals(itemTypeName)) {
                String vals = joinArrayElements(arr, n -> Integer.toString(n.intValue()));
                return "new int[] { " + vals + " }";
            }
            // fallback to object arrays
            String vals = joinArrayElements(arr, n -> render(n, itemType));
            return "new " + itemTypeName + "[] { " + vals + " }";
        }
        if (node.isObject()) {
            // render as Map<String,Object>
            String entries = "";
            Iterator<String> it = node.fieldNames();
            StringBuilder sb = new StringBuilder();
            while (it.hasNext()) {
                String key = it.next();
                JsonNode val = node.get(key);
                sb.append("put(\"").append(key.replace("\"","\\\"")).append("\", ").append(render(val, null)).append("); ");
            }
            return "new java.util.HashMap<String,Object>() {{ " + sb.toString() + "}}";
        }
        // fallback
        return "\"" + node.asText().replace("\"","\\\"") + "\"";
    }

    private static String joinArrayElements(ArrayNode arr, java.util.function.Function<JsonNode,String> mapper) {
        return java.util.stream.StreamSupport.stream(arr.spliterator(), false)
                .map(mapper)
                .collect(Collectors.joining(", "));
    }

    public static String renderTypeName(TypeDescriptor t) {
        if (t == null) return "Object";
        switch (t.kind) {
            case "primitive":
                switch (t.name) {
                    case "int": return "int";
                    case "long": return "long";
                    case "double": return "double";
                    case "string": return "String";
                    case "bool": return "boolean";
                    case "void": return "void";
                    default: return t.name == null ? "Object" : t.name;
                }
            case "array":
                return renderTypeName(t.items) + "[]";
            case "class":
                return t.name == null ? "Object" : t.name;
            case "nullable":
                return renderTypeName(t.of);
            default:
                return "Object";
        }
    }
}
