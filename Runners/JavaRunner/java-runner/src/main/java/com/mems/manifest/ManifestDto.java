// ManifestDto.java
package com.mems.manifest;

import java.util.List;

public class ManifestDto {
    public String entrypoint;
    public Signature signature = new Signature();
    public List<HelpersBlock> helpers;
    public List<AdvancedTest> advancedTests;
    public List<SampleTest> sampleTests;
}
