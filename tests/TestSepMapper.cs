namespace SepMapper;

public class TestSepMapper
{
    public class Something
    {
        public string SomeStringA { get; set; } = "";
        public string SomeStringB { get; set; } = "";
        public int SomeInt { get; set; }
        public DateTimeOffset SomeDate { get; set; }
    }

    [Fact]
    public void TestRegisterClass()
    {
        var registry = new SepMapperContext();

        registry
            .RegisterClass<Something>()
            .AddRule("SomeStringA",
                accessor: a => a.SomeStringA
            )
            .AddRule("SomeStringB",
                accessor: a => a.SomeStringB,
                toProperty: a => a.ToUpper()
                // toCsv: a => a
            )
            .AddRule("SomeInt",
                accessor: a => a.SomeInt,
                toProperty: a => Int32.Parse(a)
                // toCsv: a => a
            );


        var text = """
        SomeStringA,SomeStringB,SomeInt
        raw,transformed,1
        """;

        var result = registry.Parse<Something>(text);

        Assert.Equivalent(new Something()
        {
            SomeStringA = "raw",
            SomeStringB = "TRANSFORMED",
            SomeInt = 1,
        }, result[0]);
    }
}