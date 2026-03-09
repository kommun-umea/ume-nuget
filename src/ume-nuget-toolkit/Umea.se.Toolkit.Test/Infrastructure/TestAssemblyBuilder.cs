using System.Reflection;
using System.Reflection.Emit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Umea.se.Toolkit.Auth;

namespace Umea.se.Toolkit.Test.Infrastructure;

public class TestAssemblyBuilder
{
    private readonly AssemblyBuilder _assemblyBuilder;
    private readonly ModuleBuilder _moduleBuilder;

    private TestAssemblyBuilder()
    {
        AssemblyName name = new("FakeTestAssembly");
        _assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(name, AssemblyBuilderAccess.Run);
        _moduleBuilder = _assemblyBuilder.DefineDynamicModule("MainModule");
    }

    public static TestAssemblyBuilder CreateBuilder() => new();

    public Assembly BuildAssembly() => _assemblyBuilder;

    public TestAssemblyBuilder WithController(string controllerName, Action<ControllerOptions>? options = null)
    {
        TypeBuilder typeBuilder = _moduleBuilder.DefineType(controllerName, TypeAttributes.Public | TypeAttributes.Class, typeof(ControllerBase));

        ControllerOptions controllerOptions = new(typeBuilder);
        options?.Invoke(controllerOptions);

        typeBuilder.CreateType();

        return this;
    }

    public class ControllerOptions(TypeBuilder typeBuilder)
    {
        private readonly TypeBuilder _typeBuilder = typeBuilder;

        public ControllerOptions WithApiKeyAuthorization(string apiKey)
        {
            ConstructorInfo attributeConstructor = typeof(AuthorizeApiKeyAttribute).GetConstructor([typeof(string)])!;
            _typeBuilder.SetCustomAttribute(new CustomAttributeBuilder(attributeConstructor, [apiKey]));

            return this;
        }

        public ControllerOptions WithAllowAnonymous()
        {
            ConstructorInfo attributeConstructor = typeof(AllowAnonymousAttribute).GetConstructor(Type.EmptyTypes)!;
            _typeBuilder.SetCustomAttribute(new CustomAttributeBuilder(attributeConstructor, []));

            return this;
        }

        public ControllerOptions WithEndpoint(string endpointName, Action<EndpointOptions>? options = null)
        {
            MethodBuilder methodBuilder = _typeBuilder.DefineMethod(endpointName, MethodAttributes.Public | MethodAttributes.HideBySig, typeof(IActionResult), Type.EmptyTypes);

            EndpointOptions endpointOptions = new(methodBuilder);
            options?.Invoke(endpointOptions);

            ILGenerator ilGenerator = methodBuilder.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ldnull);
            ilGenerator.Emit(OpCodes.Ret);

            return this;
        }

        public class EndpointOptions(MethodBuilder methodBuilder)
        {
            private readonly MethodBuilder _methodBuilder = methodBuilder;

            public EndpointOptions WithApiKeyAuthorization(string apiKey)
            {
                ConstructorInfo attributeConstructor = typeof(AuthorizeApiKeyAttribute).GetConstructor([typeof(string)])!;
                _methodBuilder.SetCustomAttribute(new CustomAttributeBuilder(attributeConstructor, [apiKey]));

                return this;
            }

            public EndpointOptions WithAllowAnonymous()
            {
                ConstructorInfo attributeConstructor = typeof(AllowAnonymousAttribute).GetConstructor(Type.EmptyTypes)!;
                _methodBuilder.SetCustomAttribute(new CustomAttributeBuilder(attributeConstructor, []));

                return this;
            }
        }
    }
}
