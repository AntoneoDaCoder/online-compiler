package com.mems.Shared.Models;

public class TestCase {
    public String name = "Test";
    public String testLanguage = "";
    public String testInitialization = "";
    public String inputExpression = "";
    public String outputExpression = "";

    public TestCase(){}

    public TestCase(String name, String testLanguage, String testInitialization, String inputExpression, String outputExpression) {
        this.name = name;
        this.testLanguage = testLanguage;
        this.testInitialization = testInitialization;
        this.inputExpression = inputExpression;
        this.outputExpression = outputExpression;
    }
}
