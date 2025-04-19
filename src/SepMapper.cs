namespace SepMapper;

using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using nietras.SeparatedValues;

public interface ISepMapperContext
{
    SepPropertyRulesContainer<T> RegisterClass<T>() where T : class;
    void UnregisterClass<T>() where T : class;

    string Write<T>(List<T> instances) where T : class;
    string Write<T>(List<T> instance, StringWriter text) where T : class;

    List<T> Read<T>(TextReader csv) where T : class;
    List<T> Read<T>(Stream csv) where T : class;
    List<T> Read<T>(string csv) where T : class;
}

public class SepMapperContext : ISepMapperContext
{
    private readonly Sep sep;
    private readonly Dictionary<Type, ISepPropertyRulesContainer> mappers = new();

    public SepMapperContext(Sep sep)
    {
        this.sep = sep;
    }

    public SepPropertyRulesContainer<T> RegisterClass<T>() where T : class
    {
        var typeMapper = new SepPropertyRulesContainer<T>();
        this.mappers.Add(typeof(T), typeMapper);
        return typeMapper;
    }

    public void UnregisterClass<T>() where T : class
    {
        this.mappers.Remove(typeof(T));
    }

    public string Write<T>(List<T> instances) where T : class
    {
        return this.Write(instances, new StringWriter());
    }

    public string Write<T>(List<T> instances, StringWriter text) where T : class
    {
        var writer = sep.Writer().To(text);
        return this.WriteInner(instances, writer);
    }

    public List<T> Read<T>(TextReader csv) where T : class
    {
        using var reader = this.sep.Reader().From(csv);
        return this.ReadInner<T>(reader);
    }

    public List<T> Read<T>(Stream csv) where T : class
    {
        using var reader = this.sep.Reader().From(csv);
        return this.ReadInner<T>(reader);
    }

    public List<T> Read<T>(string csv) where T : class
    {
        using var reader = this.sep.Reader().FromText(csv);
        return this.ReadInner<T>(reader);
    }

    private string WriteInner<T>(List<T> instances, SepWriter writer) where T : class
    {
        var mapper = this.GetMapper<T>();
        var rules = mapper.GetRules();

        foreach (var instance in instances)
        {
            using var row = writer.NewRow();
            foreach (var ruleEntry in rules)
            {
                var value = ruleEntry.Value.GetValue(instance);
                if (value != null)
                {
                    row[ruleEntry.Key].Set(value.ToString());
                }
            }
        }

        return writer.ToString();
    }

    private List<T> ReadInner<T>(SepReader reader) where T : class
    {
        if (!this.mappers.ContainsKey(typeof(T)))
        {
            throw new Exception($"No mapping class registered for type: {typeof(T)}");
        }

        var mapper = this.GetMapper<T>();
        var list = new List<T>();

        foreach (var readRow in reader)
        {
            var model = (T)Activator.CreateInstance(typeof(T))!;

            foreach (var col in reader.Header.ColNames)
            {
                var rule = mapper.GetRule(col);
                rule?.SetValue(model, readRow[col].ToString());
            }

            list.Add(model);
        }

        return list;
    }

    private SepPropertyRulesContainer<T> GetMapper<T>() where T : class
    {
        return (SepPropertyRulesContainer<T>)this.mappers[typeof(T)];
    }
}

// Abstract away generics to simulate type erasure

public interface ISepPropertyRulesContainer
{
    Dictionary<string, ISepPropertyRule> GetRules();
    ISepPropertyRule? GetRule(string column);
}

public class SepPropertyRulesContainer<T> : ISepPropertyRulesContainer where T : class
{
    private readonly Dictionary<string, ISepPropertyRule> rules = new();

    public SepPropertyRulesContainer<T> AddRule<P>(string key, Expression<Func<T, P>> accessor,
        [Optional] Func<string, P> toProperty, [Optional] Func<P, string> toCsv)
    {
        var rule = new SepPropertyRule<T, P>(accessor, toProperty, toCsv);
        this.rules.Add(key, rule);
        return this;
    }

    public Dictionary<string, ISepPropertyRule> GetRules() => this.rules;

    public ISepPropertyRule? GetRule(string column)
    {
        return this.rules.TryGetValue(column, out var rule) ? rule : null;
    }
}

/*
    Abstract away generics to simulate type erasure
    This is useful for avoiding dynamic types
    For instance we can't write Dictionary<Type, SepPropertyRule<dynamic, dynamic>>, we have write Dictionary<Type, dynamic>
    It's cleaner to abstract the generic arguments with an interface and have Dictionary<Type, ISepPropertyRule> instead
    Like a simulated type erasure (types are still reified and not lost during compilation so it isn't type erasure)
*/

public interface ISepPropertyRule
{
    object? GetValue(object instance);
    void SetValue(object instance, string value);
}

public class SepPropertyRule<T, P> : ISepPropertyRule where T : class
{
    private readonly Func<T, P> getter;
    private readonly Action<T, P> setter;
    private readonly Func<string, P>? toProperty;
    private readonly Func<P, string>? toCsv;

    public SepPropertyRule(Expression<Func<T, P>> accessor, Func<string, P>? toProperty, Func<P, string>? toCsv)
    {
        this.getter = accessor.Compile();

        var memberExpression = (MemberExpression)accessor.Body;
        var property = (PropertyInfo)memberExpression.Member;
        var setMethod = property.GetSetMethod();

        var paramT = Expression.Parameter(typeof(T), "x");
        var paramP = Expression.Parameter(typeof(P), "y");

        var lambda = Expression.Lambda<Action<T, P>>(
            Expression.Call(paramT, setMethod!, paramP), paramT, paramP);

        this.setter = lambda.Compile();
        this.toProperty = toProperty;
        this.toCsv = toCsv;
    }

    public object? GetValue(object instance)
    {
        var value = this.getter((T)instance);
        return this.toCsv != null ? this.toCsv(value) : value;
    }

    public void SetValue(object instance, string value)
    {
        var parsed = toProperty != null ? toProperty(value) : (P)Convert.ChangeType(value, typeof(P));
        this.setter((T)instance, parsed);
    }
}
