package com.mems.Shared.DTOs;

import java.util.UUID;

import com.mems.Shared.Enums.RequestStatus;

public class CodeResponseDto {
    public UUID requestId;
    public RequestStatus status = RequestStatus.NO_STATUS;
    public String language = "";
    public ExecutionResultDto result;
}
