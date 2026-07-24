Feature: U7002_As Benji I want to edit my trips So that I can correct their information

    Scenario: AC2 - Update a trip with valid information
        Given I have an existing trip
        And I am editing that trip
        When I update the trip with valid information
        Then the changes to the trip are persisted
        And I am transported back to the trip page

    Scenario: AC5 - Edit trip with an empty date
        Given I have an existing trip
        And I am editing that trip
        When I update the trip with an empty start date
        Then the trip is not updated
        And I see the edit trip error message "Invalid date, date must be formatted DD/MM/YYYY"

    Scenario: AC6 - Edit trip with end date before start date
        Given I have an existing trip
        And I am editing that trip
        When I update the trip with an end date before the start date
        Then the trip is not updated
        And I see the edit trip error message "End date must not be before the start date"