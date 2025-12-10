package helpers

import com.fasterxml.jackson.databind.JsonNode
import com.fasterxml.jackson.databind.node.ArrayNode
import manifest.TypeDescriptor
import java.util.Locale

object KotlinTokenParser {
    @JvmStatic
    fun render(node: JsonNode?, type: TypeDescriptor?): String {
        if (node == null || node.isNull) return "null"

        if (node.isInt || node.isLong) {
            val lv = node.longValue()
            // если исходный узел long — добавляем L-суффикс
            return if (node.isLong && !node.isInt) "${lv}L" else lv.toString()
        }

        if (node.isDouble || node.isFloatingPointNumber) {
            val d = node.doubleValue()
            // чтобы не получить "1" для double-литерала 1.0
            return if (d % 1.0 == 0.0) "${d.toLong()}.0" else d.toString()
        }

        if (node.isBoolean) {
            return if (node.booleanValue()) "true" else "false"
        }

        if (node.isTextual) {
            val s = node.textValue()
                .replace("\\", "\\\\")
                .replace("\"", "\\\"")
                .replace("\n", "\\n")
                .replace("\r", "\\r")
                .replace("\t", "\\t")
            return "\"$s\""
        }

        if (node.isArray) {
            val arr = node as ArrayNode
            val itemType = if (type != null && type.kind == "array") type.items else null
            val itemTypeName = itemType?.let { renderTypeName(it) } ?: "Any"

            return when (itemTypeName) {
                "Int" -> {
                    val vals = joinArrayElements(arr) { n: JsonNode -> n.intValue().toString() }
                    "intArrayOf($vals)"
                }
                "Long" -> {
                    val vals = joinArrayElements(arr) { n: JsonNode ->
                        val lv = n.longValue()
                        "${lv}L"
                    }
                    "longArrayOf($vals)"
                }
                "Double" -> {
                    val vals = joinArrayElements(arr) { n: JsonNode ->
                        val d = n.doubleValue()
                        if (d % 1.0 == 0.0) "${d.toLong()}.0" else d.toString()
                    }
                    "doubleArrayOf($vals)"
                }
                else -> {
                    val vals = joinArrayElements(arr) { n: JsonNode -> render(n, itemType) }
                    if (itemTypeName == "Any") "arrayOf($vals)" else "arrayOf<$itemTypeName>($vals)"
                }
            }
        }

        if (node.isObject) {
            // Соберём список put(...) выражений, чтобы избежать проблем с append-цепочками
            val fieldsIter = node.fieldNames()
            val entries = mutableListOf<String>()
            while (fieldsIter.hasNext()) {
                val key = fieldsIter.next()
                val valNode = node.get(key)
                val escKey = key.replace("\\", "\\\\").replace("\"", "\\\"")
                val renderedVal = render(valNode, null)
                entries.add("put(\"$escKey\", $renderedVal);")
            }
            val body = entries.joinToString(" ")
            return "mutableMapOf<String, Any?>().apply { $body }"
        }

        // fallback: текстовое представление
        val text = node.asText().replace("\\", "\\\\").replace("\"", "\\\"")
        return "\"$text\""
    }

    private fun joinArrayElements(arr: ArrayNode, mapper: (JsonNode) -> String): String {
        val out = ArrayList<String>(arr.size())
        val it = arr.elements()
        while (it.hasNext()) {
            val n = it.next()
            out.add(mapper(n))
        }
        return out.joinToString(", ")
    }

    @JvmStatic
    // kotlin: KotlinTokenParser.renderTypeName patch
    fun renderTypeName(t: TypeDescriptor?): String {
        if (t == null) return "Any"

        when (t.kind.lowercase()) {
            "primitive" -> return when (t.name?.lowercase()) {
                "int" -> "Int"
                "long" -> "Long"
                "double" -> "Double"
                "string" -> "String"
                "bool", "boolean" -> "Boolean"
                "void" -> "Unit"
                else -> t.name ?: "Any"
            }

            "array" -> {
                val items = t.items
                if (items == null) {
                    return "Array<Any>"
                }
                // primitive arrays -> specialized Kotlin primitive arrays
                if (items.kind.equals("primitive", ignoreCase = true)) {
                    return when (items.name?.lowercase()) {
                        "int" -> "IntArray"
                        "long" -> "LongArray"
                        "double" -> "DoubleArray"
                        "bool", "boolean" -> "BooleanArray"
                        "string" -> "Array<String>"
                        else -> "Array<${renderTypeName(items)}>"
                    }
                }
                // nested array or object -> Array<ElemType>
                return "Array<${renderTypeName(items)}>"
            }

            "nullable" -> {
                val of = t.of
                return (renderTypeName(of) ?: "Any") + "?"
            }

            "class" -> return t.name ?: "Any"

            else -> return t.name ?: "Any"
        }
    }

}
