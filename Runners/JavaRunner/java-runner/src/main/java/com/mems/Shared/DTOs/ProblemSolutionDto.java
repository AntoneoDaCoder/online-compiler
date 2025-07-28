package com.mems.Shared.DTOs;

import java.time.LocalDateTime;
import java.util.UUID;

import com.mems.Shared.Models.Problem;

public class ProblemSolutionDto {
    public UUID requestId;
    public long maxAllowedTimeInMilliseconds;
    public String language = "";
    public String code = "";
    public Problem problem;
    public String callbackUrl = "";
    public LocalDateTime sentAt;
}
