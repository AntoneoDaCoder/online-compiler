package com.mems.Shared.DTOs;

import java.time.Duration;
import java.time.LocalDateTime;

import com.mems.Shared.Enums.ExecutionStatus;

public class ExecutionResultDto {
    public ExecutionStatus status;
    public int exitCode;
    public String consoleOutput;
    public LocalDateTime requestSentAt;
    public LocalDateTime responseSentAt;
    
    public double getLatencyInSeconds() {
        return Duration.between(requestSentAt, responseSentAt).toMillis() / 1000.0;
    }
}
