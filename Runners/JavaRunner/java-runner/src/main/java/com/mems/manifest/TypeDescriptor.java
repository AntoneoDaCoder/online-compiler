// TypeDescriptor.java
package com.mems.manifest;

public class TypeDescriptor {
    // kind: "primitive","array","class","nullable","task" (if needed)
    public String kind = "primitive";
    public String name; // primitive name or class FQN
    public TypeDescriptor items; // for arrays
    public TypeDescriptor of; // for nullable / task.of
}
