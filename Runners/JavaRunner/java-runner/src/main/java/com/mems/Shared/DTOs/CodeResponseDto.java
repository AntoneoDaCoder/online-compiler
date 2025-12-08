package com.mems.Shared.DTOs;

import com.fasterxml.jackson.annotation.JsonProperty;
import com.mems.Shared.Enums.RequestStatus;
import java.util.UUID;

public class CodeResponseDto {
    @JsonProperty("RequestId")
    public UUID RequestId;

    @JsonProperty("Status")
    public RequestStatus Status = null;

    @JsonProperty("VersionId")
    public UUID VersionId;

    @JsonProperty("UserId")
    public UUID UserId;

    @JsonProperty("UserSolution")
    public String UserSolution;

    @JsonProperty("Language")
    public String Language;

    @JsonProperty("Result")
    public ExecutionResultDto Result;
}
