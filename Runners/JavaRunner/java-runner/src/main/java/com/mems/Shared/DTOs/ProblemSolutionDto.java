package com.mems.Shared.DTOs;

import com.fasterxml.jackson.annotation.JsonProperty;
import java.time.OffsetDateTime;

public class ProblemSolutionDto {
    @JsonProperty("RequestId")
    public String RequestId;

    @JsonProperty("VersionId")
    public String VersionId;

    @JsonProperty("UserId")
    public String UserId;

    @JsonProperty("TestManifestJson")
    public String TestManifestJson;

    @JsonProperty("LanguageCode")
    public String LanguageCode;

    @JsonProperty("UserSolution")
    public String UserSolution;

    @JsonProperty("SentAt")
    public OffsetDateTime SentAt;
}
