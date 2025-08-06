package com.mems.Shared.Models;

public class TestCase {
    public String name = "Test";
    public String testInitialization = "";
    public String inputExpression = "";
    public String outputExpression = "";

    public TestCase(){}

    public TestCase(String name, String testInitialization, String inputExpression, String outputExpression) {
        this.name = name;
        this.testInitialization = testInitialization;
        this.inputExpression = inputExpression;
        this.outputExpression = outputExpression;
    }
}
