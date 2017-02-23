﻿using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Mono.Cecil;
using Mono.CompilerServices.SymbolWriter;
using Moq;
using Testura.Code.CecilHelpers.Extensions;
using Testura.Code.Generators.Common;
using Testura.Code.Generators.Common.Arguments.ArgumentTypes;
using Testura.Code.Models;
using Testura.Code.Models.References;
using Testura.Code.Models.Types;
using Testura.Code.Statements;

namespace Testura.Code.UnitTests.MockGenerators
{
    public class MoqGenerator : IMockGenerator
    {
        public IList<Field> CreateFields(TypeDefinition typeUnderTest, IEnumerable<Models.Parameter> parameters)
        {
            if (typeUnderTest.HasGenericParameters)
            {
                return new List<Field>();
            }

            var fields = new List<Field>();
            foreach (var parameter in parameters)
            {
                var typeDefinition = parameter.Type.Resolve();

                if ((typeDefinition.IsInterface || typeDefinition.IsAbstract) && !typeDefinition.IsArray)
                {
                    fields.Add(new Field($"{parameter.Name}Mock",
                        CustomType.Create($"Mock<{parameter.Type.FormatedTypeName()}>"),
                        new List<Modifiers> {Modifiers.Private}));
                }
                else if (typeDefinition.IsClass && !typeDefinition.IsValueType && typeDefinition.Name != "String")
                {
                    fields.Add(new Field($"{parameter.Name}", CustomType.Create(parameter.Type.FormatedTypeName()),
                        new List<Modifiers> {Modifiers.Private}));
                }
            }

            fields.Add(new Field(typeUnderTest.FormatedFieldName(), CustomType.Create(typeUnderTest.FormatedTypeName()),
                new List<Modifiers> {Modifiers.Private}));
            return fields;
        }

        public IList<StatementSyntax> GenerateSetUpStatements(TypeDefinition typeUnderTest,
            IEnumerable<Models.Parameter> parameters)
        {
            var statements = new List<StatementSyntax>();
            var arguments = new List<IArgument>();

            if (typeUnderTest.HasGenericParameters)
            {
                return new List<StatementSyntax>();
            }

            foreach (var parameter in parameters)
            {
                var typeReference = parameter.Type.Resolve();

                if (typeReference.IsInterface || typeReference.IsAbstract)
                {
                    statements.Add(Statement.Declaration.Assign($"{parameter.Name}Mock",
                        CustomType.Create($"Mock<{parameter.Type.FormatedTypeName()}>"),
                        ArgumentGenerator.Create()));
                    arguments.Add(
                        new ReferenceArgument(new VariableReference($"{parameter.Name}Mock",
                            new Testura.Code.Models.References.MemberReference("Object"))));
                }
                else if (typeReference.Name.StartsWith("List") || typeReference.Name.StartsWith("Collection") || typeReference.Name.StartsWith("Dictionary"))
                {
                    statements.Add(Statement.Declaration.Assign($"{parameter.Name}",
                        CustomType.Create($"{parameter.Type.FormatedTypeName()}"),
                        ArgumentGenerator.Create()));
                    arguments.Add(
                        new ReferenceArgument(new VariableReference($"{parameter.Name}")));
                }
                else if (typeReference.IsValueType)
                {
                    arguments.Add(new ValueArgument(0));
                }
                else if (typeReference.Name == "String")
                {
                    arguments.Add(
                        new ReferenceArgument(new VariableReference("string",
                            new Testura.Code.Models.References.MemberReference("Empty"))));
                }
                else
                {
                    arguments.Add(new ReferenceArgument(new NullReference()));
                }
            }
            statements.Add(Statement.Declaration.Assign(typeUnderTest.FormatedFieldName(),
                CustomType.Create(typeUnderTest.FormatedTypeName()), ArgumentGenerator.Create(arguments.ToArray())));
            return statements;
        }
    }
}

