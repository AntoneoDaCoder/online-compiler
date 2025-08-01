package com.mems.Shared.Enums;

import com.fasterxml.jackson.annotation.JsonValue;

public enum ExecutionStatus {
    NO_STATUS(0),
    SUCCEEDED(1),
    COMPILE_ERROR(2),
    RUNTIME_ERROR(3),
    TIMED_OUT(4),
    CANCELLED(5),
    FAILED_TO_EXECUTE(6),
    PENDING(7);

    private final int value;

    ExecutionStatus(int value) {
        this.value = value;
    }

    @JsonValue
    public int getValue() {
        return value;
    }
}
