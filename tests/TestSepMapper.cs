namespace SepMapper;

using System.Reflection;
using System.Runtime.InteropServices;
using nietras.SeparatedValues;

public class TestSepMapper
{
    public class Something
    {
        public string SomeString { get; set; } = "";
        public int SomeInt { get; set; }
        public DateTimeOffset SomeDate { get; set; }
    }

    public interface ISepMapperContext
    {
        public SepMapperRulesForType<T> RegisterClass<T>() where T : class;
        public void UnregisterClass<T>() where T : class;
        public string Serialize<T>(T anyObject) where T : class;
        public T Parse<T>(string csv) where T : class;
        public T ParseFile<T>(string path) where T : class;
    }

    public class SepMapperContext : ISepMapperContext
    {
        private readonly Dictionary<Type, SepMapperRulesForType<object>> mappers = new();

        public SepMapperRulesForType<T> RegisterClass<T>() where T : class
        {
            var typeMapper = new SepMapperRulesForType<T>();

            // TODO: Safe cast and Convert.ChangeType?
            this.mappers.Add(typeof(T), (SepMapperRulesForType<object>)typeMapper);
            return typeMapper;
        }

        public void UnregisterClass<T>() where T : class
        {
            this.mappers.Remove(typeof(T));
        }

        public string Serialize<T>(T anyObject) where T : class
        {
            // TODO: use mapper else default behaviour
            return "";
        }

        public T Parse<T>(string csv) where T : class
        {
            using var reader = Sep.Reader().FromText(csv);
            var model = (T)Activator.CreateInstance(typeof(T), new object[] { })!;

            if (!this.mappers.ContainsKey(typeof(T)))
            {
                return model;
            }

            var mapper = this.mappers[typeof(T)];

            foreach (var col in reader.Header.ColNames)
            {
                var rule = mapper.GetRule<object>(col);
                if (rule?.Accessor is not null)
                {
                    var property = rule.Accessor(model);
                    property = "test";
                }
            }

            return model;
        }

        public T ParseFile<T>(string path) where T : class
        {
            // TODO: use mapper else default behaviour
            var model = (T)Activator.CreateInstance(typeof(T), new object[] { })!;
            return model;
        }
    }

    public class SepMapperRulesForType<T> where T : class
    {
        private readonly Dictionary<string, SepMapperTypeRulesForProperty<object, object>> rules = new();

        public SepMapperRulesForType<T> AddRule<P>(string key, Func<T, P> accessor, [Optional] Func<string, P> toProperty, [Optional] Func<P, string> toCsv) where P : class
        {
            var propertyRules = new SepMapperTypeRulesForProperty<T, P>(accessor)
            {
                ToProperty = toProperty,
                ToCsv = toCsv,
            };
            // TODO: Safe cast and Convert.ChangeType
            this.rules.Add(key, (SepMapperTypeRulesForProperty<object, object>)propertyRules);
            return this;
        }

        public SepMapperTypeRulesForProperty<T, P>? GetRule<P>(string key) where P : class
        {
            return this.rules[key] as SepMapperTypeRulesForProperty<T, P>;
        }
    }

    public class SepMapperTypeRulesForProperty<T, P> where T : class where P : class
    {
        public SepMapperTypeRulesForProperty(Func<T, P> Accessor)
        {
            this.Accessor = Accessor;
        }

        public Func<T, P> Accessor;
        public Func<string, P>? ToProperty;
        public Func<P, string>? ToCsv;
    }

    [Fact]
    public void TestRegisterClass()
    {
        var registry = new SepMapperContext();

        registry
            .RegisterClass<Something>()
            .AddRule("SomeString",
                accessor: a => a.SomeString
                // toProperty: a => a,
                // toCsv: a => a
            );


        var text = """
        SomeString
        wowee
        """;

        var result = registry.Parse<Something>(text);

        Assert.Equal(new Something()
        {
            SomeString = "test"
        }, result);

        // using var reader = Sep.Reader().FromText(text);
        // using var writer = reader.Spec.Writer().ToText();

        // var idx = reader.Header.IndexOf("B");
        // var nms = new[] { "E", "F" };
    }
}