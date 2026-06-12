// SampleTest.java
package com.mems.manifest;

import com.fasterxml.jackson.databind.JsonNode;

public class SampleTest {
    public String name;
    public JsonNode inputs;    // arbitrary JSON
    public JsonNode expected;  // arbitrary JSON
    public String comparator;  // eq, neq, seq_eq, seq_eq_sorted, contains, lt, gt, le, ge
    public long timeoutMs;
}
