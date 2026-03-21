// <copyright file="PlaceholderProcessorConcurrencyTests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace Sqlx.Tests.Placeholders;

[TestClass]
public class PlaceholderProcessorConcurrencyTests
{
    [TestMethod]
    public void RegisterHandler_ConcurrentRegistrationAndLookup_RemainsStable()
    {
        var scope = $"concurrent_handler_{System.Guid.NewGuid():N}";
        var exceptions = new ConcurrentQueue<Exception>();

        Parallel.For(0, 64, i =>
        {
            try
            {
                var handler = new NamedHandler($"{scope}_{i}");
                PlaceholderProcessor.RegisterHandler(handler);

                if (!PlaceholderProcessor.TryGetHandler(handler.Name, out var registered) ||
                    !ReferenceEquals(handler, registered))
                {
                    throw new AssertFailedException($"Handler '{handler.Name}' was not registered correctly.");
                }
            }
            catch (Exception ex)
            {
                exceptions.Enqueue(ex);
            }
        });

        if (!exceptions.IsEmpty)
        {
            Assert.Fail(string.Join(Environment.NewLine, exceptions.Select(static ex => ex.ToString())));
        }
    }

    [TestMethod]
    public void RegisterBlockClosingTag_ConcurrentRegistrationAndLookup_RemainsStable()
    {
        var scope = $"concurrent_block_{System.Guid.NewGuid():N}";
        var registeredTags = new ConcurrentBag<string>();
        var exceptions = new ConcurrentQueue<Exception>();

        Parallel.For(0, 64, i =>
        {
            var tag = $"/{scope}_{i}";
            registeredTags.Add(tag);

            try
            {
                PlaceholderProcessor.RegisterBlockClosingTag(tag);
                if (!PlaceholderProcessor.IsBlockClosingTag(tag))
                {
                    throw new AssertFailedException($"Block closing tag '{tag}' was not registered correctly.");
                }
            }
            catch (Exception ex)
            {
                exceptions.Enqueue(ex);
            }
        });

        if (!exceptions.IsEmpty)
        {
            Assert.Fail(string.Join(Environment.NewLine, exceptions.Select(static ex => ex.ToString())));
        }

        foreach (var tag in registeredTags)
        {
            Assert.IsTrue(PlaceholderProcessor.IsBlockClosingTag(tag), $"Missing tag: {tag}");
        }
    }

    private sealed class NamedHandler : PlaceholderHandlerBase
    {
        public NamedHandler(string name)
        {
            Name = name;
        }

        public override string Name { get; }

        public override string Process(PlaceholderContext context, string options)
        {
            return Name;
        }
    }
}
