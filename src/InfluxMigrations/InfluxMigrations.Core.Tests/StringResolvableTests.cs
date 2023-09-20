using InfluxMigrations.Core;
using InfluxMigrations.Core.Resolvers;
using InfluxMigrations.Impl;
using NUnit.Framework;

namespace InfluxMigrations.Abstractions.Tests;


public class StringResolvableTests
{
    [Test]
    public void TokenizeSimpleString()
    {
        var result = StringResolvable.Tokenize("${token}");
        Assert.That(result.Count, Is.EqualTo(1));
        
        Assert.That(result[0], Is.EqualTo("${token}"));
    }
    
    [Test]
    public void TokenizeComplexString()
    {
        var result = StringResolvable.Tokenize("word1 word2 ${token} word3 word4 ${token${subtoken}} word5 word6");
        Assert.That(result.Count, Is.EqualTo(5));
        Assert.Multiple(() =>
        {
            Assert.That(result[0], Is.EqualTo("word1 word2 "));
            Assert.That(result[1], Is.EqualTo("${token}"));
            Assert.That(result[2], Is.EqualTo(" word3 word4 "));
            Assert.That(result[3], Is.EqualTo("${token${subtoken}}"));
            Assert.That(result[4], Is.EqualTo(" word5 word6"));
        });
    }

    [Test]
    public void TokenizeComplexString_ComplexTokenAtEnd()
    {
        var result = StringResolvable.Tokenize("word1 word2 ${token} word3 word4 ${token${subtoken}}");
        Assert.That(result.Count, Is.EqualTo(4));
        Assert.Multiple(() =>
        {
            Assert.That(result[0], Is.EqualTo("word1 word2 "));
            Assert.That(result[1], Is.EqualTo("${token}"));
            Assert.That(result[2], Is.EqualTo(" word3 word4 "));
            Assert.That(result[3], Is.EqualTo("${token${subtoken}}"));
        });
    }

    [Test]
    public void TokenizeComplexString_MissingClosingBrace_ShoudThrow()
    {
        try
        {
            StringResolvable.Tokenize("word1 ${token${subtoken} word2");
            Assert.Fail("Should have raised a parsing exception");
        }
        catch (MigrationResolutionException)
        {
            
        }
    }

    [Test]
    public void ResolveResultOfPreviousStep()
    {
        var environment = new DefaultEnvironmentContext();
        var migration = environment.CreateMigrationContext("0001");
        var step1 = migration.CreateExecutionContext("step1");
        step1.ExecuteResult = new
        {
            Step1Result = "abcd"
        };
        
        var step2 = migration.CreateExecutionContext("step2");
        step2.ExecuteResult = new
        {
            Step2Result = "wxyz"
        };
        
        var step3 = migration.CreateExecutionContext("step3");
        var parsed = StringResolvable.Parse("${step:step1:${result:step1result}}");
        var result = parsed.Resolve(step3);
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.EqualTo("abcd"));
    }

    [Test]
    public void ResolveVariableOfPreviousStep()
    {
        var environment = new DefaultEnvironmentContext();
        var migration = environment.CreateMigrationContext("0001");
        var step1 = migration.CreateExecutionContext("step1");
        step1.Set("secret_key", "hidden");
        
        var step2 = migration.CreateExecutionContext("step2");
        var step3 = migration.CreateExecutionContext("step3");

        var parsed = StringResolvable.Parse("${step:step1:${secret_key}}");
        var result = parsed.Resolve(step3);
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.EqualTo("hidden"));        
    }
    
    [Test]
    public void ResolveVariableOfPreviousStepUsingVariableFromPreviousStep()
    {
        var environment = new DefaultEnvironmentContext();
        var migration = environment.CreateMigrationContext("0001");
        var step1 = migration.CreateExecutionContext("step1");
        step1.Set("secret_key", "hidden");
        
        var step2 = migration.CreateExecutionContext("step2");
        step2.Set("key_name", "secret_key");
        var step3 = migration.CreateExecutionContext("step3");

        var parsed = StringResolvable.Parse("${step:step1:${${step:step2:${key_name}}}}");
        var result = parsed.Resolve(step3);
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.EqualTo("hidden"));        
    }
    
    [Test]
    public void ResolveResultOfFirstStepUsingKeyDefinedInSecondStep()
    {
        var environment = new DefaultEnvironmentContext();
        var migration = environment.CreateMigrationContext("0001");
        var step1 = migration.CreateExecutionContext("step1");
        step1.ExecuteResult = new
        {
            SecretKey = "hidden"
        };
        
        var step2 = migration.CreateExecutionContext("step2");
        step2.Set("key_name", "secretkey");
        var step3 = migration.CreateExecutionContext("step3");

        var parsed = StringResolvable.Parse("${step:step1:${result:${step:step2:${key_name}}}}");
        var result = parsed.Resolve(step3);
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.EqualTo("hidden"));        
    }

    [Test]
    public void TestResolveVariableToOtherStep()
    {
        var environment = new DefaultEnvironmentContext();
        var migration = environment.CreateMigrationContext("0001");
        migration.Set("migrationvariablename", "secret");
        var step1 = migration.CreateExecutionContext("step1");
        step1.Set("variablename", "migrationvariablename");
        
        var parsed = StringResolvable.Parse("${migration:${step:step1:${variablename}}}");
        Assert.That(parsed, Is.Not.Null);

        var result = parsed.Resolve(step1);
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.EqualTo("secret"));
    }

    [Test]
    public void TestResolveVariableBasedOnEnvironmentVariable()
    {
        var environment = new DefaultEnvironmentContext().Add("environmentvariablename", "stepvariablename");
        var migration = environment.CreateMigrationContext("0001");

        var step1 = migration.CreateExecutionContext("step1");
        step1.Set("stepvariablename", "secret");
        
        var parsed = StringResolvable.Parse("${step:step1:${${env:environmentvariablename}}}");
        Assert.That(parsed, Is.Not.Null);

        var result = parsed.Resolve(step1);
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.EqualTo("secret"));
        
    }
    
}