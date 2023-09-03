namespace SepMapper;

using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using nietras.SeparatedValues;

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
            );


        var text = """
        SomeStringA,SomeStringB
        raw,transformed
        """;

        var result = registry.Parse<Something>(text);

        Assert.Equivalent(new Something()
        {
            SomeStringA = "raw",
            SomeStringB = "TRANSFORMED",
        }, result[0]);
    }
}