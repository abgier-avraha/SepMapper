using nietras.SeparatedValues;

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
    public void TestParseToClass()
    {
        // Register
        var mapperContext = new SepMapperContext(new Sep(','));

        mapperContext
            .RegisterClass<Something>()
            .AddRule("SomeStringA",
                accessor: a => a.SomeStringA
            )
            .AddRule("SomeStringB",
                accessor: a => a.SomeStringB,
                toProperty: a => a.ToUpper()
            )
            .AddRule("SomeInt",
                accessor: a => a.SomeInt,
                toProperty: a => Int32.Parse(a)
            );


        // Parse
        var text = """
        SomeStringA,SomeStringB,SomeInt
        raw,transformed,1
        """;

        var result = mapperContext.Read<Something>(text);

        // Assert
        Assert.Equivalent(new Something()
        {
            SomeStringA = "raw",
            SomeStringB = "TRANSFORMED",
            SomeInt = 1,
        }, result[0]);
    }

    [Fact]
    public void TestWriteCsvFromClass()
    {
        // Register
        var mapperContext = new SepMapperContext(new Sep(','));

        mapperContext
            .RegisterClass<Something>()
            .AddRule("SomeStringA",
                accessor: a => a.SomeStringA
            )
            .AddRule("SomeStringB",
                accessor: a => a.SomeStringB,
                toCsv: a => a.ToLower()
            )
            .AddRule("SomeInt",
                accessor: a => a.SomeInt
            );

        var models = new List<Something>() {
            new ()
            {
                SomeStringA = "raw",
                SomeStringB = "transformed",
                SomeInt = 1
            }
        };

        // Write
        var csv = mapperContext.Write(models);

        // Assert
        Assert.Equal("SomeStringA,SomeStringB,SomeInt\nraw,transformed,1\n", csv);
    }
}