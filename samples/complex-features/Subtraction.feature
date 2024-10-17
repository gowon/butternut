# ref: https://github.com/GivePenny/GherkinSpec.ComplexExample/blob/fd3c26f8a68d178501232b926a4b8c9944d5c23c/GherkinSpec.ComplexExample.Tests/Features/Subtraction.feature
Feature: Subtraction
	In order to count how many apples the hoarder will still have
	As an apple tax official
	I want to be able to subtract one number from another

Scenario: Subtract one number from another
	Given I have 5 apples
	When I subtract 1
	Then the result should be 4

Scenario: Subtract again just because this is an example
	Given I have 6 apples
	When I subtract 3
	Then the result should be 3

Scenario: Subtract a number after a previous calculation
	Given I have 5 apples
	And I have subtracted 1
	When I subtract 1 more
	Then the result should be 3
