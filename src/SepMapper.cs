namespace SepMapper;

using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using nietras.SeparatedValues;


public interface ISepMapperContext
{
    public SepMapperRulesForType<T> RegisterClass<T>() where T : class;
    public void UnregisterClass<T>() where T : class;
    public string Serialize<T>(T anyObject) where T : class;
    public List<T> Parse<T>(string csv) where T : class;
    public List<T> ParseFile<T>(string path) where T : class;
}

public class SepMapperContext : ISepMapperContext
{
    private readonly Dictionary<Type, dynamic> mappers = new();

    public SepMapperRulesForType<T> RegisterClass<T>() where T : class
    {
        var typeMapper = new SepMapperRulesForType<T>();

        this.mappers.Add(typeof(T), typeMapper);
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

    public List<T> Parse<T>(string csv) where T : class
    {
        using var reader = Sep.Reader().FromText(csv);

        return this.Parse<T>(reader);
    }

    public List<T> ParseFile<T>(string path) where T : class
    {
        using var reader = Sep.Reader().FromFile(path);

        return this.Parse<T>(reader);
    }

    private List<T> Parse<T>(SepReader reader) where T : class
    {
        var model = (T)Activator.CreateInstance(typeof(T), new object[] { })!;

        if (!this.mappers.ContainsKey(typeof(T)))
        {
            throw new Exception($"No mapping class registered for type: {typeof(T)}");
        }

        var mapper = (SepMapperRulesForType<T>)this.mappers[typeof(T)];

        var list = new List<T>();

        foreach (var readRow in reader)
        {
            foreach (var col in reader.Header.ColNames)
            {
                // TODO: dynamic generic param
                var rule = mapper.GetRule<string>(col);
                if (rule?.Accessor is not null)
                {
                    var setter = GetSetter(rule.Accessor);

                    if (rule.ToProperty != null)
                    {
                        // Parse with ToProperty transformation
                        setter.Invoke(model, rule.ToProperty(readRow[col].ToString()));
                        list.Add(model);
                    }
                    else
                    {
                        // Default parse
                        setter.Invoke(model, readRow[col].ToString());
                        list.Add(model);
                    }
                }
            }
        }

        return list;
    }

    private static Action<T, TProperty> GetSetter<T, TProperty>(Expression<Func<T, TProperty>> expression)
    {
        var memberExpression = (MemberExpression)expression.Body;
        var property = (PropertyInfo)memberExpression.Member;
        var setMethod = property.GetSetMethod();

        var parameterT = Expression.Parameter(typeof(T), "x");
        var parameterTProperty = Expression.Parameter(typeof(TProperty), "y");

        var newExpression =
            Expression.Lambda<Action<T, TProperty>>(
                Expression.Call(parameterT, setMethod!, parameterTProperty),
                parameterT,
                parameterTProperty
            );

        return newExpression.Compile();
    }
}

public class SepMapperRulesForType<T> where T : class
{
    private readonly Dictionary<string, dynamic> rules = new();

    public SepMapperRulesForType<T> AddRule<P>(string key, Expression<Func<T, P>> accessor, [Optional] Func<string, P> toProperty, [Optional] Func<P, string> toCsv) where P : class
    {
        var propertyRules = new SepMapperTypeRulesForProperty<T, P>(accessor)
        {
            ToProperty = toProperty,
            ToCsv = toCsv,
        };
        this.rules.Add(key, propertyRules);
        return this;
    }

    public SepMapperTypeRulesForProperty<T, P>? GetRule<P>(string key) where P : class
    {
        return (SepMapperTypeRulesForProperty<T, P>)this.rules[key];
    }
}

public class SepMapperTypeRulesForProperty<T, P> where T : class where P : class
{
    public SepMapperTypeRulesForProperty(Expression<Func<T, P>> Accessor)
    {
        this.Accessor = Accessor;
    }

    public Expression<Func<T, P>> Accessor;
    public Func<string, P>? ToProperty;
    public Func<P, string>? ToCsv;
}