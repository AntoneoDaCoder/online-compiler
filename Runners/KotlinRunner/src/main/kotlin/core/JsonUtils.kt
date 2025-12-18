package core

import com.fasterxml.jackson.databind.DeserializationFeature
import com.fasterxml.jackson.databind.ObjectMapper
import com.fasterxml.jackson.datatype.jsr310.JavaTimeModule
import com.fasterxml.jackson.module.kotlin.jacksonObjectMapper

object JsonUtils {
    private val _objectMapper: ObjectMapper = jacksonObjectMapper().apply {
        registerModule(JavaTimeModule())

        // Настройки десериализации
        configure(DeserializationFeature.FAIL_ON_UNKNOWN_PROPERTIES, false)
        configure(DeserializationFeature.READ_UNKNOWN_ENUM_VALUES_AS_NULL, true)
        // Настройки сериализации
        disable(com.fasterxml.jackson.databind.SerializationFeature.WRITE_DATES_AS_TIMESTAMPS)
    }

    // Публичный геттер для неизменяемого ObjectMapper
    val objectMapper: ObjectMapper
        get() = _objectMapper.copy()

    // Или функция для создания новой копии
    fun createObjectMapper(): ObjectMapper = _objectMapper.copy()
}