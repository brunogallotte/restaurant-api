using System.Reflection;
using System.Runtime.CompilerServices;

namespace Restaurant.ArchitectureTests;

internal static class Reflexao
{
    private const BindingFlags Todos = BindingFlags.Public | BindingFlags.NonPublic
        | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    public static IEnumerable<Type> TiposEscritosPorHumano(Assembly assembly) =>
        assembly.GetTypes().Where(EscritoPorHumano);

    public static bool EstaEm(Type tipo, string prefixo) =>
        tipo.Namespace?.StartsWith(prefixo, StringComparison.Ordinal) == true;

    public static bool TerminaEm(Type tipo, string segmento) =>
        tipo.Namespace?.EndsWith($".{segmento}", StringComparison.Ordinal) == true;

    public static string UltimoSegmentoDoNamespace(Type tipo) =>
        tipo.Namespace?.Split('.')[^1] ?? string.Empty;

    public static bool Implementa(Type tipo, Type interfaceGenerica) =>
        InterfacesFechadas(tipo, interfaceGenerica).Length > 0;

    public static Type[] InterfacesFechadas(Type tipo, Type interfaceGenerica) =>
        [.. tipo.GetInterfaces().Where(contrato =>
            contrato.IsGenericType && contrato.GetGenericTypeDefinition() == interfaceGenerica)];

    public static IEnumerable<Type> TiposDeParametrosDeConstrutor(Type tipo) =>
        tipo.GetConstructors(Todos)
            .SelectMany(construtor => construtor.GetParameters())
            .Select(parametro => parametro.ParameterType)
            .SelectMany(Desembrulhar);

    public static IEnumerable<Type> TiposReferenciadosPor(Type tipo, Assembly assembly) =>
        TiposNaAssinaturaDe(tipo, assembly).Concat(
            TiposDeVariaveisLocais(tipo).Where(referencia => referencia.Assembly == assembly)).Distinct();

    public static IEnumerable<Type> TiposNaAssinaturaDe(Type tipo, Assembly assembly)
    {
        var deBase = tipo.BaseType is null ? [] : new[] { tipo.BaseType };
        var deInterfaces = tipo.GetInterfaces();
        var deCampos = tipo.GetFields(Todos).Select(campo => campo.FieldType);
        var dePropriedades = tipo.GetProperties(Todos).Select(propriedade => propriedade.PropertyType);
        var deMetodos = tipo.GetMethods(Todos)
            .SelectMany(metodo => metodo.GetParameters()
                .Select(parametro => parametro.ParameterType)
                .Append(metodo.ReturnType));
        var deConstrutores = TiposDeParametrosDeConstrutor(tipo);

        return deBase
            .Concat(deInterfaces)
            .Concat(deCampos)
            .Concat(dePropriedades)
            .Concat(deMetodos)
            .Concat(deConstrutores)
            .SelectMany(Desembrulhar)
            .Where(referencia => referencia.Assembly == assembly)
            .Distinct();
    }

    private static IEnumerable<Type> TiposDeVariaveisLocais(Type tipo) =>
        ComOsGeradosPeloCompilador(tipo)
            .SelectMany(alvo => alvo.GetMethods(Todos).Concat<MethodBase>(alvo.GetConstructors(Todos)))
            .Select(metodo => metodo.GetMethodBody())
            .Where(corpo => corpo is not null)
            .SelectMany(corpo => corpo!.LocalVariables.Select(local => local.LocalType))
            .SelectMany(Desembrulhar);

    private static IEnumerable<Type> ComOsGeradosPeloCompilador(Type tipo) =>
        tipo.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic)
            .Where(aninhado => aninhado.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false))
            .Prepend(tipo);

    public static IEnumerable<Type> Desembrulhar(Type tipo)
    {
        yield return tipo;

        if (!tipo.IsGenericType)
        {
            yield break;
        }

        foreach (var argumento in tipo.GetGenericArguments())
        {
            yield return argumento;
        }
    }

    private static bool EscritoPorHumano(Type tipo) =>
        !tipo.IsNested
        && !tipo.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false)
        && !tipo.Name.Contains('<', StringComparison.Ordinal);
}
