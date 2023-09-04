# SepMapper

Example

```csharp
// Setup your context
var mapperContext = new SepMapperContext(new Sep(','));

// Use it to register your mapped classes
/*
  TIP: Inject this instance via
  services.AddSingleton<ISepMapperContext>(mapperContext)
  in your dotnet project
*/
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

// Write
var text = mapperContext.Write(models);

// Read
var result = mapperContext.Read<Something>(text);
```