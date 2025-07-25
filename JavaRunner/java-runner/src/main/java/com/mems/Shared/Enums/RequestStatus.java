package com.mems.Shared.Enums;

public enum RequestStatus {
    NO_STATUS(0),
    ACKNOWLEDGED(1),
    EXECUTING(2),
    FAILED(3),
    SUCCEEDED(4),
    CANCELLED(5);

    private final int value;

    RequestStatus(int value) {
        this.value = value;
    }

    public int getValue() {
        return value;
    }
}
