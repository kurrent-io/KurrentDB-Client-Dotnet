using System.Text.RegularExpressions;
using AutoFixture;
using KurrentDb.Client;

namespace KurrentDb.Client.Tests;

[Trait("Category", "Target:Misc")]
public class RegularFilterExpressionTests : ValueObjectTests<RegularFilterExpression> {
	public RegularFilterExpressionTests() : base(new ScenarioFixture()) { }

	class ScenarioFixture : Fixture {
		public ScenarioFixture() => Customize<RegularFilterExpression>(composer => composer.FromFactory<Regex>(value => new(value)));
	}
}
