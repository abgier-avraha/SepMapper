# SepMapper

Example

```csharp
var mapperContext = new SepMapperContext(new Sep(','));

mapperContext
    .RegisterClass<Something>()
    .AddRule("SomeStringA",
        accessor: a => a.SomeStringA
    )
    .AddRule("SomeStringB",
        accessor: a => a.SomeStringB,
        toCsv: a => a.ToLower(),
        toProperty: a => a.ToUpper()
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
```