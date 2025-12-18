package com.mems.helpers;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.fasterxml.jackson.databind.SerializationFeature;
import com.fasterxml.jackson.datatype.jsr310.JavaTimeModule;
import com.fasterxml.jackson.databind.MapperFeature;

public class JsonUtils {
    private static final ObjectMapper objectMapper = new ObjectMapper();

    static {
        objectMapper.registerModule(new JavaTimeModule());
        objectMapper.disable(SerializationFeature.WRITE_DATES_AS_TIMESTAMPS);

        // Добавьте эту строку
        objectMapper.configure(MapperFeature.ACCEPT_CASE_INSENSITIVE_PROPERTIES, true);
    }

    public static ObjectMapper getObjectMapper() {
        return objectMapper;
    }
}
