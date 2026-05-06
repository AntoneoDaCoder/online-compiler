package com.mems.Shared.DTOs;

import com.fasterxml.jackson.annotation.JsonFormat;
import com.fasterxml.jackson.annotation.JsonProperty;
import com.mems.Shared.Enums.ExecutionStatus;

import java.time.OffsetDateTime;

public class ExecutionResultDto {
    @JsonProperty("Status")
    public ExecutionStatus Status;

    @JsonProperty("ExitCode")
    public int ExitCode;

    @JsonProperty("ConsoleOutput")
    public String ConsoleOutput;

    @JsonProperty("PassedTests")
    public int PassedTests;

    @JsonProperty("TotalTests")
    public int TotalTests;

    @JsonProperty("WallTimeMs")
    public Long WallTimeMs = 0L;
    @JsonProperty("PeakMemoryBytes")
    public Long PeakMemoryBytes = 0L;
    @JsonProperty("CpuTimeUs")

    public Long CpuTimeUs = 0L;

    @JsonProperty("RequestSentAt")
    @JsonFormat(shape = JsonFormat.Shape.STRING)
    public OffsetDateTime RequestSentAt;

    @JsonProperty("ResponseSentAt")
    @JsonFormat(shape = JsonFormat.Shape.STRING)
    public OffsetDateTime ResponseSentAt;

    // Compute latency on serialization (matches C# property)
    @JsonProperty("LatencyInSeconds")
    public double getLatencyInSeconds() {
        if (RequestSentAt == null || ResponseSentAt == null) return 0.0;
        long millis = java.time.Duration.between(RequestSentAt, ResponseSentAt).toMillis();
        return millis / 1000.0;
    }
}
