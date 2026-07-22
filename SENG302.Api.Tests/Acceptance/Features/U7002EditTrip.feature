Feature: U7002_As Benji I want to edit my trips So that I can correct their information

    Scenario: AC2 - Update a trip with valid information
        Given I have an existing trip
        And I am editing that trip
        When I update the trip with valid information
        Then the changes to the trip are persisted
        And I am transported back to the trip page