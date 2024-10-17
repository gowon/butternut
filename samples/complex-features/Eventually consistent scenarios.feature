# ref: https://github.com/GivePenny/GherkinSpec.ComplexExample/blob/fd3c26f8a68d178501232b926a4b8c9944d5c23c/GherkinSpec.ComplexExample.Tests/Features/Eventually%20consistent%20scenarios.feature
Feature: Eventually consistent scenarios

@eventuallyConsistent
Scenario: Eventually consistent scenario
	Given data has been set up
	When an action is performed
	Then the data setup step should only ever be called once
	And the action performed step should be called three times

@eventuallyConsistent
Scenario Outline: Eventually consistent scenario outline
	Given data has been set up
	When an action is performed
	Then the data setup step should only ever be called once
	And the action performed step should be called <count> times
Examples:
    | count |
    | 1     |
    | 2     |
    | 3     |
