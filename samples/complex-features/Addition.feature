# ref: https://github.com/GivePenny/GherkinSpec.ComplexExample/blob/fd3c26f8a68d178501232b926a4b8c9944d5c23c/GherkinSpec.ComplexExample.Tests/Features/Addition.feature
Feature: Addition
	In order to count how many apples I've collected
	As an apple-hoarder
	I want to add two numbers together

@category(ExampleCategory)
Scenario: Add two numbers together
	Given I have 5 apples
	When I add 6 more
	Then the result should be 11

Scenario: Add two numbers together again
	Given I have 6 apples
	When I add 7 more
	Then the result should be 13

Scenario: Add a number after a previous calculation
	Given I have 5 apples
	And I have added 6 more
	When I add 7 more
	Then the result should be 18

# Example of a Scenario Outline
Scenario Outline: Prove the universe hasn't broken
	Given I have <initial count> apples
	When I add <number to add> more
	Then the result should be <expected result>
Examples: 
	| initial count | number to add | expected result |
	| 1             | 1             | 2               |
	| 2             | 3             | 5               |
	| 200           | 300           | 500             |